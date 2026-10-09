using FinNex.Domain.Entities.HR;

namespace FinNex.Domain.Entities.Kassa
{
    /// <summary>
    /// Kassa valyuta məzənnəsini təsdiqləyə/imtina edə bilən işçilərin siyahısı.
    /// Admin tərəfindən təyin olunur (KreditBaxanIsci ilə eyni naxış) — rol
    /// yoxdur, siyahıdakı istənilən AKTİV işçi tək başına təsdiqləyə/imtina
    /// edə bilər (istifadəçi qərarı, 09.10.2026: "kassa yazır, rəhbər
    /// təsdiqləyir və ya imtina verir — belə işləyirdi sistem").
    /// </summary>
    public class KassaTesdiqEdici : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        public DateTime AktivdirFrom { get; set; }
        public DateTime? AktivdirTo { get; set; }
        public string? Qeyd { get; set; }
    }
}
