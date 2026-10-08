using System;
using System.Collections.Generic;
using System.Linq;

namespace UNO.Logica
{
    public class Juego
    {
        public const int MinJugadores = 2;
        public const int MaxJugadores = 4;
        private const int CartasIniciales = 7;

        private readonly Mazo mazo;
        private int direccion = 1;

        public List<Jugador> Jugadores { get; }
        public int TurnoActual { get; private set; }
        public Carta CartaSuperior { get; private set; }
        public ColorCarta ColorActual { get; private set; }
        public string Mensaje { get; private set; } = "";
        public bool JuegoTerminado { get; private set; }
        public int? Ganador { get; private set; }
        public bool SentidoHorario => direccion == 1;
        public int CartasEnMazo => mazo.Cantidad;

        public Juego(List<string> nombres)
        {
            if (nombres == null || nombres.Count < MinJugadores || nombres.Count > MaxJugadores)
            {
                throw new ArgumentException("La partida requiere entre " + MinJugadores + " y " + MaxJugadores + " jugadores.");
            }

            Jugadores = nombres
                .Select((nombre, i) => new Jugador(string.IsNullOrWhiteSpace(nombre) ? "Jugador " + (i + 1) : nombre.Trim()))
                .ToList();

            mazo = new Mazo();

            for (int i = 0; i < CartasIniciales; i++)
            {
                foreach (Jugador jugador in Jugadores)
                {
                    jugador.Mano.Add(mazo.Robar());
                }
            }

            Carta inicial = mazo.Robar();
            while (inicial.Tipo != TipoCarta.Numero)
            {
                mazo.Devolver(inicial);
                inicial = mazo.Robar();
            }

            mazo.Descartar(inicial);
            CartaSuperior = inicial;
            ColorActual = inicial.Color;
            TurnoActual = 0;
        }

        public bool PuedeJugar(int jugador, int carta)
        {
            return MotivoInvalido(jugador, carta) == null;
        }

        public bool PuedeRobar(int jugador)
        {
            return !JuegoTerminado && jugador == TurnoActual;
        }

        public bool JugarCarta(int jugador, int carta, ColorCarta elegido = ColorCarta.Ninguno)
        {
            Mensaje = "";

            string motivo = MotivoInvalido(jugador, carta);
            if (motivo != null)
            {
                Mensaje = motivo;
                return false;
            }

            Carta jugada = Jugadores[jugador].Mano[carta];
            bool esComodin = Reglas.EsComodin(jugada);

            if (esComodin && elegido == ColorCarta.Ninguno)
            {
                Mensaje = "Debes elegir un color";
                return false;
            }

            Jugadores[jugador].Mano.RemoveAt(carta);
            mazo.Descartar(jugada);
            CartaSuperior = jugada;
            ColorActual = esComodin ? elegido : jugada.Color;

            if (Jugadores[jugador].Mano.Count == 0)
            {
                JuegoTerminado = true;
                Ganador = jugador;
                Mensaje = "Ganó " + Jugadores[jugador].Nombre;
                return true;
            }

            AplicarEfecto(jugada);
            return true;
        }

        public bool RobarCarta(int jugador)
        {
            Mensaje = "";

            if (JuegoTerminado)
            {
                Mensaje = "La partida ya terminó";
                return false;
            }

            if (jugador != TurnoActual)
            {
                Mensaje = "No es tu turno";
                return false;
            }

            Carta carta = mazo.Robar();
            if (carta == null)
            {
                Mensaje = "No quedan cartas para robar";
            }
            else
            {
                Jugadores[jugador].Mano.Add(carta);
                Mensaje = Jugadores[jugador].Nombre + " robó una carta";
            }

            AvanzarTurno(1);
            return true;
        }

        public void CastigarUno(int jugador, int cantidad)
        {
            if (JuegoTerminado || jugador < 0 || jugador >= Jugadores.Count)
            {
                return;
            }

            int robadas = 0;
            for (int i = 0; i < cantidad; i++)
            {
                Carta carta = mazo.Robar();
                if (carta == null)
                {
                    break;
                }
                Jugadores[jugador].Mano.Add(carta);
                robadas++;
            }

            Mensaje = Jugadores[jugador].Nombre + " olvidó decir UNO y roba " + robadas + " cartas";
        }

        private string MotivoInvalido(int jugador, int carta)
        {
            if (JuegoTerminado)
            {
                return "La partida ya terminó";
            }

            if (jugador < 0 || jugador >= Jugadores.Count)
            {
                return "Jugador inexistente";
            }

            if (jugador != TurnoActual)
            {
                return "No es tu turno";
            }

            if (carta < 0 || carta >= Jugadores[jugador].Mano.Count)
            {
                return "Carta inexistente";
            }

            if (!Reglas.EsJugable(Jugadores[jugador].Mano[carta], CartaSuperior, ColorActual))
            {
                return "Esa carta no se puede jugar sobre la actual";
            }

            return null;
        }

        private void AplicarEfecto(Carta carta)
        {
            switch (carta.Tipo)
            {
                case TipoCarta.Saltar:
                    Mensaje = Jugadores[Siguiente()].Nombre + " pierde su turno";
                    AvanzarTurno(2);
                    break;

                case TipoCarta.Reversa:
                    direccion = -direccion;
                    if (Jugadores.Count == 2)
                    {
                        Mensaje = Jugadores[Siguiente()].Nombre + " pierde su turno";
                        AvanzarTurno(2);
                    }
                    else
                    {
                        Mensaje = "Cambió el sentido del juego";
                        AvanzarTurno(1);
                    }
                    break;

                case TipoCarta.MasDos:
                    ObligarARobar(2);
                    break;

                case TipoCarta.MasCuatro:
                    ObligarARobar(4);
                    break;

                default:
                    AvanzarTurno(1);
                    break;
            }
        }

        private void ObligarARobar(int cantidad)
        {
            int victima = Siguiente();

            for (int i = 0; i < cantidad; i++)
            {
                Carta carta = mazo.Robar();
                if (carta == null)
                {
                    break;
                }
                Jugadores[victima].Mano.Add(carta);
            }

            Mensaje = Jugadores[victima].Nombre + " roba " + cantidad + " cartas y pierde su turno";
            AvanzarTurno(2);
        }

        private int Siguiente()
        {
            int n = Jugadores.Count;
            return ((TurnoActual + direccion) % n + n) % n;
        }

        private void AvanzarTurno(int pasos)
        {
            int n = Jugadores.Count;
            TurnoActual = ((TurnoActual + direccion * pasos) % n + n) % n;
        }
    }
}