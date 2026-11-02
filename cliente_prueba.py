import asyncio
import websockets

async def recibir(websocket):
    try:
        async for mensaje in websocket:
            print(f"\nServidor dice: {mensaje}")
            print("Tú> ", end="", flush=True)
    except websockets.ConnectionClosed:
        print("\nConexión cerrada")

async def enviar(websocket):
    while True:
        mensaje = await asyncio.to_thread(input, "Tú> ")

        if mensaje == "salir":
            break

        await websocket.send(mensaje)

async def main():
    uri = "ws://127.0.0.1:8000/ws"

    async with websockets.connect(uri) as websocket:
        print("Conectado al servidor")

        tarea_recibir = asyncio.create_task(recibir(websocket))

        await enviar(websocket)

        tarea_recibir.cancel()

asyncio.run(main())