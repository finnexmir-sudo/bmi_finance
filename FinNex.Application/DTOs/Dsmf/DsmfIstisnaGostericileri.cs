namespace FinNex.Application.DTOs.Dsmf;

// Əlavə №4 Bölmə 3/4/5-in "combo" vərəqlərindəki qanun maddələrinin mətni —
// HƏRFİ olaraq orijinal Excel şablonlarından köçürülüb (29.10.2026). 31 mümkün
// maddədən YALNIZ sistemdə qarşılığı olan 4-ü (BMI Finance üçün, istifadəçi
// qərarı) avtomatik hesablanır — qalanı (neft-qaz podratçısı, texnologiya
// parkı, diplomatik xidmət, Ələt AİZ və s.) bu banka aid deyil, bura
// DAXİL EDİLMƏYİB.
public static class DsmfIstisnaGostericileri
{
    // Bölmə 2 (29.10.2026) — "Əlavə №4 - Hissə 1" vərəqində saxlanılan combo
    // siyahısı yalnız QISA kod daşıyır (Bölmə 3/4/5-in combo-su kimi UZUN hüquqi
    // mətn YOXDUR) — mətnlər mənbə Excel-dən hərfi köçürülüb, uydurulmayıb.
    public const string Bolme2Basliq =
        "Əlavə №4 - Bölmə 2. Gəlir vergisi üzrə güzəşt və azadolmalar barədə məlumat";

    public const string Madde1018 = "4.2.6 - VM-nin 102.1.8-ci maddəsi";      // HYS — işçi VƏ işəgötürən payı birlikdə (istifadəçi qərarı, 29.10.2026 — bax DsmfHesabatService.HysIsegoturenMapAsync)
    public const string Madde1016 = "4.2.5 - VM-nin 102.1.6-cı maddəsi";      // standart 200 AZN güzəşt (brüt ≤ 2500)
    public const string Madde10245 = "4.5.3 - VM-nin 102.4.5-ci maddəsi";     // Qaçqın → GuzestNovu.MecburiKockun (istifadəçi qərarı)
    public const string Madde1023 = "4.4 - VM-nin 102.3-cü maddəsi üzrə";     // Əlillik → GuzestNovu.Elillik
    public const string Madde10211416 = "4.2.7.7 - VM-nin 102.1.14.16-cı maddəsi"; // əsgər pulu — manual gəlir, GelirVergisiAzadMebleg

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
