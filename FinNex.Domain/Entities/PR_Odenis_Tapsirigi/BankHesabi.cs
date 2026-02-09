namespace FinNex.Domain.Entities.PR_Odenis_Tapsirigi;

public class BankHesabi : BaseEntity
{
    public int BankId { get; set; }
    public Bank Bank { get; set; } = null!; // Navigation Property

    public string Iban { get; set; } = null!;
    public string Valyuta { get; set; } = null!;
}
