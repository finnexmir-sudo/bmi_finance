namespace FinNex.Domain.Entities.HR
{
    /// <summary>
    /// İşçinin uşağı — Ə.M. 117 əlavə məzuniyyəti üçün. Yaş DOĞUM TARİXİNDƏN avtomatik
    /// hesablanır (14 yaş / əlil uşaq üçün 18 yaş), HR əl ilə sayı yeniləmir.
    /// </summary>
    public class IsciUsaq : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        public string? Ad { get; set; }
        public DateTime DogumTarixi { get; set; }

        /// <summary>Bu uşaq əlildir (18 yaşınadək → M.117 üzrə +5).</summary>
        public bool Elillidir { get; set; }
    }
}
