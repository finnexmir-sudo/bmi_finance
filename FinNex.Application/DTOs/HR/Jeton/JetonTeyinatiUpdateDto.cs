using FinNex.Domain.Entities.HR;

namespace FinNex.Application.DTOs.HR.Jeton
{
    public class JetonTeyinatiUpdateDto
    {
        public int Id { get; set; }
        public string Ad { get; set; } = null!;
        public decimal SaatDeyeri { get; set; }
        public JetonVahid Vahid { get; set; } = JetonVahid.Saat;
        public string? Tesvir { get; set; }
        public string Ikon { get; set; } = null!;
        public string RengKodu { get; set; } = null!;
        public bool BirbasaOdenishli { get; set; }
        public bool Aktivdir { get; set; }
    }
}
