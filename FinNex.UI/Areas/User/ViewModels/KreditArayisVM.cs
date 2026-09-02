namespace FinNex.UI.Areas.User.ViewModels;

/// <summary>
/// Kredit arayışlarının forma modelləri (02.09.2026).
///
/// ⚠️ BÜTÜN MƏTN SAHƏLƏRİ `string?`-dır — QƏSDƏN. .NET 8 MVC-də non-nullable
/// string avtomatik **Required** sayılır; formada göndərilməyən sahə ModelState-i
/// kəsir və «düyməni basıram, heç nə olmur» şikayətinə çevrilir (CLAUDE.md).
/// Məcburi sahələr controller-də AÇIQ yoxlanılır ki, xəta mətni anlaşılan olsun.
/// </summary>
public class DypArayisVM
{
    public string? Musteri        { get; set; }
    public string? AvtoNo         { get; set; }
    public string? Marka          { get; set; }
    public string? BuraxilisIli   { get; set; }
    public string? Muherrik       { get; set; }
    public string? Ban            { get; set; }
    public string? Reng           { get; set; }
    public string? MuqavileNo     { get; set; }

    /// <summary>Müqavilənin tarixi — sənəddə `{muqtar}` (sözlə yazılır).</summary>
    public DateTime? MuqavileTarixi { get; set; }

    /// <summary>Məktubun tarixi — `{mektarixi}` və jurnal sətrinin tarixi.</summary>
    public DateTime? MektubTarixi { get; set; } = DateTime.Today;
}

/// <summary>
/// Borcalan təmizlik arayışı. Sahələr Oracle axtarışından doldurulur, sonra
/// operator düzəldə bilər.
/// </summary>
public class BorcalanArayisVM
{
    /// <summary>Axtarış dəyəri — səhifəyə qayıdanda saxlanılır.</summary>
    public string? Regnom     { get; set; }

    public string? Borcalan   { get; set; }
    public string? MuqavileNo { get; set; }
    public decimal? Mebleg    { get; set; }
    public string? Valyuta    { get; set; }

    /// <summary>
    /// Kredit müqaviləsinin tarixi — `{muqtar}`.
    ///
    /// ⚠️ BMI-DƏN FƏRQ: orada bu xana səhifə açılanda BUGÜNKÜ tarixlə dolurdu,
    /// kreditin real tarixi isə ayrıca (istifadə olunmayan) xanada qalırdı.
    /// Şablon mətni «{borcalan} ilə {muqtar} il tarixində … bağlanmış» deyir —
    /// yəni bura MÜQAVİLƏ tarixi düşməlidir. Burada Oracle-dan gələn `date_open`
    /// ilə öncədən doldurulur, operator lazım gələrsə dəyişir.
    /// </summary>
    public DateTime? MuqavileTarixi { get; set; }

    public DateTime? MektubTarixi { get; set; } = DateTime.Today;
}

/// <summary>Zamin təmizlik arayışı.</summary>
public class ZaminArayisVM
{
    public string? Fin       { get; set; }   // axtarış dəyəri
    public string? Borcalan  { get; set; }
    public string? Zamin     { get; set; }
    public decimal? Mebleg   { get; set; }
    public string? Valyuta   { get; set; }

    /// <summary>Kredit müqaviləsinin tarixi — `{muqtar}` (BMI-də də belədir).</summary>
    public DateTime? MuqavileTarixi { get; set; }

    public DateTime? MektubTarixi { get; set; } = DateTime.Today;
}

/// <summary>
/// Saipa — iki rejim, iki şablon. Rejim dəyəri mətndir ki, sənəddəki
/// «qısa məzmun» sətri BMI ilə eyni qalsın.
/// </summary>
public class SaipaArayisVM
{
    public const string RejimGirov      = "Girovdan çıxma";
    public const string RejimTexpasport = "Texpasport dəyişmə";

    public string? Rejim          { get; set; } = RejimGirov;
    public string? AvtoNo         { get; set; }
    public string? BuraxilisIli   { get; set; }
    public string? Muherrik       { get; set; }
    public string? Ban            { get; set; }
    public string? Reng           { get; set; }

    /// <summary>Yalnız «Texpasport dəyişmə» rejimində — `{texpNo}`. Orada MƏCBURİDİR.</summary>
    public string? TexpasportNo   { get; set; }

    public DateTime? MuqavileTarixi { get; set; }
    public DateTime? MektubTarixi   { get; set; } = DateTime.Today;
}
