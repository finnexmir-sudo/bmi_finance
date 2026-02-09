namespace FinNex.Domain.Entities.PR_Odenis_Tapsirigi;

public class Musteri : BaseEntity
{
    public string Ad { get; set; } = null!;
    public string Voen { get; set; } = null!;

    public ICollection<MusteriHesabi> MusteriHesablari { get; set; } = new List<MusteriHesabi>();
}
