namespace FinNex.Domain.Entities.SenedDovriyyesi
{
    public class Tag : BaseEntity
    {
        public string Ad { get; set; } = null!;

        public ICollection<SenedTagMap> SenedTagMaps { get; set; } = new List<SenedTagMap>();
    }

}
