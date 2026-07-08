using FinNex.Domain.Entities.Pid;

namespace FinNex.Application.DTOs.Pid;

// Məhkəmə işlərinin monitorinqi (dashboard) — yalnız SQL datası (Oracle çağırışı yoxdur).
public class MehkemeMonitorDto
{
    // ── KPI sayğacları ──
    public int CemIs        { get; set; }
    public int AktivIs      { get; set; }   // Hazırlanır + Məhkəmədə + İcra
    public int Hazirlanir   { get; set; }
    public int Mehkemede    { get; set; }
    public int Icrada       { get; set; }
    public int Tamamlandi   { get; set; }
    public int Baglandi     { get; set; }

    public decimal CemQalanBorc    { get; set; }   // bütün işlər
    public decimal AktivQalanBorc  { get; set; }   // yalnız aktiv işlər (Hazırlanır+Məhkəmədə+İcra)
    public decimal CemMehkemeXerci { get; set; }

    public int ZaminliIs        { get; set; }   // ən azı bir zamini olan iş sayı
    public int YaxinlasanIclas7 { get; set; }   // növbəti 7 gün
    public int YaxinlasanIclas30{ get; set; }   // növbəti 30 gün
    public int GecikmisIclas    { get; set; }   // tarixi keçmiş, hələ açıq işdə iclas (xəbərdarlıq)

    // ── Paylanmalar (qrafiklər) ──
    public List<MonitorAdSayDto> NovUzre   { get; set; } = new();   // növ üzrə
    public List<MonitorAdSayDto> RayonUzre { get; set; } = new();   // rayon üzrə (top)
    public List<MonitorAySayDto> AyUzre    { get; set; } = new();   // son 12 ay üzrə yeni işlər

    // ── Siyahılar ──
    public List<YaxinlasanGorusDto> YaxinlasanIclaslar { get; set; } = new();   // növbəti 30 gün
    public List<MonitorBorcluDto>   EnBoyukBorclu       { get; set; } = new();   // ən yüksək qalan borc (aktiv)

    // ── Kredit portfeli (Oracle "CariKataloq" sorğusu — adi + problemli) ──
    public bool    OracleVar       { get; set; }      // CariKataloq tapıldı və icra olundu
    public string? OracleXeta      { get; set; }
    public int     OracleKreditSayi{ get; set; }      // ümumi kredit sayı
    public decimal OracleEsasBorc  { get; set; }      // Σ (qaliq + vk_qaliq) — əsas borc qalığı (ƏSAS göstərici)
    public decimal OracleVkQaliq   { get; set; }      // Σ vk_qaliq — vaxtı keçmiş əsas borc
    public decimal OracleFaizBorcu { get; set; }      // Σ (faiz_meblegi + vk_faiz_meblegi) — faiz qalığı (yalnız məlumat)
    public List<MonitorQrupDto> Item01Uzre { get; set; } = new();   // status (item_01) üzrə
    public List<MonitorQrupDto> GirovUzre  { get; set; } = new();   // girovun növü üzrə
    public List<MonitorQrupDto> GecikmeZolaqlari { get; set; } = new();   // gecikmə (real_overdue_day) zolaqları — risk evristik

    // ── Məhkəmə / İcra kartları — Kredit portfeli (item_01 status) üzrə ──
    // Məhkəmə statusları: QƏTNAMƏ, MƏHKƏMƏ BAXIŞ, APELLYASİYA MƏHKƏMƏSİ,
    //   MƏHKƏMƏ MÜHASİBATLIQ EKSPERTİZASI, ALİ MƏHKƏMƏ, MƏHKƏMƏ ƏMRİ
    // İcra statusları: İCRADA, NOTARIUS ICRA QEYDI
    public int     PortfelMehkemedeSay { get; set; }   // məhkəmə statuslarının sayı
    public int     PortfelIcradaSay    { get; set; }   // icra statuslarının sayı
    public int     PortfelCemSay       { get; set; }   // ümumi = məhkəmə + icra
    public decimal PortfelEsasBorc     { get; set; }   // Σ (qaliq + vk_qaliq) yalnız yuxarıdakı statuslarda
}

// Oracle qrup (status / girov) — say + qalıq
public class MonitorQrupDto
{
    public string  Ad     { get; set; } = "";
    public int     Say    { get; set; }
    public decimal Mebleg { get; set; }
}

public class MonitorAdSayDto
{
    public string Ad  { get; set; } = "";
    public int    Say { get; set; }
}

public class MonitorAySayDto
{
    public string Ay  { get; set; } = "";   // "MM/yy"
    public int    Say { get; set; }
}

public class MonitorBorcluDto
{
    public int             Id               { get; set; }
    public string          BorcluAd         { get; set; } = "";
    public string?         QeydiyyatNomresi { get; set; }
    public decimal?        QalanBorc        { get; set; }
    public MehkemeIsiStatus Status          { get; set; }
    public string?         Qeydiyyati       { get; set; }
}
