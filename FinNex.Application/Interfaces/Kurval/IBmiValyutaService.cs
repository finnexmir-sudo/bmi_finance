using FinNex.Application.DTOs.Kurval;

namespace FinNex.Application.Interfaces.Kurval;

/// <summary>
/// Valyuta siyahısı — BMI `kurval` cədvəlindən oxunur (YALNIZ SELECT).
///
/// FinNex-də bu siyahı üçün cədvəl SAXLANILMIR: 6 sətirdir, dəyişməsi nadirdir
/// və mənbəyi BMI-nin əsas bankçılıq sistemidir. Canlı oxumaqla həmişə sinxron
/// qalırıq — köçürsək, BMI yeni valyuta əlavə edəndə bizdə görünməzdi.
///
/// ⚠️ Layihədə `IValyutaService` adlı BAŞQA servis var (ödəniş tapşırığı modulu,
/// FinNex-in öz `Valyuta` cədvəli). Bu ikisi ayrı mənbələrdir — qarışdırma.
/// </summary>
public interface IBmiValyutaService
{
    /// <summary>
    /// Valyutalar (kod + ad), koda görə sıralı.
    /// Oracle əlçatmaz olarsa siyahı BOŞ QAYTARILMIR — sabit ehtiyat siyahı
    /// işə düşür ki, forma istifadəyə yararsız olmasın (bax: implementasiya).
    /// </summary>
    Task<IList<BmiValyutaDto>> SiyahiAsync(CancellationToken ct = default);

    /// <summary>
    /// Bir valyutanın verilmiş TARİXDƏKİ Mərkəzi Bank kursu — «1 vahid = N AZN».
    /// Mənbə: Oracle <c>odb.func_get_kurval(kod, tarix)</c> (layihədə IFRS9
    /// hesabatlarının hamısı bu funksiyanı işlədir).
    ///
    /// AZN üçün (kod «00») funksiya 1 qaytarır — çevirmə lazım deyil.
    ///
    /// ⚠️ <b>Oracle əlçatmaz olsa `null` qayıdır — SƏSSİZ 0 YOX.</b> Sıfır
    /// qaytarsaydıq USD ekvivalenti sıfıra bölünər və ya 0 yazılardı; aylıq
    /// limit cəmi səssizcə əskik çıxardı. Çağıran tərəf `null` görəndə
    /// əməliyyatı BLOKLAMALIDIR (istifadəçi qərarı 07.09.2026: «Oracle
    /// işləməsə heç nə işləməz, o, ən əsasıdır»).
    /// </summary>
    /// <param name="valyutaKodu">kurval kodu — «00» AZN, «01» USD, «02» AVRO…</param>
    Task<decimal?> KursAsync(string valyutaKodu, DateTime tarix, CancellationToken ct = default);
}
