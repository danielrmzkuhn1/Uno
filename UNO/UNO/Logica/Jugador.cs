using System.Collections.Generic;

namespace UNO.Logica
{
    public class Jugador
    {
        public string Nombre { get; }
        public List<Carta> Mano { get; } = new List<Carta>();

        public Jugador(string nombre)
        {
            Nombre = nombre;
        }
    }
}