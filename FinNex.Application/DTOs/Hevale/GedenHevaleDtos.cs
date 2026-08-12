namespace FinNex.Application.DTOs.Hevale;

// Siyahı üçün
public class GedenHevaleListDto
{
    public int       Id        { get; set; }
    public string?   HevNom    { get; set; }   // Həvalə №
    public DateTime? Tarix     { get; set; }
    public string?   Saa       { get; set; }   // Soyad Ad Ata
    public decimal?  Mebleg    { get; set; }
    public string?   ValTip    { get; set; }   // valyuta
    public string?   Olke      { get; set; }   // təyinat ölkə
    public string?   AlBank    { get; set; }   // alan bank
    public short?    Icra      { get; set; }   // xam icraçı nömrəsi (Oracle ICRA)
    public string?   IcraciAd  { get; set; }   // nömrədən tapılan işçi adı (tapılmasa null)

    // Ekranda göstəriləcək: nömrə işçiyə təyin olunubsa AD, yoxsa xam nömrə.
    // "№" prefiksi YOXDUR — sütun başlığı onsuz da "İcraçı №"dir.
    public string?   IcraciGoster => !string.IsNullOrWhiteSpace(IcraciAd)
        ? IcraciAd
        : Icra?.ToString();

    public int?      YaradanId { get; set; }   // sahiblik (AppUser id)
    public string?   FaylYolu  { get; set; }
    public bool      FaylVar   { get; set; }
}

// Yaratmaq üçün (HevNom, Icra avtomatik təyin olunur)
public class GedenHevaleCreateDto
{
    public DateTime? Tarix      { get; set; }
    public string?   Saa        { get; set; }
    public string?   HesNom     { get; set; }
    public string?   TipRes     { get; set; }
    public decimal?  Mebleg     { get; set; }
    public string?   ValTip     { get; set; }
    public string?   MenOlke    { get; set; }
    public string?   Olke       { get; set; }
    public string?   AlBank     { get; set; }
    public string?   HevTip     { get; set; }
    public string?   GonTip     { get; set; }
    public string?   ContracNom { get; set; }
    public string?   DeclarNom  { get; set; }
}

// Redaktə üçün (HevNom dəyişməz — jurnal nömrəsi)
public class GedenHevaleEditDto
{
    public int       Id         { get; set; }
    public DateTime? Tarix      { get; set; }
    public string?   Saa        { get; set; }
    public string?   HesNom     { get; set; }
    public string?   TipRes     { get; set; }
    public decimal?  Mebleg     { get; set; }
    public string?   ValTip     { get; set; }
    public string?   MenOlke    { get; set; }
    public string?   Olke       { get; set; }
    public string?   AlBank     { get; set; }
    public string?   HevTip     { get; set; }
    public string?   GonTip     { get; set; }
    public string?   ContracNom { get; set; }
    public string?   DeclarNom  { get; set; }
    // Göstərmə üçün (redaktə olunmur)
    public string?   HevNom         { get; set; }
    public string?   MovcudFaylYolu { get; set; }
    public int?      YaradanId      { get; set; }
}
