using FinNex.Application.DTOs.Kredit.Muqavile;

namespace FinNex.Application.Interfaces.Kredit;

/// <summary>
/// Müqavilə nömrələri və girova düşmə (BTİ) məktubunun qeydiyyatı.
///
/// ✅ ORACLE-A YAZI YOXDUR. Həm sayğaclar (MuqavileSayghaci), həm məktub jurnalı
/// (XaricMektub) FinNex bazasındadır. CLAUDE.md-dəki "Oracle YALNIZ SELECT"
/// qaydasının istisnası qalmayıb.
/// KreditMuqavile:NomreYaz = false olduqda (default) servis heç nə YAZMIR,
/// yalnız növbəti nömrələri göstərir (preview) — yoxlamadan sonra true edilir.
/// </summary>
public interface IKreditMuqavileNomreService
{
    /// <summary>
    /// Kredit/ipoteka/zamin nömrələrini ayırır (FinNex sayğacları).
    /// NomreYaz=true olduqda sayğacları artırır; false olduqda yalnız növbətini göstərir.
    ///
    /// muqavileTarixi — nömrə HANSI İLİN sayğacından veriləcəyini təyin edir.
    /// Sənədin öz tarixidir, "bu gün" DEYİL: 31.12-də bağlanan müqavilə 02.01-də
    /// hazırlansa belə keçən ilin sayğacından nömrə almalıdır, yoxsa sənədin
    /// üstündəki tarix ilə nömrəsinin ili uyğunsuz olur (13.08.2026 qərarı).
    /// Parametrin defolt dəyəri QƏSDƏN yoxdur — yeni çağırış yeri onu ötürməyi
    /// unutsa kod compile olmasın.
    /// </summary>
    Task<MenzilNomreleriDto> MenzilNomreleriAyirAsync(int zaminSayi, DateTime muqavileTarixi,
        CancellationToken ct = default);

    /// <summary>
    /// Zaminlik krediti üçün nömrələri ayırır — yalnız kr_zaminlik (kredit müqaviləsi)
    /// və kr_zaminler (zaminlik running) sayğacları. İpoteka (kr_menzil) TOXUNULMUR,
    /// çünki zaminlik kreditində ipoteka yoxdur. IpotekaNo = 0 qaytarılır.
    /// muqavileTarixi — yuxarıdakı ilə eyni qayda (sənədin tarixi, "bu gün" deyil).
    /// </summary>
    Task<MenzilNomreleriDto> ZaminlikNomreleriAyirAsync(int zaminSayi, DateTime muqavileTarixi,
        CancellationToken ct = default);

    /// <summary>
    /// Girova düşmə (BTİ) məktubunu FinNex məktub jurnalına (XaricMektub) qeyd edir və
    /// nömrəsini ({mekno}, "il-nömrə" formatında) qaytarır. Oracle-a YAZILMIR.
    /// NomreYaz=false olduqda yazmır, yalnız növbəti nömrəni göstərir (preview).
    ///
    /// yaradanUserId — AppUser id: həm sahiblik, həm də icraçı nömrəsi (Isci.IcraciNo)
    /// bu istifadəçidən tapılır. Jurnal səhifəsindən yaradılan məktubla eyni qayda.
    /// </summary>
    Task<string> MektubQeydiyyatiAsync(DateTime tarix, int yaradanUserId, CancellationToken ct = default);
}
