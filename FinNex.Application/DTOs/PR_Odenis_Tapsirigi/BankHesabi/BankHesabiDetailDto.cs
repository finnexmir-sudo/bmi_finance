namespace FinNex.Application.DTOs.PR_Odenis_Tapsirigi.BankHesabi
{
    public class BankHesabiDetailDto : BaseDto
    {
        public int BankId { get; set; }

        // istəsən sonradan Bank adı da əlavə edərsən
        public string Iban { get; set; } = null!;
        public string Valyuta { get; set; } = null!;
    }
}
