namespace FinNex.Application.DTOs.Dsmf;

// DSMF Vahid Hesabat Forması — Əlavə №1 Hissə 3. İşçinin işləmədiyi iş günləri
// haqqında məlumat. Rüb (3 ay) üzrə hazırlanır (29.10.2026, istifadəçi qərarı:
// "il ve rub secilsin, hemin rub uzre 3 ayin melumatlari istenilir").
public class DsmfHissesi3Dto
{
    public int Il { get; set; }
    public int Rub { get; set; } // 1-4

    public int Ay1 { get; set; }
    public int Ay2 { get; set; }
    public int Ay3 { get; set; }
    public string Ay1Adi { get; set; } = "";
    public string Ay2Adi { get; set; } = "";
    public string Ay3Adi { get; set; } = "";

    public List<DsmfHissesi3SetirDto> Setirler { get; set; } = new();

    /// <summary>
    /// Rübün ilk ayı BUGÜNDƏN sonradırsa (hələ başlamayıb) — true. DSMF yalnız
    /// keçmiş/davam edən dövrü hesabat edir; gələcək rüb üçün "işçi aktiv
    /// deyil" DEYİL, "bu dövr hələ YAŞANMAYIB" deyilir — fərqli görüntü üçün
    /// (29.10.2026, real hadisə: 2027 seçiləndə "boş" yerinə anlamsız tam-ay
    /// iş günü rəqəmləri göründü, bax Hissesi1Dto.GelecekRubdur).
    /// </summary>
    public bool GelecekRubdur { get; set; }

    /// <summary>
    /// DSMF-in "işləməyən gün səbəbi" siyahısındaki 12 koddan sistemin
    /// AVTOMATİK aşkarlaya BİLMƏDİYİ kodların mətni (2,4,7,10,12) — bunlar
    /// sistemdə qarşılığı olan modul/sahə YOXDUR (sosial məzuniyyət, təhsil
    /// məzuniyyəti, növbəli iş, işəmuzd, məhkəmə hökmü). İstifadəçiyə açıq
    /// bildirilir ki, belə hal varsa mühasib bu sətri ƏL İLƏ əlavə etsin —
    /// səssizcə buraxılmır.
    /// </summary>
    public List<string> AvtomatikAskarlanmayanSebebler { get; set; } = new();
}

public class DsmfHissesi3SetirDto
{
    public int SiraNo { get; set; }
    public int IsciId { get; set; }

    /// <summary>"Soyadı, adı, atasının adı" sırası — formanın öz başlığına uyğun.</summary>
    public string AdSoyadAta { get; set; } = "";
    public string Fin { get; set; } = "";

    public int SebebKodu { get; set; } // 1-12
    public string SebebMetni { get; set; } = "";

    /// <summary>Hər ay üçün işləmədiyi İŞ günü sayı (0 = o ayda bu səbəb yoxdur).</summary>
    public int Ay1Gun { get; set; }
    public int Ay2Gun { get; set; }
    public int Ay3Gun { get; set; }
}
