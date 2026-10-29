namespace FinNex.Application.DTOs.Dsmf;

// Əlavə №4 Bölmə 3/4/5-in "combo" vərəqlərindəki qanun maddələrinin mətni —
// HƏRFİ olaraq orijinal Excel şablonlarından köçürülüb (29.10.2026). 31 mümkün
// maddədən YALNIZ sistemdə qarşılığı olan 4-ü (BMI Finance üçün, istifadəçi
// qərarı) avtomatik hesablanır — qalanı (neft-qaz podratçısı, texnologiya
// parkı, diplomatik xidmət, Ələt AİZ və s.) bu banka aid deyil, bura
// DAXİL EDİLMƏYİB.
public static class DsmfIstisnaGostericileri
{
    public const string Bolme3Basliq =
        "Əlavə №4 - Bölmə 3.  Məcburi dövlət sosial sığorta haqqına cəlb olunmayan gəlirlər barədə məlumat";
    public const string Bolme4Basliq =
        "Əlavə №4 - Bölmə 4.  İşsizlikdən sığorta haqqına cəlb olunmayan gəlirlər barədə məlumat";
    public const string Bolme5Basliq =
        "Əlavə №4 - Bölmə 5.  İcbari tibbi sığorta haqqına cəlb olunmayan gəlirlər barədə məlumat";

    public const string Madde529 =
        "5.2.9 - Üç ildən az olmayan müddətə bağlanan müqavilə ilə həyatın yığım sığortası və pensiya sığortası üzrə qeyri-dövlət sektoruna aid edilən sığortaedənin sığortaolunanın gəlirlərinin 50 faizindən çox olmayan hissəsindən Azərbaycan Respublikasının sığortaçılarına ödənilən sığorta haqları";

    public const string Madde5212 =
        "5.2.12 - Sığortaedənin vəsaiti hesabına əmək qabiliyyətini müvəqqəti itirməyə görə verilən müavinət";

    public const string Madde63 =
        "6.3 - \"İşsizlikdən sığorta haqqında\" Qanunun 9.0.1-ci maddəsinə əsasən hesablanmış əməyin ödənişi fonduna daxil olmayan gəlirlər";

    public const string Madde72 =
        "7.2 - \"Tibbi sığorta haqqında\" Qanunun 15-10.1.2-ci maddəsinə əsasən hesablanmış əməkhaqqının tərkibinə daxil olmayan gəlirlər";
}
