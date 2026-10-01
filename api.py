from fastapi import FastAPI
import mysql.connector 

app = FastAPI()

def obtener_conexion():
  return mysql.connector.connect(
      host="localhost",
      user="root",
      password="Dani6^gerar2", 
      database="uno_db", 
  )

@app.get("/datos")
def leer_datos():
  conexion = obtener_conexion()
  cursor = conexion.cursor(dictionary=True)

  cursor.execute("SELECT * FROM jugadores")
  resultados = cursor.fetchall()

  cursor.close()
  conexion.close()

  return resultados