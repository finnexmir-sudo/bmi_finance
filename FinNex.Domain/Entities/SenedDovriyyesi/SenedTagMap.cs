namespace FinNex.Domain.Entities.SenedDovriyyesi
{
    public class SenedTagMap : BaseEntity
    {
        public int SenedId { get; set; }
        public Sened? Sened { get; set; }

        public int TagId { get; set; }
        public Tag? Tag { get; set; }
    }

}
