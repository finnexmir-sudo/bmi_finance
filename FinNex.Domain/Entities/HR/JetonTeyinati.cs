namespace FinNex.Domain.Entities.HR
{
    public class JetonTeyinati : BaseEntity
    {
        public string Ad { get; set; } = null!;
        public JetonNovu Nov { get; set; }
        public JetonRengi Rengi { get; set; }
        public decimal SaatDeyeri { get; set; }
        public JetonVahid Vahid { get; set; } = JetonVahid.Saat;
        public string Ikon { get; set; } = null!;
        public string RengKodu { get; set; } = null!;
        public string? Tesvir { get; set; }
        public bool BirbasaOdenishli { get; set; } = false;
        public bool Aktivdir { get; set; } = true;

        // Sistem tərəfindən idarə olunan tip (məs. "36 Saat Hüququ {il}") —
        // JetonService.EnsureIllikHuquqTeyinatiAsync özü yaradır. HR-in adi
        // jeton-növü kataloqunda GÖRÜNMÜR (JetonTeyinatlariGetirAsync bunu süzür)
        // və normal FIFO-xərcləmə/redim axınları bunu görməzdən gəlir — yalnız
        // Qara Jeton kəsintisi və Dashboard-un illik limiti ona toxunur.
        public bool Sistemli { get; set; } = false;

        public ICollection<IsciJetonu> IsciJetonlari { get; set; } = new List<IsciJetonu>();
    }
}
