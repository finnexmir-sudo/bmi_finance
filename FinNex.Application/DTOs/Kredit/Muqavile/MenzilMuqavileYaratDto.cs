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

    // Müqavilə tarixi (k_tar_soz üçün) və məktub tarixi
    public DateTime MuqavileTarixi { get; set; }
    public DateTime MektubTarixi { get; set; }

    // İpoteka (girov) məlumatı
    public string? IpotekaNovu { get; set; }         // {i_ipnovu}
    public string? IpotekaUnvan { get; set; }        // {i_ipoteka_unvan}
    public string? Erazi { get; set; }               // {i_erazi}
    public decimal? GirovDeyeri { get; set; }        // {Ipoteka_deyer}
    public string? CixarisNo { get; set; }           // {i_cixarisNo}
    public string? QeydiyyatNo { get; set; }         // {i_qeydiyyatNo}
    public string? ReyestrNo { get; set; }           // {i_reyestrNo}
    public string? DigerCixarisMelumati { get; set; }// {i_diger_cixaris_melumati}
    public string? GirovTipi { get; set; }           // {girov_tipi} — məktubda

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
