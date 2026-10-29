using FinNex.Application.DTOs.Dsmf;

namespace FinNex.Application.Interfaces.Dsmf;

// DSMF Vahid Hesabat Forması — rüblük hesabatlar (29.10.2026, istifadəçi tapşırığı:
// "indi atdigim excellerdeki melumatlari hazirlamaq lazimdir, il ve rub secilsin").
// Pilot əhatə YALNIZ Əlavə №1 Hissə 3-dür (istifadəçi qərarı: "mexanikdir, HR
// modullarından dəqiq hesablana bilər" — bax yuxarı CLAUDE.md qeydi). Qalan 5 cədvəl
// (Hissə 1 — maaş tərkibi, Bölmə 2-5 — vergi güzəşti cədvəlləri) ayrıca mərhələdir.
public interface IDsmfHesabatService
{
    /// <summary>
    /// Əlavə №1 Hissə 3 — işçinin işləmədiyi iş günləri. <paramref name="rub"/> 1-4
    /// (1=Yan-Mar, 2=Apr-İyun, 3=İyul-Sen, 4=Okt-Dek). Yalnız həmin rübdə İSTƏNİLƏN
    /// gün aktiv olmuş işçilər (işə qəbul/işdən çıxma tarixi rüblə kəsişən) nəzərə
    /// alınır; işləməyən gün aşkarlanmayan işçi ÜMUMİYYƏTLƏ sətir almır.
    /// </summary>
    Task<DsmfHissesi3Dto> Hissesi3HazirlaAsync(int il, int rub);

    /// <summary>
    /// Əlavə №4 Bölmə 3 — Məcburi dövlət sosial sığorta haqqına cəlb olunmayan
    /// gəlirlər. 2 sətir avtomatik: 5.2.9 (HYS — işçi+işəgötürən payı) və
    /// 5.2.12 (xəstəlik vərəqəsi üzrə şirkət ödənişi).
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
