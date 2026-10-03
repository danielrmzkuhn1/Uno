namespace UNO
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.lblTurno = new System.Windows.Forms.Label();
            this.lblSentido = new System.Windows.Forms.Label();
            this.lblMazo = new System.Windows.Forms.Label();
            this.lblCartaSuperior = new System.Windows.Forms.Label();
            this.lblColorActivo = new System.Windows.Forms.Label();
            this.lblAviso = new System.Windows.Forms.Label();
            this.lstJugadores = new System.Windows.Forms.ListBox();
            this.flpMano = new System.Windows.Forms.FlowLayoutPanel();
            this.btnRobar = new System.Windows.Forms.Button();
            this.btnVerCartas = new System.Windows.Forms.Button();
            this.pnlMenu = new System.Windows.Forms.Panel();
            this.txtJugador4 = new System.Windows.Forms.TextBox();
            this.txtJugador2 = new System.Windows.Forms.TextBox();
            this.txtJugador3 = new System.Windows.Forms.TextBox();
            this.txtJugador1 = new System.Windows.Forms.TextBox();
            this.btnJugar = new System.Windows.Forms.Button();
            this.pnlMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblTurno
            // 
            this.lblTurno.AutoSize = true;
            this.lblTurno.ForeColor = System.Drawing.SystemColors.ButtonFace;
            this.lblTurno.Location = new System.Drawing.Point(53, 85);
            this.lblTurno.Name = "lblTurno";
            this.lblTurno.Size = new System.Drawing.Size(64, 16);
            this.lblTurno.TabIndex = 0;
            this.lblTurno.Text = "Turno de:";
            // 
            // lblSentido
            // 
            this.lblSentido.AutoSize = true;
            this.lblSentido.ForeColor = System.Drawing.SystemColors.ButtonFace;
            this.lblSentido.Location = new System.Drawing.Point(53, 126);
            this.lblSentido.Name = "lblSentido";
            this.lblSentido.Size = new System.Drawing.Size(56, 16);
            this.lblSentido.TabIndex = 1;
            this.lblSentido.Text = "Sentido:";
            // 
            // lblMazo
            // 
            this.lblMazo.AutoSize = true;
            this.lblMazo.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.lblMazo.Location = new System.Drawing.Point(540, 56);
            this.lblMazo.Name = "lblMazo";
            this.lblMazo.Size = new System.Drawing.Size(117, 16);
            this.lblMazo.TabIndex = 2;
            this.lblMazo.Text = "Cartas en el Mazo:";
            // 
            // lblCartaSuperior
            // 
            this.lblCartaSuperior.ForeColor = System.Drawing.SystemColors.ButtonFace;
            this.lblCartaSuperior.Location = new System.Drawing.Point(576, 142);
            this.lblCartaSuperior.Name = "lblCartaSuperior";
            this.lblCartaSuperior.Size = new System.Drawing.Size(144, 190);
            this.lblCartaSuperior.TabIndex = 3;
            this.lblCartaSuperior.Text = "label1";
            this.lblCartaSuperior.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblColorActivo
            // 
            this.lblColorActivo.AutoSize = true;
            this.lblColorActivo.ForeColor = System.Drawing.SystemColors.Control;
            this.lblColorActivo.Location = new System.Drawing.Point(747, 56);
            this.lblColorActivo.Name = "lblColorActivo";
            this.lblColorActivo.Size = new System.Drawing.Size(82, 16);
            this.lblColorActivo.TabIndex = 4;
            this.lblColorActivo.Text = "Color Activo:";
            // 
            // lblAviso
            // 
            this.lblAviso.AutoSize = true;
            this.lblAviso.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.lblAviso.Location = new System.Drawing.Point(979, 56);
            this.lblAviso.Name = "lblAviso";
            this.lblAviso.Size = new System.Drawing.Size(44, 16);
            this.lblAviso.TabIndex = 5;
            this.lblAviso.Text = "label1";
            // 
            // lstJugadores
            // 
            this.lstJugadores.FormattingEnabled = true;
            this.lstJugadores.ItemHeight = 16;
            this.lstJugadores.Location = new System.Drawing.Point(1187, 17);
            this.lstJugadores.Name = "lstJugadores";
            this.lstJugadores.Size = new System.Drawing.Size(120, 84);
            this.lstJugadores.TabIndex = 6;
            // 
            // flpMano
            // 
            this.flpMano.AutoScroll = true;
            this.flpMano.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.flpMano.Location = new System.Drawing.Point(0, 478);
            this.flpMano.Name = "flpMano";
            this.flpMano.Size = new System.Drawing.Size(1335, 224);
            this.flpMano.TabIndex = 7;
            // 
            // btnRobar
            // 
            this.btnRobar.Location = new System.Drawing.Point(818, 289);
            this.btnRobar.Name = "btnRobar";
            this.btnRobar.Size = new System.Drawing.Size(126, 43);
            this.btnRobar.TabIndex = 8;
            this.btnRobar.Text = "Robar Carta";
            this.btnRobar.UseVisualStyleBackColor = true;
            this.btnRobar.Click += new System.EventHandler(this.btnRobar_Click);
            // 
            // btnVerCartas
            // 
            this.btnVerCartas.Location = new System.Drawing.Point(818, 338);
            this.btnVerCartas.Name = "btnVerCartas";
            this.btnVerCartas.Size = new System.Drawing.Size(126, 43);
            this.btnVerCartas.TabIndex = 9;
            this.btnVerCartas.Text = "Ver mis Cartas";
            this.btnVerCartas.UseVisualStyleBackColor = true;
            this.btnVerCartas.Click += new System.EventHandler(this.btnVerCartas_Click);
            // 
            // pnlMenu
            // 
            this.pnlMenu.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("pnlMenu.BackgroundImage")));
            this.pnlMenu.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.pnlMenu.Controls.Add(this.txtJugador4);
            this.pnlMenu.Controls.Add(this.txtJugador2);
            this.pnlMenu.Controls.Add(this.txtJugador3);
            this.pnlMenu.Controls.Add(this.txtJugador1);
            this.pnlMenu.Controls.Add(this.btnJugar);
            this.pnlMenu.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlMenu.Location = new System.Drawing.Point(0, 0);
            this.pnlMenu.Name = "pnlMenu";
            this.pnlMenu.Size = new System.Drawing.Size(1329, 694);
            this.pnlMenu.TabIndex = 10;
            // 
            // txtJugador4
            // 
            this.txtJugador4.Location = new System.Drawing.Point(963, 619);
            this.txtJugador4.Name = "txtJugador4";
            this.txtJugador4.Size = new System.Drawing.Size(213, 22);
            this.txtJugador4.TabIndex = 4;
            // 
            // txtJugador2
            // 
            this.txtJugador2.Location = new System.Drawing.Point(418, 619);
            this.txtJugador2.Name = "txtJugador2";
            this.txtJugador2.Size = new System.Drawing.Size(213, 22);
            this.txtJugador2.TabIndex = 3;
            // 
            // txtJugador3
            // 
            this.txtJugador3.Location = new System.Drawing.Point(688, 619);
            this.txtJugador3.Name = "txtJugador3";
            this.txtJugador3.Size = new System.Drawing.Size(213, 22);
            this.txtJugador3.TabIndex = 2;
            // 
            // txtJugador1
            // 
            this.txtJugador1.Location = new System.Drawing.Point(149, 619);
            this.txtJugador1.Name = "txtJugador1";
            this.txtJugador1.Size = new System.Drawing.Size(213, 22);
            this.txtJugador1.TabIndex = 1;
            // 
            // btnJugar
            // 
            this.btnJugar.Location = new System.Drawing.Point(543, 478);
            this.btnJugar.Name = "btnJugar";
            this.btnJugar.Size = new System.Drawing.Size(204, 63);
            this.btnJugar.TabIndex = 0;
            this.btnJugar.Text = "JUGAR";
            this.btnJugar.UseVisualStyleBackColor = true;
            this.btnJugar.Click += new System.EventHandler(this.btnJugar_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.WindowFrame;
            this.ClientSize = new System.Drawing.Size(1329, 694);
            this.Controls.Add(this.pnlMenu);
            this.Controls.Add(this.btnVerCartas);
            this.Controls.Add(this.btnRobar);
            this.Controls.Add(this.flpMano);
            this.Controls.Add(this.lstJugadores);
            this.Controls.Add(this.lblAviso);
            this.Controls.Add(this.lblColorActivo);
            this.Controls.Add(this.lblCartaSuperior);
            this.Controls.Add(this.lblMazo);
            this.Controls.Add(this.lblSentido);
            this.Controls.Add(this.lblTurno);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.pnlMenu.ResumeLayout(false);
            this.pnlMenu.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblTurno;
        private System.Windows.Forms.Label lblSentido;
        private System.Windows.Forms.Label lblMazo;
        private System.Windows.Forms.Label lblCartaSuperior;
        private System.Windows.Forms.Label lblColorActivo;
        private System.Windows.Forms.Label lblAviso;
        private System.Windows.Forms.ListBox lstJugadores;
        private System.Windows.Forms.FlowLayoutPanel flpMano;
        private System.Windows.Forms.Button btnRobar;
        private System.Windows.Forms.Button btnVerCartas;
        private System.Windows.Forms.Panel pnlMenu;
        private System.Windows.Forms.TextBox txtJugador4;
        private System.Windows.Forms.TextBox txtJugador2;
        private System.Windows.Forms.TextBox txtJugador3;
        private System.Windows.Forms.TextBox txtJugador1;
        private System.Windows.Forms.Button btnJugar;
    }
}

