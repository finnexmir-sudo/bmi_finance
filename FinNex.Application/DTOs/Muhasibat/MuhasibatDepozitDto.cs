namespace FinNex.Application.DTOs.Muhasibat;

// Depozitlər tab — bir tarixə depozit portfeli.
// Mənbə: odb.arh_saldo_ls (saldo_ish_nacval AZN, passiv → mənfi, servisdə çevrilir).
// Hüquqi: 40 / 3x (texniki 35020/25/26/40 istisna); Fiziki: 41.
public class MuhasibatDepozitDto
{
    public DateTime Tarix   { get; set; }
    public bool     Ugurlu  { get; set; }
    public string?  Xeta    { get; set; }

    public decimal  UmumiPortfel { get; set; }
    public decimal  HuquqiCem    { get; set; }
    public decimal  FizikiCem    { get; set; }
    public decimal  SahibkarCem  { get; set; }   // fərdi sahibkar (regnom.predprinimatel=1)
    public int      MusteriSayi  { get; set; }

    // Konsentrasiya (risk): ən böyük depozitorların portfeldəki payı
    public decimal  Top10Pay     { get; set; }
    public decimal  Top20Pay     { get; set; }

    // Əlaqəli tərəf (normativ) — şirkət + təsisçi/imza sahibi (işçi hələ yox)
    public decimal  ElaqeliDepozit     { get; set; }
    public decimal  ElaqeliPortfel     { get; set; }   // top_qal (35-49)
    public decimal  ElaqeliXususiCeki  { get; set; }   // %

    public List<BalansMaddeDto> TipBolgusu     { get; set; } = new();   // Hüquqi / Fiziki
    public List<BalansMaddeDto> ValyutaBolgusu { get; set; } = new();   // AZN / USD / Digər
    public List<BalansMaddeDto> TopHuquqi      { get; set; } = new();   // ən böyük 10 hüquqi
    public List<BalansMaddeDto> TopFiziki      { get; set; } = new();   // ən böyük 10 fiziki
    public List<BalansMaddeDto> TopSahibkar    { get; set; } = new();   // ən böyük 10 sahibkar
}
