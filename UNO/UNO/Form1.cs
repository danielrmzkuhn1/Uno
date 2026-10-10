using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using UNO.Logica;

namespace UNO
{
    public partial class Form1 : Form
    {
        private Juego juego;
        private ApiService api = new ApiService();
        private int idPartida;
        private List<int> idsJugadores = new List<int>();
        private int turnoHistorial = 0;
        private bool manoVisible = false;
        private string aviso = "";

        private Control[] controlesJuego;
        private Point[] posOriginal;
        private Size tamDiseno;

        private const int SEGUNDOS_UNO = 5;
        private const int CARTAS_CASTIGO_UNO = 2;
        private Button btnUno;
        private System.Windows.Forms.Timer tmrUno = new System.Windows.Forms.Timer();
        private int unoJugador = -1;  
        private int segundosUno = 0;

        private Panel pnlIzq, pnlArriba, pnlDer;

        private Dictionary<string, Image> cacheCartas = new Dictionary<string, Image>();

        public Form1()
        {
            InitializeComponent();

            CrearBotonUno();
            CrearMesa();

            controlesJuego = new Control[]
            {
                lblTurno, lblSentido, lblMazo, lblColorActivo, lblAviso,
                lblCartaSuperior, lstJugadores, flpMano, btnRobar, btnVerCartas, btnUno
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

        private async void btnJugar_Click(object sender, EventArgs e)
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

            try
            {
                idsJugadores.Clear();

                foreach (string nombre in nombres)
                {
                    int id = await api.CrearJugador(nombre);
                    idsJugadores.Add(id);
                }

                idPartida = await api.CrearPartida();
                turnoHistorial = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al conectar con la API: " + ex.Message);
                return;
            }


            idPartida = await api.CrearPartida();
            turnoHistorial = 0;

            juego = new Juego(nombres);
            manoVisible = false;
            aviso = "";
            pnlMenu.Visible = false;
            Redibujar();
        }

        private void pnlMenu_Paint(object sender, PaintEventArgs e)
        {

        }


        private async void Redibujar()
        {
            if (juego.JuegoTerminado)
            {
                CerrarVentanaUno();
                try
                {
                    if (juego.Ganador.HasValue)
                    {
                        int indiceGanador = juego.Ganador.Value;
                        int idGanador = idsJugadores[indiceGanador];

                        await api.GuardarGanador(idPartida, idGanador);
                        await api.GuardarJugada(
                            idPartida,
                            idsJugadores[juego.TurnoActual],
                            ++turnoHistorial,
                            "Gano",
                            null,
                            null
                        );
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al guardar ganador: " + ex.Message);
                }
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

            DibujarMesa();

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

        private async void ClicCarta(int indice)
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
            if (ok)
            {
                await api.GuardarJugada(
                    idPartida,
                    idsJugadores[turno],
                    ++turnoHistorial,
                    "Jugar",
                    carta.Color.ToString(),
                    Texto(carta)
                );
            }
            aviso = juego.Mensaje;
            if (ok) manoVisible = false;

            if (ok && !juego.JuegoTerminado && juego.Jugadores[turno].Mano.Count == 1)
            {
                IniciarVentanaUno(turno);
            }
            Redibujar();
        }

        private async void btnRobar_Click(object sender, EventArgs e)
        {
            bool ok = juego.RobarCarta(juego.TurnoActual);
            aviso = juego.Mensaje;
            if (ok)
            {
                manoVisible = false;
                await api.GuardarJugada(
                    idPartida,
                    idsJugadores[juego.TurnoActual],
                    ++turnoHistorial,
                    "Robar",
                    null,
                    null
                );
            }
            Redibujar();
        }

        private void btnVerCartas_Click(object sender, EventArgs e)
        {
            manoVisible = true;
            Redibujar();
        }

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

            Image img;
            if (!cacheCartas.TryGetValue(nombre, out img))
            {
                img = UNO.Properties.Resources.ResourceManager.GetObject(nombre) as Image;
                cacheCartas[nombre] = img;
            }
            return img;
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

        // =====================================================
        //  CENTRAR EL JUEGO AL CAMBIAR EL TAMAÑO DE LA VENTANA
        // =====================================================

        private void CentrarJuego()
        {
            int dx = Math.Max(0, (ClientSize.Width - tamDiseno.Width) / 2);
            int dy = Math.Max(0, (ClientSize.Height - tamDiseno.Height) / 2);

            for (int i = 0; i < controlesJuego.Length; i++)
            {
                controlesJuego[i].Location = new Point(posOriginal[i].X + dx, posOriginal[i].Y + dy);
            }
        }

        private void CrearBotonUno()
        {
            btnUno = new Button();
            btnUno.Text = "¡UNO!";
            btnUno.Size = new Size(150, 60);
            btnUno.Location = new Point(btnRobar.Left, btnRobar.Bottom + 70);
            btnUno.BackColor = Color.FromArgb(220, 50, 47);
            btnUno.ForeColor = Color.White;
            btnUno.FlatStyle = FlatStyle.Flat;
            btnUno.FlatAppearance.BorderSize = 0;
            btnUno.Font = new Font("Segoe UI", 18, FontStyle.Bold);
            btnUno.Visible = false;
            btnUno.Click += btnUno_Click;
            Controls.Add(btnUno);

            tmrUno.Interval = 1000; 
            tmrUno.Tick += tmrUno_Tick;
        }

        private void IniciarVentanaUno(int jugador)
        {
            unoJugador = jugador;
            segundosUno = SEGUNDOS_UNO;
            btnUno.Text = "¡UNO! (" + segundosUno + ")";
            btnUno.Visible = true;
            btnUno.BringToFront();
            tmrUno.Start();

            aviso = (aviso == "" ? "" : aviso + "  |  ") + juego.Jugadores[jugador].Nombre + " tiene 1 carta: ¡UNO!";
        }

        private void CerrarVentanaUno()
        {
            tmrUno.Stop();
            btnUno.Visible = false;
            unoJugador = -1;
        }

        private async void btnUno_Click(object sender, EventArgs e)
        {
            if (unoJugador < 0) return;
            aviso = juego.Jugadores[unoJugador].Nombre + " dijo ¡UNO!";
            await api.GuardarJugada(
                idPartida,
                idsJugadores[juego.TurnoActual],
                ++turnoHistorial,
                "Dijo Uno",
                null,
                null
            );
            CerrarVentanaUno();
            Redibujar();
        }

        private void tmrUno_Tick(object sender, EventArgs e)
        {
            segundosUno--;
            if (segundosUno > 0)
            {
                btnUno.Text = "¡UNO! (" + segundosUno + ")";
                return;
            }

            int castigado = unoJugador;
            CerrarVentanaUno();

            juego.CastigarUno(castigado, CARTAS_CASTIGO_UNO);
            aviso = juego.Mensaje;
            Redibujar();
        }


        private void CrearMesa()
        {
            pnlIzq = new Panel(); pnlIzq.Size = new Size(110, 380); pnlIzq.BackColor = Color.Transparent;
            pnlArriba = new Panel(); pnlArriba.Size = new Size(420, 130); pnlArriba.BackColor = Color.Transparent;
            pnlDer = new Panel(); pnlDer.Size = new Size(110, 380); pnlDer.BackColor = Color.Transparent;

            Controls.Add(pnlIzq);
            Controls.Add(pnlArriba);
            Controls.Add(pnlDer);

            Resize += (s, e) => PosicionarMesa();
            PosicionarMesa();
        }

        private void PosicionarMesa()
        {
            pnlArriba.Location = new Point((ClientSize.Width - pnlArriba.Width) / 2, 10);
            pnlIzq.Location = new Point(20, (ClientSize.Height - pnlIzq.Height) / 2 - 40);
            pnlDer.Location = new Point(ClientSize.Width - pnlDer.Width - 20, (ClientSize.Height - pnlDer.Height) / 2 - 40);
        }

        private void lblCartaSuperior_Click(object sender, EventArgs e)
        {

        }

        private void DibujarMesa()
        {
            int t = juego.TurnoActual;
            int n = juego.Jugadores.Count;

            pnlIzq.Controls.Clear();
            pnlArriba.Controls.Clear();
            pnlDer.Controls.Clear();

            if (n == 2)
            {
                DibujarRival(pnlArriba, juego.Jugadores[(t + 1) % n], false);
            }
            else if (n == 3)
            {
                DibujarRival(pnlIzq, juego.Jugadores[(t + 1) % n], true);
                DibujarRival(pnlDer, juego.Jugadores[(t + 2) % n], true);
            }
            else if (n >= 4)
            {
                DibujarRival(pnlIzq, juego.Jugadores[(t + 1) % n], true);
                DibujarRival(pnlArriba, juego.Jugadores[(t + 2) % n], false);
                DibujarRival(pnlDer, juego.Jugadores[(t + 3) % n], true);
            }
        }

        private void DibujarRival(Panel zona, Jugador j, bool lateral)
        {
            int n = j.Mano.Count;

            Label nombre = new Label();
            nombre.Text = j.Nombre + " (" + n + ")";
            nombre.ForeColor = Color.White;
            nombre.BackColor = Color.Transparent;
            nombre.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            nombre.AutoSize = true;
            nombre.Location = new Point(0, 0);
            zona.Controls.Add(nombre);

            int w = 56, h = 80;
            int ini = 28;

            int espacio = lateral ? zona.Height - ini - h : zona.Width - w;
            int paso = n > 1 ? Math.Min(30, espacio / (n - 1)) : 0;

            for (int i = 0; i < n; i++)
            {
                Label c = new Label();
                c.Size = new Size(w, h);
                c.BackColor = Color.Transparent;
                AplicarCarta(c, j.Mano[i]);
                c.Location = lateral ? new Point(0, ini + i * paso) : new Point(i * paso, ini);
                zona.Controls.Add(c);
                c.BringToFront();
            }
        }

    }
}