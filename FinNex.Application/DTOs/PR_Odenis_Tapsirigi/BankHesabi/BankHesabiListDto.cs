namespace FinNex.Application.DTOs.PR_Odenis_Tapsirigi.BankHesabi
{
    public class BankHesabiListDto : BaseDto
    {
        public string Iban { get; set; } = null!;
        public string Valyuta { get; set; } = null!;
    }
}
