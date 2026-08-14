namespace FinNex.Application.DTOs.Kredit.Muqavile;

/// <summary>
/// Bir müqavilə dəsti üçün ayrılan nömrələr (daşınmaz əmlak / zaminlik / avtomobil).
/// Hər axın yalnız özünə lazım olan sahələri doldurur, qalanı 0 qalır:
///   • daşınmaz əmlak → KreditNo + IpotekaNo + ZaminNolar
///   • zaminlik       → KreditNo + ZaminNolar
///   • avtomobil      → KreditNo + AvtoNo + ZaminNolar
/// </summary>
public class MenzilNomreleriDto
{
    // Sayğaclar FinNex bazasındadır (MuqavileSayghaci) — Oracle qarşılığı izah üçündür.
    public int KreditNo { get; set; }        // {k_mno} — KrZaminlik (BMI: kr_zaminlik)
    public int IpotekaNo { get; set; }       // {i_mno} — KrMenzil   (BMI: kr_menzil)
    public int AvtoNo { get; set; }          // {a_mno} — KrAvtomobil (BMI: kr_avtomobil)
    public List<int> ZaminNolar { get; set; } = new(); // {zmno1} — KrZaminler (BMI: kr_zaminler)
    public bool Yazildi { get; set; }        // sayğac real artırıldımı (NomreYaz=true)?
}
