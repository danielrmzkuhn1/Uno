using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using UNO.Logica;

namespace UNO
{
    public partial class Form1 : Form
    {
        private ApiService api;
        private JObject estado;
        private bool ocupado;
        private bool cerrado;
        private bool conectado;
        private int versionDibujada = -1;
        private Control[] controlesJuego;
        private readonly TextBox servidor = new TextBox { Text = "127.0.0.1:8000", Width = 350 };
        private readonly TextBox nombre = new TextBox { Width = 350, MaxLength = 50 };
        private readonly TextBox sala = new TextBox { Width = 350, MaxLength = 8 };
        private readonly Button crear = new Button { Text = "Crear sala", Width = 170, Height = 36 };
        private readonly Button entrar = new Button { Text = "Unirse a sala", Width = 170, Height = 36 };
        private readonly Button iniciar = new Button { Text = "Iniciar partida", Width = 350, Height = 40 };
        private readonly Button btnRevancha = new Button { Text = "Nueva partida", Width = 210, Height = 40 };

        private readonly Button btnCancelar = new Button
        {
            Text = "Cancelar sala",
            Width = 350,
            Height = 36,
            BackColor = Color.Firebrick,
            ForeColor = Color.White
        };
        private readonly Button btnSalir = new Button
        {
            Text = "Salir de sala",
            Width = 350,
            Height = 36
        };
        private readonly Button btnUno = new Button { Text = "¡UNO!", Width = 164, Height = 50 };
        private readonly Label espera = new Label { AutoSize = true, MaximumSize = new Size(370, 0) };
        private readonly Label conexion = new Label { AutoSize = true, ForeColor = Color.DarkRed, MaximumSize = new Size(370, 0) };
        private readonly Dictionary<string, Image> cacheCartas = new Dictionary<string, Image>();

        private readonly Button btnEstadisticas = new Button
        {
            Text = "Estadísticas",
            Width = 350,
            Height = 36
        };

        private readonly Label tituloEstadisticas = new Label
        {
            Text = "RANKING DE VICTORIAS",
            AutoSize = true,
            Font = new Font("Segoe UI", 15, FontStyle.Bold)
        };

        private readonly ListBox listaEstadisticas = new ListBox
        {
            Width = 350,
            Height = 250
        };

        private readonly Button btnCerrarEstadisticas = new Button
        {
            Text = "Volver al menú",
            Width = 350,
            Height = 36
        };

        private Control[] controlesMenu;
        private Dictionary<Control, bool> visibilidadMenuAntes;

        public Form1()
        {
            InitializeComponent();
            AutoScaleMode = AutoScaleMode.None;
            Text = "UNO · Sockets TCP";
            MinimumSize = new Size(1100, 730);
            // El Designer y los recursos originales se conservan.
            pnlMenu.Controls.Clear();
            var menu = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Size = new Size(410, 530),
                BackColor = Color.White,
                Padding = new Padding(15),
                Anchor = AnchorStyles.None
            };
            menu.Controls.Add(new Label { Text = "UNO · Partida en Wi-Fi", AutoSize = true, Font = new Font("Segoe UI", 18, FontStyle.Bold) });
            menu.Controls.Add(new Label { Text = "Servidor TCP (IP:puerto)", AutoSize = true });
            menu.Controls.Add(servidor);
            menu.Controls.Add(new Label { Text = "Tu nombre", AutoSize = true });
            menu.Controls.Add(nombre);
            menu.Controls.Add(new Label { Text = "Código (solo para unirse)", AutoSize = true });
            menu.Controls.Add(sala);
            menu.Controls.Add(crear);
            menu.Controls.Add(entrar);
            menu.Controls.Add(iniciar);
            menu.Controls.Add(btnCancelar);
            menu.Controls.Add(btnSalir);
            menu.Controls.Add(espera);
            menu.Controls.Add(conexion);
            menu.Controls.Add(btnEstadisticas);

            controlesMenu = menu.Controls.Cast<Control>().ToArray();

            menu.Controls.Add(tituloEstadisticas);
            menu.Controls.Add(listaEstadisticas);
            menu.Controls.Add(btnCerrarEstadisticas);

