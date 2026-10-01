using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UNO.Logica;

namespace UNO
{
    // =====================================================================
    // PROGRAMA DE PRUEBA EN CONSOLA
    //
    // Simula lo que hará la interfaz gráfica de tu compañera, pero en texto.
    // No forma parte de la entrega final: solo sirve para probar tu lógica.
    //
    // CONTRATO (todo lo que la interfaz usa de la clase Juego):
    //
    //   LEE (propiedades):                     LLAMA (métodos):
    //   juego.Jugadores                        new Juego(nombres)
    //   juego.TurnoActual                      juego.JugarCarta(jugador, carta, color)
    //   juego.CartaSuperior                    juego.RobarCarta(jugador)
    //   juego.ColorActual                      juego.PuedeJugar(jugador, carta)
    //   juego.CartasEnMazo                     juego.PuedeRobar(jugador)
    //   juego.SentidoHorario
    //   juego.Mensaje
    //   juego.JuegoTerminado
    //   juego.Ganador
    //
    // Ciclo de cada jugada:
    //   1. El usuario hace algo        -> se llama un método (JugarCarta o RobarCarta)
    //   2. El juego valida y cambia su estado, y devuelve true o false
    //   3. Se vuelve a leer el estado  -> Dibujar() redibuja todo desde cero
    // =====================================================================
    internal static class correrConsolaTEST
    {
        public static void Ejecutar()
        {
            Console.OutputEncoding = Encoding.UTF8;

            try
            {
                Jugar();
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error: " + ex.Message);
            }

            Console.WriteLine();
            Console.Write("Presiona Enter para cerrar...");
            Console.ReadLine();
        }


        private static void Jugar()
        {
            // Equivale a lo que haría el menú de tu compañera: recoger los nombres y crear la partida.
            Juego juego = CrearJuego();

            // Para que el jugador siguiente no vea la mano del anterior (todos comparten pantalla).
            bool pasarPantalla = true;

            // Último mensaje a mostrar (error de una jugada rechazada o aviso de un efecto).
            string aviso = "";

            // JuegoTerminado pasa a true cuando alguien se queda sin cartas.
            while (!juego.JuegoTerminado)
            {
                // TurnoActual es la POSICIÓN en juego.Jugadores del jugador que debe actuar.
                int turno = juego.TurnoActual;
                Jugador jugador = juego.Jugadores[turno];

                if (pasarPantalla)
                {
                    // Se dibuja el estado SIN mostrar la mano, y se espera confirmación.
                    Dibujar(juego, false, aviso);
                    Console.WriteLine();
                    Console.Write("Pasa el equipo a " + jugador.Nombre + " y presiona Enter para ver tus cartas...");
                    Console.ReadLine();
                    pasarPantalla = false;
                }

                Dibujar(juego, true, aviso);
                Console.WriteLine();
                Console.Write("Número de carta a jugar, 'r' para robar, 's' para salir: ");
                string entrada = (Console.ReadLine() ?? "").Trim().ToLower();
                if (entrada == "s")
                {
                    return;
                }

                bool ok;
                int numero;

                if (entrada == "r")
                {
                    // Equivale a hacer clic en el mazo.
                    // RobarCarta devuelve true si la acción fue aceptada, false si no.
                    ok = juego.RobarCarta(turno);
                }
                else if (int.TryParse(entrada, out numero))
                {
                    // En pantalla las cartas se numeran desde 1, pero el juego espera
                    // la posición dentro de la mano, que empieza en 0.
                    int indice = numero - 1;
                    ColorCarta color = ColorCarta.Ninguno;

                    // Si la carta es comodín, la interfaz debe pedir el color ANTES de llamar
                    // a JugarCarta, y mandarlo en el tercer parámetro (una sola llamada).
                    if (indice >= 0
                        && indice < jugador.Mano.Count
                        && juego.PuedeJugar(turno, indice)
                        && EsComodin(jugador.Mano[indice]))
                    {
                        color = PedirColor();
                    }

                    // Equivale a hacer clic en una carta de la mano.
                    // El juego valida, cambia el estado (mano, carta superior, turno, efectos)
                    // y devuelve true/false. Si es false, el motivo queda en juego.Mensaje.
                    ok = juego.JugarCarta(turno, indice, color);
                }
                else
                {
                    aviso = "Entrada no válida";
                    continue;
                }

                // Mensaje trae el error (si ok es false) o un aviso del efecto (si ok es true).
                aviso = juego.Mensaje;

                // Solo se cambia de pantalla si la acción fue aceptada.
                if (ok)
                {
                    pasarPantalla = true;
                }
            }

            Console.Clear();
            Console.WriteLine("=============== FIN ===============");

            // Ganador es null mientras nadie gana; al terminar trae la posición del jugador.
            if (juego.Ganador.HasValue)
            {
                Console.WriteLine("Ganó " + juego.Jugadores[juego.Ganador.Value].Nombre);
            }
        }

        private static Juego CrearJuego()
        {
            Console.Clear();
            Console.WriteLine("=============== UNO ===============");

            int cantidad = 0;

            // Juego.MinJugadores y Juego.MaxJugadores son constantes para validar sin números mágicos.
            while (cantidad < Juego.MinJugadores || cantidad > Juego.MaxJugadores)
            {
                Console.Write("Número de jugadores (" + Juego.MinJugadores + "-" + Juego.MaxJugadores + "): ");
                int.TryParse(Console.ReadLine(), out cantidad);
            }

            List<string> nombres = new List<string>();
            for (int i = 0; i < cantidad; i++)
            {
                Console.Write("Nombre del jugador " + (i + 1) + ": ");
                nombres.Add(Console.ReadLine() ?? "");
            }

            // Único momento en que se crea la partida. Para "jugar otra vez" se crea otro Juego.
            return new Juego(nombres);
        }

