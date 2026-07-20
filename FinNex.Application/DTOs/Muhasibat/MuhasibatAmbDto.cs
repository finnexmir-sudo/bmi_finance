namespace FinNex.Application.DTOs.Muhasibat;

// AMB (Mərkəzi Bank) MHBS 9 — Cədvəl A1: amortizasiya olunmuş dəyərdə kredit portfeli.
// IFRS 9 ECL nəticəsi AMB kredit-növü kateqoriyalarına (Biznes 1.1–1.7, İstehlak 2.1–2.5,
// Daşınmaz əmlak 3, Digər 4) aqreqasiya olunur; hər kateqoriya × Mərhələ 1/2/3 üzrə brüt + ECL.
// POCI (satın alınmış kredit-zərərli) — bankda yoxdur, 0.
public class MuhasibatAmbA1Dto
{
    public DateTime Tarix { get; set; }
    public bool Ugurlu { get; set; } = true;
    public string? Xeta { get; set; }

    // Kateqoriya açarı → hüceyrə. Açarlar: 1_1..1_7, 2_1..2_5, 3, 4.
    public Dictionary<string, AmbHuceyre> Butun { get; set; } = new();   // A alt-cədvəli: bütün valyuta
    public Dictionary<string, AmbHuceyre> Xarici { get; set; } = new();  // B alt-cədvəli: yalnız xarici valyuta

    // A1.2 — mərhələ × gecikmə günü bölgüsü. Açar: "{qrup}|{mərhələ}", məs. "biznes|1".
    // Qruplar: biznes / istehlak / dasinmaz / diger. Dəyər: brüt EAD gecikmə qutularında.
    public Dictionary<string, AmbDpdSetir> Dpd { get; set; } = new();        // A1.2 A: bütün valyuta
    public Dictionary<string, AmbDpdSetir> DpdXarici { get; set; } = new();  // A1.2 B: xarici valyuta
}

// A1.2 — bir (qrup, mərhələ) üçün gecikmə günü qutuları (brüt EAD; AZN saxlanılır, ixracda ÷1000).
public class AmbDpdSetir
{
    public decimal Cari { get; set; }    // 0 gün
    public decimal D1_30 { get; set; }   // 1–30 gün
    public decimal D31_90 { get; set; }  // 31–90 gün
    public decimal D90 { get; set; }     // 90 gündən artıq
}

// Bir AMB kateqoriyası üçün brüt (G) və ECL (E) — mərhələ 1/2/3 üzrə (min manat AZN saxlanılır; ixracda ÷1000).
public class AmbHuceyre
{
    public decimal G1 { get; set; }
    public decimal G2 { get; set; }
    public decimal G3 { get; set; }
    public decimal E1 { get; set; }
    public decimal E2 { get; set; }
    public decimal E3 { get; set; }
    public decimal GCem => G1 + G2 + G3;
    public decimal ECem => E1 + E2 + E3;
}
