namespace FinNex.Application.DTOs.Muhasibat;

// Drill-down — bir kartın/sətrin arxasındakı hesab (və ya sətir) detalı.
// KRİTİK: `Cem` həmişə müvafiq kartdakı rəqəmlə üst-üstə düşməlidir. Detal
// aqreqasiya ilə EYNİ sorğunu və EYNİ təsnifat helper-lərini (Tesnif/LikvidQrup/
// dep_tip/TipAd...) işlədir — ona görə ayrı bir "detal məntiqi" yazılmır.
public class MuhasibatDetalDto
{
    public bool     Ugurlu  { get; set; }
    public string?  Xeta    { get; set; }

    public string   Sahe    { get; set; } = "";   // balans / likvidlik / depozit / kredit / valyuta / rezident
    public string   Madde   { get; set; } = "";   // kliklənən maddə/bölgü
    public string   Baslik  { get; set; } = "";   // modal başlığı
    public decimal  Cem     { get; set; }          // sətirlərin cəmi = kartdakı rəqəm
    public int      Say     { get; set; }          // sətir sayı

    public List<MuhasibatDetalSetirDto> Setirler { get; set; } = new();
}

public class MuhasibatDetalSetirDto
{
    public string   Kod     { get; set; } = "";    // hesab kodu / müqavilə / müştəri qeydi / valyuta
    public string   Ad      { get; set; } = "";    // hesab adı / müştəri adı / təsvir
    public string?  Valyuta     { get; set; }
    public decimal  Mebleg      { get; set; }        // manat qarşılığı (saldo_ish_nacval)
    public decimal? MeblegInval { get; set; }        // öz valyutası (saldo_ish_inval) — AZN-də null/manatla eyni
    public string?  Elave       { get; set; }        // DPD / kurs / tarix və s. (istəyə bağlı)
}
