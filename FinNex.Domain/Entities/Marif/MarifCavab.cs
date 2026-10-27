namespace FinNex.Domain.Entities.Marif
{
    /// <summary>İşçinin bir suala verdiyi cavab (CoxSecimli üçün variant hərfi, Acig üçün sərbəst mətn).</summary>
    public class MarifCavab : BaseEntity
    {
        public int TesyinatId { get; set; }
        public MarifTesyinat Tesyinat { get; set; } = null!;

        public int SualId { get; set; }
        public MarifSual Sual { get; set; } = null!;

        public string IsciCavabi { get; set; } = string.Empty;

        /// <summary>CoxSecimli-də true/false; Acig-də null (avtomatik qiymətləndirilmir).</summary>
        public bool? DuzgunduMu { get; set; }

        public DateTime CavabTarixi { get; set; } = DateTime.Now;
    }
}
