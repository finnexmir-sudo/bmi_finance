using FinNex.Domain.Entities.HR;

namespace FinNex.Domain.Entities.Communication
{
    public class Elan : BaseEntity
    {
        public string Bashliq { get; set; } = null!;
        public string Metn { get; set; } = null!;

        public int GonderenIsciId { get; set; }
        public Isci GonderenIsci { get; set; } = null!;

        public bool Vacibdir { get; set; }
        public DateTime? BitirmeTarixi { get; set; }

        public bool Aktivdir { get; set; } = true;

        // Şəkil (banner/cover)
        public string? SekilYolu { get; set; }

        // Sənəd əlavəsi
        public string? FaylAdi { get; set; }
        public string? FaylYolu { get; set; }
        public string? FaylTipi { get; set; }
        public long? FaylOlcusu { get; set; }
    }
}
