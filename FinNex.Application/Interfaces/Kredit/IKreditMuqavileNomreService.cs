using FinNex.Application.DTOs.Kredit.Muqavile;

namespace FinNex.Application.Interfaces.Kredit;

/// <summary>
/// Müqavilə nömrələrinin Oracle (odb.muqavile_nomreleri) üzərində idarəsi
/// və girova düşmə (BTİ) məktubunun qeydiyyatı.
///
/// DİQQƏT: Bu, Oracle-a YAZAN yeganə servisdir. İcazə verilən istisna indi
/// YALNIZ `odb.muqavile_nomreleri`-dir — məktub jurnalı FinNex-ə köçürüldüyü üçün
/// məktub qeydi öz bazamıza yazılır. Başqa Oracle yazısı QADAĞANDIR.
/// KreditMuqavile:NomreYaz = false olduqda (default) servis heç nə YAZMIR,
/// yalnız cari nömrələri oxuyur (preview) — Samir yoxlayandan sonra true edilir.
/// </summary>
public interface IKreditMuqavileNomreService
{
    /// <summary>
    /// Cari il üçün kredit/ipoteka/zamin nömrələrini ayırır.
    /// NomreYaz=true olduqda sayğacları atomik artırır; false olduqda yalnız oxuyur.
    /// </summary>
    Task<MenzilNomreleriDto> MenzilNomreleriAyirAsync(int zaminSayi, CancellationToken ct = default);

    /// <summary>
    /// Zaminlik krediti üçün nömrələri ayırır — yalnız kr_zaminlik (kredit müqaviləsi)
    /// və kr_zaminler (zaminlik running) sayğacları. İpoteka (kr_menzil) TOXUNULMUR,
    /// çünki zaminlik kreditində ipoteka yoxdur. IpotekaNo = 0 qaytarılır.
    /// </summary>
    Task<MenzilNomreleriDto> ZaminlikNomreleriAyirAsync(int zaminSayi, CancellationToken ct = default);

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
