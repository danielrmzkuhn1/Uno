using System;
using System.Collections.Generic;

namespace UNO.Logica
{
    internal class Mazo
    {
        private static readonly Random azar = new Random();
        private readonly List<Carta> cartas = new List<Carta>();
        private readonly List<Carta> descarte = new List<Carta>();

        public int Cantidad => cartas.Count;

        public Mazo()
        {
            CrearCartas();
            Barajar(cartas);
        }

        public Carta Robar()
        {
            if (cartas.Count == 0)
            {
                Rebarajar();
            }

            if (cartas.Count == 0)
            {
                return null;
            }

            Carta carta = cartas[cartas.Count - 1];
            cartas.RemoveAt(cartas.Count - 1);
            return carta;
        }

        public void Devolver(Carta carta)
        {
            cartas.Insert(0, carta);
        }

        public void Descartar(Carta carta)
        {
            descarte.Add(carta);
        }

        private void CrearCartas()
        {
            ColorCarta[] colores = { ColorCarta.Rojo, ColorCarta.Azul, ColorCarta.Verde, ColorCarta.Amarillo };
            TipoCarta[] especiales = { TipoCarta.Saltar, TipoCarta.Reversa, TipoCarta.MasDos };

            foreach (ColorCarta color in colores)
            {
                cartas.Add(new Carta(color, TipoCarta.Numero, 0));

                for (int n = 1; n <= 9; n++)
                {
                    cartas.Add(new Carta(color, TipoCarta.Numero, n));
                    cartas.Add(new Carta(color, TipoCarta.Numero, n));
                }

                foreach (TipoCarta tipo in especiales)
                {
                    cartas.Add(new Carta(color, tipo));
                    cartas.Add(new Carta(color, tipo));
                }
            }

            for (int i = 0; i < 4; i++)
            {
                cartas.Add(new Carta(ColorCarta.Ninguno, TipoCarta.Comodin));
                cartas.Add(new Carta(ColorCarta.Ninguno, TipoCarta.MasCuatro));
            }
        }

        private void Rebarajar()
        {
            if (descarte.Count <= 1)
            {
                return;
            }

            Carta tope = descarte[descarte.Count - 1];
            descarte.RemoveAt(descarte.Count - 1);
            cartas.AddRange(descarte);
            descarte.Clear();
            descarte.Add(tope);
            Barajar(cartas);
        }

        private static void Barajar(List<Carta> lista)
        {
            for (int i = lista.Count - 1; i > 0; i--)
            {
                int j = azar.Next(i + 1);
                Carta temporal = lista[i];
                lista[i] = lista[j];
                lista[j] = temporal;
            }
        }
    }
}