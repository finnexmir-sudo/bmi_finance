namespace FinNex.Application.DTOs.Aml;

/// <summary>
/// AML → «Hesab üzrə sorğu» formasının giriş parametrləri.
/// BMI-dəki `frmhesabsorgu` formasının eynisidir: hesab № + iki tarix + sorğu növü.
/// </summary>
public class AmlHesabSorguDto
{
    /// <summary>Çıxarış veriləcək hesab nömrəsi (20 simvol).</summary>
    public string? Hesab { get; set; }

    /// <summary>Başlama tarixi.</summary>
    public DateTime? BasTarix { get; set; }

    /// <summary>Bitmə tarixi.</summary>
    public DateTime? SonTarix { get; set; }

    /// <summary>
    /// false = «Fiziki şəxs», true = «Sahibkar / hüquqi şəxs VÖEN».
    /// BMI-də bu iki radio düymə İKİ AYRI SQL işə salır — birləşdirilə bilməz,
    /// nəticələr fərqlidir (VÖEN sahələri, marşrut şərtləri, bank adı mənbəyi).
    /// </summary>
    public bool Huquqi { get; set; }
}

/// <summary>
/// Sorğunun nəticəsi — şapka məlumatı + xam sətirlər.
/// Sətirlər sütun ADI ilə deyil, SIRA ilə saxlanılır: Excel şablonunun
/// sütunları ilə birbaşa uyğun gəlir (A…AU = 0…46).
/// </summary>
public class AmlHesabNeticeDto
{
    public bool Ugurlu { get; set; }
    public string? Xeta { get; set; }

    // ── Giriş parametrləri (ekranda və Excel şapkasında göstərilir) ──────
    public string Hesab { get; set; } = "";
    public DateTime BasTarix { get; set; }
    public DateTime SonTarix { get; set; }
    public bool Huquqi { get; set; }

    // ── Şapka (BMI: `hesabad_qaliq` sorğusu) ─────────────────────────────
    /// <summary>Hesabın adı — `odb.accounts.name_latin`.</summary>
    public string? HesabAdi { get; set; }
    public decimal? GirisQaliq { get; set; }
    public decimal? SonQaliq { get; set; }

    /// <summary>
    /// Hesabın valyutası — hesab nömrəsinin 6–7-ci simvolundan.
    /// BMI-də bu dəyər hesablanır, amma köhnə şablonda yeri yox idi (ölü kod).
    /// Yeni şablonda `A8` sətri var → `D8` xanasına yazılır.
    /// </summary>
    public string? Valyuta { get; set; }

    // ── Cədvəl ───────────────────────────────────────────────────────────
    public List<string> Sutunlar { get; set; } = new();
    public List<object?[]> Setirler { get; set; } = new();

    public int SetirSayi => Setirler.Count;
}