        // Equivale al método Redibujar() de la interfaz gráfica:
        // lee el estado completo del juego y pinta todo desde cero.
        private static void Dibujar(Juego juego, bool mostrarMano, string aviso)
        {
            Console.Clear();
            Console.WriteLine("=============== UNO ===============");

            // TurnoActual + Jugadores[...].Nombre: de quién es el turno.
            Console.WriteLine("Turno de: " + juego.Jugadores[juego.TurnoActual].Nombre);

            // SentidoHorario: dirección actual (cambia con la carta Reversa).
            Console.WriteLine("Sentido: " + (juego.SentidoHorario ? "horario >>" : "antihorario <<"));

            // CartasEnMazo: solo el número de cartas por robar, nunca las cartas en sí.
            Console.WriteLine("Cartas en el mazo: " + juego.CartasEnMazo);
            Console.WriteLine();

            // CartaSuperior: la carta de arriba de la pila de descarte.
            Console.Write("Carta actual: ");
            Pintar(juego.CartaSuperior);
            Console.WriteLine();

            // ColorActual: el color que rige. Puede diferir del de la carta superior tras un comodín.
            Console.Write("Color activo: ");
            Console.ForegroundColor = ColorConsola(juego.ColorActual);
            Console.WriteLine(juego.ColorActual);
            Console.ResetColor();
            Console.WriteLine();

            // Jugadores[i].Mano.Count: de los rivales solo se muestra cuántas cartas tienen.
            Console.WriteLine("Jugadores:");
            for (int i = 0; i < juego.Jugadores.Count; i++)
            {
                Jugador j = juego.Jugadores[i];
                string marca = i == juego.TurnoActual ? ">" : " ";
                Console.WriteLine("  " + marca + " " + j.Nombre + " (" + j.Mano.Count + " cartas)");
            }

            if (!string.IsNullOrEmpty(aviso))
            {
                Console.WriteLine();
                Console.WriteLine("Aviso: " + aviso);
            }

            if (!mostrarMano)
            {
                return;
            }

            Console.WriteLine();
            Console.WriteLine("Tus cartas (* = se puede jugar):");

            // Mano completa del jugador en turno. Cada carta trae Color, Tipo y Numero.
            List<Carta> mano = juego.Jugadores[juego.TurnoActual].Mano;
            for (int i = 0; i < mano.Count; i++)
            {
                // PuedeJugar le pregunta al juego si esa carta es válida ahora mismo.
                // La interfaz gráfica lo usaría para habilitar o deshabilitar cada botón.
                bool jugable = juego.PuedeJugar(juego.TurnoActual, i);

                Console.Write("  " + (i + 1).ToString().PadLeft(2) + ") ");
                Pintar(mano[i]);
                if (jugable)
                {
                    Console.Write(" *");
                }
                Console.WriteLine();
            }
        }

        // Convierte una Carta (color, tipo, número) en algo visible.
        // En la interfaz gráfica esto sería escoger la imagen de la carta.
        private static void Pintar(Carta carta)
        {
            Console.ForegroundColor = ColorConsola(carta.Color);
            Console.Write("[" + Texto(carta) + "]");
            Console.ResetColor();
        }

        private static string Texto(Carta carta)
        {
            switch (carta.Tipo)
            {
                case TipoCarta.Numero: return carta.Color + " " + carta.Numero;
                case TipoCarta.Saltar: return carta.Color + " Saltar";
                case TipoCarta.Reversa: return carta.Color + " Reversa";
                case TipoCarta.MasDos: return carta.Color + " +2";
                case TipoCarta.Comodin: return "Comodín";
                case TipoCarta.MasCuatro: return "Comodín +4";
                default: return "";
            }
        }

        private static ConsoleColor ColorConsola(ColorCarta color)
        {
            switch (color)
            {
                case ColorCarta.Rojo: return ConsoleColor.Red;
                case ColorCarta.Azul: return ConsoleColor.Cyan;
                case ColorCarta.Verde: return ConsoleColor.Green;
                case ColorCarta.Amarillo: return ConsoleColor.Yellow;
                default: return ConsoleColor.White;
            }
        }

        // La interfaz sabe si una carta es comodín mirando su Tipo.
        private static bool EsComodin(Carta carta)
        {
            return carta.Tipo == TipoCarta.Comodin || carta.Tipo == TipoCarta.MasCuatro;
        }

        // Selector de color: en la interfaz gráfica sería un cuadro con 4 botones.
        private static ColorCarta PedirColor()
        {
            while (true)
            {
                Console.Write("Elige color: 1) Rojo  2) Azul  3) Verde  4) Amarillo: ");
                string opcion = (Console.ReadLine() ?? "").Trim();

                switch (opcion)
                {
                    case "1": return ColorCarta.Rojo;
                    case "2": return ColorCarta.Azul;
                    case "3": return ColorCarta.Verde;
                    case "4": return ColorCarta.Amarillo;
                }
            }
        }
    }
}