            tituloEstadisticas.Visible = false;
            listaEstadisticas.Visible = false;
            btnCerrarEstadisticas.Visible = false;
            pnlMenu.Controls.Add(menu);
            Action centrar = () => menu.Location = new Point(Math.Max(0, (pnlMenu.Width - menu.Width) / 2), Math.Max(0, (pnlMenu.Height - menu.Height) / 2));
            pnlMenu.Resize += (s, e) => centrar();
            centrar();
            iniciar.Visible = false;
            btnCancelar.Visible = false;
            btnSalir.Visible = false;

            crear.Click += async (s, e) => await Conectar(true);
            entrar.Click += async (s, e) => await Conectar(false);
            iniciar.Click += async (s, e) => await Accion("iniciar");
            btnRevancha.Click += async (s, e) => await NuevaPartida();
            btnSalir.Click += async (s, e) => await SalirSala();
            btnCancelar.Click += async (s, e) => await CancelarSala();
            btnEstadisticas.Click += async (s, e) =>await AbrirEstadisticas();

            btnCerrarEstadisticas.Click += (s, e) =>CerrarEstadisticas();
            Controls.Add(lblAviso);
            lblAviso.Location = new Point(20, 400);
            lblAviso.AutoSize = false;
            lblAviso.Size = new Size(1000, 65);
            btnVerCartas.Visible = false;
            btnUno.Location = new Point(btnRobar.Left, btnRobar.Bottom + 15);
            btnUno.BackColor = Color.Firebrick;
            btnUno.ForeColor = Color.White;
            btnUno.Click += async (s, e) => await Accion("uno");
            Controls.Add(btnUno);
            btnUno.Visible = false;
            Controls.Add(btnRevancha);
            btnRevancha.Visible = false;
            flpMano.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            controlesJuego = new Control[] { lblTurno, lblSentido, lblMazo, lblCartaSuperior,
                lblColorActivo, lblAviso, lstJugadores, flpMano, btnRobar, btnRevancha };
            foreach (var control in controlesJuego) control.Visible = false;
            Resize += (s, e) => PosicionarJuego();
            PosicionarJuego();
            FormClosed += (s, e) => { cerrado = true; if (api != null) api.Dispose(); };
            pnlMenu.BringToFront();
        }

        private void PosicionarJuego()
        {
            int ancho = ClientSize.Width;
            int alto = ClientSize.Height;
            lblTurno.Location = new Point(20, 20);
            lblSentido.Location = new Point(20, 70);
            lblColorActivo.Location = new Point(20, 110);
            lblMazo.Location = new Point(20, 145);
            lstJugadores.Location = new Point(ancho - 245, 20);
            lstJugadores.Size = new Size(225, 125);
            lblCartaSuperior.Location = new Point(ancho / 2 - 65, 110);
            lblCartaSuperior.Size = new Size(130, 188);
            btnRobar.Location = new Point(ancho / 2 + 100, 180);
            btnUno.Location = new Point(ancho / 2 + 100, 240);
            btnRevancha.Location = new Point(ancho / 2 + 100, 300);
            lblAviso.Location = new Point(20, alto - 260);
            lblAviso.Size = new Size(ancho - 40, 65);
            flpMano.Location = new Point(0, alto - 185);
            flpMano.Size = new Size(ancho, 185);
        }

        private async Task Conectar(bool nueva)
        {
            if (ocupado || estado != null || api != null) return;
            if (String.IsNullOrWhiteSpace(nombre.Text) || (!nueva && String.IsNullOrWhiteSpace(sala.Text)))
            { conexion.Text = "Escribe tu nombre y, para unirte, el código de sala."; return; }
            ocupado = true;
            crear.Enabled = entrar.Enabled = false;
            ApiService candidato = null;
            try
            {
                candidato = new ApiService(servidor.Text);
                candidato.EstadoRecibido += notificacion => EnUI(() => {
                    if (!Object.ReferenceEquals(api, candidato)) return;
                    AplicarEstado(notificacion);
                    Dibujar();
                });
                candidato.Desconectado += motivo => EnUI(() => {
                    if (!Object.ReferenceEquals(api, candidato)) return;
                    conectado = false;
                    conexion.Text = "Conexión TCP cerrada: " + motivo + " Reinicia el juego para crear otra sala.";
                    Text = "UNO · Desconectado";
                    Dibujar();
                });
                var recibido = await candidato.Entrar(nombre.Text.Trim(), sala.Text, nueva);
                if (cerrado) { candidato.Dispose(); return; }
                api = candidato;
                conectado = true;
                AplicarEstado(recibido);
                if (candidato.UltimoEstado != null) AplicarEstado(candidato.UltimoEstado);
                servidor.Enabled = nombre.Enabled = sala.Enabled = false;
                crear.Visible = entrar.Visible = false;
                conexion.Text = "";
                Text = "UNO TCP · Sala " + (string)estado["codigo"];
            }
            catch (Exception ex)
            {
                if (candidato != null) candidato.Dispose();
                if (!cerrado) conexion.Text = "No se pudo entrar: " + ex.Message;
            }
            finally
            {
                ocupado = false;
                if (!cerrado) { crear.Enabled = entrar.Enabled = true; Dibujar(); }
            }
        }


