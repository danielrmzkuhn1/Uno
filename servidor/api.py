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


def obtener_jugador(cur, nombre):
    cur.execute('SELECT id_jugador, nombre FROM jugadores ORDER BY id_jugador')
    for jugador in cur.fetchall():
        if jugador['nombre'].casefold() == nombre.casefold():
            return jugador['id_jugador']
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


def registrar_participantes(cur, id_partida, jugadores):
    for jugador in jugadores:
        cur.execute('INSERT INTO partida_jugadores(id_partida,id_jugador) VALUES (%s,%s)',
                    (id_partida, jugador['id']))


def salud():
    with transaccion() as cur:
        cur.execute('SELECT 1 FROM salas_wifi LIMIT 1')
        cur.fetchall()
    return {'mensaje': 'API y MySQL disponibles'}


def crear(datos: Nombre):
    nombre = nombre_valido(datos)
    with transaccion() as cur:
        codigo = secrets.token_hex(4).upper()
        s = motor.nueva(nombre, obtener_jugador(cur, nombre))
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
            motor.validar_entrada(s, nombre)
        except ValueError as e:
            raise ErrorServicio(409, str(e))
        jugador_id = obtener_jugador(cur, nombre)
        i = motor.entrar(s, nombre, jugador_id)
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
                    if datos.accion == 'iniciar':
                        registrar_participantes(cur, fila['id_partida'], s['jugadores'])
                    s['version'] += 1
                    s['solicitudes'] = (s['solicitudes'] + [clave])[-200:]
                except ValueError as e:
                    error = str(e)
        guardar(cur, fila, s)
    if error:
        raise ErrorServicio(409, error)
    return motor.vista(s, i, fila['codigo'])


def revancha(codigo: str, authorization: Optional[str] = None):
    with transaccion() as cur:
        fila, s = cargar(cur, codigo)
        i = autenticar(s, authorization)
        if i != 0:
            raise ErrorServicio(403, 'Solo el creador puede iniciar otra partida')
        try:
            nueva = motor.revancha(s)
        except ValueError as e:
            raise ErrorServicio(409, str(e))

        cur.execute('INSERT INTO partidas(id_ganador) VALUES (NULL)')
        id_partida = cur.lastrowid
        cur.execute('UPDATE salas_wifi SET id_partida=%s, estado=%s WHERE codigo=%s',
                    (id_partida, json.dumps(nueva), fila['codigo']))

    return motor.vista(nueva, i, fila['codigo'])


def cancelar(codigo: str, authorization: Optional[str] = None):
    with transaccion() as cur:
        fila, s = cargar(cur, codigo)
        i = autenticar(s, authorization)

        if i != 0:
            raise ErrorServicio(403, 'Solo el creador puede cancelar la sala')

        if s['fase'] != 'espera':
            raise ErrorServicio(409, 'La partida ya inició o fue cancelada')

        s['fase'] = 'cancelada'
        s['mensaje'] = s['jugadores'][i]['nombre'] + ' canceló la sala'
        s['version'] += 1

        guardar(cur, fila, s)

    return motor.vista(s, i, fila['codigo'])

def salir(codigo: str, authorization: Optional[str] = None):
    with transaccion() as cur:
        fila, s = cargar(cur, codigo)
        i = autenticar(s, authorization)

        if s['fase'] != 'espera':
            raise ErrorServicio(
                409,
                'Solo puedes salir antes de que inicie la partida'
            )

        if i == 0:
            raise ErrorServicio(
                409,
                'El creador debe cancelar la sala'
            )

        nombre = s['jugadores'][i]['nombre']
        s['jugadores'].pop(i)
        s['version'] += 1
        s['mensaje'] = nombre + ' salió de la sala'

        guardar(cur, fila, s)

    return {'salio': True}