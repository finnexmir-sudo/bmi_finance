namespace FinNex.Application.DTOs.Muhasibat;

// Mənfəət / Zərər (P&L) tab — tarix aralığında gəlir/xərc.
// Mənbə: arh_saldo_ls dövriyyəsi. Gəlir hesabları sinif 6/7 (kredit dövriyyəsi),
// xərc hesabları sinif 8 (debet dövriyyəsi). Hesablar hər gün mənfəətə bağlanır
// (debet=kredit), ona görə dövr üzrə: gəlir = SUM(kredit), xərc = SUM(debet).
// Doğrulama: (gəlir − xərc) ≈ 50130 "cari ilin mənfəəti" (GL).
public class MuhasibatMenfeetDto
{
    public DateTime BasTarix { get; set; }
    public DateTime SonTarix { get; set; }
    public bool     Ugurlu   { get; set; }
    public string?  Xeta     { get; set; }

    public decimal  FaizGeliri       { get; set; }   // faiz gəliri (60/61/63/64/65)
    public decimal  FaizXerci        { get; set; }   // faiz xərci (81/82/84/85)
    public decimal  XalisFaizGeliri  { get; set; }   // NII = faiz gəliri − faiz xərci
    public decimal  Nim              { get; set; }   // xalis faiz marjası % (illik, təxmini)

    public decimal  UmumiGelir       { get; set; }   // bütün gəlir
    public decimal  UmumiXerc        { get; set; }   // bütün xərc
    public decimal  XalisMenfeet     { get; set; }   // gəlir − xərc (hesablanmış)
    public decimal  EhtiyatXerci     { get; set; }   // provision (89)
    public decimal  XercGelirNisbeti { get; set; }   // Cost/Income % = xərc / gəlir

    public decimal  IsleyenAktiv     { get; set; }   // NIM məxrəci (faiz gətirən aktiv, SON tarixə)
    public decimal  MenfeetGL        { get; set; }   // 50130 GL mənfəəti (yoxlama üçün)
    public decimal  Ferq             { get; set; }   // hesablanmış − GL (reconciliation)

    public List<BalansMaddeDto> GelirBolgusu { get; set; } = new();  // gəlir strukturu
    public List<BalansMaddeDto> XercBolgusu  { get; set; } = new();  // xərc strukturu
}
