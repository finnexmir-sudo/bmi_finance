using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Kassa;

namespace FinNex.Application.Interfaces.Kassa
{
    /// <summary>
    /// Kassanın günlük valyuta alış-satış məzənnəsi — BMI "frmExchange"
    /// modulunun FinNex qarşılığı (09.10.2026). Oracle-a yazılmır, tamamilə
    /// öz bazamızdadır.
    /// </summary>
    public interface IKassaKursService
    {
        /// <summary>Verilən günün beyannaməsini (varsa) sətirləri ilə qaytarır.
        /// Yoxdursa boş template (5 valyuta, Status=null) qaytarır.</summary>
        Task<KassaKursGunlukDto> GunlukGetirAsync(DateTime tarix);

        /// <summary>Konkret beyannaməni Id ilə gətirir (Word sənədi generasiyası üçün —
        /// gün ərzində bir neçə beyannamə ola bilər, "Son qeydlər"dəki hər sətir öz
        /// Id-sini daşıyır). Tapılmasa/silinibsə `null` qaytarır.</summary>
        Task<KassaKursGunlukDto?> BeyannameGetirAsync(int beyannameId);

        /// <summary>Kassir məzənnəni yazır/yenidən göndərir. Mövcud beyannamə
        /// "Gözləyir" və ya "Təsdiqləndi" statusundadırsa BLOKLANIR (yeni
        /// qeyd yazılmır) — BMI-dəki kimi səssiz no-op YOXDUR, Result.Fail
        /// aydın səbəblə qayıdır.</summary>
        Task<Result> SaxlaAsync(KassaKursSaxlaDto dto, int icraciIsciId);

        /// <summary>Son N günün bütün sətirləri (cədvəl üçün, hər valyuta ayrı sətir).</summary>
        Task<IList<KassaKursSiyahiDto>> SonBeyannameleriGetirAsync(int gunSayi = 60);

        /// <summary>Təsdiq gözləyən beyannamələr (ən son öndə).</summary>
        Task<IList<KassaKursBeyannameOzetDto>> GozleyenleriGetirAsync();

        /// <summary>Son N təsdiqlənmiş/imtina olunmuş beyannamə (təsdiq ekranının tarixçə hissəsi).</summary>
        Task<IList<KassaKursBeyannameOzetDto>> SonNeticelenmisleriGetirAsync(int say = 20);

        /// <summary>Yalnız "Gözləyir" statusundakı konkret beyannaməni təsdiqləyir.</summary>
        Task<Result> TesdiqEtAsync(int beyannameId, int tesdiqEdenIsciId);

        /// <summary>Yalnız "Gözləyir" statusundakı konkret beyannaməni imtina edir.</summary>
        Task<Result> ImtinaEtAsync(int beyannameId, int tesdiqEdenIsciId, string sebeb);
    }
}
