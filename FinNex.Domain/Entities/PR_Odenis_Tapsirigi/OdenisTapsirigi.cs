namespace FinNex.Domain.Entities.PR_Odenis_Tapsirigi;

public class OdenisTapsirigi : BaseEntity
{
    public string Nomre { get; set; } = null!;
    public DateTime Tarix { get; set; }

    // A1 - Emitent (oduyun) bank
    public int OduyenBankId { get; set; }
    public Bank OduyenBank { get; set; } = null!;

    // A2 - Oduyun musteri
    public int OduyenMusteriId { get; set; }
    public Musteri OduyenMusteri { get; set; } = null!;

    public int OduyenHesabId { get; set; }
    public MusteriHesabi OduyenHesab { get; set; } = null!;

    // B1 - Benefisiar bank
    public int AlanBankId { get; set; }
    public Bank AlanBank { get; set; } = null!;

    // B2 - Alan musteri
    public int AlanMusteriId { get; set; }
    public Musteri AlanMusteri { get; set; } = null!;

    public int AlanHesabId { get; set; }
    public MusteriHesabi AlanHesab { get; set; } = null!;

    // C - Mebleg
    public decimal Mebleg { get; set; }
    public string Valyuta { get; set; } = null!;
    public string? MeblegYazi { get; set; }

    // D1 - Odenishin teyinati ve esasi
    public string Teyinat { get; set; } = null!;

    // D2 - Odenishle elaqedar elave informasiya
    public string? ElaveInformasiya { get; set; }

    // D3 - Budce tesnifatinin kodu
    public string? BudceTesnifatininKodu { get; set; }

    // D4 - Budce seviyyesinin kodu
    public string? BudceSeviyyesininKodu { get; set; }
}
