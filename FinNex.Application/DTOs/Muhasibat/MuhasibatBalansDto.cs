namespace FinNex.Application.DTOs.Muhasibat;

// Balans İcmalı — bir tarixə bankın aktiv / öhdəlik / kapital şəkli.
// Mənbə: Oracle odb.arh_saldo_ls (saldo_ish_nacval ARTIQ AZN-dədir — nacval = milli valyuta).
// Təsnifat hesab kodunun ilk rəqəmi üzrə (dashboard səviyyəsi) — tənzimləyici
// Row 6-70 detalı deyil; real Daily Report ilə tutuşdurulub dəqiqləşdirilə bilər.
public class MuhasibatBalansDto
{
    public DateTime Tarix   { get; set; }
    public bool     Ugurlu  { get; set; }
    public string?  Xeta    { get; set; }

    public decimal  UmumiAktiv   { get; set; }
    public decimal  UmumiOhdelik { get; set; }
    public decimal  Kapital      { get; set; }
    public decimal  Tesnifsiz    { get; set; }   // ilk rəqəmi tanınmayan qalıq (heç nə itməsin)

    // Balans eyniliyi: Aktiv = Öhdəlik + Kapital. Fərq sıfıra yaxın olmalıdır.
    public decimal  BalansFerqi => UmumiAktiv - (UmumiOhdelik + Kapital + Tesnifsiz);

    public List<BalansMaddeDto> Aktivler       { get; set; } = new();
    public List<BalansMaddeDto> Ohdelikler     { get; set; } = new();
    public List<BalansMaddeDto> ValyutaBolgusu { get; set; } = new();  // aktivlərin valyuta strukturu
}

public class BalansMaddeDto
{
    public string  Ad     { get; set; } = "";
    public decimal Mebleg { get; set; }
    public decimal Faiz   { get; set; }   // öz tərəfindəki (aktiv/öhdəlik) pay, %
}
