
namespace FinNex.Application.DTOs.PR_Odenis_Tapsirigi.Bank
{
    public class BankListDto : BaseDto
    {
        public string Ad { get; set; } = null!;
        public string Kod { get; set; } = null!;
        public string Voen { get; set; } = null!;
    }
    public class BankDetailDto : BaseDto
    {
        public string Ad { get; set; } = null!;
        public string Kod { get; set; } = null!;
        public string Voen { get; set; } = null!;
        public string SwiftBic { get; set; } = null!;
        public string MuxHesab { get; set; } = null!;
    }
    public class BankCreateDto
    {
        public string Ad { get; set; } = null!;
        public string Kod { get; set; } = null!;
        public string Voen { get; set; } = null!;
        public string SwiftBic { get; set; } = null!;
        public string MuxHesab { get; set; } = null!;
    }
    public class BankUpdateDto : BaseDto
    {
        public string Ad { get; set; } = null!;
        public string Kod { get; set; } = null!;
        public string SwiftBic { get; set; } = null!;
        public string MuxHesab { get; set; } = null!;

        // ❗ Voen update ETMİRİK (real bank qaydası)
    }
    public class BankHesabiDetailDto : BaseDto
    {
        public int BankId { get; set; }
        public string BankAdi { get; set; } = null!;

        public string Iban { get; set; } = null!;
        public string Valyuta { get; set; } = null!;
    }
}
