namespace FinNex.Domain.Entities.HR
{
    public class MaasParametri : BaseEntity
    {
        public MaasParametrNovu Nov { get; set; }
        public MaasParametrTipi Tip { get; set; }

        public decimal Deyer { get; set; }
        public string? Aciqlama { get; set; }

        public DateTime BaslamaTarixi { get; set; }
        public DateTime? BitmeTarixi { get; set; }

        public bool Aktivdir { get; set; } = true;
    }
}