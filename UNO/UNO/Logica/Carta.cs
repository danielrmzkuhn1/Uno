namespace UNO.Logica
{
    public class Carta
    {
        public ColorCarta Color { get; }
        public TipoCarta Tipo { get; }
        public int Numero { get; }

        public Carta(ColorCarta color, TipoCarta tipo, int numero = -1)
        {
            Color = color;
            Tipo = tipo;
            Numero = numero;
        }
    }
}