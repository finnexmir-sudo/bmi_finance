namespace FinNex.Application.Tests.Maas;

/// <summary>
/// Xəstəlik ödənişinin sosial töhmətlərdən azad olması qaydası üçün testlər.
///
/// Qayda:
///   - Gəlir vergisi:           ŞAMİL OLUR (xəstəlik vergi bazasına daxildir)
///   - DSMF / İşsizlik / İTSS:  AZADDIR (bu bazalardan çıxılır)
///
/// Bazalar (FerdiHesablaAsync-də):
///   vergiBazasi = max(0, esasBrut − HYS)                       // xəstəlik DAXİL
///   dsmfBazasi  = max(0, esasBrut − HYS − xestelikSirketOdenis)
///   itssBazasi  = max(0, esasBrut − xestelikSirketOdenis)      // unemployment və ITSS
/// </summary>
public class XestelikVergiBazasiTests
{
    private static (decimal vergi, decimal dsmf, decimal itss) Bazalar(
        decimal esasBrut, decimal hys, decimal xstSirketOdenis)
    {
        decimal vergi = System.Math.Max(0, esasBrut - hys);
        decimal dsmf = System.Math.Max(0, esasBrut - hys - xstSirketOdenis);
        decimal itss = System.Math.Max(0, esasBrut - xstSirketOdenis);
        return (vergi, dsmf, itss);
    }

    [Fact]
    public void Xestelik_vergi_bazasinda_qalir_amma_DSMF_ITSS_den_cixilir()
    {
        // esasBrut=2000, HYS yoxdur, xəstəlik şirkət ödənişi=300
        var (vergi, dsmf, itss) = Bazalar(2000m, 0m, 300m);

        vergi.Should().Be(2000m, "xəstəlik gəlir vergisinə cəlb olunur");
        dsmf.Should().Be(1700m, "xəstəlik DSMF-dən azaddır");
        itss.Should().Be(1700m, "xəstəlik İTSS və İşsizlikdən də azaddır");
    }

    [Fact]
    public void HYS_ve_xestelik_eyni_anda_olduqda_DSMF_hem_ikisini_de_cixir()
    {
        // esasBrut=2500, HYS=200, xəstəlik=300
        var (vergi, dsmf, itss) = Bazalar(2500m, 200m, 300m);

        vergi.Should().Be(2300m, "vergi bazası: 2500 − HYS 200 = 2300 (xəstəlik daxil)");
        dsmf.Should().Be(2000m, "DSMF: 2500 − HYS 200 − xəstəlik 300 = 2000");
        itss.Should().Be(2200m, "İTSS: 2500 − xəstəlik 300 = 2200 (HYS daxil)");
    }

    [Fact]
    public void Xestelik_olmasa_bazalar_ferqlenmir()
    {
        var (vergi, dsmf, itss) = Bazalar(2000m, 100m, 0m);

        vergi.Should().Be(1900m);
        dsmf.Should().Be(1900m, "xəstəlik 0 olduqda DSMF və vergi bazası eynidir");
        itss.Should().Be(2000m, "İTSS HYS-i çıxmır");
    }

    [Fact]
    public void Konkret_misal_xestelik_DSMF_de_yox_amma_vergide_var()
    {
        // İşçi: maaş 1500, 5 iş günü xəstəlik = 300 ₼ şirkət ödənişi
        // Brüt gəlirə xəstəlik daxildir: 1500 + 300 = 1800
        decimal esasBrut = 1800m;
        decimal xstOdenis = 300m;

        var (vergi, dsmf, itss) = Bazalar(esasBrut, 0m, xstOdenis);

        // Gəlir vergisi (14% nümunə) — xəstəlik daxil → 1800 × 14% = 252
        // DSMF (3%) — xəstəlik xaric → 1500 × 3% = 45
        // İşsizlik (0.5%) — xəstəlik xaric → 1500 × 0.5% = 7.5
        // İTSS (2%) — xəstəlik xaric → 1500 × 2% = 30
        var verginumune = System.Math.Round(vergi * 0.14m, 2);
        var dsmfNumune = System.Math.Round(dsmf * 0.03m, 2);
        var issizlikNumune = System.Math.Round(itss * 0.005m, 2);
        var itssNumune = System.Math.Round(itss * 0.02m, 2);

        verginumune.Should().Be(252.00m);
        dsmfNumune.Should().Be(45.00m);
        issizlikNumune.Should().Be(7.50m);
        itssNumune.Should().Be(30.00m);
    }
}
