namespace FinNex.Domain.Entities.HR
{
    public class IsciJetonu : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        public int JetonTeyinatiId { get; set; }
        public JetonTeyinati JetonTeyinati { get; set; } = null!;

        public DateTime QazanmaTarixi { get; set; } = DateTime.Now;
        public string Sebeb { get; set; } = null!;
        public int VerenUserId { get; set; }

        public IsciJetonuStatus Status { get; set; } = IsciJetonuStatus.Aktiv;

        // null = tam dəyər qalıb; dəyər varsa o qədər saat istifadə olunmayıb
        public decimal? QalanSaat { get; set; }

        public int? RedimTelebiId { get; set; }
        public JetonRedimTelebi? RedimTelebi { get; set; }

        // İcazə-based FIFO xərclənməsinin izlənməsi
        public int? IcazeId { get; set; }
        public Icaze? Icaze { get; set; }
    }
}
