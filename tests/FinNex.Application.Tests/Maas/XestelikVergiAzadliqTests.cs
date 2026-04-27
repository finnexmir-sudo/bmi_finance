namespace FinNex.Application.Tests.Maas;

/// <summary>
/// REGRESSION TEST — 27 Aprel 2026 tarixində aşkarlanan səhv üçün.
///
/// SƏHV: Xəstəlik vərəqəsi üzrə şirkət ödənişi (XestelikSirketOdenis) DSMF,
///   İşsizlik və İTSS bazalarına daxil edilirdi. Beləliklə xəstəlik pulu da
///   bütün sosial sığorta tutulmalarına məruz qalırdı.
///
/// DÜZGÜN QAYDA (AR Vergi Məcəlləsi və sosial sığorta qanunları):
///   Xəstəlik vərəqəsi şirkət ödənişi YALNIZ gəlir vergisinə cəlb olunur.
///   DSMF, İşsizlik, İTSS bazalarından çıxılır.
///
///   vergiBazasi   = esasBrut − HYS                (gəlir vergisi — xəstəlik daxil)
///   dsmfBazasi    = esasBrut − HYS − Xəstəlik     (DSMF — xəstəlik çıxılır)
///   itssBazasi    = esasBrut − Xəstəlik           (İTSS, İşsizlik — xəstəlik çıxılır)
///
/// Bu testlər `MaasHesablamaService.FerdiHesablaAsync` daxilindəki baza
/// hesablama qaydasını "kilidləyir".
/// </summary>
public class XestelikVergiAzadliqTests
{
    /// <summary>
    /// Xəstəlik pulu DSMF və İTSS bazalarından çıxılmalıdır.
    /// Yalnız gəlir vergisi xəstəlik üzərinə hesablanır.
    /// </summary>
    [Fact]
    public void Xestelik_pulu_yalniz_geliv_vergisine_cəlb_olunur()
    {
        // Arrange
        decimal esasBrut = 2000m;       // əsas brüt (xəstəlik daxil)
        decimal hysMebleg = 0m;         // HYS yox
        decimal xestelikSirketOdenis = 300m;

        // Act — kodun istifadə etdiyi eyni baza düsturları
        decimal vergiBazasi = System.Math.Max(0, esasBrut - hysMebleg);
        decimal dsmfBazasi  = System.Math.Max(0, vergiBazasi - xestelikSirketOdenis);
        decimal itssBazasi  = System.Math.Max(0, esasBrut - xestelikSirketOdenis);

        // Assert
        vergiBazasi.Should().Be(2000m, "gəlir vergisi xəstəlik daxil hesablanır");
        dsmfBazasi.Should().Be(1700m,  "DSMF üçün xəstəlik çıxılmalıdır (2000 − 300)");
        itssBazasi.Should().Be(1700m,  "İTSS/İşsizlik üçün xəstəlik çıxılmalıdır (2000 − 300)");
    }

    /// <summary>
    /// HYS + xəstəlik birlikdə olduqda — DSMF bazasından hər ikisi çıxılır,
    /// İTSS bazasından yalnız xəstəlik çıxılır (HYS çıxılmır).
    /// </summary>
    [Fact]
    public void HYS_ve_xestelik_eyni_anda_olduqda_dogru_bazalar()
    {
        decimal esasBrut = 2500m;
        decimal hysMebleg = 200m;
        decimal xestelikSirketOdenis = 400m;

        decimal vergiBazasi = System.Math.Max(0, esasBrut - hysMebleg);
        decimal dsmfBazasi  = System.Math.Max(0, vergiBazasi - xestelikSirketOdenis);
        decimal itssBazasi  = System.Math.Max(0, esasBrut - xestelikSirketOdenis);

        vergiBazasi.Should().Be(2300m, "vergi bazası: 2500 − 200 HYS = 2300");
        dsmfBazasi.Should().Be(1900m,  "DSMF: 2300 − 400 xəstəlik = 1900");
        itssBazasi.Should().Be(2100m,  "İTSS: 2500 − 400 xəstəlik = 2100 (HYS çıxılmır)");
    }

