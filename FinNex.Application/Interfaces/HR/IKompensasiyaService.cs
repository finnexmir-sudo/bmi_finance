using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Kompensasiya;
using FinNex.Domain.Entities.HR;

namespace FinNex.Application.Interfaces.HR
{
    /// <summary>
    /// İstifadə edilməmiş əmək məzuniyyəti günlərinə görə kompensasiya
    /// hesablama servisi. Adi məzuniyyət ödənişi düsturu ilə eyni
    /// gündəlik dərəcəni istifadə edir, sadəcə günlərin sayını fərqli
    /// hesablayır (keçmiş illərin qalığı + cari il prorate).
    /// </summary>
    public interface IKompensasiyaService
    {
        /// <summary>
        /// Anlıq hesablama — yadda saxlamır, yalnız preview nəticə qaytarır.
        /// </summary>
        Task<Result<KompensasiyaHesablamaNeticesiDto>> HesablaAsync(
            int isciId, DateTime ayrilmaTarixi);

        /// <summary>
        /// Hesablamanı yadda saxla (Layihe statusu ilə).
        /// </summary>
        Task<Result<int>> YaratAsync(KompensasiyaYaratDto dto, int hesablayanIsciId);

        /// <summary>
        /// Bütün kompensasiya qeydlərini gətir (tarixçə).
        /// </summary>
        Task<Result<IList<KompensasiyaListDto>>> HamisiniGetirAsync();

        /// <summary>
        /// Tək qeyd detalı.
        /// </summary>
        Task<Result<KompensasiyaDetalDto?>> IdIleGetirAsync(int id);

        /// <summary>
        /// Ləğv et (status → LegvEdildi).
        /// </summary>
        Task<Result> LegvEtAsync(int id);

        /// <summary>
        /// Maaş engine üçün: bu işçi üçün bu il/ay-da aktiv
        /// (Layihe və ya Tesdiqlenib) kompensasiya varsa qaytarır.
        /// </summary>
        Task<MezuniyyetKompensasiyasi?> GetAktivKompensasiyaAsync(int isciId, int il, int ay);

        /// <summary>
        /// Maaş engine kompensasiyanı maaşa daxil etdikdə statusu yenilə.
        /// </summary>
        Task IsareLamasiniYadda(int kompensasiyaId, int maasId);
    }
}
