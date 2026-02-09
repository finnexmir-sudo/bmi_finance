namespace FinNex.Domain.Entities.PR_Odenis_Tapsirigi;

public class OdenisTapsirigi : BaseEntity
{
    public string Nomre { get; set; } = null!;
    public DateTime Tarix { get; set; }

    // Ödəyən tərəf
    public int OduyenMusteriId { get; set; }
    public Musteri OduyenMusteri { get; set; } = null!;

    public int OduyenHesabId { get; set; }
    public MusteriHesabi OduyenHesab { get; set; } = null!;

    // Alan (Benefisiar) tərəf
    public int AlanMusteriId { get; set; }
    public Musteri AlanMusteri { get; set; } = null!;

    public int AlanHesabId { get; set; }
    public MusteriHesabi AlanHesab { get; set; } = null!;

    public decimal Mebleg { get; set; }
    public string Valyuta { get; set; } = null!;
    public string Teyinat { get; set; } = null!;
}
