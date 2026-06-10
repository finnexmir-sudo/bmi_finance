using FinNex.Application.DTOs.HR.Ezamiyyet;
using FinNex.Domain.Entities.HR;

namespace FinNex.Application.Services.HR
{
    public interface IEzamiyyetService
    {
        Task<IList<EzamiyyetMuracietListDto>> IsciMuracietleriAsync(int isciId);
        Task<IList<EzamiyyetMuracietListDto>> HamisiniGetirAsync(EzamiyyetFiltrDto? filtr = null);
        Task<IList<EzamiyyetMuracietListDto>> GozleyenlerAsync();
        Task<EzamiyyetMuracietListDto?> DetayAsync(int id);

        Task<(bool ok, string? error, int id)> YaratAsync(
            int isciId,
            EzamiyyetMuracietCreateDto dto,
            string dmsRoot);

        Task<(bool ok, string? error)> RehberTesdiqAsync(
            int id, bool tesdiq, string? qeyd, int rehberId);

        Task<(bool ok, string? error)> LegvEtAsync(int id, int isciId);
        Task<(bool ok, string? error)> GeriQeydElavEtAsync(int id, int isciId, string? qeyd);

        // HR əl ilə cihaz çıxış/qayıdış düzəlişi (insan faktoru — qayıtmayıb qeydləri)
        Task<(bool ok, string? error)> CihazQayidisDuzeltAsync(
            int id, DateTime? cixisVaxt, DateTime? qayidisVaxt);

        Task<IList<EzamiyyetMekanListDto>> MekanlarAsync();
        Task<EzamiyyetMekan?> YeniMekanYaratAsync(string ad);

        // ADMS inteqrasiyası üçün
        Task<EzamiyyetMuraciet?> BugunAktivEzamiyyetAsync(int isciId, DateTime tarix);

        Task<IList<EzamiyyetStatistikDto>> StatistikaAsync(EzamiyyetFiltrDto filtr);
    }
}
