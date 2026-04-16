namespace FinNex.Domain.Entities.HR
{
    /// <summary>
    /// İşçi üçün təyin olunmuş güzəşt (junction / assignment).
    /// Hesablamada yalnız BaslamaTarixi ≤ hesabTarixi ≤ BitmeTarixi (və ya null) olan
    /// aktiv qeydlər nəzərə alınır. Bir neçə aktiv qeyd varsa, ən böyük Mebleg götürülür.
    /// </summary>
    public class IsciGuzest : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        public int GuzestId { get; set; }
        public Guzest Guzest { get; set; } = null!;

        public DateTime BaslamaTarixi { get; set; }
        public DateTime? BitmeTarixi { get; set; } // null = müddətsiz
        public string? Qeyd { get; set; }
    }
}
