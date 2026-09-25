namespace FinNex.Application.DTOs.Aml;

/// <summary>
/// «Məlumat Bazası» axtarış rejimi — Excel siyahısındakı hansı sütunlar
/// uyğunluq üçün nəzərə alınsın. `BmiLatin.SiyahiQur`-da sentinel
/// (`AdYoxdur`/`Bos`) məcburiyyəti ilə tətbiq olunur, SQL mətninə toxunmur.
/// </summary>
public enum AxtarisRejimi
{
    /// <summary>Ad VƏ FİN/VÖEN — hər ikisi (hansı doludursa) nəzərə alınır. Default.</summary>
    HerIkisi = 0,

    /// <summary>Yalnız ad — FİN/VÖEN Excel-də olsa belə nəzərə alınmır.</summary>
    Ad = 1,

    /// <summary>Yalnız FİN/VÖEN — ad şərti söndürülür.</summary>
    FinVoen = 2
}

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

    /// <summary>Exceldən neçə şəxs axtarıldı (ümumi hədd: <c>MelumatBazasiService.MaxUmumiSetir</c> —
    /// bundan çoxu batch-lərə bölünmür, sadəcə kəsilir).</summary>
    public int AxtarilanSay { get; set; }

    /// <summary>Hansı sütunlar üzrə axtarılıb — Ad / FinVoen / HerIkisi.</summary>
    public AxtarisRejimi Rejim { get; set; }

    /// <summary>Paket alındı, amma deməli bir şey var (məs. siyahı kəsildi).</summary>
    public string? Xeberdarliq { get; set; }

    /// <summary>Paket bütövlükdə alınmadısa (məs. Oracle bağlanmadı).</summary>
    public string? Xeta { get; set; }

    public bool Hazirdir  => Xeta == null && Vereqler.Count > 0;
    public int  CemiSetir => Vereqler.Sum(v => v.Say);
    public List<MelumatBazasiVereqDto> XetaliVereqler => Vereqler.Where(v => v.Xeta != null).ToList();
}
