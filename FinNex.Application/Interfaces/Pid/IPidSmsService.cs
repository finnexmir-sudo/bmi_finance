using FinNex.Domain.Entities.Pid;

namespace FinNex.Application.Interfaces.Pid;

public interface IPidSmsService
{
    /// <summary>
    /// Telefon nömrəsini Göyərçin tələbinə uyğun formata gətirir: yalnız rəqəm,
    /// ölkə kodu ilə (994... formatında).
    /// </summary>
    string TelefonNormallasdir(string raw);

    /// <summary>
    /// SMS göndərir və log yazır. Göyərçin uğursuz olsa belə log yaradılır (status = Xeta).
    /// </summary>
    Task<PidSmsLog> GonderAsync(string telefon, string metn, int? sablonId, int? gonderenIsciId);

    Task<IList<PidSmsLog>> SonGonderilenler(int say = 100);

    Task<(int Ugur, int Xeta)> TopluGonderAsync(
        IEnumerable<(string Ad, string Telefon)> alicilar,
        string smsMetni,
        int? gonderenIsciId);

    Task<PidSmsKpiDto> GetKpiAsync();
}

public class PidSmsKpiDto
{
    public int BuGun { get; set; }
    public int BuAy { get; set; }
    public int UgurluSon30 { get; set; }
    public int XetaSon30 { get; set; }
}
