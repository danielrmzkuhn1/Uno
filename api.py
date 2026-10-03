from fastapi import FastAPI
from pydantic import BaseModel
import mysql.connector

app = FastAPI()


def obtener_conexion():
    return mysql.connector.connect(
        host="localhost",
        user="root",
        password="Alex_17020673",
        database="uno_db"
    )




# =========================
# MODELOS
# =========================

class JugadorEntrada(BaseModel):
    nombre: str


class HistorialEntrada(BaseModel):
    id_partida: int
    id_jugador: int
    turno: int
    accion: str
    color_carta: str | None = None
    valor_carta: str | None = None


class GanadorEntrada(BaseModel):
    id_ganador: int


# =========================
# LEER JUGADORES
# =========================

@app.get("/datos")
def leer_datos():
    conexion = obtener_conexion()
    cursor = conexion.cursor(dictionary=True)

    cursor.execute("SELECT * FROM jugadores")
    resultados = cursor.fetchall()

    cursor.close()
    conexion.close()

    return resultados


# =========================
# CREAR JUGADOR
# =========================

@app.post("/jugadores")
def crear_jugador(jugador: JugadorEntrada):
    conexion = obtener_conexion()
    cursor = conexion.cursor(dictionary=True)

    # Buscar si ya existe
    cursor.execute(
        "SELECT id_jugador, nombre FROM jugadores WHERE nombre = %s",
        (jugador.nombre,)
    )

    existente = cursor.fetchone()

    if existente:
        cursor.close()
        conexion.close()

        return {
            "id_jugador": existente["id_jugador"],
            "nombre": existente["nombre"],
            "existia": True
        }

    # Si no existe, crearlo
    cursor.execute(
        "INSERT INTO jugadores(nombre) VALUES (%s)",
        (jugador.nombre,)
    )

    conexion.commit()

    id_jugador = cursor.lastrowid

    cursor.close()
    conexion.close()

    return {
        "id_jugador": id_jugador,
        "nombre": jugador.nombre,
        "existia": False
    }


# =========================
# CREAR PARTIDA
# =========================

@app.post("/partidas")
def crear_partida():
    conexion = obtener_conexion()
    cursor = conexion.cursor()

    cursor.execute(
        "INSERT INTO partidas(id_ganador) VALUES (NULL)"
    )

    conexion.commit()

    id_partida = cursor.lastrowid

    cursor.close()
    conexion.close()

    return {
        "id_partida": id_partida
    }


# =========================
# GUARDAR JUGADA
# =========================

@app.post("/historial")
def guardar_historial(datos: HistorialEntrada):
    conexion = obtener_conexion()
    cursor = conexion.cursor()

    sql = """
        INSERT INTO historial_partida
        (
            id_partida,
            id_jugador,
            turno,
            accion,
            color_carta,
            valor_carta
        )
        VALUES (%s, %s, %s, %s, %s, %s)
    """

    cursor.execute(
        sql,
        (
            datos.id_partida,
            datos.id_jugador,
            datos.turno,
            datos.accion,
            datos.color_carta,
            datos.valor_carta
        )
    )

    conexion.commit()

    cursor.close()
    conexion.close()

    return {
        "mensaje": "Jugada guardada"
    }


# =========================
# GUARDAR GANADOR
# =========================

@app.put("/partidas/{id_partida}/ganador")
def guardar_ganador(id_partida: int, datos: GanadorEntrada):
    conexion = obtener_conexion()
    cursor = conexion.cursor()

    sql = """
        UPDATE partidas
        SET id_ganador = %s
        WHERE id_partida = %s
    """

    cursor.execute(
        sql,
        (datos.id_ganador, id_partida)
    )

    conexion.commit()

    cursor.close()
    conexion.close()

    return {
        "mensaje": "Ganador registrado"
    }