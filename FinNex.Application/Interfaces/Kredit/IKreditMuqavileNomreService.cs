using FinNex.Application.DTOs.Kredit.Muqavile;

namespace FinNex.Application.Interfaces.Kredit;

/// <summary>
/// Müqavilə nömrələrinin Oracle (odb.muqavile_nomreleri) üzərində idarəsi
/// və məktub qeydiyyatı (odb.xaric_mektub).
///
/// DİQQƏT: Bu, Oracle-a YAZAN yeganə servisdir. CLAUDE.md-də icazə verilən
/// istisna yalnız bu iki cədvəldir. Başqa Oracle yazısı QADAĞANDIR.
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
    /// Girova düşmə (BTİ) məktubunu odb.xaric_mektub-a qeyd edir və məktub nömrəsini ({mekno}) qaytarır.
    /// NomreYaz=false olduqda yazmır, cari maksimum əsasında növbəti nömrəni qaytarır (preview).
    /// </summary>
    Task<string> MektubQeydiyyatiAsync(DateTime tarix, string icraci, CancellationToken ct = default);
}
