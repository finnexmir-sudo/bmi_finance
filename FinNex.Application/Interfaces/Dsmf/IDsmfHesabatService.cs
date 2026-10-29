using FinNex.Application.DTOs.Dsmf;

namespace FinNex.Application.Interfaces.Dsmf;

// DSMF Vahid Hesabat Forması — rüblük hesabatlar (29.10.2026, istifadəçi tapşırığı:
// "indi atdigim excellerdeki melumatlari hazirlamaq lazimdir, il ve rub secilsin").
// Altı rəsmi əlavədən BEŞİ qurulub (Hissə 1/3, Bölmə 2/3/4/5) — Hissə 1 bu
// sessiyanın son parçasıdır, qalanı yoxdur.
public interface IDsmfHesabatService
{
    /// <summary>
    /// Əlavə №1 Hissə 1 — işçilər üzrə ümumi məlumat (maaş tərkibi). Hər işçinin
    /// hər ay üzrə 11 göstəricisi (iş günü, hesablanmış məbləğ, Bölmə 2/3/4/5-in
    /// bu işçiyə aid cəmləri) — bax DsmfHesabatService.Hissesi1HazirlaAsync-in
    /// öz şərhi (sütun xəritəsi).
    /// </summary>
    Task<DsmfHissesi1Dto> Hissesi1HazirlaAsync(int il, int rub);

    /// <summary>
    /// Əlavə №1 Hissə 3 — işçinin işləmədiyi iş günləri. <paramref name="rub"/> 1-4
    /// (1=Yan-Mar, 2=Apr-İyun, 3=İyul-Sen, 4=Okt-Dek). Yalnız həmin rübdə İSTƏNİLƏN
    /// gün aktiv olmuş işçilər (işə qəbul/işdən çıxma tarixi rüblə kəsişən) nəzərə
    /// alınır; işləməyən gün aşkarlanmayan işçi ÜMUMİYYƏTLƏ sətir almır.
    /// </summary>
    Task<DsmfHissesi3Dto> Hissesi3HazirlaAsync(int il, int rub);

    /// <summary>
    /// Əlavə №4 Bölmə 2 — Gəlir vergisi üzrə güzəşt və azadolmalar. 5 sətir
    /// avtomatik: 4.2.6/102.1.8 (HYS — yalnız işçi payı), 4.2.5/102.1.6
    /// (standart 200 AZN güzəşt), 4.5.3/102.4.5 (Qaçqın — GuzestNovu.
    /// MecburiKockun), 4.4/102.3 (Əlillər — GuzestNovu.Elillik),
    /// 4.2.7.7/102.1.14.16 (əsgər pulu — manual gəlir, GelirVergisiAzadMebleg).
    /// </summary>
    Task<DsmfIstisnaBolmesiDto> Bolme2HazirlaAsync(int il, int rub);

    /// <summary>
    /// Əlavə №4 Bölmə 3 — Məcburi dövlət sosial sığorta haqqına cəlb olunmayan
    /// gəlirlər. 2 sətir avtomatik: 5.2.9 (HYS — YALNIZ işçi payı, 29.10.2026-da
    /// düzəldildi) və 5.2.12 (xəstəlik vərəqəsi üzrə şirkət ödənişi).
    /// </summary>
    Task<DsmfIstisnaBolmesiDto> Bolme3HazirlaAsync(int il, int rub);

    /// <summary>
    /// Əlavə №4 Bölmə 4 — İşsizlikdən sığorta haqqına cəlb olunmayan gəlirlər.
    /// 1 sətir (6.3) — xəstəlik şirkət ödənişi + manual gəlir növlərindən
    /// IssizliyeCelb=false olanlar birlikdə.
    /// </summary>
    Task<DsmfIstisnaBolmesiDto> Bolme4HazirlaAsync(int il, int rub);

    /// <summary>
    /// Əlavə №4 Bölmə 5 — İcbari tibbi sığorta haqqına cəlb olunmayan gəlirlər.
    /// 1 sətir (7.2) — xəstəlik şirkət ödənişi + manual gəlir növlərindən
    /// ItsseCelb=false olanlar birlikdə.
    /// </summary>
    Task<DsmfIstisnaBolmesiDto> Bolme5HazirlaAsync(int il, int rub);
}
