namespace FinNex.Application.DTOs.Kredit.Muqavile;

/// <summary>
/// Daşınmaz Əmlak (ipoteka) müqaviləsi hazırlama formasının POST məlumatı.
/// Oracle-dan gələn kredit datası (k_*) yenidən HesabNo+Ks ilə çəkilir;
/// bu DTO yalnız əl ilə daxil edilən hissəni daşıyır.
/// </summary>
public class MenzilMuqavileYaratDto
{
    // Kreditin kimliyi — POST-da Oracle-dan yenidən oxumaq üçün
    public string HesabNo { get; set; } = "";   // t.licschkre
    public string Ks { get; set; } = "";         // t.subschkre
    public DateTime KreditTarixi { get; set; }   // list-dəki verilmə tarixi (Oracle sorğusu üçün)

    // Müqavilə tarixi — sətirdən (verilmə tarixi) gəlir, dəyişməzdir (k_tar_soz üçün)
    public DateTime MuqavileTarixi { get; set; }

    // Kredit təyinatı — formada seçilir ({k_teyinat})
    public string? Teyinat { get; set; }

    // Borcalanın ölkəsi — formada seçilir ({k_olke})
    public string? BorcalanOlke { get; set; }

    // Borcalan hüquqi şəxs olduqda (r.yurik=1) — direktorun məlumatı əl ilə daxil edilir.
    // Kimlik bəndi ({k_kimlik}): "hüquqi şəxs ŞİRKƏT (VÖEN: ...), direktoru ... DİREKTOR (vəsiqə)".
    public string? DirektorAd { get; set; }
    public string? DirektorVesiqe { get; set; }   // seriya, FİN
    public string? DirektorOlke { get; set; }      // hüquqi şəxsdə "vətəndaşı" ölkəsi direktora aiddir

    // İpoteka (girov) məlumatı
    public string? IpotekaNovu { get; set; }         // {i_ipnovu} — İpoteka / Sonrakı İpoteka
    public string? GirovNovu { get; set; }           // girov obyektinin növü → {girov_tipi} (yiyəlik hal)
    public string? IpotekaUnvan { get; set; }        // {i_ipoteka_unvan}
    public string? Erazi { get; set; }               // {i_erazi}
    public decimal? GirovDeyeri { get; set; }        // {Ipoteka_deyer}
    public string? CixarisNo { get; set; }           // {i_cixarisNo}
    public string? QeydiyyatNo { get; set; }         // {i_qeydiyyatNo}
    public string? ReyestrNo { get; set; }           // {i_reyestrNo}
    public string? DigerCixarisMelumati { get; set; }// {i_diger_cixaris_melumati} — əl ilə əlavə mətn

    // Çıxarış (girov obyekti) detalları — köhnə BMI "Girova aid məlumatlar" formasından.
    // Hamısı {i_diger_cixaris_melumati} blokuna (ipoteka predmeti təsviri) yığılır.
    // Sahə dəyərləri sərbəst mətndir (məs. "120 m²", "6 sot"). Sahələr obyekt tipinə görə
    // formada göstərilir/gizlədilir (ObyektTipi kodu): menzil / ferdi / torpaq / qeyri.
    public string? ObyektTipi { get; set; }          // Hüquq obyektinin adı (kod)
    public string? SeriyaNomre { get; set; }         // çıxarış seriya və nömrəsi
    public string? UmumiSahe { get; set; }           // ümumi sahə
    public string? YasayisSahe { get; set; }         // yaşayış sahəsi
    public string? YardimciSahe { get; set; }        // yardımçı sahə
    public string? TorpaqSahe { get; set; }          // torpaq sahəsi (fərdi ev / torpaq)
    public string? OtaqSayi { get; set; }            // otaqların sayı
    public string? ReyestrKitabNo { get; set; }      // reyestr kitab nömrəsi
    public string? ReyestrVereqNo { get; set; }      // reyestr vərəq nömrəsi
    public DateTime? QeydiyyatTarixi { get; set; }   // qeydiyyat tarixi

    // Girov sahibi borcalandan fərqlidirmi?
    public bool GirovSahibiFerqli { get; set; }
    // Fərqli olduqda — girov sahibinin məlumatı (yalnız GirovSahibiFerqli=true)
    public string? SahibAd { get; set; }             // {i_saa}
    public string? SahibVesiqe { get; set; }         // {i_ves}
    public string? SahibUnvan { get; set; }          // {i_unvan}
    public string? SahibOlke { get; set; }           // {i_olke}
    public string? SahibTel { get; set; }            // {i_tel}

    // Zaminlər (0…N)
    public List<ZaminDaxilDto> Zaminler { get; set; } = new();
}
