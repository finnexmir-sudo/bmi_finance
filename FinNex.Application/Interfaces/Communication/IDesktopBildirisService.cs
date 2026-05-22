using FinNex.Domain.Entities.Communication;

namespace FinNex.Application.Interfaces.Communication
{
    /// <summary>
    /// İşçinin masaüstü proqramına anlıq (real-time) bildiriş göndərir.
    /// Yalnız push üçündür — verilənlər bazına yazmır (bu, IBildirisService-in məsuliyyətidir).
    /// Xəta baş verərsə səssizcə uğursuzu olur: bildiriş çatdırılmaması əsas əməliyyatı dayandırmamalıdır.
    /// </summary>
    public interface IDesktopBildirisService
    {
        Task PushAsync(int isciId, string bashliq, string metn, string? url = null, BildirisNovu nov = default);
    }
}
