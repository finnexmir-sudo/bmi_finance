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

    // ── Səhifələmə ──────────────────────────────────────────────────────
    // "Bütün illər" seçiləndə nəticə 32 min sətrə çata bilər — bir səhifədə
    // göstərmək nə brauzerə, nə də istifadəçiyə fayda verir.
    public int Sehife       { get; set; } = 1;
    public int SehifeOlcusu { get; set; } = 50;

    public bool ButunIller => Il == 0;

    // Filtrin DB sorğusunda işlədiləcək il dəyəri (bütün illər seçilibsə null)
    public int? SorguIli => (Il == 0) ? null : Il;

    // Cari il defoltu tətbiq olunmuş nüsxə — controller-lər bunu çağırır.
    public static MektubFiltrDto Normalla(MektubFiltrDto? filtr)
    {
        var f = filtr ?? new MektubFiltrDto();
        f.Il ??= DateTime.Today.Year;
        f.Axtaris = string.IsNullOrWhiteSpace(f.Axtaris) ? null : f.Axtaris.Trim();
        if (f.Sehife < 1) f.Sehife = 1;
        if (f.SehifeOlcusu is < 10 or > 500) f.SehifeOlcusu = 50;
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

// Səhifələnmiş nəticə — siyahı + ümumi say (pager üçün).
public class MektubSehifeDto<T>
{
    public IList<T> Setirler     { get; set; } = new List<T>();
    public int      CemiSay      { get; set; }
    public int      Sehife       { get; set; } = 1;
    public int      SehifeOlcusu { get; set; } = 50;

    public int SehifeSayi => CemiSay <= 0 ? 1 : (int)Math.Ceiling(CemiSay / (double)SehifeOlcusu);
    public int Ilk        => CemiSay == 0 ? 0 : (Sehife - 1) * SehifeOlcusu + 1;
    public int Son        => Math.Min(Sehife * SehifeOlcusu, CemiSay);
    public bool EvvelVar  => Sehife > 1;
    public bool SonraVar  => Sehife < SehifeSayi;
}
