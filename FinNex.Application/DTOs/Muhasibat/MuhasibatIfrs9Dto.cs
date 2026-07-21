namespace FinNex.Application.DTOs.Muhasibat;

// IFRS 9 ECL (Gözlənilən Kredit İtkiləri) — hesabat DTO-su.
// Metodologiya: roll-rate / stage-keçid (istifadəçinin Excel modelinin proqram versiyası).
//   • Staging DPD-dən: 0–30 = Stage 1, 31–90 = Stage 2, 90+ = Stage 3.
//   • Tarixi keçid (son 5 il, sahə səviyyəsində) → risk faizi = AVG(M/F).
//   • Floor: mənzil (1902/1904) 0.1% · Stage1 1% · Stage2 2%.
//   • Bərpa: mənzil Q2, digər P2 (Stage3 (J+K)/F ortalaması).
//   • ECL = cari EAD × risk_faiz(sahə, stage).
public class MuhasibatIfrs9Dto
{
    public DateTime Tarix { get; set; }
    public bool Ugurlu { get; set; } = true;
    public string? Xeta { get; set; }

    public decimal UmumiPortfel { get; set; }   // cari EAD cəmi
    public int Say { get; set; }                 // açıq kredit sayı
    public decimal UmumiEcl { get; set; }        // IFRS 9 ehtiyat cəmi
    public decimal EclFaiz { get; set; }         // ECL / EAD %
    public decimal BankEhtiyat { get; set; }     // FINA/prudensial ehtiyat (procstavrez) — müqayisə üçün
    public decimal P2 { get; set; }              // bərpa əmsalı — digər sahələr (Stage 3), fraction
    public decimal Q2 { get; set; }              // bərpa əmsalı — mənzil (1902/1904, Stage 3), fraction

    public List<Ifrs9StageDto> Stagelar { get; set; } = new();
    public List<Ifrs9SaheDto> Saheler { get; set; } = new();
    public List<Ifrs9SetirDto> Setirler { get; set; } = new();   // xam sətirlər (drill/AMB ixracı üçün)
}

// Stage üzrə xülasə (Stage 1 / 2 / 3).
public class Ifrs9StageDto
{
    public string Stage { get; set; } = "";
    public int Say { get; set; }
    public decimal Ead { get; set; }
    public decimal RiskFaiz { get; set; }   // çəkili orta ECL %
    public decimal Ecl { get; set; }
    public decimal BankEhtiyat { get; set; }
    public decimal Pay { get; set; }        // EAD payı %
    public string Reng { get; set; } = "";
}

// Sahə (index_otrasli) üzrə bölgü.
public class Ifrs9SaheDto
{
    public string Kod { get; set; } = "";
    public string Ad { get; set; } = "";
    public int Say { get; set; }
    public decimal Ead { get; set; }
    public decimal Ecl { get; set; }
    public decimal RiskFaiz { get; set; }   // çəkili orta (bütün mərhələlər): ECL/EAD
    public decimal Pay { get; set; }
    // Mərhələ üzrə tətbiq olunan risk % (ECL/EAD həmin mərhələdə). -1 = o mərhələdə kredit yoxdur.
    public decimal Risk1 { get; set; } = -1m;
    public decimal Risk2 { get; set; } = -1m;
    public decimal Risk3 { get; set; } = -1m;
}

// Bir cari kredit sətri (xam) — hesabat/AMB aqreqasiyası C#-da bunun üstündə qurulur.
public class Ifrs9SetirDto
{
    public string Hesab { get; set; } = "";
    public int Tip { get; set; }            // tipkredita: 1 hüquqi, 2 fiziki, 3 sahibkar
    public string Valyuta { get; set; } = "";  // 00=AZN, 01=USD ... (xarici valyuta alt-cədvəli üçün)
    public string SaheKodu { get; set; } = "";
    public string SaheAdi { get; set; } = "";
    public string Stage { get; set; } = "";
    public int Dpd { get; set; }            // gecikmə günü
    public decimal Ead { get; set; }
    public decimal RiskFaiz { get; set; }
    public decimal Ecl { get; set; }
    public decimal BankEhtiyat { get; set; }
}
