namespace FinNex.UI.ViewModels.PR_Odenis_Tapsirigi;

public class OdenisTapsirigiWordDto
{
    // Ödəyən bank (A1)
    public string OduyenBankAd { get; set; } = "";
    public string OduyenBankKod { get; set; } = "";
    public string OduyenBankVoen { get; set; } = "";
    public string OduyenBankMuxbirHesab { get; set; } = "";
    public string OduyenBankSwift { get; set; } = "";

    // Ödəyən müştəri (A2)
    public string OduyenMusteriAd { get; set; } = "";
    public string OduyenMusteriHesab { get; set; } = "";
    public string OduyenMusteriVoen { get; set; } = "";

    // Alan bank (B1)
    public string AlanBankAd { get; set; } = "";
    public string AlanBankKod { get; set; } = "";
    public string AlanBankVoen { get; set; } = "";
    public string AlanBankMuxbirHesab { get; set; } = "";
    public string AlanBankSwift { get; set; } = "";

    // Alan müştəri (B2)
    public string AlanMusteriAd { get; set; } = "";
    public string AlanMusteriHesab { get; set; } = "";
    public string AlanMusteriVoen { get; set; } = "";

    // Məbləğ
    public string Valyuta { get; set; } = "";
    public string Mebleg { get; set; } = "";
    public string MeblegYazi { get; set; } = "";

    // Təyinat
    public string Teyinat { get; set; } = "";
    public string ElaveInfo { get; set; } = "";
}
