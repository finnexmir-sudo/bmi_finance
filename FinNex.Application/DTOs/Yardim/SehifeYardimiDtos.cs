namespace FinNex.Application.DTOs.Yardim;

/// <summary>«?» panelinə göndərilən məlumat (oxumaq üçün).</summary>
public class YardimPanelDto
{
    public int      Id         { get; set; }
    public string?  Basliq     { get; set; }
    public string?  Modul      { get; set; }
    public string?  Xulase     { get; set; }
    /// <summary>HTML mətn. `Hazirlanir=true` olduqda boş göndərilir.</summary>
    public string?  Metn       { get; set; }
    public string?  Slug       { get; set; }
    public bool     Hazirlanir { get; set; }
    /// <summary>Bu səhifə üçün ümumiyyətlə qeyd varmı (yoxdursa «hələ yazılmayıb»).</summary>
    public bool     Var        { get; set; }
    /// <summary>Qeyd yoxdursa admin «indi yaz» düyməsi üçün açarı görməlidir.</summary>
    public string?  Acar       { get; set; }
    public DateTime? Yenilenme { get; set; }
}

/// <summary>İndeks siyahısının bir sətri.</summary>
public class YardimListDto
{
    public int     Id         { get; set; }
    public string? Basliq     { get; set; }
    public string? Modul      { get; set; }
    public string? Xulase     { get; set; }
    public string? Slug       { get; set; }
    public string? Acar       { get; set; }
    public bool    Hazirlanir { get; set; }
    public int     BaxisSayi  { get; set; }
    public DateTime? Yenilenme { get; set; }
}

/// <summary>Admin redaktoru — yaratmaq/dəyişmək.</summary>
public class YardimUpsertDto
{
    public int     Id          { get; set; }
    public string? Acar        { get; set; }
    public string? Slug        { get; set; }
    public string? Basliq      { get; set; }
    public string? Modul       { get; set; }
    public string? Xulase      { get; set; }
    public string? Metn        { get; set; }
    public bool    Hazirlanir  { get; set; }
    public bool    YalnizAdmin { get; set; }
}

/// <summary>
/// Əhatə ekranının bir sətri: koddakı səhifə → yardımı varmı?
/// Boşluq təxmin edilmir, GÖRÜNÜR.
/// </summary>
public class YardimEhateDto
{
    public string  Acar       { get; set; } = string.Empty;
    public string? Sahe       { get; set; }
    public string? Kontroller { get; set; }
    public string? Emel       { get; set; }
    public bool    Yazilib    { get; set; }
    public bool    Hazirlanir { get; set; }
    public string? Basliq     { get; set; }
}
