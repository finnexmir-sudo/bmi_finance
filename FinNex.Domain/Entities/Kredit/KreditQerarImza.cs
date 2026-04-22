using FinNex.Domain.Entities.HR;

namespace FinNex.Domain.Entities.Kredit
{
    /// <summary>
    /// Komitə qərarını imzalayan üzv. Bir qərara bir neçə imza düşür (5-7 üzv
    /// iştirak edir, protokolu imzalayır). Sonradan hansı üzvlərin imzası
    /// olduğunu tarixi hesabatlarda göstərmək üçün saxlanır.
    /// </summary>
    public class KreditQerarImza : BaseEntity
    {
        public int KreditQerarId { get; set; }
        public KreditQerar KreditQerar { get; set; } = null!;

        public int KomiteUzvuId { get; set; }
        public KomiteUzvu KomiteUzvu { get; set; } = null!;

        // Sənədsiz-də bəzi üzvlər iştirak etməyə bilər — qeyd etmək üçün
        public string? Qeyd { get; set; }
    }
}
