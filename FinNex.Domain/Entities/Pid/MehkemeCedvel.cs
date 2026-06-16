namespace FinNex.Domain.Entities.Pid;

// Məhkəmə İşləri — müstəqil cədvəl (Excel "Məhkəmə 2021-2026" sheet-inə uyğun).
// Sütunlar: a.Sıra  b.Müştəri adı  c.Girovun növü  d.Məhkəməyə verilmə tarixi
//           e.Məhkəmə iş nömrəsi + Hakim  f.İclas tarix/saatları (1-çox → MehkemeCedvelIclas)
public class MehkemeCedvel : BaseEntity
{
    public int? Sira { get; set; }                          // a. sıra №
    public string BorcluAd { get; set; } = "";              // b. müştəri adı
    public string? GirovunNovu { get; set; }                // c. girovun növü (Daşınmaz / Avtomobil...)
    public DateTime? MehkemeyeVerilmeTarixi { get; set; }   // d. məhkəməyə verilmə tarixi
    public string? MehkemeIsNomresi { get; set; }           // e. məhkəmə iş / sənəd nömrəsi
    public string? Hakim { get; set; }                      // e. hakimin adı

    public ICollection<MehkemeCedvelIclas> Iclaslar { get; set; } = new List<MehkemeCedvelIclas>();
}

// Bir məhkəmə işinə aid iclas (Excel-dəki təkrar tarix/saat cütləri) — f.
public class MehkemeCedvelIclas : BaseEntity
{
    public int MehkemeCedvelId { get; set; }
    public MehkemeCedvel MehkemeCedvel { get; set; } = null!;

    public DateTime? Tarix { get; set; }    // iclas tarixi
    public string? Saat { get; set; }       // saat (mətn — Excel-də 14.40, 9.30 qarışıq)
}
