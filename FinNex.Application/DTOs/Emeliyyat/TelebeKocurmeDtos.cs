namespace FinNex.Application.DTOs.Emeliyyat;

// Siyahı üçün
public class TelebeKocurmeListDto
{
    public int       Id        { get; set; }
    public string?   HevaleNo  { get; set; }
    public DateTime? Tarix     { get; set; }
    public string?   Adi       { get; set; }
    public string?   UniAd     { get; set; }
    public decimal?  Mebleg    { get; set; }
    public decimal?  Komissiya { get; set; }
    public string?   AlanBank  { get; set; }
    public short?    Icra      { get; set; }
    public string?   IcraciAd  { get; set; }
    public int?      YaradanId { get; set; }
}

// Yaratma / redaktə üçün ortaq sahələr
public class TelebeKocurmeFormDto
{
    public DateTime? Tarix       { get; set; }
    public string?   HevaleNo    { get; set; }
    public string?   Adi         { get; set; }
    public string?   Passport    { get; set; }
    public decimal?  Mebleg      { get; set; }
    public string?   BmiFilial   { get; set; }
    public string?   RefNo       { get; set; }
    public string?   UniAd       { get; set; }
    public string?   AlanBank    { get; set; }
    public string?   TelebeKursu { get; set; }
    public decimal?  XH          { get; set; }
    public decimal?  Kurs        { get; set; }
    // Hesablar (standart, dəyişdirilə bilər)
    public string?   Hes35025    { get; set; }
    public string?   Hes45023    { get; set; }
    public string?   Hes45011    { get; set; }
    public string?   Hes67013    { get; set; }
}

public class TelebeKocurmeCreateDto : TelebeKocurmeFormDto { }

public class TelebeKocurmeEditDto : TelebeKocurmeFormDto
{
    public int     Id        { get; set; }
    public int?    YaradanId { get; set; }
}

// Muhasibat sətri (debet/kredit) — hesablanmış
public class MuhasibatSetirDto
{
    public string?  Debet   { get; set; }
    public string?  Kredit  { get; set; }
    public decimal  Mebleg  { get; set; }
    public string?  Teyinat { get; set; }
}

// Detal — qeyd + hesablanmış 3 sətir
public class TelebeKocurmeDetalDto
{
    public int       Id        { get; set; }
    public string?   HevaleNo  { get; set; }
    public DateTime? Tarix     { get; set; }
    public string?   Adi       { get; set; }
    public string?   Passport  { get; set; }
    public string?   UniAd     { get; set; }
    public string?   AlanBank  { get; set; }
    public string?   BmiFilial { get; set; }
    public string?   RefNo     { get; set; }
    public string?   TelebeKursu { get; set; }
    public decimal?  Mebleg    { get; set; }
    public decimal?  Kurs      { get; set; }
    public decimal?  XH        { get; set; }
    public decimal?  Komissiya { get; set; }
    public int?      YaradanId { get; set; }
    public IList<MuhasibatSetirDto> Setirler { get; set; } = new List<MuhasibatSetirDto>();
}
