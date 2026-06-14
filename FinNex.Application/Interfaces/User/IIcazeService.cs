using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Icaze;

namespace FinNex.Application.Interfaces
{
    public interface IIcazeService
    {
        Task<Result<IList<IcazeIsciIstatistikDto>>> GetIsciIzlemeAsync(IcazeIzlemeFiltrDto filtr);
        Task<Result<IList<IcazeListDto>>> GetIsciIcazeleriAsync(int isciId);
        Task<Result<IcazeListDto>> YaratAsync(IcazeCreateDto dto);
        Task<Result> LegvEtAsync(int icazeId, int isciId);
        // Rəhbər/HR — təsdiqlənmiş icazəni ləğv edir (sahibə bağlı deyil, səbəb məcburi,
        // keçmiş deyil). Jeton istifadə olunubsa geri qaytarır.
        Task<Result> RehberHrLegvEtAsync(int icazeId, int legvEdenIsciId, string sebeb);
        Task<Result<IcazeDetailDto>> GetDetayAsync(int icazeId);

        // Təsdiq paneli üçün
        Task<Result<IList<IcazeListDto>>> GetAllAsync();
        Task<Result<IList<IcazeListDto>>> GetGozlemededeAsync();
        Task<Result<IList<IcazeListDto>>> GetSobeyeGoreIcazelerAsync(int departamentId, int sobeReisiIsciId);
        Task<Result<IList<IcazeListDto>>> GetRehberTesdiqindeAsync();
        Task<Result<IList<IcazeListDto>>> GetHrTesdiqindeAsync();
        Task<Result> SobeReisiTesdiqAsync(int id, bool status, string? qeyd, int sobeReisiId = 0);
        // jetonOdenenSaat: rəhbər icazənin neçə saatını jetonla ödənmiş kimi
        // qeydə alır. 0 = tam adi icazə. HR final təsdiqdə həmin saat qədər
        // jeton işçidən FIFO ilə xərclənir.
        Task<Result> RehberTesdiqAsync(int id, bool status, string? qeyd, int rehberId = 0, decimal jetonOdenenSaat = 0, bool naharNezereAlinmasin = false, bool birdefelik = false);
        Task<Result> HrTesdiqAsync(int id, bool status, string? qeyd, int hrId = 0, bool birdefelik = false);
        Task<Result<IList<IcazeListDto>>> GetFiltrliAsync(DateTime? tarixFrom, DateTime? tarixTo, int? departamentId, int? status, string? axtaris);

        // Cihaz çıxış/qayıdış dövriyyəsi
        Task<Result<IList<IcazeDovriyyeDto>>> GetDovriyyeAsync(DateTime? tarixFrom, DateTime? tarixTo, int? departamentId, string? axtaris);

        // HR əl ilə düzəliş — cihaz çıxış/qayıdış vaxtlarını yeniləyir (insan faktoru halları)
        Task<Result> CixisGirisDuzeltAsync(int icazeId, DateTime? cixisVaxt, DateTime? qayidisVaxt);
    }
}