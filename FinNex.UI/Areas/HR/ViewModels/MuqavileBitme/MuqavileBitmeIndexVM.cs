using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinNex.UI.Areas.HR.ViewModels.MuqavileBitme
{
    public class MuqavileBitmeIndexVM
    {
        public List<MuqavileBitmeRowVM> Rows { get; set; } = new();

        public int     Gun           { get; set; } = 30;
        public int?    DepartamentId { get; set; }
        public string? Search        { get; set; }

        public List<SelectListItem> Departamentler { get; set; } = new();

        public int CemiSay    => Rows.Count;
        public int TecibiSay  => Rows.Count(r => r.QalanGun <= 7);
        public int DiqqetSay  => Rows.Count(r => r.QalanGun > 7 && r.QalanGun <= 30);
        public int KecmisKimi => Rows.Count(r => r.QalanGun < 0);
    }

    public class MuqavileBitmeRowVM
    {
        public int      IsciId        { get; set; }
        public int      TeyinatId     { get; set; }
        public int      DepartamentId { get; set; }
        public string   Ad            { get; set; } = "";
        public string   Soyad         { get; set; } = "";
        public string?  AtaAdi        { get; set; }
        public string   FIN           { get; set; } = "";
        public string   DepartamentAd { get; set; } = "—";
        public string   VezifeAd      { get; set; } = "—";
        public DateTime IsheQebulTarixi { get; set; }
        public DateTime BitmeTarixi   { get; set; }
        public int      QalanGun      { get; set; }

        public string TamAd => $"{Ad} {Soyad} {AtaAdi}".Trim();

        public string Tecililik =>
            QalanGun < 0  ? "kecmis" :
            QalanGun <= 7  ? "qirmizi" :
            QalanGun <= 30 ? "sari"    : "normal";
    }
}
