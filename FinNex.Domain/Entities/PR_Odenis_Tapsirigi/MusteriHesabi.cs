namespace FinNex.Domain.Entities.PR_Odenis_Tapsirigi;

public class MusteriHesabi : BaseEntity
{
    public int MusteriId { get; set; }
    public Musteri Musteri { get; set; } = null!;

    public string Iban { get; set; } = null!;
    public string Valyuta { get; set; } = null!;
}
