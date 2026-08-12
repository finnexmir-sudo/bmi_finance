namespace FinNex.Application.DTOs.Hevale;

// Həvalə jurnallarının (gedən/gələn) ortaq filtri.
//
// QEYD: Bu tip `MektubFiltrDto`-nun ƏKİZİDİR — eyni məntiq, ayrı modul. Fərq:
// həvalə cədvəllərində `Il` SÜTUNU YOXDUR (il `Tarix`-dən çıxarılır) və icraçı
// nömrəsi `short`-dur (Oracle NUMBER(3) → SQL smallint). Birində filtr/səhifələmə
// qaydası dəyişirsə, o birinə də bax — ikisi istifadəçi üçün eyni ekrandır.
public class HevaleFiltrDto
{
    // null  → CARİ İL (səhifə ilk açılanda)
    // 0     → BÜTÜN İLLƏR (istifadəçi açıq şəkildə seçir)
    // digər → həmin il
    public int? Il { get; set; }

    public short?    IcraciNo  { get; set; }   // icraçı nömrəsi (Isci.IcraciNo → Icra)
    public DateTime? TarixFrom { get; set; }
    public DateTime? TarixTo   { get; set; }
    public string?   Axtaris   { get; set; }   // S.A.A. / № / bank / ölkə üzrə mətn axtarışı

    public int Sehife       { get; set; } = 1;
    public int SehifeOlcusu { get; set; } = 50;

    public bool ButunIller => Il == 0;

    // Filtrin sorğuda işlədiləcək il dəyəri (bütün illər seçilibsə null)
    public int? SorguIli => (Il == 0) ? null : Il;

    // Cari il defoltu tətbiq olunmuş nüsxə — controller-lər bunu çağırır.
    public static HevaleFiltrDto Normalla(HevaleFiltrDto? filtr)
    {
        var f = filtr ?? new HevaleFiltrDto();
        f.Il ??= DateTime.Today.Year;
        f.Axtaris = string.IsNullOrWhiteSpace(f.Axtaris) ? null : f.Axtaris.Trim();
        if (f.Sehife < 1) f.Sehife = 1;
        if (f.SehifeOlcusu is < 10 or > 500) f.SehifeOlcusu = 50;
        return f;
    }
}

// Filtr açılan siyahılarının mənbəyi — həmin jurnalda REAL mövcud olan dəyərlər.
public class HevaleFiltrMenbeDto
{
    public List<int>             Iller     { get; set; } = new();
    public List<HevaleIcraciDto> Icracilar { get; set; } = new();
}

// Jurnalda işlənən icraçı nömrəsi + (varsa) işçi adı.
public class HevaleIcraciDto
{
    public short   No  { get; set; }
    public string? Ad  { get; set; }   // Isci.IcraciNo ilə tapılan ad; təyin edilməyibsə null
    public int     Say { get; set; }   // həmin nömrə ilə neçə həvalə var

    public string Goster => string.IsNullOrWhiteSpace(Ad)
        ? $"№ {No} (təyin edilməyib)"
        : $"{No} — {Ad}";
}

// Səhifələnmiş nəticə — siyahı + ümumi say (pager üçün).
public class HevaleSehifeDto<T>
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
