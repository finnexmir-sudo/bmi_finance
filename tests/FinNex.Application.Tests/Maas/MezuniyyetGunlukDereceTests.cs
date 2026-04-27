namespace FinNex.Application.Tests.Maas;

/// <summary>
/// Məzuniyyət ödənişinin GÜNDƏLİK DƏRƏCƏ qaydası üçün testlər.
///
/// Qayda (BMI Finance):
///   gunlukMaas    = CariMaas / AyİşGün
///   gunlukMezPul  = S / 12 / 30.4
///   Ödəniş        = MAX(gunlukMaas, gunlukMezPul) × İGS
///
/// Əvvəlki uyğunsuzluq: MH təqvim günü, ƏH iş günü ilə hesablanırdı —
/// total-ları müqayisə etmək həftəsonlarına görə şişirdilmiş ödəniş verirdi.
/// </summary>
public class MezuniyyetGunlukDereceTests
{
    private static decimal Hesabla(decimal cariMaas, int ayIsGun, decimal S, int isGun)
    {
        if (isGun <= 0) return 0;
        decimal gunlukMaas = (cariMaas > 0 && ayIsGun > 0) ? cariMaas / ayIsGun : 0m;
        decimal gunlukMezPul = (S > 0) ? S / 12m / 30.4m : 0m;
        decimal gunlukDerece = System.Math.Max(gunlukMaas, gunlukMezPul);
        return System.Math.Round(gunlukDerece * isGun, 2);
    }

    [Fact]
    public void Gunluk_maas_buyukdurse_o_secilir()
    {
        // CariMaas = 1900, ayIsGun = 22 → gunlukMaas = 86.36
        // S = 22800, S/12/30.4 = 62.50 → gunlukMezPul = 62.50
        // İGS = 4 → 86.36 × 4 = 345.45
        var odenis = Hesabla(1900m, 22, 22800m, 4);
        odenis.Should().Be(345.45m);
    }

    [Fact]
    public void Gunluk_mezuniyyet_pulu_buyukdurse_o_secilir()
    {
        // İşçi son ay artım almışdı: S təqvim əsasında daha böyük gündəlik verir
        // CariMaas = 1500, ayIsGun = 22 → gunlukMaas = 68.18
        // S = 30000, S/12/30.4 = 82.24 → gunlukMezPul böyükdür
        // İGS = 5 → 82.24 × 5 = 411.18
        var odenis = Hesabla(1500m, 22, 30000m, 5);
        odenis.Should().Be(411.18m);
    }

    [Theory]
    [InlineData(1900, 22, 22800, 0, 0)]       // İGS=0 → ödəniş yox
    [InlineData(0,    22, 22800, 4, 250.00)]  // CariMaas yoxdur → yalnız gündəlik məz.pulu
    [InlineData(1900, 22, 0,     4, 345.45)]  // S yoxdur → yalnız gündəlik maaş
    public void Sherh_helleri(decimal cariMaas, int ayIsGun, decimal S, int isGun, decimal gozlenen)
    {
        Hesabla(cariMaas, ayIsGun, S, isGun).Should().Be(gozlenen);
    }

    /// <summary>
    /// Köhnə qayda hektarlardan boş ödənişlər verirdi: təqvim günü dövrünü
    /// (məs. 11 təqvim günü) iş günü dövrünə (məs. 4 iş günü) qarşı keçirirdi.
    /// Yeni qaydada hər iki dərəcə eyni iş günü sayına vurulduğundan,
    /// nəticə əmək haqqına bərabər və ya yüksək olur — heç vaxt iki qat deyil.
    /// </summary>
    [Fact]
    public void Hefte_sonu_padding_artiq_yoxdur()
    {
        // 11 təqvim günü = 4 iş günü + 7 həftəsonu (köhnə MH bunu hesaba alırdı)
        // Köhnə: MH = 22800/12/30.4 × 11 = 687.50, ƏH = 1900/22 × 4 = 345.45 → 687.50 (şişirdilmiş)
        // Yeni:  MAX(86.36, 62.50) × 4 iş günü = 345.45 (ədalətli)
        var yeni = Hesabla(1900m, 22, 22800m, 4);
        yeni.Should().Be(345.45m);

        // Şişirdilmiş köhnə dəyər artıq qəbul olunmur
        var kohne = System.Math.Round(22800m / 12m / 30.4m * 11m, 2);
        kohne.Should().Be(687.50m);
        yeni.Should().NotBe(kohne);
    }
}
