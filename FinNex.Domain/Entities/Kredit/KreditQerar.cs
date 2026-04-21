using FinNex.Domain.Entities.HR;

namespace FinNex.Domain.Entities.Kredit
{
    /// <summary>
    /// Komitə yığıncağında verilmiş qərar. Protokol fiziki imzalanır və Word/PDF
    /// formasında bura əlavə olunur. Hər müraciət üçün bir aktiv qərar olur
    /// (lakin dəyişdirmə olarsa tarixçə saxlanmaq üçün soft-delete istifadə edilir).
    /// İmzalayan üzvlər KreditQerarImza cədvəlində saxlanılır.
    /// </summary>
    public class KreditQerar : BaseEntity
    {
        public int KreditMuracietId { get; set; }
        public KreditMuraciet KreditMuraciet { get; set; } = null!;

        public KreditQerarTipi Qerar { get; set; }

        public string ProtokolNo { get; set; } = null!;
        public DateTime QerarTarixi { get; set; }

        // Protokol faylı (Word/PDF skan) — wwwroot/Files/Kredit/Qerarlar/ altında saxlanır
        public string? ProtokolFaylAdi { get; set; }
        public string? ProtokolFaylYolu { get; set; }

        // Təsdiqlənmiş şərtlər (yalnız Qerar = Tesdiqlenib olanda dolur)
        public decimal? TesdiqMebleg { get; set; }
        public string? TesdiqMuddet { get; set; }
        public decimal? FaizDerecesi { get; set; }
        public string? Teminat { get; set; }

        // Ümumi qeyd (rədd səbəbi və ya təsdiq şərtləri izahı)
        public string? Qeyd { get; set; }

        // Qərarı sistemə daxil edən işçi (adətən katib)
        public int DaxilEdenIsciId { get; set; }
        public Isci DaxilEdenIsci { get; set; } = null!;

        public ICollection<KreditQerarImza> Imzalar { get; set; } = new List<KreditQerarImza>();
    }

    public enum KreditQerarTipi
    {
        Tesdiqlenib = 1,
        ReddEdilib = 2
    }
}
