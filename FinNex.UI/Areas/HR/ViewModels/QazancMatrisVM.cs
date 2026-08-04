namespace FinNex.UI.Areas.HR.ViewModels
{
    /// <summary>
    /// Aylıq Qazanc Matrisi — il üzrə işçi × 12 ay cədvəli.
    /// Mühasibin Excel cədvəli ilə üz-üzə müqayisə və yerində düzəliş üçün.
    /// </summary>
    public class QazancMatrisVM
    {
        public int Il { get; set; }
        public List<QazancMatrisSetirVM> Setirler { get; set; } = new();
    }

    public class QazancMatrisSetirVM
    {
        public int IsciId { get; set; }
        public string AdSoyad { get; set; } = "";
        public bool Aktiv { get; set; }

        // Ay (1..12) → dəyər; qeyd olmayan ay xəritədə yoxdur
        public Dictionary<int, QazancHucreVM> Aylar { get; set; } = new();

        public decimal Cemi => Aylar.Values.Sum(x => x.Qazanc);
    }

    public class QazancHucreVM
    {
        public decimal Qazanc { get; set; }
        public bool ElIle { get; set; }
        public string? Qeyd { get; set; }
    }
}
