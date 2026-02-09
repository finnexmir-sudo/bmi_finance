
namespace FinNex.Application.DTOs.PR_Odenis_Tapsirigi.Bank
{
    public class BankUpdateDto : BaseDto
    {
        public string Ad { get; set; } = null!;
        public string Kod { get; set; } = null!;
        public string SwiftBic { get; set; } = null!;
        public string MuxHesab { get; set; } = null!;

        // ❗ Voen update ETMİRİK (real bank qaydası)
    }
}
