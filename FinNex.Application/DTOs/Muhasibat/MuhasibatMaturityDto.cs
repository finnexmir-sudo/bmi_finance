namespace FinNex.Application.DTOs.Muhasibat;

// Kredit Pul Axını (Maturity Ladder) — gözlənilən kredit ödənişləri (əsas + faiz)
// müddət qutularında. Mənbə: graphpogkre (ödəniş qrafiki), date_pog üzrə buckets.
// Qeyd: bu bankda depozitlər müddətsiz/tələblidir (date_close_licsch yoxdur), ona
// görə klassik aktiv/öhdəlik GAP-ı deyil — bu, AKTİV tərəf pul axını proqnozudur.
public class MuhasibatMaturityDto
{
    public DateTime Tarix   { get; set; }
    public bool     Ugurlu  { get; set; }
    public string?  Xeta    { get; set; }

    public List<MaturityQutuDto> Qutular { get; set; } = new();

    public decimal EsasCem  { get; set; }   // gələcək əsas cəmi
    public decimal FaizCem  { get; set; }   // gələcək faiz cəmi
    public decimal CemAxin  { get; set; }   // əsas + faiz
    public decimal Axin1Ay  { get; set; }   // növbəti 1 ayda gözlənilən (əsas+faiz)
    public decimal Axin3Ay  { get; set; }   // növbəti 3 ay (kumulyativ)
    public decimal Axin12Ay { get; set; }   // növbəti 12 ay (kumulyativ)

    // Kontekst — tələbli depozit bazası (öhdəlik, müddətsiz) + likvid tampon.
    public decimal TelebliDepozit { get; set; }
    public decimal Hqla           { get; set; }
}

public class MaturityQutuDto
{
    public string  Ad         { get; set; } = "";   // "0–1 ay" ...
    public decimal Esas       { get; set; }
    public decimal Faiz       { get; set; }
    public decimal Cem        { get; set; }          // əsas + faiz
    public decimal Kumulyativ { get; set; }          // bu qutuya qədər cəmi axın
    public decimal Faiz_Pay   { get; set; }          // bu qutunun ümumi axında payı %
}
