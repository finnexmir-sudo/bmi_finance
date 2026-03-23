namespace FinNex.Domain.Entities.HR
{
    public class Permission : BaseEntity
    {
        public string Kod { get; set; } = null!;
        public string Ad { get; set; } = null!;
        public string? Aciqlama { get; set; }
    }
}