namespace FinNex.Domain.Entities.HR
{
    public class HesabatKateqoriyasi : BaseEntity
    {
        public string Ad { get; set; } = null!;
        public bool Aktivdir { get; set; } = true;

        public ICollection<HesabatSablonu> Sablonlar { get; set; } = new List<HesabatSablonu>();
    }
}
