
namespace FinNex.Application.DTOs.PR_Odenis_Tapsirigi.MusteriHesabi
{
    public class MusteriHesabiCreateDto
    {
        public int MusteriId { get; set; }

        public string Iban { get; set; } = null!;
        public int ValyutaId { get; set; }
    }
}
