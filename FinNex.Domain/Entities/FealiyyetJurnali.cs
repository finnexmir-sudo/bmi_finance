using FinNex.Domain.Entities.Structure;

namespace FinNex.Domain.Entities
{
    // Baza səviyyəsindəki bütün mühüm dəyişikliklərin jurnalı.
    // BaseEntity-dən MIRAS ALMIR — özü loq olunmasın deyə.
    public class FealiyyetJurnali
    {
        public int Id { get; set; }

        public int? UserId { get; set; }
        public AppUser? User { get; set; }

        // "C" = Yaratdı, "U" = Yenilədi, "D" = Sildi
        public string Emeliyyat { get; set; } = null!;

        public string CedvelAdi { get; set; } = null!;      // Entity type adı
        public string CedvelFarsi { get; set; } = null!;    // Azərbaycanca ad

        public int RecordId { get; set; }
        public string? Acıqlama { get; set; }

        public DateTime Tarix { get; set; } = DateTime.Now;
    }
}
