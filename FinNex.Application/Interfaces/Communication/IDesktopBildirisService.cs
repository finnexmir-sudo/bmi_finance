namespace FinNex.Application.Interfaces.Communication
{
    /// <summary>
    /// İşçinin masaüstü proqramına anlıq (real-time) bildiriş göndərir.
    /// Yalnız push üçündür — verilənlər bazasına yazmır (bu, IBildirisService-in məsuliyyətidir).
    /// Xəta baş verərsə səssizcə uğursuz olur: bildiriş çatdırılmaması əsas əməliyyatı dayandırmamalıdır.
    /// </summary>
    public interface IDesktopBildirisService
    {
        Task PushAsync(int isciId, string bashliq, string metn);
    }
}
