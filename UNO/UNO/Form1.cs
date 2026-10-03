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
        private Control[] controlesJuego;
        private Point[] posOriginal;
        private Size tamDiseno;

        public Form1()
        {
            InitializeComponent();
            controlesJuego = new Control[]
            {
                lblTurno, lblSentido, lblMazo, lblColorActivo, lblAviso,
                lblCartaSuperior, lstJugadores, flpMano, btnRobar, btnVerCartas
            };

            posOriginal = new Point[controlesJuego.Length];
            for (int i = 0; i < controlesJuego.Length; i++)
            {
                posOriginal[i] = controlesJuego[i].Location;
            }
            tamDiseno = ClientSize;

            Resize += (s, e) => CentrarJuego();
            CentrarJuego();

            pnlMenu.BringToFront();

            pnlMenu.Resize += (s, e) => CentrarMenu();
            CentrarMenu();
            btnJugar.BackColor = Color.FromArgb(245, 200, 66);
            btnJugar.ForeColor = Color.FromArgb(40, 40, 40);
            btnJugar.FlatStyle = FlatStyle.Flat;
            btnJugar.FlatAppearance.BorderSize = 0;
            btnJugar.FlatAppearance.MouseOverBackColor = Color.FromArgb(250, 215, 110);
            btnJugar.Font = new Font("Segoe UI", 16, FontStyle.Bold);
        }

        private void CentrarMenu()
        {
            int centro = pnlMenu.Width / 2;
            int alto = pnlMenu.Height;

            TextBox[] cajas = { txtJugador1, txtJugador2, txtJugador3, txtJugador4 };
            int ancho = cajas[0].Width;
            int altoCaja = cajas[0].Height;
            int sepH = 20;
            int sepV = 15;

            int x0 = centro - (2 * ancho + sepH) / 2;
            int y0 = (int)(alto * 0.68);

            for (int i = 0; i < cajas.Length; i++)
            {
                int col = i % 2;
                int fila = i / 2;
                cajas[i].Left = x0 + col * (ancho + sepH);
                cajas[i].Top = y0 + fila * (altoCaja + sepV);
            }

            btnJugar.Left = centro - btnJugar.Width / 2;
            btnJugar.Top = y0 + 2 * (altoCaja + sepV) + 10;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void btnJugar_Click(object sender, EventArgs e)
        {
            TextBox[] cajas = { txtJugador1, txtJugador2, txtJugador3, txtJugador4 };
            List<string> nombres = new List<string>();

            for (int i = 0; i < cajas.Length; i++)
            {
                string nombre = cajas[i].Text.Trim();
                if (nombre == "") nombre = "Jugador " + (i + 1);

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

            AplicarCarta(lblCartaSuperior, juego.CartaSuperior);
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
                int indice = i;
                Button b = new Button();
                b.Size = new Size(90, 130);
                b.FlatStyle = FlatStyle.Flat;
                b.FlatAppearance.BorderSize = 0;
                AplicarCarta(b, mano[i]);

                bool jugable = juego.PuedeJugar(turno, indice);
                b.Margin = new Padding(3, jugable ? 3 : 25, 3, 3);

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

        private void pnlMenu_Paint(object sender, PaintEventArgs e)
        {

        }

        private Image ImagenCarta(Carta carta)
        {
            string nombre;
            switch (carta.Tipo)
            {
                case TipoCarta.Numero: nombre = carta.Color + "_" + carta.Numero; break;
                case TipoCarta.Saltar: nombre = carta.Color + "_Saltar"; break;
                case TipoCarta.Reversa: nombre = carta.Color + "_Reversa"; break;
                case TipoCarta.MasDos: nombre = carta.Color + "_MasDos"; break;
                case TipoCarta.Comodin: nombre = "Comodin"; break;
                case TipoCarta.MasCuatro: nombre = "MasCuatro"; break;
                default: return null;
            }
            return UNO.Properties.Resources.ResourceManager.GetObject(nombre) as Image;
        }


        private void AplicarCarta(Control ctrl, Carta carta)
        {
            Image img = ImagenCarta(carta);
            ctrl.BackgroundImage = img;
            ctrl.BackgroundImageLayout = ImageLayout.Zoom;
            if (img != null)
            {
                ctrl.Text = "";
            }
            else
            {
                ctrl.Text = Texto(carta);
                ctrl.BackColor = ColorUI(carta.Color);
            }
        }

        private void CentrarJuego()
        {
            int dx = Math.Max(0, (ClientSize.Width - tamDiseno.Width) / 2);
            int dy = Math.Max(0, (ClientSize.Height - tamDiseno.Height) / 2);

            for (int i = 0; i < controlesJuego.Length; i++)
            {
                controlesJuego[i].Location = new Point(posOriginal[i].X + dx, posOriginal[i].Y + dy);
            }
        }
    }

}