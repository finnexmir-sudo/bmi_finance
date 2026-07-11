namespace FinNex.Application.DTOs.Emeliyyat;

// Siyahı üçün
public class KocurmeListDto
{
    public int       Id            { get; set; }
    public string?   HevaleNo      { get; set; }
    public DateTime? Tarix         { get; set; }
    public string?   GonderenTamAd { get; set; }   // ad soyad ata (birləşdirilmiş)
    public string?   AlanTamAd     { get; set; }
    public decimal?  Mebleg        { get; set; }
    public string?   KocurulenValyuta { get; set; }
    public string?   BankAd        { get; set; }
    public short?    Icra          { get; set; }
    public string?   IcraciAd      { get; set; }
    public int?      YaradanId     { get; set; }
}

// Yaratma / redaktə üçün ortaq sahələr
public class KocurmeFormDto
{
    public DateTime? Tarix { get; set; }

    public string?   GonderenAd       { get; set; }
    public string?   GonderenSoyad    { get; set; }
    public string?   GonderenAtaAd    { get; set; }
    public string?   GonderenPassport { get; set; }
    public string?   GonderenTelefon  { get; set; }

    public string?   AlanAd       { get; set; }
    public string?   AlanSoyad    { get; set; }
    public string?   AlanAtaAd    { get; set; }
    public string?   AlanPassport { get; set; }
    public string?   AlanTelefon  { get; set; }

    public decimal?  Mebleg           { get; set; }
    public decimal?  RialCbar         { get; set; }
    public decimal?  ValyutaCbar      { get; set; }
    public decimal?  IranRial         { get; set; }
    public string?   MedaxilValyuta   { get; set; }
    public string?   KocurulenValyuta { get; set; }

    public string?   Secim { get; set; }

    public string?   BankAd    { get; set; }
    public string?   Filial    { get; set; }
    public string?   AlanHesab { get; set; }

    public string?   Elave  { get; set; }
    public string?   Meqsed { get; set; }
    public string?   Qeyd   { get; set; }
}

public class KocurmeCreateDto : KocurmeFormDto { }

public class KocurmeEditDto : KocurmeFormDto
{
    public int     Id        { get; set; }
    // Göstərmə üçün (redaktə olunmur)
    public string? HevaleNo  { get; set; }
    public int?    YaradanId { get; set; }
}
