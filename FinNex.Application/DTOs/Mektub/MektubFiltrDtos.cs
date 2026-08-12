namespace FinNex.Application.DTOs.Mektub;

// Məktub jurnallarının (daxil/xaric) ortaq filtri.
public class MektubFiltrDto
{
    // null  → CARİ İL (səhifə ilk açılanda; 32 min sətrin hamısı yüklənməsin)
    // 0     → BÜTÜN İLLƏR (istifadəçi açıq şəkildə seçir)
    // digər → həmin il
    public int? Il { get; set; }

    public int?      IcraciNo  { get; set; }   // icraçı nömrəsi (Isci.IcraciNo)
    public DateTime? TarixFrom { get; set; }
    public DateTime? TarixTo   { get; set; }
    public string?   Axtaris   { get; set; }   // təyinat / məzmun / nömrə üzrə mətn axtarışı

    public bool ButunIller => Il == 0;

    // Filtrin DB sorğusunda işlədiləcək il dəyəri (bütün illər seçilibsə null)
    public int? SorguIli => (Il == 0) ? null : Il;

    // Cari il defoltu tətbiq olunmuş nüsxə — controller-lər bunu çağırır.
    public static MektubFiltrDto Normalla(MektubFiltrDto? filtr)
    {
        var f = filtr ?? new MektubFiltrDto();
        f.Il ??= DateTime.Today.Year;
        f.Axtaris = string.IsNullOrWhiteSpace(f.Axtaris) ? null : f.Axtaris.Trim();
        return f;
    }
}

// Filtr açılan siyahılarının mənbəyi — həmin jurnalda REAL mövcud olan dəyərlər.
public class MektubFiltrMenbeDto
{
    public List<int>              Iller     { get; set; } = new();
    public List<MektubIcraciDto>  Icracilar { get; set; } = new();
}

// Jurnalda işlənən icraçı nömrəsi + (varsa) işçi adı.
public class MektubIcraciDto
{
    public int     No  { get; set; }
    public string? Ad  { get; set; }   // Isci.IcraciNo ilə tapılan ad; təyin edilməyibsə null
    public int     Say { get; set; }   // həmin nömrə ilə neçə məktub var

    // Açılan siyahıda görünən mətn: ad varsa "68 — Rafael Quliyev", yoxsa "№ 68 (təyin edilməyib)"
    public string Goster => string.IsNullOrWhiteSpace(Ad)
        ? $"№ {No} (təyin edilməyib)"
        : $"{No} — {Ad}";
}
