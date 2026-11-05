"""Prueba rutas y persistencia con SQLite como doble local de MySQL.
No sustituye una prueba de integración con el servidor MySQL real.
"""
import json
import sqlite3
import unittest
from contextlib import contextmanager
from unittest.mock import patch
from api import ErrorServicio
import api


class Cursor:
    def __init__(self, db): self.cur = db.cursor()
    @property
    def lastrowid(self): return self.cur.lastrowid
    def execute(self, sql, args=()):
        self.cur.execute(sql.replace(' FOR UPDATE', '').replace('%s', '?'), args)
    def fetchone(self):
        row = self.cur.fetchone()
        return dict(row) if row is not None else None
    def fetchall(self): return [dict(row) for row in self.cur.fetchall()]


class ApiTests(unittest.TestCase):
    def setUp(self):
        self.db = sqlite3.connect(':memory:', check_same_thread=False)
        self.db.row_factory = sqlite3.Row
        self.db.executescript('''
            CREATE TABLE jugadores(id_jugador INTEGER PRIMARY KEY, nombre TEXT COLLATE NOCASE UNIQUE);
            CREATE TABLE partidas(id_partida INTEGER PRIMARY KEY, id_ganador INTEGER);
            CREATE TABLE historial_partida(id_evento INTEGER PRIMARY KEY, id_partida INTEGER,
                id_jugador INTEGER, turno INTEGER, accion TEXT, color_carta TEXT, valor_carta TEXT);
            CREATE TABLE salas_wifi(codigo TEXT PRIMARY KEY, id_partida INTEGER, estado TEXT);
        ''')
        @contextmanager
        def transaccion():
            try:
                yield Cursor(self.db)
                self.db.commit()
            except Exception:
                self.db.rollback()
                raise
        self.parche = patch.object(api, 'transaccion', transaccion)
        self.parche.start()
        self.a = api.crear(api.Nombre(nombre='Ana'))
        self.codigo = self.a['estado']['codigo']
        self.b = api.entrar(self.codigo, api.Nombre(nombre='Luis'))
        self.token = 'Bearer ' + self.a['token']

    def tearDown(self):
        self.parche.stop()
        self.db.close()

    def accion(self, accion, solicitud='1', **kw):
        version = api.consultar(self.codigo, self.token)['version']
        return api.Accion(accion=accion, version=version, solicitud=solicitud, **kw)

    def test_sesion_requerida(self):
        with self.assertRaises(ErrorServicio) as ex: api.consultar(self.codigo, None)
        self.assertEqual(401, ex.exception.status_code)

    def test_reintento_no_duplica_jugada(self):
        inicio = self.accion('iniciar')
        api.actuar(self.codigo, inicio, self.token)
        robo = self.accion('robar', '2')
        primera = api.actuar(self.codigo, robo, self.token)
        segunda = api.actuar(self.codigo, robo, self.token)
        self.assertEqual(primera, segunda)
        self.assertEqual(1, self.db.execute("SELECT COUNT(*) FROM historial_partida WHERE accion='Robar'").fetchone()[0])
        self.assertEqual(8, len(segunda['mano']))

    def test_version_vieja_rechazada(self):
        vieja = api.Accion(accion='iniciar', version=0, solicitud='vieja')
        with self.assertRaises(ErrorServicio) as ex: api.actuar(self.codigo, vieja, self.token)
        self.assertEqual(409, ex.exception.status_code)
        self.assertEqual('espera', api.consultar(self.codigo, self.token)['fase'])

    def test_no_anfitrion_no_inicia(self):
        with self.assertRaises(ErrorServicio):
            api.actuar(self.codigo, self.accion('iniciar'), 'Bearer ' + self.b['token'])
        self.assertEqual(0, self.db.execute('SELECT COUNT(*) FROM historial_partida').fetchone()[0])

    def test_nombre_repetido_revierte_alta(self):
        with self.assertRaises(ErrorServicio) as ex: api.entrar(self.codigo, api.Nombre(nombre='ANA'))
        self.assertEqual('Ese nombre ya está en la sala', ex.exception.detail)
        self.assertEqual(2, self.db.execute('SELECT COUNT(*) FROM jugadores').fetchone()[0])

    def test_reutiliza_jugador_en_otra_partida(self):
        otra = api.crear(api.Nombre(nombre='ANA'))
        primera = json.loads(self.db.execute('SELECT estado FROM salas_wifi WHERE codigo=?',
                                             (self.codigo,)).fetchone()[0])
        segunda = json.loads(self.db.execute('SELECT estado FROM salas_wifi WHERE codigo=?',
                                             (otra['estado']['codigo'],)).fetchone()[0])
        self.assertEqual(primera['jugadores'][0]['id'], segunda['jugadores'][0]['id'])
        self.assertEqual(2, self.db.execute('SELECT COUNT(*) FROM jugadores').fetchone()[0])

    def test_expiracion_se_guarda_aun_con_version_vieja(self):
        api.actuar(self.codigo, self.accion('iniciar'), self.token)
        row = self.db.execute('SELECT estado FROM salas_wifi').fetchone()
        s = json.loads(row[0])
        s['uno'] = dict(jugador=0, hasta=0)
        self.db.execute('UPDATE salas_wifi SET estado=?', (json.dumps(s),))
        self.db.commit()
        vieja = api.Accion(accion='robar', version=s['version'], solicitud='expirada')
        with self.assertRaises(ErrorServicio): api.actuar(self.codigo, vieja, self.token)
        actual = api.consultar(self.codigo, self.token)
        self.assertEqual(9, len(actual['mano']))
        self.assertEqual(-1, actual['uno_jugador'])
        self.assertEqual(1, self.db.execute("SELECT COUNT(*) FROM historial_partida WHERE accion='Castigo UNO'").fetchone()[0])


if __name__ == '__main__': unittest.main()
