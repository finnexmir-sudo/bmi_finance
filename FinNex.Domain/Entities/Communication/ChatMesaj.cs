using FinNex.Domain.Entities.HR;

namespace FinNex.Domain.Entities.Communication
{
    public class ChatMesaj : BaseEntity
    {
        public int GonderenIsciId { get; set; }
        public Isci GonderenIsci { get; set; } = null!;

        public int AlanIsciId { get; set; }
        public Isci AlanIsci { get; set; } = null!;

        public string Metn { get; set; } = null!;
        public bool Oxunub { get; set; }
        public DateTime GonderilmeTarixi { get; set; } = DateTime.Now;
        public DateTime? OxunmaTarixi { get; set; }

        public Guid? TopluMesajGrupId { get; set; }

        // Fayl əlavəsi
        public string? FaylAdi { get; set; }
        public string? FaylYolu { get; set; }
        public long? FaylOlcusu { get; set; }
        public string? FaylTipi { get; set; }
    }
}
