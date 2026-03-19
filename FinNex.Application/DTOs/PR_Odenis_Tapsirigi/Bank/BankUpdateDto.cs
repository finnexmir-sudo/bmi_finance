
namespace FinNex.Application.DTOs.PR_Odenis_Tapsirigi.Bank
{
    public class BankUpdateDto : BaseDto
    {
        public string Ad { get; set; } = null!;
        public string Kod { get; set; } = null!;
        public string SwiftBic { get; set; } = null!;
        public string MuxHesab { get; set; } = null!;

        // ❗ Voen update ETMİRİK (real bank qaydası)

        public class BankYoxlaDto
        {
            public string Ad { get; set; } = "";
            public string Kod { get; set; } = "";
            public string Voen { get; set; } = "";
            public string SwiftBic { get; set; } = "";
            public string MuxHesab { get; set; } = "";
        }

        public class MusteriYoxlaDto
        {
            public string Ad { get; set; } = "";
            public string Voen { get; set; } = "";
            public string Hesab { get; set; } = "";
        }
    }
}
