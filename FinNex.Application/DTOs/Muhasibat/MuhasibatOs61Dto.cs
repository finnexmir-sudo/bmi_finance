namespace FinNex.Application.DTOs.Muhasibat;

/// <summary>
/// ÖS-6.1 (AMB "Ödəniş Statistikası" Əlavə 6.1) — statistik vahidin müştəri hesabları
/// haqqında statistik hesabat.
///
/// 19.11.2026 — real "OS old 2026.xls" ("ÖS-6" vərəqi) VƏ "OS yeni 2026.xlsm" ("ÖS-Bank-6"
/// vərəqi) şablonları canlı tutuşdurulub: Kod (1-22) YALNIZ KÖHNƏ şablonda bizim bu DTO-nun
/// sətir sırası ilə BİRƏ-BİR üst-üstə düşür (sətirlərin şifrəsi = Kod, sətir = Kod+19).
/// YENİ (AMB-nin restrukturizasiya etdiyi) şablonda isə: (a) "depozit"→"əmanət",
/// "tələbolunanadək"→"tələbli" kimi adlar DƏYİŞİB, (b) "kredit hesabları" artıq TOP-LEVEL
/// sətir deyil, "cari hesablar"ın ALT-sətridir, (c) hər bir "ondan məsafədən açılan..."
/// üçün ƏLAVƏ, bizim DTO-da QARŞILIĞI OLMAYAN boş sətirlər var — ona görə YENİ şablona
/// yazarkən Kod-un ÖZÜ YOX, AYRI bir (bizim Kod → yeni şablonun Kod-u) xəritəsi işlədilir
/// (bax `HesabatController.Os61YeniKodXeritesi`).
/// </summary>
public class MuhasibatOs61Dto
{
    public int Il { get; set; }
    public int Ay { get; set; }
    public bool Ugurlu { get; set; } = true;
    public string? Xeta { get; set; }
    public List<MuhasibatOs61SetirDto> Setirler { get; set; } = new();
}

public class MuhasibatOs61SetirDto
{
    /// <summary>Bölmələrin N-si (A sütunu) — "1", "1.1", "2.1" və s. Alt-sətirlərdə (B13/B20/B26/B27/B33/B40
    /// tipli "ondan sahibkarlıq..." sətirləri) boşdur — bax <see cref="AltSetir"/>.</summary>
    public string? BolmeNo { get; set; }

    /// <summary>Göstəricinin adı (B sütunu).</summary>
    public string Ad { get; set; } = "";

    /// <summary>Sətirlərin şifrəsi (C sütunu) — 1..22, old/yeni şablonlarının HƏR İKİSİNDƏ EYNİ.</summary>
    public int Kod { get; set; }

    /// <summary>Girinti/alt-sətir olub-olmaması (yalnız göstərmə üçün, "ondan ..." sətirləri).</summary>
    public bool AltSetir { get; set; }

    // Hesabat ayı üzrə — Hesab sayı
    public decimal? D_HesabSayiCemi { get; set; }
    public decimal? E_HesabSayiXariciValyuta { get; set; }
    // Hesabat ayı üzrə — Hesab sahiblərinin sayı
    public decimal? F_SahibSayi { get; set; }

    // Dövrün sonuna cəmi — Hesab sayı
    public decimal? G_HesabSayiCemi { get; set; }
    public decimal? H_HesabSayiXariciValyuta { get; set; }
    // Dövrün sonuna cəmi — ondan aktiv hesablar üzrə
    public decimal? I_AktivCemi { get; set; }
    public decimal? J_AktivXariciValyuta { get; set; }
    // Dövrün sonuna cəmi — Hesab sahiblərinin sayı
    public decimal? K_SahibSayi { get; set; }

    /// <summary>Bu sətrin dəyəri mənbə (old/yeni) şablonunda ƏL İLƏ yazılan/sabit saxlanılan bir
    /// dəyərdir (məs. J23="2" sabit ədəd, K12/K14=0 sabit) — Oracle-dan HESABLANMIR, boş qalır.
    /// Önizləmədə operatora açıq göstərilməlidir ki, "0/boş" ilə "əl ilə dəyər" qarışdırılmasın.</summary>
    public string? ElIleQeyd { get; set; }
}
