import json
import os
import secrets
from contextlib import contextmanager
from typing import Optional

import mysql.connector
from pydantic import BaseModel, Field
import motor

# Operaciones de salas y persistencia, llamadas directamente por el servidor TCP.
class ErrorServicio(Exception):
    def __init__(self, status_code, detail):
        super().__init__(detail)
        self.status_code = status_code
        self.detail = detail


def conexion():
    return mysql.connector.connect(host=os.getenv('UNO_DB_HOST', 'localhost'),
                                   user=os.getenv('UNO_DB_USER', 'root'),
                                   password=os.environ["UNO_DB_PASSWORD"],
                                   database=os.getenv('UNO_DB_NAME', 'uno_db'),
                                   connection_timeout=5)


@contextmanager
def transaccion():
    db = conexion()
    cur = db.cursor(dictionary=True)
    try:
        db.start_transaction()
        yield cur
        db.commit()
    except Exception:
        db.rollback()
        raise
    finally:
        cur.close()
        db.close()


class Nombre(BaseModel):
    nombre: str = Field(min_length=1, max_length=50)


class Accion(BaseModel):
    accion: str
    version: int
    solicitud: str = Field(min_length=1, max_length=64)
    carta: Optional[int] = None
    color: Optional[str] = None


def nombre_valido(datos):
    nombre = datos.nombre.strip()
    if not nombre:
        raise ErrorServicio(422, 'Escribe tu nombre')
    return nombre


def crear_jugador(cur, nombre):
    # Cada entrada representa una sesión distinta, aunque existan nombres históricos iguales.
    cur.execute('INSERT INTO jugadores(nombre) VALUES (%s)', (nombre,))
    return cur.lastrowid


def cargar(cur, codigo):
    codigo = codigo.upper()
    cur.execute('SELECT * FROM salas_wifi WHERE codigo=%s FOR UPDATE', (codigo,))
    fila = cur.fetchone()
    if not fila:
        raise ErrorServicio(404, 'Sala inexistente')
    return fila, json.loads(fila['estado'])


def autenticar(s, authorization):
    token = (authorization or '').removeprefix('Bearer ')
    for i, j in enumerate(s['jugadores']):
        if secrets.compare_digest(j['token'], token):
            return i
    raise ErrorServicio(401, 'Sesión inválida')


def guardar(cur, fila, s):
    cur.execute('SELECT COALESCE(MAX(turno),0) AS ultimo FROM historial_partida WHERE id_partida=%s',
                (fila['id_partida'],))
    ultimo = cur.fetchone()['ultimo']
    for e in s['eventos']:
        ultimo += 1
        cur.execute('''INSERT INTO historial_partida
                       (id_partida,id_jugador,turno,accion,color_carta,valor_carta)
                       VALUES (%s,%s,%s,%s,%s,%s)''',
                    (fila['id_partida'], e['jugador'], ultimo, e['accion'], e['color'], e['valor']))
    s['eventos'] = []
    if s['ganador'] is not None:
        cur.execute('UPDATE partidas SET id_ganador=%s WHERE id_partida=%s',
                    (s['jugadores'][s['ganador']]['id'], fila['id_partida']))
    cur.execute('UPDATE salas_wifi SET estado=%s WHERE codigo=%s',
                (json.dumps(s), fila['codigo']))


def salud():
    with transaccion() as cur:
        cur.execute('SELECT 1 FROM salas_wifi LIMIT 1')
        cur.fetchall()
    return {'mensaje': 'API y MySQL disponibles'}


def crear(datos: Nombre):
    nombre = nombre_valido(datos)
    with transaccion() as cur:
        codigo = secrets.token_hex(4).upper()
        s = motor.nueva(nombre, crear_jugador(cur, nombre))
        cur.execute('INSERT INTO partidas(id_ganador) VALUES (NULL)')
        partida = cur.lastrowid
        cur.execute('INSERT INTO salas_wifi(codigo,id_partida,estado) VALUES (%s,%s,%s)',
                    (codigo, partida, json.dumps(s)))
    return dict(token=s['jugadores'][0]['token'], estado=motor.vista(s, 0, codigo))


def entrar(codigo: str, datos: Nombre):
    nombre = nombre_valido(datos)
    with transaccion() as cur:
        fila, s = cargar(cur, codigo)
        try:
            i = motor.entrar(s, nombre, crear_jugador(cur, nombre))
        except ValueError as e:
            raise ErrorServicio(409, str(e))
        guardar(cur, fila, s)
    return dict(token=s['jugadores'][i]['token'], estado=motor.vista(s, i, fila['codigo']))


def consultar(codigo: str, authorization: Optional[str] = None):
    with transaccion() as cur:
        fila, s = cargar(cur, codigo)
        i = autenticar(s, authorization)
        if motor.expirar_uno(s):
            guardar(cur, fila, s)
    return motor.vista(s, i, fila['codigo'])


def actuar(codigo: str, datos: Accion, authorization: Optional[str] = None):
    error = None
    with transaccion() as cur:
        fila, s = cargar(cur, codigo)
        i = autenticar(s, authorization)
        motor.expirar_uno(s)
        clave = str(i) + ':' + datos.solicitud
        if clave not in s['solicitudes']:
            if datos.version != s['version']:
                error = 'El estado cambió. Revisa tu mano e intenta de nuevo.'
            else:
                try:
                    motor.actuar(s, i, datos.accion, datos.carta, datos.color)
                    s['version'] += 1
                    s['solicitudes'] = (s['solicitudes'] + [clave])[-200:]
                except ValueError as e:
                    error = str(e)
        guardar(cur, fila, s)
    if error:
        raise ErrorServicio(409, error)
    return motor.vista(s, i, fila['codigo'])
