using FinNex.Domain.Entities.HR;

namespace FinNex.Domain.Entities.Communication
{
    /// <summary>
    /// Paylaşımın hədəf işçisi (27.10.2026). Bir `Melumat`-ın HEÇ bir `MelumatHedef`
    /// sətri yoxdursa — "Hamısına" deməkdir (defolt, geriyə uyğundur — əvvəlki
    /// paylaşımların hədəfi yox idi, hamısı görünməyə davam edir). Sətirlər varsa —
    /// YALNIZ o işçilərə görünür.
    /// </summary>
    public class MelumatHedef : BaseEntity
    {
        public int MelumatId { get; set; }
        public Melumat Melumat { get; set; } = null!;

        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;
    }
}
