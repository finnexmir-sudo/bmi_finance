namespace FinNex.Application.DTOs.Muhasibat;

/// <summary>
/// ÖS-6.1 (AMB "Ödəniş Statistikası" Əlavə 6.1) — statistik vahidin müştəri hesabları
/// haqqında statistik hesabat. "Old" (ÖS-6 kombinə şablonu) və "yeni" (OVS yeni 6.1.xlsm)
/// submission-shell-lərinin HƏR İKİSİNDƏ eyni 22 sətir, eyni "sətirlərin şifrləri" (Kod,
/// 1-22) işlədilir — yalnız hüceyrə mövqeyi (sheet/sətir) fərqlidir. Bu DTO Kod-a görə
/// saxlayır ki, nəticə HƏR İKİ şablona (ayrıca ixrac koduna) birə-bir köçürülə bilsin.
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
