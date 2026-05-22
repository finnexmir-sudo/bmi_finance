namespace FinNex.DesktopAgent;

partial class LoginForm
{
    private System.ComponentModel.IContainer? components = null;

    private Label lblBashliq;
    private Label lblIstifadeciAdi;
    private TextBox txtIstifadeciAdi;
    private Label lblSifre;
    private TextBox txtSifre;
    private Button btnGiris;
    private Label lblXeta;

    private void InitializeComponent()
    {
        lblBashliq = new Label();
        lblIstifadeciAdi = new Label();
        txtIstifadeciAdi = new TextBox();
        lblSifre = new Label();
        txtSifre = new TextBox();
        btnGiris = new Button();
        lblXeta = new Label();
        SuspendLayout();

        // lblBashliq
        lblBashliq.AutoSize = true;
        lblBashliq.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
        lblBashliq.Location = new Point(20, 20);
        lblBashliq.Size = new Size(260, 25);
        lblBashliq.Text = "FinNex — Masaüstü Bildiriş Agenti";

        // lblIstifadeciAdi
        lblIstifadeciAdi.AutoSize = true;
        lblIstifadeciAdi.Location = new Point(20, 65);
        lblIstifadeciAdi.Text = "İstifadəçi adı:";

        // txtIstifadeciAdi
        txtIstifadeciAdi.Location = new Point(20, 85);
        txtIstifadeciAdi.Size = new Size(300, 23);
        txtIstifadeciAdi.TabIndex = 0;

        // lblSifre
        lblSifre.AutoSize = true;
        lblSifre.Location = new Point(20, 120);
        lblSifre.Text = "Şifrə:";

        // txtSifre
        txtSifre.Location = new Point(20, 140);
        txtSifre.PasswordChar = '●';
        txtSifre.Size = new Size(300, 23);
        txtSifre.TabIndex = 1;
        // Enter düyməsi Giriş düyməsini tetiklesin
        txtSifre.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) btnGiris.PerformClick();
        };

        // btnGiris
        btnGiris.Location = new Point(20, 180);
        btnGiris.Size = new Size(300, 34);
        btnGiris.TabIndex = 2;
        btnGiris.Text = "Giriş";
        btnGiris.UseVisualStyleBackColor = true;
        btnGiris.Click += btnGiris_Click;

        // lblXeta
        lblXeta.AutoSize = false;
        lblXeta.ForeColor = Color.Red;
        lblXeta.Location = new Point(20, 225);
        lblXeta.Size = new Size(300, 40);
        lblXeta.Text = "";

        // LoginForm
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(342, 280);
        Controls.Add(lblBashliq);
        Controls.Add(lblIstifadeciAdi);
        Controls.Add(txtIstifadeciAdi);
        Controls.Add(lblSifre);
        Controls.Add(txtSifre);
        Controls.Add(btnGiris);
        Controls.Add(lblXeta);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "LoginForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "FinNex — Giriş";
        ResumeLayout(false);
        PerformLayout();
    }
}
