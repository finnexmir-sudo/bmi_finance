namespace FinNex.Domain.Entities.HR
{
    public class ErkenCixisIcaze : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;
        public DateTime Tarix { get; set; }
        public int IcazeVerenIsciId { get; set; }
    }
}
