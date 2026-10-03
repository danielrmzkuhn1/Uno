CREATE DATABASE uno_db;
USE uno_db;

CREATE TABLE jugadores (
    id_jugador INT AUTO_INCREMENT PRIMARY KEY,
    nombre VARCHAR(50) NOT NULL
);

CREATE TABLE partidas (
    id_partida INT AUTO_INCREMENT PRIMARY KEY,
    fecha DATETIME DEFAULT CURRENT_TIMESTAMP,
    id_ganador INT,

    FOREIGN KEY (id_ganador)
        REFERENCES jugadores(id_jugador)
);

CREATE TABLE historial_partida (
    id_evento INT AUTO_INCREMENT PRIMARY KEY,
    id_partida INT NOT NULL,
    id_jugador INT NOT NULL,
    turno INT NOT NULL,
    accion VARCHAR(30) NOT NULL,
    color_carta VARCHAR(20),
    valor_carta VARCHAR(20),

    FOREIGN KEY (id_partida)
        REFERENCES partidas(id_partida),

    FOREIGN KEY (id_jugador)
        REFERENCES jugadores(id_jugador)
);

