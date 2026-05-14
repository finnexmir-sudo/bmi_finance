namespace FinNex.Domain.Entities.HR
{
    public class Teklif : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        public TeklifNov Nov { get; set; }
        public TeklifPrioritet Prioritet { get; set; } = TeklifPrioritet.Orta;
        public TeklifStatus Status { get; set; } = TeklifStatus.Gozlemede;

        public string Bashliq { get; set; } = null!;
        public string Mezmun { get; set; } = null!;

        public string? Cavab { get; set; }
        public int? CavabVerenIsciId { get; set; }
        public Isci? CavabVeren { get; set; }
        public DateTime? CavabTarixi { get; set; }
    }

    public enum TeklifNov
    {
        Teklif = 0,
        BosluqBildirisi = 1,
        Sikayat = 2
    }

    public enum TeklifStatus
    {
        Gozlemede = 0,
        Baxilir = 1,
        Tamamlandi = 2,
        ReddEdildi = 3
    }

    public enum TeklifPrioritet
    {
        Asagi = 0,
        Orta = 1,
        Yuksek = 2
    }
}
