namespace FinNex.Application.DTOs.Aml;

/// <summary>
/// «Məlumat Bazası» paketinin BİR vərəqi — BMI-dəki `MelumatBazasi.xlsx`
/// şablonunun bir sekmesinə uyğun gəlir.
/// </summary>
public sealed class MelumatBazasiVereqDto
{
    /// <summary>Vərəq adı — BMI şablonundakı adla EYNİ olmalıdır (Open_Accounts, Exchange…).</summary>
    public string Ad { get; set; } = "";

    /// <summary>İnsan üçün başlıq (ekranda göstərilir).</summary>
    public string Baslik { get; set; } = "";

    /// <summary>`OracleSorgular.SorguAdi` — problem olanda hansı sorğu olduğu bilinsin.</summary>
    public string SorguAdi { get; set; } = "";

    /// <summary>Sorğu tarix aralığı qəbul edirmi (BMI-də 11 sorğudan 8-i qəbul edir).</summary>
    public bool Dovrlu { get; set; }

    public List<string>   Sutunlar { get; set; } = new();
    public List<object?[]> Setirler { get; set; } = new();

    /// <summary>Bu vərəq alınmadısa səbəb — paketin qalanı yenə hazırlanır.</summary>
    public string? Xeta { get; set; }

    public int Say => Setirler.Count;
}

/// <summary>«Məlumat Bazası» — dövr üzrə AML paketi (BMI: <c>MelumatBazasi.cs</c>).</summary>
public sealed class MelumatBazasiNeticeDto
{
    public DateTime BasTarix { get; set; }
    public DateTime SonTarix { get; set; }
    public List<MelumatBazasiVereqDto> Vereqler { get; set; } = new();

    /// <summary>Paket bütövlükdə alınmadısa (məs. Oracle bağlanmadı).</summary>
    public string? Xeta { get; set; }

    public bool Hazirdir  => Xeta == null && Vereqler.Count > 0;
    public int  CemiSetir => Vereqler.Sum(v => v.Say);
    public List<MelumatBazasiVereqDto> XetaliVereqler => Vereqler.Where(v => v.Xeta != null).ToList();
}
