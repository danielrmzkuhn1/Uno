using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using UNO.Logica;

namespace UNO
{
    public partial class Form1 : Form
    {
        private Juego juego;
        private bool manoVisible = false;
        private string aviso = "";

        public Form1()
        {
            InitializeComponent();
            pnlMenu.BringToFront();

            pnlMenu.Resize += (s, e) => CentrarMenu(); 
            CentrarMenu();
        }

        private void CentrarMenu()
        {
            int centro = pnlMenu.Width / 2;
            int alto = pnlMenu.Height;

            TextBox[] cajas = { txtJugador1, txtJugador2, txtJugador3, txtJugador4 };
            int separacion = 15;
            int total = cajas.Length * cajas[0].Width + (cajas.Length - 1) * separacion;
            int x = centro - total / 2;

            for (int i = 0; i < cajas.Length; i++)
            {
                cajas[i].Left = x + i * (cajas[i].Width + separacion);
                cajas[i].Top = (int)(alto * 0.70);
            }

            btnJugar.Left = centro - btnJugar.Width / 2;
            btnJugar.Top = (int)(alto * 0.80);
        } 

        private void Form1_Load(object sender, EventArgs e)
        {
            // vacío: la partida se crea al dar JUGAR, cuando ya tenemos los nombres
        }

        private void btnJugar_Click(object sender, EventArgs e)
        {
            TextBox[] cajas = { txtJugador1, txtJugador2, txtJugador3, txtJugador4 };
            List<string> nombres = new List<string>();

            for (int i = 0; i < cajas.Length; i++)
            {
                string nombre = cajas[i].Text.Trim();
                if (nombre == "") nombre = "Jugador " + (i + 1); // si lo dejan vacío, nombre por defecto

                if (nombres.Exists(x => x.Equals(nombre, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show("Hay nombres repetidos: \"" + nombre + "\". Cambia uno.");
                    return;
                }
                nombres.Add(nombre);
            }

            juego = new Juego(nombres);
            manoVisible = false;
            aviso = "";
            pnlMenu.Visible = false;
            Redibujar();
        }

        // Equivale al Dibujar() de la consola.
        private void Redibujar()
        {
            if (juego.JuegoTerminado)
            {
                MessageBox.Show("Ganó " + juego.Jugadores[juego.Ganador.Value].Nombre, "Fin del juego");
                Close();
                return;
            }

            int turno = juego.TurnoActual;

            lblTurno.Text = "Turno de: " + juego.Jugadores[turno].Nombre;
            lblSentido.Text = juego.SentidoHorario ? "Sentido: horario >>" : "Sentido: antihorario <<";
            lblMazo.Text = "Cartas en el mazo: " + juego.CartasEnMazo;
            lblAviso.Text = aviso;

            lblCartaSuperior.Text = Texto(juego.CartaSuperior);
            lblCartaSuperior.BackColor = ColorUI(juego.CartaSuperior.Color);
            lblColorActivo.Text = "Color activo: " + juego.ColorActual;
            lblColorActivo.BackColor = ColorUI(juego.ColorActual);

            lstJugadores.Items.Clear();
            for (int i = 0; i < juego.Jugadores.Count; i++)
            {
                Jugador j = juego.Jugadores[i];
                lstJugadores.Items.Add((i == turno ? "> " : "  ") + j.Nombre + " (" + j.Mano.Count + " cartas)");
            }

            // Pantalla compartida: la mano solo se ve después de "Ver mis cartas".
            btnVerCartas.Visible = !manoVisible;
            flpMano.Visible = manoVisible;
            btnRobar.Enabled = manoVisible && juego.PuedeRobar(turno);
            flpMano.Controls.Clear();
            if (!manoVisible) return;

            List<Carta> mano = juego.Jugadores[turno].Mano;
            for (int i = 0; i < mano.Count; i++)
            {
                int indice = i; // copia, para que el clic use la posición correcta
                Button b = new Button();
                b.Text = Texto(mano[i]);
                b.BackColor = ColorUI(mano[i].Color);
                b.Size = new Size(80, 110);
                b.Enabled = juego.PuedeJugar(turno, indice); // deshabilita las no jugables
                b.Click += (s, ev) => ClicCarta(indice);
                flpMano.Controls.Add(b);
            }
        }

        private void ClicCarta(int indice)
        {
            int turno = juego.TurnoActual;
            Carta carta = juego.Jugadores[turno].Mano[indice];
            ColorCarta color = ColorCarta.Ninguno;

            if (carta.Tipo == TipoCarta.Comodin || carta.Tipo == TipoCarta.MasCuatro)
            {
                color = PedirColor();
                if (color == ColorCarta.Ninguno) return;
            }

            bool ok = juego.JugarCarta(turno, indice, color);
            aviso = juego.Mensaje;
            if (ok) manoVisible = false;
            Redibujar();
        }

        private void btnRobar_Click(object sender, EventArgs e)
        {
            bool ok = juego.RobarCarta(juego.TurnoActual);
            aviso = juego.Mensaje;
            if (ok) manoVisible = false;
            Redibujar();
        }

        private void btnVerCartas_Click(object sender, EventArgs e)
        {
            manoVisible = true;
            Redibujar();
        }

        // Cuadro con 4 botones para escoger color del comodín.
        private ColorCarta PedirColor()
        {
            ColorCarta elegido = ColorCarta.Ninguno;
            ColorCarta[] colores = { ColorCarta.Rojo, ColorCarta.Azul, ColorCarta.Verde, ColorCarta.Amarillo };

            using (Form f = new Form())
            {
                f.Text = "Elige un color";
                f.StartPosition = FormStartPosition.CenterParent;
                f.FormBorderStyle = FormBorderStyle.FixedDialog;
                f.ControlBox = false;
                f.ClientSize = new Size(330, 80);

                for (int i = 0; i < colores.Length; i++)
                {
                    ColorCarta c = colores[i];
                    Button b = new Button();
                    b.Text = c.ToString();
                    b.BackColor = ColorUI(c);
                    b.Size = new Size(70, 50);
                    b.Location = new Point(10 + i * 78, 15);
                    b.Click += (s, ev) => { elegido = c; f.Close(); };
                    f.Controls.Add(b);
                }
                f.ShowDialog(this);
            }
            return elegido;
        }

        private string Texto(Carta carta)
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

        private Color ColorUI(ColorCarta c)
        {
            switch (c)
            {
                case ColorCarta.Rojo: return Color.IndianRed;
                case ColorCarta.Azul: return Color.CornflowerBlue;
                case ColorCarta.Verde: return Color.MediumSeaGreen;
                case ColorCarta.Amarillo: return Color.Gold;
                default: return Color.LightGray;
            }
        }
    }
}
