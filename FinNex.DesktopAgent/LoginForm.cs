using System.Net.Http.Json;
using System.Text.Json;

namespace FinNex.DesktopAgent;

/// <summary>
/// İşçi öz FinNex istifadəçi adı və şifrəsini daxil edib JWT alır.
/// Uğurlu giriş nəticəsini Token və IsciId xüsusiyyətlərinde saxlayır.
/// </summary>
public partial class LoginForm : Form
{
    private readonly AppConfig _config;
    private readonly HttpClient _http;

    public string? Token { get; private set; }
    public int IsciId { get; private set; }
    public string Ad { get; private set; } = "";

    public LoginForm(AppConfig config)
    {
        _config = config;
        // İnkişaf mühitlərindəki self-signed sertifikatları qəbul et
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
        _http = new HttpClient(handler);
        InitializeComponent();
    }

    private async void btnGiris_Click(object sender, EventArgs e)
    {
        btnGiris.Enabled = false;
        lblXeta.Text = "";

        var username = txtIstifadeciAdi.Text.Trim();
        var password = txtSifre.Text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            lblXeta.Text = "İstifadəçi adı və şifrə tələb olunur.";
            btnGiris.Enabled = true;
            return;
        }

        try
        {
            var response = await _http.PostAsJsonAsync(
                _config.FullTokenUrl,
                new { username, password });

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                Token = root.GetProperty("token").GetString();
                IsciId = root.GetProperty("isciId").GetInt32();
                Ad = root.GetProperty("ad").GetString() ?? "";

                DialogResult = DialogResult.OK;
                Close();
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                lblXeta.Text = "İstifadəçi adı və ya şifrə yanlışdır.";
            }
            else if ((int)response.StatusCode == 429)
            {
                lblXeta.Text = "Hesab müvəqqəti bloklanıb. Bir az sonra yenidən cəhd edin.";
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                lblXeta.Text = "Bu hesab masaüstü agentə qoşula bilmir (işçiyə bağlı deyil).";
            }
            else
            {
                lblXeta.Text = $"Server xətası: {response.StatusCode}";
            }
        }
        catch (HttpRequestException ex)
        {
            lblXeta.Text = $"Serverə qoşulmaq mümkün olmadı: {ex.Message}";
        }
        catch (Exception ex)
        {
            lblXeta.Text = $"Gözlənilməz xəta: {ex.Message}";
        }
        finally
        {
            btnGiris.Enabled = true;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _http.Dispose();
            components?.Dispose();
        }
        base.Dispose(disposing);
    }
}
