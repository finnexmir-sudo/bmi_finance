using FinNex.DesktopAgent;

// Yalnız bir nüsxənin işləməsini təmin edir
using var mutex = new System.Threading.Mutex(true, "FinNexDesktopAgent_SingleInstance", out bool isNew);
if (!isNew)
{
    MessageBox.Show(
        "FinNex Bildiriş Agenti artıq işləyir.",
        "FinNex Agent",
        MessageBoxButtons.OK,
        MessageBoxIcon.Information);
    return;
}

Application.EnableVisualStyles();
Application.SetCompatibleTextRenderingDefault(false);
Application.SetHighDpiMode(HighDpiMode.SystemAware);

var config = AppConfig.Load();

// Giriş forması göstərilir; istifadəçi JWT alır
using var loginForm = new LoginForm(config);
if (loginForm.ShowDialog() != DialogResult.OK || string.IsNullOrEmpty(loginForm.Token))
    return; // Giriş ləğv edildi və ya xəta baş verdi

// TrayAgent arxa fonda işləyir — forma yoxdur, yalnız system tray
var agent = new TrayAgent(config, loginForm.Token, loginForm.Ad);
Application.Run(agent);
