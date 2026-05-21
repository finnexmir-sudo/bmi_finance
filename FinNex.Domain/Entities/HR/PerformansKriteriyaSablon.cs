namespace FinNex.Domain.Entities.HR
{
    public class PerformansKriteriyaSablon : BaseEntity
    {
        public string Ad { get; set; } = "";
        public decimal Ceki { get; set; }
        public KampaniyaTipi KampaniyaTipi { get; set; }
        public bool Aktiv { get; set; } = true;
        public int Sira { get; set; }
    }
}
