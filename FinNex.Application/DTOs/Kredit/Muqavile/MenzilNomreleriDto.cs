namespace FinNex.Application.DTOs.Kredit.Muqavile;

/// <summary>Bir daşınmaz əmlak müqaviləsi üçün ayrılan nömrələr.</summary>
public class MenzilNomreleriDto
{
    // Sayğaclar FinNex bazasındadır (MuqavileSayghaci) — Oracle qarşılığı izah üçündür.
    public int KreditNo { get; set; }        // {k_mno} — KrZaminlik (BMI: kr_zaminlik)
    public int IpotekaNo { get; set; }       // {i_mno} — KrMenzil   (BMI: kr_menzil)
    public List<int> ZaminNolar { get; set; } = new(); // {zmno1} — KrZaminler (BMI: kr_zaminler)
    public bool Yazildi { get; set; }        // sayğac real artırıldımı (NomreYaz=true)?
}
