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

// Yaratmaq üçün (Icra avtomatik; HevNom 2026-da ƏL İLƏ yazılır)
public class GelenHevaleCreateDto
{
    // HƏVALƏ № — 2026-cı ildə istifadəçi ÖZÜ yazır.
    //
    // BMI-də bu jurnalın nömrəsi `{VV}{Y}{NNN}` formasındadır (məs. 046001 =
    // 04 valyuta + 6 ilin son rəqəmi + 001 sıra) və 10 596 sətrin hamısı belədir.
    // Nömrə bu günə qədər kağız jurnaldan götürülüb əl ilə yazılırdı.
    // 2027-dən avtomatlaşdırılacaq: `{VV}{YY}{NNN}`, valyuta kodu kurval
    // (SOKNAMEVALUT) siyahısından — o vaxta qədər əl ilə davam edir.
    public string?   HevNom  { get; set; }

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

// Redaktə üçün (HevNom da dəyişdirilə bilər — əl ilə yazıldığı üçün səhv olarsa
// düzəldilməlidir; dublikat yoxlaması servisdədir)
public class GelenHevaleEditDto
{
    public int       Id      { get; set; }
    public string?   HevNom  { get; set; }
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
    public string?   MovcudFaylYolu { get; set; }
    public int?      YaradanId      { get; set; }
}
