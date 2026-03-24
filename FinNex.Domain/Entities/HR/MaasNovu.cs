namespace FinNex.Domain.Entities.HR
{
    // Maaşın növlərini idarə edən kitabça
    public class MaasNovu : BaseEntity
    {
        public string Ad { get; set; } = null!;
        public bool Gelirdir { get; set; } // true (+), false (-)
        public bool Aktivdir { get; set; } = true;

        public ICollection<MaasDetay> MaasDetallari { get; set; } = new List<MaasDetay>();
    }
}