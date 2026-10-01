namespace UNO.Logica
{
    internal static class Reglas
    {
        public static bool EsJugable(Carta carta, Carta superior, ColorCarta colorActual)
        {
            if (EsComodin(carta))
            {
                return true;
            }

            if (carta.Color == colorActual)
            {
                return true;
            }

            if (carta.Tipo == TipoCarta.Numero
                && superior.Tipo == TipoCarta.Numero
                && carta.Numero == superior.Numero)
            {
                return true;
            }

            if (carta.Tipo != TipoCarta.Numero && carta.Tipo == superior.Tipo)
            {
                return true;
            }

            return false;
        }

        public static bool EsComodin(Carta carta)
        {
            return carta.Tipo == TipoCarta.Comodin || carta.Tipo == TipoCarta.MasCuatro;
        }
    }
}