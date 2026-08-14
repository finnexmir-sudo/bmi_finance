namespace FinNex.Application.DTOs.Kredit.Muqavile;

/// <summary>
/// Avtomobil ipoteka (girov) müqaviləsi hazırlama formasının POST məlumatı.
///
/// Daşınmaz əmlakdan FƏRQLƏR:
///  • Girov sahibi HƏMİŞƏ borcalandır — şablon «Borcalan və İpoteka qoyan» deyir,
///    ona görə `GirovSahibiFerqli` kimi sahə YOXDUR;
///  • Avtomobilin məlumatları BMI-də saxlanılmır (yoxlandı 14.08.2026: açıq
///    avtomobil kreditlərində `creditinfo.anyinfotodisting` və `registryno`
///    sahələri 0-dır) — hamısı formada əl ilə yazılır;
///  • `AvtoDeyeri` isə Oracle-dan (`t.summa_zaloga`) öncədən doldurulur və
///    operator düzəldə bilər — ipotekadakı `GirovDeyeri` ilə eyni qayda.
///
/// Kredit datası (k_*) POST-da HesabNo+Ks ilə Oracle-dan YENİDƏN oxunur —
/// bu DTO yalnız əl ilə daxil edilən hissəni daşıyır.
/// </summary>
public class AvtomobilMuqavileYaratDto
{
    // Kreditin kimliyi — POST-da Oracle-dan yenidən oxumaq üçün
    public string HesabNo { get; set; } = "";   // t.licschkre
    public string Ks { get; set; } = "";         // t.subschkre
    public DateTime KreditTarixi { get; set; }   // siyahıdakı verilmə tarixi (Oracle sorğusu üçün)

    // Müqavilə tarixi — sətirdən (verilmə tarixi) gəlir ({k_tar_soz}).
    // Nömrə sayğacının İLİ də bundan götürülür, "bu gün"dən yox.
    public DateTime MuqavileTarixi { get; set; }

    public string? Teyinat { get; set; }         // {k_teyinat}
    public string? BorcalanOlke { get; set; }    // {k_olke}

    // Borcalan hüquqi şəxs olduqda (r.yurik=1) — direktorun məlumatı əl ilə.
    public string? DirektorAd { get; set; }
    public string? DirektorVesiqe { get; set; }
    public string? DirektorOlke { get; set; }

    // ── Avtomobil (ipoteka predmeti) — hamısı əl ilə yazılır ──────────────
    public string? Model { get; set; }           // {k_model}
    public string? Muherrik { get; set; }        // {k_muherrik} — mühərrik nömrəsi
    public string? Ban { get; set; }             // {k_ban}      — ban (kuzov) nömrəsi
    public string? Reng { get; set; }            // {k_reng}
    public string? Il { get; set; }              // {k_il}       — buraxılış ili (sərbəst mətn)

    // Bazar dəyəri — şablonda «… manat məbləğində» sabit yazılıb, ona görə AZN.
    public decimal? AvtoDeyeri { get; set; }     // {k_avto_deyer} + {k_avto_deyer_soz}

    // Zaminlər (0…N) — kredit müqaviləsinin təminat bəndinə düşür
    public List<ZaminDaxilDto> Zaminler { get; set; } = new();
}
