namespace FinNex.Application.DTOs.PR_Odenis_Tapsirigi.BankHesabi
{
    public class BankHesabiUpdateDto : BaseDto
    {

        public string Iban { get; set; } = null!;
        public string Valyuta { get; set; } = null!;
    }
}
