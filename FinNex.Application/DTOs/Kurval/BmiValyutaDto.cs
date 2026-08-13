namespace FinNex.Application.DTOs.Kurval;

/// <summary>
/// Valyuta — BMI `kurval` cədvəlindən (Oracle, YALNIZ OXU).
/// Kod = SOKNAMEVALUT ("00", "01", …), Ad = NAMEVALUTI ("ABŞ DOLLARI", …).
///
/// ⚠️ FinNex-in ÖZ `Valyuta` cədvəli ilə QARIŞDIRMA. O, ödəniş tapşırığı
/// modulunundur (`IValyutaService` → `ValyutaListDto`) və ayrıca idarə olunur.
/// Bu DTO isə BMI-nin əsas bankçılıq siyahısıdır — həvalə jurnallarında
/// valyuta məhz oradan seçilir ki, BMI ilə eyni kodlar işlənsin.
///
/// DİQQƏT: `kurval`-da beynəlxalq qısaltma (USD/EUR) YOXDUR — yalnız kod və
/// azərbaycanca ad var. Ona görə həvalə qeydində saxlanılan dəyər KODDUR.
/// </summary>
public class BmiValyutaDto
{
    public string Kod { get; set; } = "";   // SOKNAMEVALUT — "01"
    public string Ad  { get; set; } = "";   // NAMEVALUTI  — "ABŞ DOLLARI"

    // Açılan siyahıda görünən mətn: "01 — ABŞ DOLLARI"
    public string Goster => string.IsNullOrWhiteSpace(Ad) ? Kod : $"{Kod} — {Ad}";
}
