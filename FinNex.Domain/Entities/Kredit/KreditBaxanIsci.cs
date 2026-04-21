using FinNex.Domain.Entities.HR;

namespace FinNex.Domain.Entities.Kredit
{
    /// <summary>
    /// Kredit müraciətlərinə baxa bilən işçilərin siyahısı. Admin tərəfindən
    /// təyin olunur — yalnız burada olan işçilər müraciətlər bölməsinə girə,
    /// detal görüb "Yoxlanılır" / "Komitəyə göndər" statuslarını dəyişə bilər.
    /// AktivdirFrom/AktivdirTo ilə müvəqqəti təyinatlar da idarə oluna bilir.
    /// </summary>
    public class KreditBaxanIsci : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        public DateTime AktivdirFrom { get; set; }
        public DateTime? AktivdirTo { get; set; }
        public string? Qeyd { get; set; }
    }
}
