using FinNex.Domain.Entities.HR;

namespace FinNex.Domain.Entities.Marif
{
    /// <summary>Bir sual dəstinin bir işçiyə göndərilməsi.</summary>
    public class MarifTesyinat : BaseEntity
    {
        public int SualSetiId { get; set; }
        public MarifSualSeti SualSeti { get; set; } = null!;

        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        public DateTime GonderilmeTarixi { get; set; } = DateTime.Now;

        public bool TamamlandiMi { get; set; }
        public DateTime? TamamlanmaTarixi { get; set; }

        public ICollection<MarifCavab> Cavablar { get; set; } = new List<MarifCavab>();
    }
}
