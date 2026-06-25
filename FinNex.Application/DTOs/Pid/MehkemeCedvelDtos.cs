namespace FinNex.Application.DTOs.Pid;

// Məhkəmə İşləri — siyahı sətri (6 sütun)
public class MehkemeCedvelListDto
{
    public int Id { get; set; }
    public int? Sira { get; set; }                          // a
    public string BorcluAd { get; set; } = "";              // b
    public string? GirovunNovu { get; set; }                // c
    public DateTime? MehkemeyeVerilmeTarixi { get; set; }   // d
    public string? MehkemeIsNomresi { get; set; }           // e
    public string? Hakim { get; set; }                      // e
    public List<MehkemeCedvelIclasDto> Iclaslar { get; set; } = new();   // f
}

public class MehkemeCedvelIclasDto
{
    public int Id { get; set; }
    public DateTime? Tarix { get; set; }
    public string? Saat { get; set; }
}

// Excel-dən parse olunmuş iş (controller → servis)
public class MehkemeCedvelImportDto
{
    public int? Sira { get; set; }
    public string? Hesab { get; set; }       // kredit hesabı (Oracle linki) — "42"/boş = köhnə/arxiv
    public string? Subkod { get; set; }      // subkod
    public string? StatusMetn { get; set; }  // Excel status (BORC BAĞLANDI / QƏTNAMƏ GƏLDİ)
    public string BorcluAd { get; set; } = "";
    public string? GirovunNovu { get; set; }
    public DateTime? MehkemeyeVerilmeTarixi { get; set; }
    public string? MehkemeIsNomresi { get; set; }
    public string? Hakim { get; set; }
    public List<MehkemeCedvelIclasImportDto> Iclaslar { get; set; } = new();
}

public class MehkemeCedvelIclasImportDto
{
    public DateTime? Tarix { get; set; }
    public string? Saat { get; set; }
}

// Əl ilə yeni iş (forma)
public class MehkemeCedvelCreateDto
{
    public int? Sira { get; set; }
    public string BorcluAd { get; set; } = "";
    public string? GirovunNovu { get; set; }
    public string? MehkemeyeVerilmeTarixi { get; set; }     // input type=date "yyyy-MM-dd"
    public string? MehkemeIsNomresi { get; set; }
    public string? Hakim { get; set; }
    // Paralel siyahılar (formadan dinamik iclas sətirləri)
    public List<string>? IclasTarix { get; set; }
    public List<string>? IclasSaat { get; set; }
}

// ── İcra cədvəli idxalı (PID Excel "İCRADA OLAN İŞLƏR" vərəqi) ──
// Bir (hesab, subkod) = bir iş. Borclu sətri iş səviyyəsini, zamin sətirləri
// (Excel-də ZAMİN sütunu dolu) ayrıca MehkemeZamin qeydlərini doldurur.
public class IcraCedvelImportDto
{
    public string Hesab  { get; set; } = "";
    public string Subkod { get; set; } = "";
    public string? BorcluAd { get; set; }
    public bool Arxiv { get; set; }                 // subkod=42 və ya "BORC BAĞLANDI" → status Bağlandı
    public string? StatusMetn { get; set; }         // Excel [1] xam ("BORC BAĞLANDI" və s.)
    public decimal? QalanBorc { get; set; }
    public DateTime? SonOdenisTarixi { get; set; }
    public string? Qeydiyyati { get; set; }
    public string? EmekHaqqiMelumati { get; set; }
    public DateTime? DypSorguTarixi { get; set; }
    public string? AdinaSorgu { get; set; }
    public string? EmlakaHebs { get; set; }
    public string? Stop { get; set; }
    public string? IcraMemuru { get; set; }
    public string? IcraSonIsler { get; set; }
    public DateTime? DogumTarixi { get; set; }
    public DateTime? QetnameTarixi { get; set; }
    public string? IsYeri { get; set; }
    public string? IcraQeyd { get; set; }
    public List<IcraZaminImportDto> Zaminler { get; set; } = new();
}

// Zamin (icra subyekti) — Excel sətrindəki ZAMİN üçün; tarixlər mətn kimi saxlanır.
public class IcraZaminImportDto
{
    public string Ad { get; set; } = "";
    public string? DogumTarixi { get; set; }
    public string? EmekHaqqiTutulma { get; set; }
    public string? DypSorgu { get; set; }
    public string? AdinaSorgu { get; set; }
    public string? EmlakaHebs { get; set; }
    public string? Stop { get; set; }
    public string? IcraMemuru { get; set; }
    public string? IcraSonIsler { get; set; }
    public string? IsYeri { get; set; }
    public string? IcraQeyd { get; set; }
}

// İcra idxalının nəticəsi — önizləmə (yazısız) və real idxal üçün eyni.
public class IcraImportNeticeDto
{
    public bool Onizleme { get; set; }          // true = DB-yə yazılmadı
    public int CemQrup { get; set; }            // Excel-dəki unikal (hesab+subkod)
    public int YeniIs  { get; set; }            // yaradılacaq / yaradılan
    public int Atlanan { get; set; }            // artıq mövcud (dedup)
    public int Aktiv   { get; set; }
    public int Arxiv   { get; set; }
    public int Zamin   { get; set; }
    public int Xeta    { get; set; }            // hesabsız / yanlış sətir
    public List<string> Numuneler { get; set; } = new();
}
