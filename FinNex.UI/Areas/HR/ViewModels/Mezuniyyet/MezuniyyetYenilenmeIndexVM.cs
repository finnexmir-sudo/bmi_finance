using FinNex.Domain.Entities.HR;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinNex.UI.Areas.HR.ViewModels.Mezuniyyet
{
    public class MezuniyyetYenilenmeIndexVM
    {
        public List<MezuniyyetYenilenmeRowVM> Rows { get; set; } = new();

        // Filter state
        public int      Gun           { get; set; } = 30;
        public int?     DepartamentId { get; set; }
        public string?  Search        { get; set; }

        public List<SelectListItem> Departamentler { get; set; } = new();

        // Statistika
        public int CemiSay        => Rows.Count;
        public int TecibiSay      => Rows.Count(r => r.QalanGun <= 7);
        public int OrtaTecili     => Rows.Count(r => r.QalanGun > 7 && r.QalanGun <= 14);
        public int BalansYoxSay   => Rows.Count(r => !r.CariIldeBalansVar);
    }

    public class MezuniyyetYenilenmeRowVM
    {
        public int      IsciId          { get; set; }
        public string   Ad              { get; set; } = "";
        public string   Soyad           { get; set; } = "";
        public string?  AtaAdi          { get; set; }
        public string   FIN             { get; set; } = "";
        public string?  SeriyaNomre     { get; set; }
        public DateTime DogumTarixi     { get; set; }
        public Cins     Cins            { get; set; }
        public string?  Telefon         { get; set; }
        public string?  Email           { get; set; }
        public string?  Unvan           { get; set; }
        public DateTime IsheQebulTarixi { get; set; }

        // Computed
        public DateTime NextRenewDate   { get; set; }
        public int      QalanGun        { get; set; }
        public int      IslediyiIl      { get; set; }

        // Teyinat (aktiv)
        public int      DepartamentId   { get; set; }
        public string   DepartamentAd   { get; set; } = "—";
        public string   VezifeAd        { get; set; } = "—";

        // Maliyye
        public decimal? CariMaas        { get; set; }
        public string?  Iban            { get; set; }

        // Cari il balansy
        public bool     CariIldeBalansVar  { get; set; }
        public int?     CariIldeToplamGun  { get; set; }

        // Yardımcı göstəricilər
        public string TamAd => $"{Ad} {Soyad} {AtaAdi}".Trim();
        public string CinsAd => Cins == Cins.Kisi ? "Kişi" : "Qadın";
        public string Tecililik =>
            QalanGun <= 7  ? "qirmizi" :
            QalanGun <= 14 ? "sari"    :
            QalanGun <= 30 ? "mavi"    : "normal";
    }
}
