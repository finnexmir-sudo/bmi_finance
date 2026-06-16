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
