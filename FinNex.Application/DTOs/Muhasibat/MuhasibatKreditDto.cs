namespace FinNex.Application.DTOs.Muhasibat;

// Kredit portfeli tab — CARI vəziyyət (odb.licschkre + view_nacpogprokre_all, sysdate snapshot).
// Qalıq = (summa + summa_19) × valyuta kursu (AZN); DPD = tar_ferq360.
public class MuhasibatKreditDto
{
    public DateTime Tarix   { get; set; }   // cari iş günü (göstərmək üçün)
    public bool     Ugurlu  { get; set; }
    public string?  Xeta    { get; set; }

    public decimal  UmumiPortfel { get; set; }   // əsas + VK, AZN
    public int      MuqavileSayi { get; set; }
    public decimal  VkMebleg     { get; set; }    // vaxtı keçmiş (summa_19), AZN
    public decimal  NplMebleg    { get; set; }    // 90+ gün gecikən qalıq
    public decimal  NplFaiz      { get; set; }    // NPL / portfel, %

    public List<BalansMaddeDto> TipBolgusu     { get; set; } = new();   // Hüquqi / Fiziki / Sahibkar
    public List<BalansMaddeDto> TeyinatBolgusu { get; set; } = new();   // ipoteka / avto / kart ...
    public List<BalansMaddeDto> ValyutaBolgusu { get; set; } = new();   // AZN / USD ...
    public List<BalansMaddeDto> GecikmeBolgusu { get; set; } = new();   // aging: 0 / 1-30 / 31-90 / 90+
}
