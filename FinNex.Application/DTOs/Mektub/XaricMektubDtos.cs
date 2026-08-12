namespace FinNex.Application.DTOs.Mektub;

// Siyahı üçün
public class XaricMektubListDto
{
    public int       Id        { get; set; }
    public string?   QeyNom    { get; set; }   // Qeydiyyat №
    public DateTime? Tarix     { get; set; }   // Tarix
    public string?   GonYer    { get; set; }   // Göndərilən yer (təyinat)
    public string?   QisaMez   { get; set; }   // Qısa məzmun

    // İCRAÇI — Oracle `ICRACI` sütunu RƏQƏM saxlayır (icraçı nömrəsi: 68, 25, 48…),
    // ad yox. Xam dəyər `Icraci`-də qalır (Oracle ilə 1:1), adı isə göstərmə anında
    // `Isci.IcraciNo`-dan tapılır — Daxil məktub və Həvalə ilə eyni qayda.
    public string?   Icraci    { get; set; }   // xam dəyər (adətən nömrə)
    public int?      IcraciNo  { get; set; }   // rəqəmə parse olunanda
    public string?   IcraciAd  { get; set; }   // nömrədən tapılan işçi adı (tapılmasa null)

    // Ekranda göstəriləcək: nömrə işçiyə təyin olunubsa AD, yoxsa xam nömrə.
    // "№" prefiksi YOXDUR — sütun başlığı onsuz da "İcraçı №"dir, təkrar olardı.
    public string?   IcraciGoster => !string.IsNullOrWhiteSpace(IcraciAd)
        ? IcraciAd
        : (string.IsNullOrWhiteSpace(Icraci) ? null : Icraci.Trim());

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
