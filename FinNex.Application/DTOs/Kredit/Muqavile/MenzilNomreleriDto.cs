namespace FinNex.Application.DTOs.Kredit.Muqavile;

/// <summary>Bir daşınmaz əmlak müqaviləsi üçün ayrılan nömrələr.</summary>
public class MenzilNomreleriDto
{
    public int KreditNo { get; set; }        // {k_mno} — odb.muqavile_nomreleri.kr_zaminlik
    public int IpotekaNo { get; set; }       // {i_mno} — odb.muqavile_nomreleri.kr_menzil
    public List<int> ZaminNolar { get; set; } = new(); // hər zamin üçün {zmno1} — kr_zaminler
    public bool Yazildi { get; set; }        // Oracle sayğacı real artırıldımı (NomreYaz=true)?
}
