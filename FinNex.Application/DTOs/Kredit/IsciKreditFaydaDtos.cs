namespace FinNex.Application.DTOs.Kredit;

/// <summary>
/// VM 98.2.1 — bir işçi üzrə hesabi gəlir nəticəsi (bir dövr üçün).
/// Bir işçinin bir neçə krediti ola bilər — sətirlər `IsciId` üzrə toplanır.
/// </summary>
public class IsciKreditFaydaDto
{
    /// <summary>FIN ilə tapılan işçi. Tapılmayıbsa null — sətir cəmə DÜŞMÜR.</summary>
    public int?    IsciId      { get; set; }
    public string? IsciAdSoyad { get; set; }

    public string  MusteriKodu { get; set; } = "";
    public string? Fin         { get; set; }
    /// <summary>BMI-dəki ad — işçi tapılmayanda mühasib kimi olduğunu görsün.</summary>
    public string? BmiAdi      { get; set; }

    public string  ValyutaKodu { get; set; } = "00";

    /// <summary>Dövrdə hesablanmış adi faiz (Excel `M` sütunu).</summary>
    public decimal FaizAdi     { get; set; }
    /// <summary>Dövrdə hesablanmış vaxtı keçmiş faiz (Excel `N`).</summary>
    public decimal FaizVk      { get; set; }

    public decimal IsciFaizi   { get; set; }   // 8
    public decimal VkFaizi     { get; set; }   // 13

    /// <summary>Tətbiq olunan bazar dərəcəsi. Tapılmayıbsa null.</summary>
    public decimal? BazarDerecesi { get; set; }

    /// <summary>Hesabi gəlir — 2 onluğa yuvarlaqlaşdırılmış.</summary>
    public decimal HesabiGelir { get; set; }

    /// <summary>Doldurulubsa sətir problemlidir və cəmə DÜŞMÜR.</summary>
    public string? Problem     { get; set; }

    public bool Etibarlidir => Problem == null && IsciId.HasValue;
}

/// <summary>Bir dövrün tam nəticəsi — ekran və TopluHesabla üçün.</summary>
public class IsciKreditFaydaNeticeDto
{
    public DateTime Bas { get; set; }
    public DateTime Son { get; set; }

    public IList<IsciKreditFaydaDto> Setirler { get; set; } = new List<IsciKreditFaydaDto>();

    /// <summary>Oracle əlçatmaz olubsa dolur — çağıran tərəf bunu GÖSTƏRMƏLİDİR.</summary>
    public string? Xeta { get; set; }

    public IEnumerable<IsciKreditFaydaDto> Etibarlilar => Setirler.Where(x => x.Etibarlidir);
    public IEnumerable<IsciKreditFaydaDto> Problemliler => Setirler.Where(x => !x.Etibarlidir);

    public decimal Cemi => Etibarlilar.Sum(x => x.HesabiGelir);

    /// <summary>IsciId → hesabi gəlir. TopluHesabla sahəsini doldurmaq üçün.</summary>
    public Dictionary<int, decimal> IsciUzre =>
        Etibarlilar.GroupBy(x => x.IsciId!.Value)
                   .ToDictionary(g => g.Key, g => Math.Round(g.Sum(x => x.HesabiGelir), 2));
}
