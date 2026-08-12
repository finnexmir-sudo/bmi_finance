namespace FinNex.Application.DTOs.Hevale;

// Siyahı üçün
public class GelenHevaleListDto
{
    public int       Id        { get; set; }
    public string?   HevNom    { get; set; }
    public DateTime? Tarix     { get; set; }
    public string?   Saa       { get; set; }
    public decimal?  Mebleg    { get; set; }
    public string?   ValTip    { get; set; }
    public string?   GelOlke   { get; set; }   // gələn (mənbə) ölkə
    public string?   AlBank    { get; set; }
    public short?    Icra      { get; set; }   // xam icraçı nömrəsi (Oracle ICRA)
    public string?   IcraciAd  { get; set; }   // nömrədən tapılan işçi adı (tapılmasa null)

    // Ekranda göstəriləcək: nömrə işçiyə təyin olunubsa AD, yoxsa xam nömrə.
    // "№" prefiksi YOXDUR — sütun başlığı onsuz da "İcraçı №"dir.
    public string?   IcraciGoster => !string.IsNullOrWhiteSpace(IcraciAd)
        ? IcraciAd
        : Icra?.ToString();

    public int?      YaradanId { get; set; }
    public string?   FaylYolu  { get; set; }
    public bool      FaylVar   { get; set; }
}

// Yaratmaq üçün (HevNom, Icra avtomatik təyin olunur)
public class GelenHevaleCreateDto
{
    public DateTime? Tarix   { get; set; }
    public string?   Saa     { get; set; }
    public string?   HesNom  { get; set; }
    public string?   TipRes  { get; set; }
    public decimal?  Mebleg  { get; set; }
    public string?   ValTip  { get; set; }
    public string?   MenOlke { get; set; }
    public string?   GelOlke { get; set; }
    public string?   AlBank  { get; set; }
    public string?   HevTip  { get; set; }
    public string?   GonTip  { get; set; }
    public string?   DecNom  { get; set; }
}

// Redaktə üçün (HevNom dəyişməz)
public class GelenHevaleEditDto
{
    public int       Id      { get; set; }
    public DateTime? Tarix   { get; set; }
    public string?   Saa     { get; set; }
    public string?   HesNom  { get; set; }
    public string?   TipRes  { get; set; }
    public decimal?  Mebleg  { get; set; }
    public string?   ValTip  { get; set; }
    public string?   MenOlke { get; set; }
    public string?   GelOlke { get; set; }
    public string?   AlBank  { get; set; }
    public string?   HevTip  { get; set; }
    public string?   GonTip  { get; set; }
    public string?   DecNom  { get; set; }
    // Göstərmə üçün (redaktə olunmur)
    public string?   HevNom         { get; set; }
    public string?   MovcudFaylYolu { get; set; }
    public int?      YaradanId      { get; set; }
}
