import asyncio
import json
import time
import unittest
import motor
import servidor_sockets
import test_api


class SocketsTests(unittest.IsolatedAsyncioTestCase):
    async def asyncSetUp(self):
        self.fixture = test_api.ApiTests()
        self.fixture.setUp()
        self.servidor = servidor_sockets.Servidor()
        self.listener = await asyncio.start_server(self.servidor.atender, '127.0.0.1', 0, limit=65536)
        self.puerto = self.listener.sockets[0].getsockname()[1]
        self.conexiones = []

    async def asyncTearDown(self):
        for _, w in self.conexiones:
            w.close()
            await w.wait_closed()
        self.listener.close()
        await self.listener.wait_closed()
        for t in self.servidor.plazos.values(): t.cancel()
        for _ in range(100):
            if not self.servidor.clientes: break
            await asyncio.sleep(0.01)
        self.fixture.tearDown()

    async def conectar(self):
        c = await asyncio.open_connection('127.0.0.1', self.puerto)
        self.conexiones.append(c)
        return c

    async def leer(self, c, predicado=lambda m: True):
        async with asyncio.timeout(3):
            while True:
                linea = await c[0].readline()
                if not linea: raise EOFError('Socket cerrado')
                m = json.loads(linea)
                if predicado(m): return m

    async def pedir(self, c, tipo, **kw):
        identificador = str(time.monotonic_ns())
        c[1].write((json.dumps(dict(id=identificador, tipo=tipo, **kw)) + '\n').encode())
        await c[1].drain()
        return await self.leer(c, lambda m: m.get('id') == identificador)

    async def sala(self):
        a, b = await self.conectar(), await self.conectar()
        creado = await self.pedir(a, 'crear', nombre='Host')
        codigo = creado['estado']['codigo']
        unido = await self.pedir(b, 'entrar', codigo=codigo, nombre='Invitado')
        push = await self.leer(a, lambda m: m['tipo'] == 'estado' and len(m['estado']['jugadores']) == 2)
        return a, b, codigo, push['estado']

    async def iniciar(self):
        a, b, codigo, estado = await self.sala()
        respuesta = await self.pedir(a, 'accion', datos=dict(accion='iniciar', version=estado['version'], solicitud='inicio'))
        self.assertTrue(respuesta['ok'])
        invitado = await self.leer(b, lambda m: m['tipo'] == 'estado' and m['estado']['fase'] == 'jugando')
        return a, b, codigo, respuesta['estado'], invitado['estado']

    async def test_union_se_publica_sin_sondeo(self):
        a, b, codigo, estado = await self.sala()
        self.assertEqual(2, len(estado['jugadores']))
        self.assertEqual(0, estado['yo'])

    async def test_accion_se_publica_y_manos_son_privadas(self):
        a, b, codigo, estado, invitado = await self.iniciar()
        self.assertEqual(1, invitado['yo'])
        self.assertEqual(7, len(invitado['mano']))
        self.assertNotIn('token', json.dumps(invitado))
        self.assertNotIn('mano', json.dumps(invitado['jugadores']))
        r = await self.pedir(a, 'accion', datos=dict(accion='robar', version=estado['version'], solicitud='robo'))
        nuevo = await self.leer(b, lambda m: m['tipo'] == 'estado' and m['estado']['version'] == r['estado']['version'])
        self.assertEqual(1, nuevo['estado']['turno'])
        self.assertEqual(8, nuevo['estado']['jugadores'][0]['cantidad'])
        self.assertEqual(7, len(nuevo['estado']['mano']))

    async def test_tcp_fragmentado_y_mensajes_concatenados(self):
        c = await self.conectar()
        raw = (json.dumps(dict(id='uno', tipo='crear', nombre='José')) + '\n').encode()
        for byte in raw:
            c[1].write(bytes([byte]))
            await c[1].drain()
        r = await self.leer(c, lambda m: m.get('id') == 'uno')
        self.assertTrue(r['ok'])
        c[1].write(b'{"id":"dos","tipo":"consultar"}\n{"id":"tres","tipo":"consultar"}\n')
        await c[1].drain()
        self.assertTrue((await self.leer(c, lambda m: m.get('id') == 'dos'))['ok'])
        self.assertTrue((await self.leer(c, lambda m: m.get('id') == 'tres'))['ok'])

    async def test_castigo_uno_se_envia_sin_peticion(self):
        a, b, codigo, estado, invitado = await self.iniciar()
        async with self.servidor.lock:
            row = self.fixture.db.execute('SELECT estado FROM salas_wifi WHERE codigo=?', (codigo,)).fetchone()
            s = json.loads(row[0])
            s['uno'] = dict(jugador=0, hasta=time.time() + 0.1)
            self.fixture.db.execute('UPDATE salas_wifi SET estado=? WHERE codigo=?', (json.dumps(s), codigo))
            self.fixture.db.commit()
            self.servidor.programar(motor.vista(s, 0, codigo))
        push = await self.leer(b, lambda m: m['tipo'] == 'estado' and m['estado']['version'] > estado['version'])
        self.assertEqual(9, push['estado']['jugadores'][0]['cantidad'])
        self.assertEqual(-1, push['estado']['uno_jugador'])

    async def test_reenvio_no_duplica_y_version_vieja_falla(self):
        a, b, codigo, estado, _ = await self.iniciar()
        datos = dict(accion='robar', version=estado['version'], solicitud='robo')
        primera = await self.pedir(a, 'accion', datos=datos)
        segunda = await self.pedir(a, 'accion', datos=datos)
        self.assertEqual(primera['estado']['version'], segunda['estado']['version'])
        datos['solicitud'] = 'otro'
        self.assertFalse((await self.pedir(a, 'accion', datos=datos))['ok'])

    async def test_json_invalido_no_rompe_servidor(self):
        c = await self.conectar()
        c[1].write(b'basura\n')
        await c[1].drain()
        self.assertFalse((await self.leer(c))['ok'])
        self.assertTrue((await self.pedir(c, 'salud'))['ok'])

    async def test_no_puede_jugar_sin_sesion(self):
        c = await self.conectar()
        self.assertFalse((await self.pedir(c, 'accion', datos={}))['ok'])


if __name__ == '__main__': unittest.main()
