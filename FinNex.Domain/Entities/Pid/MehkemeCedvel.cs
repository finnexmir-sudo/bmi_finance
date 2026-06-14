namespace FinNex.Domain.Entities.Pid;

// Məhkəmə cədvəli işi — Excel "Məhkəmə 2021-2026" sheet-inə uyğun.
// Təkrarlanan iclas sütunları ayrıca MehkemeCedvelIclas cədvəlinə çıxır (1-çox).
public class MehkemeCedvel : BaseEntity
{
    public int? Sira { get; set; }                          // Excel sıra №
    public string? Status { get; set; }                     // BORC BAĞLANDI, davam edir...
    public string BorcluAd { get; set; } = "";
    public string? KreditNovu { get; set; }                 // Daşınmaz / Avtomobil...
    public string? KreditHesabi { get; set; }               // hesab № (müştəri açarı)
    public string? Subkod { get; set; }                     // subkod
    public DateTime? MehkemeyeVerilmeTarixi { get; set; }
    public string? MehkemeSenedi { get; set; }              // sənəd № + hakim
    public DateTime? QetnameTarixi { get; set; }
    public string? Qeyd { get; set; }

    public ICollection<MehkemeCedvelIclas> Iclaslar { get; set; } = new List<MehkemeCedvelIclas>();
}

// Bir məhkəmə cədvəl işinə aid iclas (Excel-dəki təkrar tarix/saat cütləri).
public class MehkemeCedvelIclas : BaseEntity
{
    public int MehkemeCedvelId { get; set; }
    public MehkemeCedvel MehkemeCedvel { get; set; } = null!;

    public DateTime? Tarix { get; set; }
    public string? Saat { get; set; }       // mətn (Excel-də 14.40, 9.30 qarışıq)
    public string? Netice { get; set; }
}
