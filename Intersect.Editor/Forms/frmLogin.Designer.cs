using System.ComponentModel;
using System.Windows.Forms;
using DarkUI.Controls;
using Timer = System.Windows.Forms.Timer;

namespace Intersect.Editor.Forms
{
    partial class FrmLogin
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

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
            components = new Container();
            var resources = new ComponentResourceManager(typeof(FrmLogin));
            lblStatus = new Label();
            tmrSocket = new Timer(components);
            txtUsername = new DarkTextBox();
            txtPassword = new DarkTextBox();
            btnLogin = new DarkButton();
            picLogo = new PictureBox();
            lblVersion = new Label();
            lblUsername = new Label();
            lblPassword = new Label();
            chkRemember = new DarkCheckBox();
            ((ISupportInitialize)picLogo).BeginInit();
            SuspendLayout();
            // 
            // lblStatus
            // 
            lblStatus.Dock = DockStyle.Bottom;
            lblStatus.ForeColor = System.Drawing.Color.White;
            lblStatus.Location = new System.Drawing.Point(0, 363);
            lblStatus.Margin = new Padding(4, 0, 4, 0);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(513, 27);
            lblStatus.TabIndex = 0;
            lblStatus.Text = "Connecting to server, please wait...";
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // tmrSocket
            // 
            tmrSocket.Enabled = true;
            tmrSocket.Tick += tmrSocket_Tick;
            // 
            // txtUsername
            // 
            txtUsername.BackColor = System.Drawing.Color.FromArgb(69, 73, 74);
            txtUsername.BorderStyle = BorderStyle.FixedSingle;
            txtUsername.ForeColor = System.Drawing.Color.FromArgb(220, 220, 220);
            txtUsername.Location = new System.Drawing.Point(17, 265);
            txtUsername.Margin = new Padding(4, 3, 4, 3);
            txtUsername.Name = "txtUsername";
            txtUsername.Size = new Size(148, 23);
            txtUsername.TabIndex = 1;
            txtUsername.KeyDown += txtUsername_KeyDown;
            // 
            // txtPassword
            // 
            txtPassword.BackColor = System.Drawing.Color.FromArgb(69, 73, 74);
            txtPassword.BorderStyle = BorderStyle.FixedSingle;
            txtPassword.ForeColor = System.Drawing.Color.FromArgb(220, 220, 220);
            txtPassword.Location = new System.Drawing.Point(17, 312);
            txtPassword.Margin = new Padding(4, 3, 4, 3);
            txtPassword.Name = "txtPassword";
            txtPassword.PasswordChar = '*';
            txtPassword.Size = new Size(148, 23);
            txtPassword.TabIndex = 2;
            txtPassword.KeyDown += txtPassword_KeyDown;
            // 
            // btnLogin
            // 
            btnLogin.Location = new System.Drawing.Point(412, 333);
            btnLogin.Margin = new Padding(4, 3, 4, 3);
            btnLogin.Name = "btnLogin";
            btnLogin.Padding = new Padding(6);
            btnLogin.Size = new Size(88, 27);
            btnLogin.TabIndex = 4;
            btnLogin.Text = "Acessar";
            btnLogin.Click += btnLogin_Click;
            // 
            // picLogo
            // 
            picLogo.BackColor = System.Drawing.Color.Transparent;
            picLogo.BackgroundImage = (Image)resources.GetObject("picLogo.BackgroundImage");
            picLogo.BackgroundImageLayout = ImageLayout.Stretch;
            picLogo.Location = new System.Drawing.Point(0, -1);
            picLogo.Margin = new Padding(4, 3, 4, 3);
            picLogo.Name = "picLogo";
            picLogo.Size = new Size(513, 238);
            picLogo.TabIndex = 4;
            picLogo.TabStop = false;
            // 
            // lblVersion
            // 
            lblVersion.BackColor = System.Drawing.Color.Transparent;
            lblVersion.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold, GraphicsUnit.Point);
            lblVersion.ForeColor = System.Drawing.Color.Gainsboro;
            lblVersion.Location = new System.Drawing.Point(213, -1);
            lblVersion.Margin = new Padding(4, 0, 4, 0);
            lblVersion.Name = "lblVersion";
            lblVersion.Size = new Size(300, 23);
            lblVersion.TabIndex = 5;
            lblVersion.Text = "Editor v1.0.0.0";
            lblVersion.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblUsername
            // 
            lblUsername.AutoSize = true;
            lblUsername.ForeColor = System.Drawing.Color.White;
            lblUsername.Location = new System.Drawing.Point(17, 247);
            lblUsername.Margin = new Padding(4, 0, 4, 0);
            lblUsername.Name = "lblUsername";
            lblUsername.Size = new Size(50, 15);
            lblUsername.TabIndex = 6;
            lblUsername.Text = "Usuário:";
            // 
            // lblPassword
            // 
            lblPassword.AutoSize = true;
            lblPassword.ForeColor = System.Drawing.Color.White;
            lblPassword.Location = new System.Drawing.Point(17, 294);
            lblPassword.Margin = new Padding(4, 0, 4, 0);
            lblPassword.Name = "lblPassword";
            lblPassword.Size = new Size(42, 15);
            lblPassword.TabIndex = 7;
            lblPassword.Text = "Senha:";
            // 
            // chkRemember
            // 
            chkRemember.AutoSize = true;
            chkRemember.ForeColor = System.Drawing.Color.White;
            chkRemember.Location = new System.Drawing.Point(17, 341);
            chkRemember.Margin = new Padding(4, 3, 4, 3);
            chkRemember.Name = "chkRemember";
            chkRemember.Size = new Size(92, 19);
            chkRemember.TabIndex = 3;
            chkRemember.Text = "Lembrar-me";
            // 
            // FrmLogin
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
            BackgroundImage = (Image)resources.GetObject("$this.BackgroundImage");
            ClientSize = new Size(513, 390);
            Controls.Add(chkRemember);
            Controls.Add(lblPassword);
            Controls.Add(lblUsername);
            Controls.Add(lblVersion);
            Controls.Add(picLogo);
            Controls.Add(btnLogin);
            Controls.Add(txtPassword);
            Controls.Add(txtUsername);
            Controls.Add(lblStatus);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            KeyPreview = true;
            Margin = new Padding(4, 3, 4, 3);
            MaximizeBox = false;
            Name = "FrmLogin";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Rebornia - Editor Login";
            Load += frmLogin_Load;
            KeyDown += FrmLogin_KeyDown;
            ((ISupportInitialize)picLogo).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Timer tmrSocket;
        private DarkTextBox txtUsername;
        private DarkTextBox txtPassword;
        private DarkButton btnLogin;
        private PictureBox picLogo;
        private Label lblVersion;
        private Label lblUsername;
        private Label lblPassword;
        private DarkCheckBox chkRemember;
        public Label lblStatus;
    }
}