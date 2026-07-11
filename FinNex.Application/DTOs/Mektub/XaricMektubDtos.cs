namespace FinNex.Application.DTOs.Mektub;

// Siyahı üçün
public class XaricMektubListDto
{
    public int       Id        { get; set; }
    public string?   QeyNom    { get; set; }   // Qeydiyyat №
    public DateTime? Tarix     { get; set; }   // Tarix
    public string?   GonYer    { get; set; }   // Göndərilən yer (təyinat)
    public string?   QisaMez   { get; set; }   // Qısa məzmun
    public string?   Icraci    { get; set; }   // İcraçı (mətn)
    public int?      Il        { get; set; }
    public int?      YaradanId { get; set; }   // sahiblik yoxlaması üçün (AppUser id)
    public string?   FaylYolu  { get; set; }   // DMS nisbi yol (yeni yükləmə) — /dms/ ilə serve olunur
    public bool      FaylVar   { get; set; }   // qoşma varmı
}

// Yaratmaq üçün (QeyNom, Il, Icraci avtomatik təyin olunur)
public class XaricMektubCreateDto
{
    public DateTime? Tarix      { get; set; }
    public string?   GonYer     { get; set; }
    public string?   QisaMez    { get; set; }
    public string?   MektubMetn { get; set; }
}

// Redaktə üçün (Qeydiyyat №, İl dəyişməz — yalnız məlumat sahələri)
public class XaricMektubEditDto
{
    public int       Id         { get; set; }
    public DateTime? Tarix      { get; set; }
    public string?   GonYer     { get; set; }
    public string?   QisaMez    { get; set; }
    public string?   MektubMetn { get; set; }
    // Yalnız göstərmə üçün (redaktə olunmur)
    public string?   QeyNom         { get; set; }
    public int?      Il             { get; set; }
    public string?   MovcudFaylYolu { get; set; }
    public int?      YaradanId      { get; set; }
}
