namespace FinNex.Application.DTOs.Mektub;

// Siyahı üçün
public class DaxilMektubListDto
{
    public int       Id        { get; set; }
    public int?      Nom1      { get; set; }   // Qeydiyyat №
    public DateTime? DaxTarix  { get; set; }   // Daxil olma tarixi
    public string?   IdareAdi  { get; set; }   // Göndərən idarə
    public DateTime? GonTarix  { get; set; }   // Göndərilmə tarixi
    public string?   DaxNom    { get; set; }   // Məktub №
    public int?      Il        { get; set; }
    public int?      IcraciNo  { get; set; }   // MEK_UNVAN
    public string?   IcraciAd  { get; set; }   // icraçı nömrəsindən tapılan işçi adı
    public int?      YaradanId { get; set; }   // sahiblik yoxlaması üçün (AppUser id)
    public string?   FaylYolu  { get; set; }   // DMS nisbi yol (yeni yükləmə) — /dms/ ilə serve olunur
    public bool      FaylVar   { get; set; }   // qoşma (DMS və ya köhnə binar) varmı
}

// Yaratmaq üçün (Il, Nom1, MekUnvan avtomatik təyin olunur)
public class DaxilMektubCreateDto
{
    public DateTime? DaxTarix { get; set; }
    public string?   IdareAdi { get; set; }
    public DateTime? GonTarix { get; set; }
    public string?   DaxNom   { get; set; }
}

// Redaktə üçün (Qeydiyyat №, İl dəyişməz — yalnız məlumat sahələri)
public class DaxilMektubEditDto
{
    public int       Id       { get; set; }
    public DateTime? DaxTarix { get; set; }
    public string?   IdareAdi { get; set; }
    public DateTime? GonTarix { get; set; }
    public string?   DaxNom   { get; set; }
    // Yalnız göstərmə üçün (redaktə olunmur)
    public int?      Nom1           { get; set; }
    public int?      Il             { get; set; }
    public string?   MovcudFaylYolu { get; set; }
    public int?      YaradanId      { get; set; }
}
