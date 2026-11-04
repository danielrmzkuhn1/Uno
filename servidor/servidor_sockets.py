"""TCP persistente: un objeto JSON UTF-8 por línea. Sin sondeo HTTP."""
import asyncio
import json
import logging
import os

from api import ErrorServicio
from pydantic import ValidationError
import api as servicio

log = logging.getLogger('uno.tcp')


class Cliente:
    def __init__(self, writer):
        self.writer = writer
        self.codigo = None
        self.token = None

    async def enviar(self, datos):
        self.writer.write((json.dumps(datos, ensure_ascii=False) + '\n').encode('utf-8'))
        await asyncio.wait_for(self.writer.drain(), 5)


class Servidor:
    def __init__(self):
        self.clientes = set()
        self.lock = asyncio.Lock()
        self.plazos = {}

    async def publicar(self, codigo):
        # Bajo lock: cada destinatario recibe únicamente su propia mano.
        for c in list(self.clientes):
            if c.codigo != codigo:
                continue
            try:
                estado = await asyncio.to_thread(servicio.consultar, codigo, 'Bearer ' + c.token)
                self.programar(estado)
                await c.enviar(dict(tipo='estado', estado=estado))
            except (ConnectionError, asyncio.TimeoutError, OSError):
                c.writer.close()
                self.clientes.discard(c)

    def programar(self, estado):
        codigo = estado['codigo']
        if estado['uno_jugador'] < 0:
            anterior = self.plazos.pop(codigo, None)
            if anterior and anterior is not asyncio.current_task():
                anterior.cancel()
            return
        if codigo not in self.plazos:
            self.plazos[codigo] = asyncio.create_task(self.vencer(codigo, estado['uno_segundos']))

    def expirar(self, codigo):
        with servicio.transaccion() as cur:
            fila, s = servicio.cargar(cur, codigo)
            if servicio.motor.expirar_uno(s):
                servicio.guardar(cur, fila, s)

    async def vencer(self, codigo, segundos):
        try:
            await asyncio.sleep(max(0, segundos) + 0.05)
            async with self.lock:
                await asyncio.to_thread(self.expirar, codigo)
                self.plazos.pop(codigo, None)
                await self.publicar(codigo)
        except asyncio.CancelledError:
            pass
        except Exception:
            self.plazos.pop(codigo, None)
            log.exception('No se pudo aplicar el plazo UNO de %s', codigo)

    async def procesar(self, c, m):
        tipo = m.get('tipo')
        if tipo == 'salud':
            return await asyncio.to_thread(servicio.salud)
        if tipo in ('crear', 'entrar', 'reanudar'):
            if c.codigo:
                raise ValueError('Ya estás en una sala')
            if tipo == 'reanudar':
                codigo, token = str(m.get('codigo', '')).upper(), str(m.get('token', ''))
                estado = await asyncio.to_thread(servicio.consultar, codigo, 'Bearer ' + token)
                sesion = dict(token=token, estado=estado)
            else:
                nombre = servicio.Nombre(nombre=m.get('nombre', ''))
                if tipo == 'crear':
                    sesion = await asyncio.to_thread(servicio.crear, nombre)
                else:
                    sesion = await asyncio.to_thread(servicio.entrar, str(m.get('codigo', '')), nombre)
            c.codigo = sesion['estado']['codigo']
            c.token = sesion['token']
            return sesion
        if not c.codigo:
            raise ValueError('Primero crea una sala o únete')
        if tipo == 'consultar':
            return dict(estado=await asyncio.to_thread(servicio.consultar, c.codigo, 'Bearer ' + c.token))
        if tipo == 'accion':
            datos = servicio.Accion(**m['datos'])
            estado = await asyncio.to_thread(
                servicio.actuar, c.codigo, datos, 'Bearer ' + c.token
            )
            return dict(estado=estado)

        if tipo == 'cancelar':
            estado = await asyncio.to_thread(
                servicio.cancelar, c.codigo, 'Bearer ' + c.token
            )
            return dict(estado=estado)

        raise ValueError('Tipo de mensaje desconocido')

    async def atender(self, reader, writer):
        c = Cliente(writer)
        self.clientes.add(c)
        try:
            while True:
                linea = await reader.readline()
                if not linea:
                    break
                identificador = None
                async with self.lock:
                    try:
                        m = json.loads(linea.decode('utf-8'))
                        if not isinstance(m, dict):
                            raise ValueError('Se esperaba un objeto JSON')
                        identificador = m.get('id')
                        if not isinstance(identificador, str) or not 1 <= len(identificador) <= 64:
                            raise ValueError('Identificador inválido')
                        resultado = await self.procesar(c, m)
                        await c.enviar(dict(tipo='respuesta', id=identificador, ok=True, **resultado))
                    except (ErrorServicio, ValidationError, ValueError, KeyError, TypeError) as e:
                        detalle = str(e.detail) if isinstance(e, ErrorServicio) else str(e)
                        await c.enviar(dict(tipo='respuesta', id=identificador, ok=False, error=detalle))
                    except (ConnectionError, asyncio.TimeoutError, OSError):
                        raise
                    except Exception:
                        log.exception('Error interno al procesar mensaje TCP')
                        await c.enviar(dict(tipo='respuesta', id=identificador, ok=False,
                                            error='Error del servidor. Revisa MySQL y su consola.'))
                    if c.codigo:
                        try:
                            await self.publicar(c.codigo)
                        except Exception:
                            log.exception('No se pudo publicar el estado')
        except (ConnectionError, asyncio.TimeoutError, OSError, ValueError):
            pass
        finally:
            self.clientes.discard(c)
            writer.close()
            try:
                await writer.wait_closed()
            except (OSError, ConnectionError):
                pass


async def main():
    await asyncio.to_thread(servicio.salud)
    servidor = Servidor()
    puerto = int(os.getenv('UNO_PORT', '8000'))
    listener = await asyncio.start_server(servidor.atender, '0.0.0.0', puerto, limit=65536)
    print(f'UNO TCP escuchando en 0.0.0.0:{puerto}. MySQL disponible.', flush=True)
    async with listener:
        await listener.serve_forever()


if __name__ == '__main__':
    logging.basicConfig(level=logging.INFO)
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        pass
