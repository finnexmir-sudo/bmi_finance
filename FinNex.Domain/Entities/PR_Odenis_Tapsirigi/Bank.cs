namespace FinNex.Domain.Entities.PR_Odenis_Tapsirigi;

public class Bank : BaseEntity
{
    public string Ad { get; set; } = null!;
    public string Kod { get; set; } = null!;
    public string Voen { get; set; } = null!;
    public string SwiftBic { get; set; } = null!;
    public string MuxHesab { get; set; } = null!;

    // Bir bankın çoxlu hesabı ola bilər
    public ICollection<BankHesabi> BankHesablari { get; set; } = new List<BankHesabi>();
}
