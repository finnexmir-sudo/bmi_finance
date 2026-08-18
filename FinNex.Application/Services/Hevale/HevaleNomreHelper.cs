using FinNex.Domain.Entities.Emeliyyat;
using FinNex.Domain.Entities.Hevale;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.Hevale;

/// <summary>
/// «{YY}-T-{N}» həvalə nömrəsinin <b>TƏK MƏNBƏYİ</b>.
///
/// PROBLEM (18.08.2026): eyni jurnalın nömrəsini <b>iki servis ayrı-ayrılıqda</b>
/// verirdi və bir-birini görmürdü:
///   • <see cref="GedenHevaleService"/> → yalnız <c>GedenHevale</c> cədvəlinə baxırdı (2026-da 23 sətir → 24)
///   • <c>KocurmeService</c>            → yalnız <c>Kocurme</c> cədvəlinə baxırdı (boş → <b>1</b>)
/// Nəticədə Pul köçürməsi səhifəsi «26-T-1» təklif edirdi, halbuki 26-T-1 … 26-T-23
/// Gedən həvalə jurnalında ARTIQ mövcud idi — ilk 23 köçürmə zəmanətli dublikat olardı.
///
/// Onlar EYNİ jurnaldır (istifadəçi təsdiqi, 18.08.2026): Gedən həvalə BMI-dən
/// idxal olunmuş tarixi jurnaldır, Pul köçürməsi isə həmin əməliyyatı FinNex-də
/// etmək üçündür. Ona görə nömrə <b>hər iki cədvəlin birləşməsindən</b> hesablanır.
///
/// QAYDALAR (dəyişəndə hər ikisini saxla):
///  1. <c>QueryAll()</c> — silinmiş sətirlər DƏ sayılır. Nömrə bir dəfə veriləndən
///     sonra sənəd artıq o nömrə ilə gedib; qeydin silinməsi nömrəni geri qaytarmır.
///     (<c>HamisiniGetirAsync</c>/<c>Query()</c> avtomatik <c>!Silinib</c> tətbiq edir —
///     EfRepositoryAsync:25,123 — onunla ən böyük nömrəli sətir silinsə nömrə təkrarlanır.)
///     Bax: CLAUDE.md — «Jurnal Nömrəsi Geri Qaytarılmır — Silinmişlər DƏ Sayılır».
///  2. Dublikat qoruyucusu: gözlənilməz formatlı sətir səbəbindən hesablanan nömrə
///     artıq mövcuddursa boş nömrəyə qədər irəliləyir.
///  3. Prefiks ayırıcı ilə birlikdə müqayisə olunur («26-T-»), ona görə «26-TL-5»
///     səhvən T nömrəsi kimi sayılmır.
///
/// BMI FORMAT ÖLÇMƏSİ (13.08.2026, <c>odb.geden_hevale</c>, 9835 sətir):
/// <code>
///   9696 sətir  "26-T-9"    → ortada LATIN T (bayt 84)   ← cari qayda
///    138 sətir  "07-T-698"  → ortada KİRİL Т (208,162)   ← yalnız 2006–2007
///      1 sətir  "07-T474"   → ikinci tire yoxdur (səhv giriş)
/// </code>
/// Ona görə şablon LATIN T ilə yoxlanılır; kiril «Т»-li və formatsız köhnə
/// sətirlər cari ilin sayğacına qarışmır (onsuz da 2006–2007-dədirlər).
/// </summary>
internal static class HevaleNomreHelper
{
    /// <summary>Pul köçürməsi / gedən həvalə prefiksi.</summary>
    public const string PulPrefiksi = "T";

    /// <summary>Tələbə köçürməsi prefiksi.</summary>
    public const string TelebePrefiksi = "TL";

    /// <summary>
    /// «{YY}-{prefiks}-{N}» şablonuna uyğun gəlirsə N-i qaytarır, əks halda null.
    /// null (0 yox) qaytarılır — 0 «sıfırıncı nömrə» kimi max hesabını yanılda bilərdi.
    /// Köhnə BMI sətirləri (kiril «Т», ikinci tiresi olmayan «07-T474») şablona
    /// uymur və cari ilin sayğacına qarışmır.
    /// </summary>
    public static int? Nomre(string? xam, int il, string prefiks)
    {
        if (string.IsNullOrWhiteSpace(xam)) return null;

        var pref = Prefiks(il, prefiks);
        var t = xam.Trim();
        if (!t.StartsWith(pref, StringComparison.Ordinal)) return null;

        return int.TryParse(t[pref.Length..], out var n) ? n : null;
    }

    private static string Prefiks(int il, string prefiks) => $"{il % 100:D2}-{prefiks}-";

    /// <summary>
    /// Həmin il üçün növbəti boş nömrəni qaytarır — HƏR İKİ cədvələ baxaraq.
    /// </summary>
    public static async Task<string> NovbetiAsync(IUnitOfWork uow, int il, string prefiks)
    {
        // Gedən həvalə jurnalı (BMI idxalı + FinNex-də yaradılanlar)
        var hevaleNomreleri = await uow.Repository<GedenHevale>().QueryAll()
            .AsNoTracking()
            .Where(x => x.Tarix != null && x.Tarix.Value.Year == il)
            .Select(x => x.HevNom)
            .ToListAsync();

        // Pul/Tələbə köçürməsi (Əməliyyat modulu)
        var kocurmeNomreleri = await uow.Repository<Kocurme>().QueryAll()
            .AsNoTracking()
            .Where(x => x.Tarix != null && x.Tarix.Value.Year == il)
            .Select(x => x.HevaleNo)
            .ToListAsync();

        var hamisi = hevaleNomreleri.Concat(kocurmeNomreleri).ToList();

        var movcud = hamisi
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var novbeti = hamisi
            .Select(x => Nomre(x, il, prefiks))
            .Where(n => n.HasValue)
            .Select(n => n!.Value)
            .DefaultIfEmpty(0)
            .Max() + 1;

        var pref = Prefiks(il, prefiks);
        string netice;
        do
        {
            netice = $"{pref}{novbeti}";
            novbeti++;
        }
        while (movcud.Contains(netice));

        return netice;
    }
}