        private async Task CancelarSala()
        {
            if (ocupado || estado == null || !conectado || (int)estado["yo"] != 0)
                return;

            var confirmar = MessageBox.Show(
                this,
                "¿Estás seguro de que quieres cancelar la sala?\nLos demás jugadores serán notificados.",
                "Cancelar sala",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (confirmar != DialogResult.Yes)
                return;

            ocupado = true;
            btnCancelar.Enabled = false;

            try
            {
                var recibido = await api.Cancelar();

                if (!cerrado)
                {
                    AplicarEstado(recibido);
                    VolverAlMenu();
                }
            }
            catch (Exception ex)
            {
                if (!cerrado)
                    conexion.Text = "No se pudo cancelar la sala: " + ex.Message;
            }
            finally
            {
                ocupado = false;
                if (!cerrado)
                    Dibujar();
            }
        }

        private void VolverAlMenu()
        {
            ApiService anterior = api;
            api = null;
            estado = null;
            conectado = false;
            ocupado = false;
            versionDibujada = -1;

            if (anterior != null)
                anterior.Dispose();

            servidor.Enabled = nombre.Enabled = sala.Enabled = true;
            sala.Clear();

            crear.Visible = entrar.Visible = true;
            crear.Enabled = entrar.Enabled = true;

            iniciar.Visible = btnCancelar.Visible = btnSalir.Visible = btnRevancha.Visible = false;

            espera.Text = "";
            conexion.Text = "";
            pnlMenu.Visible = true;

            foreach (var control in controlesJuego)
                control.Visible = false;

            Text = "UNO · Sockets TCP";
            pnlMenu.BringToFront();
        }

        private async Task SalirSala()
        {
            if (ocupado || estado == null)
                return;

            if ((string)estado["fase"] == "cancelada")
            {
                VolverAlMenu();
                return;
            }

            if (!conectado || (string)estado["fase"] != "espera"
                || (int)estado["yo"] == 0)
                return;

            ocupado = true;
            btnSalir.Enabled = false;

            try
            {
                await api.Salir();
                if (!cerrado)
                    VolverAlMenu();
            }
            catch (Exception ex)
            {
                if (!cerrado)
                    conexion.Text = "No se pudo salir: " + ex.Message;
            }
            finally
            {
                ocupado = false;
                if (!cerrado && estado != null)
                    Dibujar();
            }
        }

        private async Task NuevaPartida()
        {
            if (ocupado || estado == null || !conectado
                || (string)estado["fase"] != "terminada" || (int)estado["yo"] != 0)
                return;

            var confirmar = MessageBox.Show(
                this,
                "Todos volverán a la sala actual. ¿Quieres preparar otra partida?",
                "Nueva partida",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );
            if (confirmar != DialogResult.Yes)
                return;

            ocupado = true;
            btnRevancha.Enabled = false;
            try
            {
                var recibido = await api.Revancha();
                if (!cerrado)
                    AplicarEstado(recibido);
            }
            catch (Exception ex)
            {
                if (!cerrado)
                    MessageBox.Show(this, ex.Message, "No se pudo preparar la partida");
            }
            finally
            {
                ocupado = false;
                if (!cerrado)
                    Dibujar();
            }
        }

        private void EnUI(Action accion)
        {
            if (cerrado || IsDisposed || !IsHandleCreated) return;
            try { BeginInvoke(new Action(() => { if (!cerrado) accion(); })); }
            catch (InvalidOperationException) { }
        }

        private void AplicarEstado(JObject recibido)
        {
            if (estado == null || (int)recibido["version"] >= (int)estado["version"])
                estado = recibido;
        }

        private async Task Accion(string accion, int? carta = null, string color = null, int? version = null)
        {
            if (ocupado || estado == null || cerrado || !conectado) return;
            ocupado = true;
            btnRobar.Enabled = btnUno.Enabled = iniciar.Enabled = flpMano.Enabled = false;
            try
            {
                var recibido = await api.Actuar(accion, version ?? (int)estado["version"], carta, color);
                if (!cerrado) { AplicarEstado(recibido); if (conectado) conexion.Text = ""; }
            }
            catch (Exception ex)
            {
                if (!cerrado) MessageBox.Show(this, ex.Message + "\nSe consultará el estado del servidor antes de continuar.", "UNO");
                try { var recibido = await api.Consultar(); if (!cerrado) AplicarEstado(recibido); }
                catch (Exception consulta) { if (!cerrado) conexion.Text = "Sin conexión: " + consulta.Message; }
            }
            finally { ocupado = false; if (!cerrado) Dibujar(); }
        }

        private void Dibujar()
        {
            if (estado == null) return;

            string fase = (string)estado["fase"];
            int yo = (int)estado["yo"];
            var jugadores = (JArray)estado["jugadores"];
            int turno = (int)estado["turno"];
            int pendiente = (int)estado["uno_jugador"];

            bool esperando = fase == "espera";
            bool cancelada = fase == "cancelada";

            pnlMenu.Visible = esperando || cancelada;

            foreach (var control in controlesJuego)
                control.Visible = fase == "jugando" || fase == "terminada";

            btnUno.Visible = false;
            btnRevancha.Visible = fase == "terminada" && yo == 0;
            btnRevancha.Enabled = conectado && !ocupado;

            iniciar.Visible = esperando && yo == 0;
            iniciar.Enabled = conectado && !ocupado && jugadores.Count >= 2;

            btnCancelar.Visible = esperando && yo == 0;
            btnCancelar.Enabled = conectado && !ocupado;
            btnSalir.Visible = cancelada || (esperando && yo != 0);
            btnSalir.Text = cancelada ? "Volver al menú" : "Salir de sala";
            btnSalir.Enabled = !ocupado && (conectado || cancelada);

            if (cancelada)
            {
                espera.Text = "SALA CANCELADA\n\n" +
                    (string)estado["mensaje"] +
                    "\n\nEl creador ha cancelado esta partida.";
            }
            else
            {
                espera.Text =
                    "CÓDIGO DE SALA: " + (string)estado["codigo"] +
                    "\n\nJUGADORES EN LA SALA:\n" +
                    String.Join("\n", jugadores.Select(
                        (j, i) => (i + 1) + ". " + (string)j["nombre"]
                    )) +
                    (yo == 0
                        ? "\n\nEres el creador. Puedes iniciar o cancelar la sala."
                        : "\n\nEsperando a que el creador inicie la partida.");
            }

            if (esperando || cancelada) return;

            bool miTurno = fase == "jugando" && turno == yo && pendiente == -1;
            lblTurno.Text = fase == "terminada" ? "Ganó " + (string)jugadores[(int)estado["ganador"]]["nombre"] : "Turno de: " + (string)jugadores[turno]["nombre"];
            lblSentido.Text = (bool)estado["horario"] ? "Sentido: horario" : "Sentido: antihorario";
            lblMazo.Text = "Cartas en mazo: " + (int)estado["mazo"];
            lblColorActivo.Text = "Color activo: " + (string)estado["color"];
            lblColorActivo.BackColor = ColorUI((ColorCarta)Enum.Parse(typeof(ColorCarta), (string)estado["color"]));
            string avisoRevancha = fase == "terminada" && yo != 0
                ? "\nEsperando a que el creador decida si habrá otra partida."
                : "";
            lblAviso.Text = (string)estado["mensaje"] + (pendiente >= 0 ? " · Esperando ¡UNO! de " + (string)jugadores[pendiente]["nombre"] : "") + avisoRevancha + "\n" + conexion.Text;
            btnRobar.Enabled = conectado && miTurno && !ocupado;
            btnUno.Visible = pendiente == yo;
            btnUno.Enabled = conectado && !ocupado;
            btnUno.Text = "¡UNO!";
            flpMano.Enabled = conectado && !ocupado && fase == "jugando";
            int version = (int)estado["version"];
            if (versionDibujada == version) return;
            versionDibujada = version;
            lstJugadores.Items.Clear();
            foreach (var j in jugadores)
                lstJugadores.Items.Add((string)j["nombre"] + " (" + (int)j["cantidad"] + " cartas)");
            AplicarCarta(lblCartaSuperior, Convertir(estado["superior"]));
            while (flpMano.Controls.Count > 0) flpMano.Controls[0].Dispose();
            var mano = (JArray)estado["mano"];
            var jugables = new HashSet<int>(estado["jugables"].Select(x => (int)x));
            for (int i = 0; i < mano.Count; i++)
            {
                int indice = i;
                Carta c = Convertir(mano[i]);
                var b = new Button { Size = new Size(90, 130), Enabled = miTurno && jugables.Contains(i), Margin = new Padding(3, jugables.Contains(i) ? 3 : 25, 3, 3) };
                AplicarCarta(b, c);
                b.Click += async (s, e) => {
                    if (ocupado) return;
                    string color = null;
                    if (c.Tipo == TipoCarta.Comodin || c.Tipo == TipoCarta.MasCuatro)
                    {
                        var elegido = PedirColor();
                        if (elegido == ColorCarta.Ninguno) return;
                        color = elegido.ToString();
                    }
                    await Accion("jugar", indice, color, version);
                };
                flpMano.Controls.Add(b);
            }
        }

        private static Carta Convertir(JToken c)
        {
            return new Carta((ColorCarta)Enum.Parse(typeof(ColorCarta), (string)c["Color"]),
                (TipoCarta)Enum.Parse(typeof(TipoCarta), (string)c["Tipo"]), (int)c["Numero"]);
        }

        private async void btnRobar_Click(object sender, EventArgs e) { await Accion("robar"); }
        private void btnJugar_Click(object sender, EventArgs e) { }
        private void btnVerCartas_Click(object sender, EventArgs e) { }
        private void Form1_Load(object sender, EventArgs e) { }
        private void pnlMenu_Paint(object sender, PaintEventArgs e) { }
        private void lblCartaSuperior_Click(object sender, EventArgs e) { }

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

        private async Task AbrirEstadisticas()
        {
            if (ocupado || api != null || estado != null)
                return;

            visibilidadMenuAntes = controlesMenu.ToDictionary(
                c => c, c => c.Visible
            );

            foreach (Control c in controlesMenu)
                c.Visible = false;

            tituloEstadisticas.Visible = true;
            listaEstadisticas.Visible = true;
            btnCerrarEstadisticas.Visible = true;

            listaEstadisticas.Items.Clear();
            listaEstadisticas.Items.Add("Cargando estadísticas...");

            try
            {
                using (var consulta = new ApiService(servidor.Text))
                {
                    JArray resultados = await consulta.Estadisticas();

                    listaEstadisticas.Items.Clear();

                    if (resultados.Count == 0)
                    {
                        listaEstadisticas.Items.Add(
                            "Todavía no hay partidas ganadas."
                        );
                    }
                    else
                    {
                        for (int i = 0; i < resultados.Count; i++)
                        {
                            string jugador = (string)resultados[i]["nombre"];
                            int victorias = (int)resultados[i]["victorias"];

                            listaEstadisticas.Items.Add(
                                $"{i + 1}. {jugador} - {victorias} victorias"
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                listaEstadisticas.Items.Clear();
                listaEstadisticas.Items.Add(
                    "Error al consultar: " + ex.Message
                );
            }
        }

        private void CerrarEstadisticas()
        {
            tituloEstadisticas.Visible = false;
            listaEstadisticas.Visible = false;
            btnCerrarEstadisticas.Visible = false;

            if (visibilidadMenuAntes != null)
            {
                foreach (var elemento in visibilidadMenuAntes)
                    elemento.Key.Visible = elemento.Value;

                visibilidadMenuAntes = null;
            }
        }


    }
}
