import asyncio
from fastapi import FastAPI, WebSocket, WebSocketDisconnect

app = FastAPI()

clientes = set()


@app.websocket("/ws")
async def websocket_endpoint(websocket: WebSocket):
    await websocket.accept()
    clientes.add(websocket)

    print("Cliente conectado")

    try:
        while True:
            mensaje = await websocket.receive_text()
            print(f"\nC# dice: {mensaje}")

            # Responder al cliente
            await websocket.send_text(f"Python recibió: {mensaje}")

    except WebSocketDisconnect:
        print("Cliente desconectado")

    finally:
        clientes.discard(websocket)


async def enviar_mensajes():
    while True:
        mensaje = await asyncio.to_thread(input, "Python> ")

        for cliente in list(clientes):
            try:
                await cliente.send_text(mensaje)
            except Exception:
                clientes.discard(cliente)


@app.on_event("startup")
async def startup():
    asyncio.create_task(enviar_mensajes())