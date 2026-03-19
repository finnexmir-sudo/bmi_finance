namespace FinNex.UI.Areas.PR_Odenis_Tapsirigi.ViewModels;

public class OdenisTapsirigiWordDto
{
    // Tapshiriq nomresi ve tarix
    public string Nomre { get; set; } = "";
    public string Tarix { get; set; } = "";

    // Oduyun bank (A1)
    public string OduyenBankAd { get; set; } = "";
    public string OduyenBankKod { get; set; } = "";
    public string OduyenBankVoen { get; set; } = "";
    public string OduyenBankMuxbirHesab { get; set; } = "";
    public string OduyenBankSwift { get; set; } = "";

    // Oduyun mushteri (A2)
    public string OduyenMusteriAd { get; set; } = "";
    public string OduyenMusteriHesab { get; set; } = "";
    public string OduyenMusteriVoen { get; set; } = "";

    // Alan bank (B1)
    public string AlanBankAd { get; set; } = "";
    public string AlanBankKod { get; set; } = "";
    public string AlanBankVoen { get; set; } = "";
    public string AlanBankMuxbirHesab { get; set; } = "";
    public string AlanBankSwift { get; set; } = "";
    public string AlanBankVbank { get; set; } = "";

    // Alan mushteri (B2)
    public string AlanMusteriAd { get; set; } = "";
    public string AlanMusteriHesab { get; set; } = "";
    public string AlanMusteriVoen { get; set; } = "";

    // C - Mebleg
    public string Valyuta { get; set; } = "";
    public string Mebleg { get; set; } = "";
    public string MeblegYazi { get; set; } = "";

    // D1 - Teyinat
    public string Teyinat { get; set; } = "";

    // D2 - Elave informasiya
    public string ElaveInfo { get; set; } = "";

    // D3 - Budce tesnifatinin kodu
    public string BudceTesnifatininKodu { get; set; } = "";

    // D4 - Budce seviyyesinin kodu
    public string BudceSeviyyesininKodu { get; set; } = "";

    public string OduyenBankId { get; set; } = "";
    public string AlanBankId { get; set; } = "";
    public string OduyenMusteriId { get; set; } = "";
    public string OduyenHesabId { get; set; } = "";
    public string AlanMusteriId { get; set; } = "";
    public string AlanHesabId { get; set; } = "";
    public string ValyutaId { get; set; } = "";
}
