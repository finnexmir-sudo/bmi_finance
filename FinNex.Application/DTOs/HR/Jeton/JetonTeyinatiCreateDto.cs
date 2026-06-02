using FinNex.Domain.Entities.HR;

namespace FinNex.Application.DTOs.HR.Jeton
{
    public class JetonTeyinatiCreateDto
    {
        public string Ad { get; set; } = null!;
        public JetonNovu Nov { get; set; }
        public JetonRengi Rengi { get; set; }
        public decimal SaatDeyeri { get; set; }
        public string Ikon { get; set; } = "bi bi-award-fill";
        public string RengKodu { get; set; } = "#6b7280";
        public string? Tesvir { get; set; }
        public bool BirbasaOdenishli { get; set; } = false;
    }
}
