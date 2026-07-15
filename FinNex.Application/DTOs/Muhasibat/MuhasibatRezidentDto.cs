namespace FinNex.Application.DTOs.Muhasibat;

// Rezident / Qeyri-rezident tab — bir tarixə hesab qalıqlarının rezidentlik bölgüsü.
// Mənbə: odb.arh_saldo_ls (ABS qalıq). Təsnifat frm_reziden_ve_qeyri_rezident məntiqi:
// qr = (409* və adın son mötərizəsinin 5-ci simvolu '5') və ya 45029*; əks halda r.
public class MuhasibatRezidentDto
{
    public DateTime Tarix   { get; set; }
    public bool     Ugurlu  { get; set; }
    public string?  Xeta    { get; set; }

    public decimal  Rezident         { get; set; }
    public decimal  QeyriRezident    { get; set; }
    public decimal  Umumi            { get; set; }
    public decimal  QeyriRezidentPay { get; set; }   // %
    public int      RezidentSay      { get; set; }
    public int      QeyriRezidentSay { get; set; }
}