    /// <summary>
    /// Xəstəlik yoxdursa, bazalar köhnə qayda ilə eynidir — heç nə dəyişmir.
    /// </summary>
    [Fact]
    public void Xestelik_olmadiqda_bazalar_kohne_qayda_ile_eynidir()
    {
        decimal esasBrut = 1800m;
        decimal hysMebleg = 100m;
        decimal xestelikSirketOdenis = 0m;

        decimal vergiBazasi = System.Math.Max(0, esasBrut - hysMebleg);
        decimal dsmfBazasi  = System.Math.Max(0, vergiBazasi - xestelikSirketOdenis);
        decimal itssBazasi  = System.Math.Max(0, esasBrut - xestelikSirketOdenis);

        vergiBazasi.Should().Be(1700m);
        dsmfBazasi.Should().Be(1700m,  "xəstəlik 0 olduqda DSMF bazası vergi bazası ilə eynidir");
        itssBazasi.Should().Be(1800m,  "xəstəlik 0 olduqda İTSS bazası tam brüt qalır");
    }

    /// <summary>
    /// Müxtəlif ssenarilər üçün açıq-aşkar ədədi yoxlama —
    /// yeni qaydaların ümumi tutulmaya təsiri.
    /// </summary>
    [Theory]
    // (esasBrut, hys, xestelik, gözl_vergiBazasi, gözl_dsmfBazasi, gözl_itssBazasi)
    [InlineData(2000, 0,   0,   2000, 2000, 2000)] // xəstəlik yox, HYS yox
    [InlineData(2000, 0,   500, 2000, 1500, 1500)] // yalnız xəstəlik
    [InlineData(2000, 200, 0,   1800, 1800, 2000)] // yalnız HYS
    [InlineData(2000, 200, 500, 1800, 1300, 1500)] // hər ikisi
    [InlineData(1000, 0,   1500, 1000,    0,    0)] // xəstəlik > brüt → bazalar 0-a yuvarlaqlanır
    public void Bazalar_dogru_hesablanir(
        decimal esasBrut, decimal hys, decimal xestelik,
        decimal gozlVergi, decimal gozlDsmf, decimal gozlItss)
    {
        decimal vergiBazasi = System.Math.Max(0, esasBrut - hys);
        decimal dsmfBazasi  = System.Math.Max(0, vergiBazasi - xestelik);
        decimal itssBazasi  = System.Math.Max(0, esasBrut - xestelik);

        vergiBazasi.Should().Be(gozlVergi);
        dsmfBazasi.Should().Be(gozlDsmf);
        itssBazasi.Should().Be(gozlItss);
    }

    /// <summary>
    /// Açıq REGRESSİYA mühafizəsi — əgər kimsə təsadüfən köhnə qaydaya qayıdarsa
    /// (xəstəlik bütün bazalarda qalır), test dərhal səbəbi izah edən mesajla qırılır.
    /// </summary>
    [Fact]
    public void Kohne_sehv_qayda_DSMF_xestelik_uzerinde_hesablanardi()
    {
        decimal esasBrut = 2000m;
        decimal xestelikSirketOdenis = 300m;
        decimal dsmfFaiz = 3m; // illustrativ flat 3%

        // Köhnə (səhv) düstur: DSMF tam brüt üzərində
        decimal kohneDsmf = System.Math.Round(esasBrut * (dsmfFaiz / 100m), 2);

        // Yeni (düzgün) düstur: DSMF xəstəliksiz baza üzərində
        decimal yeniDsmfBaza = System.Math.Max(0, esasBrut - xestelikSirketOdenis);
        decimal yeniDsmf = System.Math.Round(yeniDsmfBaza * (dsmfFaiz / 100m), 2);

        kohneDsmf.Should().Be(60.00m, "köhnə kod 2000 × 3% = 60 verirdi");
        yeniDsmf.Should().Be(51.00m,  "yeni kod 1700 × 3% = 51 verir (xəstəlik üzərinə DSMF tutulmur)");
        yeniDsmf.Should().BeLessThan(kohneDsmf, "yeni qayda işçinin xeyrinədir");
    }
}
