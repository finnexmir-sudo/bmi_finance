namespace FinNex.Domain.Entities.HR
{
    public class JetonRedimTelebi : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        public RedimNovu RedimNovu { get; set; }
        public decimal CemiSaat { get; set; }

        public RedimStatus Status { get; set; } = RedimStatus.Gozlenilir;

        public DateTime TelabTarixi { get; set; } = DateTime.Now;
        public DateTime? NeticeTarixi { get; set; }

        public int? TesdiqleyenUserId { get; set; }
        public string? Qeyd { get; set; }

        // Yalnız RedimNovu == Icaze olduqda doldurulur. HR final təsdiq
        // edəndə bu məlumatlardan avtomatik Icaze qeydi yaranır.
        public DateTime? IcazeTarixi { get; set; }
        public TimeSpan? BaslamaSaati { get; set; }
        public TimeSpan? BitisSaati { get; set; }

        // Rəhbər təsdiq mərhələsi (jeton səhifəsindən gələn sorğular üçün).
        // null = baxılmayıb, true = təsdiqləyib, false = rədd edib.
        // HR yalnız RehberTesdiq == true olduqda təsdiq edə bilər.
        public bool? RehberTesdiq { get; set; }
        public int? RehberUserId { get; set; }
        public DateTime? RehberTesdiqTarixi { get; set; }
        public string? RehberQeyd { get; set; }

        public ICollection<IsciJetonu> XerclenenJetonlar { get; set; } = new List<IsciJetonu>();
    }
}
