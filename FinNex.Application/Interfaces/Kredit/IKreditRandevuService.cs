using FinNex.Domain.Entities.Kredit;

namespace FinNex.Application.Interfaces.Kredit
{
    /// <summary>
    /// Təsdiqlənmiş kredit müraciətləri üçün müştərinin banka gəlmə randevusu.
    /// Calendar mantığı — gün seçilə, o günə təyin olunanlar görünə bilər.
    /// </summary>
    public interface IKreditRandevuService
    {
        Task<IList<KreditRandevu>> GunUzreGetirAsync(DateTime gun);
        Task<IList<KreditRandevu>> AraliqUzreGetirAsync(DateTime baslangic, DateTime son);
        Task<KreditRandevu?> MuracietUzreGetirAsync(int muracietId);
        Task<KreditRandevu> TeyinEtAsync(int muracietId, DateTime tarix, int muddetDeqiqe,
                                          string? qeyd, int teyinEdenIsciId);
        Task DurumDeyisAsync(int randevuId, KreditRandevuStatus yeniDurum, int? yenileyenIcraciId);
        Task LegvEtAsync(int randevuId, int? legvEdenIcraciId);
    }
}
