using FinNex.Domain.Entities.HR;

namespace FinNex.Application.DTOs.HR.Jeton
{
    public class JetonTeyinatiListDto
    {
        public int Id { get; set; }
        public string Ad { get; set; } = null!;
        public JetonNovu Nov { get; set; }
        public JetonRengi Rengi { get; set; }
        public decimal SaatDeyeri { get; set; }
        public JetonVahid Vahid { get; set; }
        public string Ikon { get; set; } = null!;
        public string RengKodu { get; set; } = null!;
        public string? Tesvir { get; set; }
        public bool BirbasaOdenishli { get; set; }
        public bool Aktivdir { get; set; }
    }
}
