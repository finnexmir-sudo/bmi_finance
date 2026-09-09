using FinNex.Application.DTOs.HR.Vesiqe;

namespace FinNex.Application.Services.HR
{
    /// <summary>
    /// Şəxsiyyət vəsiqəsinin etibarlılıq tarixi — BMI-dən (Oracle) CANLI oxunur.
    ///
    /// ⚠️ HEÇ NƏ YAZILMIR (istifadəçi qərarı 09.09.2026): nə Oracle-a (onsuz da
    /// qadağandır), nə də öz bazamıza. `Isci`-də vəsiqə tarixi sütunu YOXDUR.
    /// Səhifə hər açılanda rəqəm mənbədən gəlir — köhnəlmiş nüsxə riski yoxdur.
    /// </summary>
    public interface IVesiqeService
    {
        /// <summary>
        /// Aktiv işçiləri BMI-dəki vəsiqə tarixləri ilə birləşdirib qaytarır.
        /// BMI əlçatmazdırsa `Ugurlu = false` və `Xeta` dolur — siyahı boş qalır.
        /// </summary>
        Task<VesiqeNeticesi> SiyahiAsync(CancellationToken ct = default);
    }
}
