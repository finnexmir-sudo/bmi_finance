namespace FinNex.Application.DTOs.Muhasibat;

// Likvidlik tab (v1) — likvid aktivlər + sadə likvidlik nisbətləri.
// Mənbə: odb.arh_saldo_ls (balans qalıqları). Tam Basel LCR (haircut + net outflow)
// sonrakı addımda; bu v1 sürətli likvidlik şəklidir.
public class MuhasibatLikvidlikDto
{
    public DateTime Tarix   { get; set; }
    public bool     Ugurlu  { get; set; }
    public string?  Xeta    { get; set; }

    public decimal  LikvidAktiv    { get; set; }   // HQLA-tipli likvid aktivlər (AZN)
    public decimal  UmumiOhdelik   { get; set; }   // ümumi öhdəlik
    public decimal  AniLikvidlik   { get; set; }   // likvid aktiv / öhdəlik, %
    public decimal  LikvidAktivPay { get; set; }   // likvid aktiv / ümumi aktiv, %

    public List<BalansMaddeDto> LikvidStruktur { get; set; } = new();   // qruplar
    public List<BalansMaddeDto> ValyutaBolgusu { get; set; } = new();   // likvid aktivlərin valyutası

    // Təxmini LCR (Basel-vari) — fərziyyələr: Level 2 (15020/25) haircut 25%;
    // xalis məxaric = fiziki depozit×10% + hüquqi depozit×40%. Regulyativ LCR.cs deyil.
    public decimal  Hqla          { get; set; }   // haircut tətbiq olunmuş likvid aktiv
    public decimal  FizikiDepozit { get; set; }
    public decimal  HuquqiDepozit { get; set; }
    public decimal  XalisMexaric  { get; set; }   // təxmini 30 günlük net outflow
    public decimal  Lcr           { get; set; }   // HQLA / xalis məxaric, %
}
