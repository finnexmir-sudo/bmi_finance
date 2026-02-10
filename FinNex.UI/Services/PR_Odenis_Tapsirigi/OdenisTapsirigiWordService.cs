using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FinNex.UI.ViewModels.PR_Odenis_Tapsirigi;

namespace FinNex.UI.Services.PR_Odenis_Tapsirigi;

public static class OdenisTapsirigiWordService
{
    public static byte[] GenerateFromTemplate(
        string templatePath,
        OdenisTapsirigiWordDto dto)
    {
        // Şablonu RAM-a yüklə
        byte[] templateBytes = File.ReadAllBytes(templatePath);

        using var ms = new MemoryStream();
        ms.Write(templateBytes, 0, templateBytes.Length);

        using (var doc = WordprocessingDocument.Open(ms, true))
        {
            var body = doc.MainDocumentPart!.Document.Body!;

            Replace(body, "{nom}", dto.Nomre);
            Replace(body, "{A1Adi}", dto.OduyenBankAd);
            Replace(body, "{A1kod}", dto.OduyenBankKod);
            Replace(body, "{A1voen}", dto.OduyenBankVoen);
            Replace(body, "{A1mhes}", dto.OduyenBankMuxbirHesab);
            Replace(body, "{A1swift}", dto.OduyenBankSwift);

            Replace(body, "{B1Adi}", dto.AlanBankAd);
            Replace(body, "{B1kod}", dto.AlanBankKod);
            Replace(body, "{B1voen}", dto.AlanBankVoen);
            Replace(body, "{B1mhes}", dto.AlanBankMuxbirHesab);
            Replace(body, "{B1vbank}", dto.AlanBankSwift);

            Replace(body, "{A2adi}", dto.OduyenMusteriAd);
            Replace(body, "{A2hesab}", dto.OduyenMusteriHesab);
            Replace(body, "{A2voen}", dto.OduyenMusteriVoen);

            Replace(body, "{B2adi}", dto.AlanMusteriAd);
            Replace(body, "{B2hesab}", dto.AlanMusteriHesab);
            Replace(body, "{B2voen}", dto.AlanMusteriVoen);

            Replace(body, "{mebleg}", dto.Mebleg);
            Replace(body, "{mebleg_yazi}", dto.MeblegYazi);

            Replace(body, "{teyinat}", dto.Teyinat);
            Replace(body, "{elave}", dto.ElaveInfo);

            Replace(body, "{tesnifat_kodu}", dto.BudceTesnifatininKodu);
            Replace(body, "{budce_kodu}", dto.BudceSeviyyesininKodu);

            doc.MainDocumentPart.Document.Save();
        }

        return ms.ToArray();
    }

    private static void Replace(Body body, string key, string value)
    {
        foreach (var text in body.Descendants<Text>())
        {
            if (text.Text.Contains(key))
            {
                text.Text = text.Text.Replace(key, value ?? "");
            }
        }
    }
}
