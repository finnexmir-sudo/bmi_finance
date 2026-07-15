namespace FinNex.Application.DTOs.Muhasibat;

// Valyuta əməliyyatları tab — tarix aralığında alış/satış.
// Mənbə: arh_dd (alış: debet 10060 ← kredit 10050; satış: debet 10050 ← kredit 10060).
// summa_v_inval = valyuta məbləği, ×kurs_valuti = AZN qarşılığı.
public class MuhasibatValyutaDto
{
    public DateTime BasTarix { get; set; }
    public DateTime SonTarix { get; set; }
    public bool     Ugurlu   { get; set; }
    public string?  Xeta     { get; set; }

    public decimal  AlisAzn    { get; set; }   // ümumi alış (AZN)
    public decimal  SatisAzn   { get; set; }   // ümumi satış (AZN)
    public decimal  Xalis      { get; set; }   // satış − alış (AZN)
    public int      EmeliyyatSayi { get; set; }

    public List<ValyutaSetirDto> Setirler { get; set; } = new();
}

public class ValyutaSetirDto
{
    public string  Valyuta       { get; set; } = "";
    public decimal AlisHecm      { get; set; }   // valyuta ilə
    public decimal AlisAzn       { get; set; }
    public decimal SatisHecm     { get; set; }
    public decimal SatisAzn      { get; set; }
    public decimal OrtaAlisKurs  { get; set; }
    public decimal OrtaSatisKurs { get; set; }
    public decimal Spred         { get; set; }   // orta satış − orta alış kursu
    public decimal AcigMovqe     { get; set; }   // alış həcm − satış həcm (valyuta)
}
