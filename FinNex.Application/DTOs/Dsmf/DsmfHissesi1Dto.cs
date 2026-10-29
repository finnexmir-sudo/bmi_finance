namespace FinNex.Application.DTOs.Dsmf;

// DSMF Vahid Hesabat Forması — Əlavə №1 Hissə 1. "İşçilər (sığortaolunanlar)
// üzrə ümumi məlumat — muzdlu işə aid olan fəaliyyətlər üzrə" (29.10.2026).
// Hissə 3 kimi İŞÇİ-sətridir (qanun-maddəsi-sətri YOX, Bölmə 2/3/4/5-dən
// fərqli), hər işçinin hər ay üzrə 11 göstəricisini daşıyır. Mənbə şablon:
// UNIFIED_APPENDIX_1_EMPLOYEE_LIST.xlsx — sütun xəritəsi
// DsmfHesabatService.Hissesi1HazirlaAsync-in şərhindədir.
public class DsmfHissesi1Dto
{
    public int Il { get; set; }
    public int Rub { get; set; } // 1-4

    public int Ay1 { get; set; }
    public int Ay2 { get; set; }
    public int Ay3 { get; set; }
    public string Ay1Adi { get; set; } = "";
    public string Ay2Adi { get; set; } = "";
    public string Ay3Adi { get; set; } = "";

    public List<DsmfHissesi1SetirDto> Setirler { get; set; } = new();
}

public class DsmfHissesi1SetirDto
{
    public int SiraNo { get; set; }
    public int IsciId { get; set; }

    /// <summary>"Soyadı, adı, atasının adı" sırası — formanın öz başlığına uyğun.</summary>
    public string AdSoyadAta { get; set; } = "";
    public string Fin { get; set; } = "";

    public DsmfHissesi1AyDto Ay1 { get; set; } = new();
    public DsmfHissesi1AyDto Ay2 { get; set; } = new();
    public DsmfHissesi1AyDto Ay3 { get; set; } = new();
}

/// <summary>Bir ayın 11 göstəricisi (2.1/2.2/2.3/3.1/4.1/5.1/6.1 — 2.3.1/2.5/2.6/4.5 həmişə boşdur, DTO-da yoxdur).</summary>
public class DsmfHissesi1AyDto
{
    /// <summary>
    /// 2.1 — Əsas iş yeri olması. Sistemdə bu bayraq YOXDUR (istifadəçi qərarı,
    /// 29.10.2026: "Hamısına Bəli yaz") — HƏMİŞƏ true. Rəsmi şablonun real
    /// nümunəsində 27 işçidən 2-si bəzi aylarda "Xeyr" idi, amma FİN/ad
    /// göstərilmədiyi üçün KONKRET kimlər olduğu bilinmir — bu, bilinən,
    /// açıq qeyd olunmuş bir fərziyyədir.
    /// </summary>
    public bool EsasIsYeri { get; set; } = true;

    /// <summary>2.2 — Faktiki iş günü (ayın real iş günü − Hissə 3-dəki işləmədiyi gün cəmi).</summary>
    public int FaktikiIsGunu { get; set; }

    /// <summary>2.3 — Hesablanmış əmək haqqı və digər gəlirlər (Maas.BrutMebleg).</summary>
    public decimal HesablananMebleg { get; set; }

    /// <summary>3.1 — Azadolunan və güzəşt verilən cəmi gəlirin məbləği (Bölmə 2 cəmi).</summary>
    public decimal AzadGuzestCemi { get; set; }

    /// <summary>4.1 — Məcburi Dövlət Sosial Sığorta haqqına cəlb olunmayan cəmi məbləğ (Bölmə 3 cəmi).</summary>
    public decimal DsmfAzadCemi { get; set; }

    /// <summary>5.1 — İşsizlikdən sığorta haqqına cəlb olunmayan cəmi məbləğ (Bölmə 4 cəmi).</summary>
    public decimal IssizlikAzadCemi { get; set; }

    /// <summary>6.1 — İcbari tibbi sığorta haqqına cəlb olunmayan cəmi məbləğ (Bölmə 5 cəmi).</summary>
    public decimal ItssAzadCemi { get; set; }
}
