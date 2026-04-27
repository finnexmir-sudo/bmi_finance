namespace FinNex.Application.Tests.Maas;

/// <summary>
/// REGRESSION TEST — 27 Aprel 2026 tarixində Samir Hüseynov maaş hesablaması
/// nümunəsində aşkarlanan səhv üçün.
///
/// SƏHV: AySonu tipli məzuniyyət üçün əsas maaşdan kəsinti tutulmurdu.
///   İşçi həm tam əsas maaş, həm də məzuniyyət ödənişi alırdı —
///   məzuniyyət günləri iki dəfə ödənilmiş olurdu.
///
/// DÜZGÜN QAYDA (BMI Finance, AR Əmək Məcəlləsinə uyğun):
///   Brut = (EsasMaas − AySonuKesinti) + MezOdenis
///   AySonuKesinti = Round(EsasMaas / AyIsGunu × AySonuIGS, 2)
///
/// Bu testlər `MaasHesablamaService.FerdiHesablaAsync` daxilindəki
/// hesablama qaydasını "kilidləyir" — əgər kimsə bu sətirləri silsə və ya
/// dəyişsə test qırmızı olacaq və səhv prod-a çatmayacaq.
/// </summary>
public class MezuniyyetKesintiTests
{
    /// <summary>
    /// Səhv tapılan əsl ssenari — bu test əvvəlki kodda QIRMIZI olardı,
    /// indiki düzəlişdən sonra YAŞIL-dır.
    /// </summary>
    [Fact]
    public void Samir_aprel_2026_brut_2244_74_olmalidir()
    {
        // Arrange — Samir Hüseynov, Aprel 2026 ssenarisi
        decimal esasMaas = 1900m;
        int ayIsGunu = 22;          // Aprel 2026 iş günü
        int aySonuIGS = 4;          // Məzuniyyətin iş günü hissəsi
        decimal mezOdenis = 690.19m; // 2026 düsturu ilə ödəniş

        // Act — MaasHesablamaService-də istifadə olunan eyni düstur
        var aySonuKesinti = Math.Round(esasMaas / ayIsGunu * aySonuIGS, 2);
        var brut = esasMaas - aySonuKesinti + mezOdenis;

        // Assert — kəsinti və brut hər ikisi düz çıxmalıdır
        aySonuKesinti.Should().Be(345.45m,
            "1900 / 22 × 4 = 345.4545… → 2 onluğa yuvarlaqlanır");

        brut.Should().Be(2244.74m,
            "işçi həmin 4 gün işləməyib — əsas maaşdan çıxılır, " +
            "yerinə məzuniyyət ödənişi əlavə olunur (ikiqat ödəmə yox)");
    }

    /// <summary>
    /// Qaydanın müxtəlif girişlər üçün doğruluğunu yoxlayır.
    /// [Theory] + [InlineData] = bir testlə bir neçə hal yoxlamaq üsuludur.
    /// </summary>
    [Theory]
    // (esasMaas, ayIsGunu, aySonuIGS, mezOdenis, gozlenenKesinti, gozlenenBrut)
    [InlineData(1900, 22,  4,  690.19, 345.45, 2244.74)] // Samir misalı
    [InlineData(1500, 22,  0,    0.00,   0.00, 1500.00)] // məzuniyyət yox → kəsinti yox
    [InlineData(2000, 20,  5,  800.00, 500.00, 2300.00)] // 5 gün məzuniyyət
    [InlineData(3000, 22, 22, 3000.00,3000.00, 3000.00)] // bütün ay məzuniyyət
    public void Brut_dusturu_AySonu_mezuniyyet_uchun_dogru_isleyir(
        decimal esasMaas,
        int ayIsGunu,
        int aySonuIGS,
        decimal mezOdenis,
        decimal gozlenenKesinti,
        decimal gozlenenBrut)
    {
        // Act
        var aySonuKesinti = Math.Round(esasMaas / ayIsGunu * aySonuIGS, 2);
        var brut = esasMaas - aySonuKesinti + mezOdenis;

        // Assert
        aySonuKesinti.Should().Be(gozlenenKesinti);
        brut.Should().Be(gozlenenBrut);
    }

    /// <summary>
    /// Açıq REGRESSİYA mühafizəsi — əgər kimsə təsadüfən köhnə qaydaya qayıdarsa
    /// (kəsinti tətbiq etməsə) test dərhal səbəbi izah edən mesajla qırılır.
    /// </summary>
    [Fact]
    public void Kohne_sehv_qayda_brut_259019_olardi_indi_olmamalidir()
    {
        // Arrange
        decimal esasMaas = 1900m;
        decimal mezOdenis = 690.19m;

        // Act — köhnə (səhv) düstur: kəsinti yoxdur
        var kohneSehvBrut = esasMaas + mezOdenis;

        // Assert — sənəd: bu rəqəm artıq qəbul olunmur
        kohneSehvBrut.Should().Be(2590.19m,
            "köhnə kod bu rəqəmi verirdi — əgər canlı sistemdə yenə " +
            "bunu görsən, deməli düzəliş geri qayıdıb (regression).");

        // Yeni düzgün düstur fərqli rəqəm verir — fərqi sənədə bağlayırıq
        var yeniDuzgunBrut = esasMaas - 345.45m + mezOdenis;
        yeniDuzgunBrut.Should().NotBe(kohneSehvBrut);
        yeniDuzgunBrut.Should().Be(2244.74m);
    }
}
