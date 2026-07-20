namespace FinNex.Application.DTOs.Muhasibat;

// IFRS 9 ECL — "iş kağızları" (audit izi). Nəticəyə necə gəlindiyini addım-addım göstərir:
//   1. Tarixi keçid matrisi (F..K, M, N)  2. Risk faizləri (sahə×mərhələ)
//   3. Cari portfel (kredit-kredit)         4. Nəticə (mərhələ cəmi)
// Auditor/müfəttiş hər rəqəmi izləyə bilsin deyə — istifadəçinin Excel modelinin proqram versiyası.
public class MuhasibatIfrs9AuditDto
{
    public DateTime Tarix { get; set; }
    public bool Ugurlu { get; set; } = true;
    public string? Xeta { get; set; }

    public decimal P2 { get; set; }   // bərpa əmsalı — digər sahələr (Stage 3)
    public decimal Q2 { get; set; }   // bərpa əmsalı — mənzil (1902/1904, Stage 3)

    public List<Ifrs9KechidSetir> Kechidler { get; set; } = new();  // tarixi keçid matrisi
    public MuhasibatIfrs9Dto Ecl { get; set; } = new();             // cari portfel + mərhələ nəticəsi
}

// Bir tarixi keçid sətri (il × sahə × mərhələ) — Excel 01_SQL_Keçidlər ekvivalenti.
public class Ifrs9KechidSetir
{
    public int Il { get; set; }              // il_start (keçid ilinin əvvəli)
    public string SaheKodu { get; set; } = "";
    public string SaheAdi { get; set; } = "";
    public string Stage { get; set; } = "";  // stage_start
    public decimal F { get; set; }   // başlanğıc qalıq
    public decimal G { get; set; }   // → Mərhələ 1
    public decimal H { get; set; }   // → Mərhələ 2
    public decimal I { get; set; }   // → Mərhələ 3
    public decimal J { get; set; }   // bağlanan
    public decimal K { get; set; }   // ödənilən hissə
    public decimal M { get; set; }   // risk məbləği (floor + bərpa)
    public decimal N { get; set; }   // risk faizi = M / F (fraction)
}
