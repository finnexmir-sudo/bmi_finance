using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Maas;
using FinNex.Application.DTOs.HR.Mezuniyyet;

namespace FinNex.Application.Interfaces.Maas_If
{
    public interface IMaasHesablamaService
    {
        // saxla=true → bazaya yazır (normal hesablama). saxla=false → DRY-RUN:
        // eyni rəqəmləri hesablayır, amma heç nə bazaya yazılmır (önizləmə üçün).
        Task<Result<MaasHesablaNeticesiDto>> FerdiHesablaAsync(FerdiHesablaInputDto input, bool saxla = true);
        Task<Result<TopluHesablamaNeticesiDto>> TopluHesablaAsync(TopluHesablaInputDto input, bool saxla = true);

        /// <summary>
        /// Hemin aya dusen tesdiqli mezuniyyet IS gunlerini sayir (köhnə — backward compat).
        /// </summary>
        Task<int> MezuniyyetGunleriniSayAsync(int isciId, int il, int ay);

        /// <summary>
        /// Həmin aya düşən təsdiqli məzuniyyət üçün həm təqvim günü (GS),
        /// həm iş günü (İGS) qaytarır. İş günü = şənbə/bazar/bayram çıxılmış.
        /// </summary>
        Task<(int TeqvimGun, int IsGun)> MezuniyyetGunleriniSayGenisAsync(int isciId, int il, int ay);

        /// <summary>
        /// 2026 qaydası (köhnə imza — geri uyğunluq).
        /// </summary>
        Task<decimal> MezuniyyetOdenisiniHesablaAsync(int isciId, int il, int ay, int isGunSayi);

        /// <summary>
        /// 2026 qaydası — V2 formula:
        ///   S    = Son 12 ayın cəmi qazancı (IsciAyliqQazanc cədvəli)
        ///   MH   = S / 12 / 30.4 × GS    (təqvim günü əsaslı)
        ///   ƏH   = CariMaas / AyİşGün × İGS  (iş günü əsaslı)
        ///   Ödəniş = MAX(MH, ƏH)
        /// </summary>
        Task<decimal> MezuniyyetOdenisiniHesablaV2Async(int isciId, int il, int ay, int teqvimGun, int isGun);

        /// <summary>
        /// Verilmiş tarix aralığı üçün tam məzuniyyət ödənişi hesablamasını
        /// (ay-ay bölünməsi + düz mətn izahatı ilə) qaytarır. Məzuniyyət bir
        /// neçə aya düşdükdə hər ay üçün ayrıca MH/ƏH hesablanır və cəmlənir.
        /// </summary>
        Task<MezuniyyetOdenisHesablamaDto> MezuniyyetOdenisiDetalliHesablaAsync(
            int isciId, DateTime baslama, DateTime bitme);

        /// <summary>
        /// Qabaqcadan ödənilmiş məzuniyyətin AY-AY paylarını (brüt / vergi / net)
        /// qaytarır — mühasibin «aylara bölünmüş» uçot modeli üçün TƏK MƏNBƏ.
        /// Həm maaş hesablaması (vergi bazası), həm Mühasib Detail səhifəsi
        /// bunu çağırır; iki nüsxə saxlanılmır.
        /// <paramref name="hedefNet"/> — faktiki ödənilmiş NET; verilsə netlərin
        /// cəmi ona qəpiyinə bağlanır (qalıq son aya yazılır).
        /// </summary>
        Task<List<MezuniyyetAvansAyPayiDto>> MezuniyyetAvansAyPaylariAsync(
            int isciId, DateTime baslama, DateTime bitme, decimal? hedefNet = null);

        /// <summary>
        /// Verilmiş brüt məbləğ üçün bütün tutulmaları (gəlir vergisi, DSMF,
        /// İşsizlik, İTSS) və net məbləği hesablayır. FerdiHesablaAsync-ın
        /// istifadə etdiyi eyni VergiPille/MaasParametri mənbəyindən.
        /// </summary>
        Task<MezuniyyetTutulmaDto> TutulmalariHesablaAsync(decimal brut, DateTime tarix, int? isciId = null);

        /// <summary>
        /// Verilmiş tarixdə işçinin ştat maaşını IsciMaasTarixcesi-dən tapır.
        /// Məzuniyyət pulunun artım əmsalı (K_i = SonMaas / Ay_i_Maas) üçün
        /// istifadə olunur.
        /// </summary>
        Task<decimal> StatMaasiTarixeGoreTapAsync(int isciId, DateTime tarix);

        /// <summary>
        /// Toplu hesablama ekranında preview üçün məzuniyyətin 3 dəyərini
        /// qaytarır (həqiqi FerdiHesablaAsync ilə eyni məntiqdə):
        ///  - IsGun      — həmin aya düşən təsdiqli məzuniyyət iş günləri (bütün tiplər)
        ///  - Kesinti    — esas maaş / ayıniş günü × mez.iş günü (hər 2 tipdə tətbiq olunur)
        ///  - AySonuOdenisi — yalnız AySonuOdenis tipli qeydlər üçün 2026 düsturu ilə ödəniş
        /// </summary>
        Task<(int IsGun, decimal Kesinti, decimal AySonuOdenisi)> MezuniyyetPreviewAsync(
            int isciId, int il, int ay);

        /// <summary>
        /// Verilmiş ay üçün iş günlərini sayır (BayramGunu + həftəsonu qaydaları ilə).
        /// </summary>
        Task<int> AyIsGunSayiniHesablaAsync(int il, int ay);

        /// <summary>
        /// Öz hesabına (ödənişsiz) məzuniyyətin bu aydakı REAL İŞ GÜNLƏRİ sayı — Ə.M. 129.
        /// Yalnız baza maaş KƏSİNTİSİ üçün (məzuniyyət haqqı ÖDƏNMİR). Toplu hesablama
        /// preview-ı bu rəqəmi FerdiHesablaAsync-ın saxladığı kəsinti ilə eyni etmək
        /// üçün istifadə edir. Həftəsonu və hesablanmayan bayramlar çıxılır.
        /// </summary>
        Task<int> OzHesabinaIsGunuSayAsync(int isciId, int il, int ay);

        /// <summary>
        /// İşdən çıxma (çıxış) kəsintisi preview — işçi həmin ay işdən çıxıbsa,
        /// ayrılma tarixindən sonrakı iş günlərinə görə tutulan kəsinti (yoxsa 0).
        /// Toplu hesablama önizləməsini real hesablama ilə uyğunlaşdırmaq üçün.
        /// </summary>
        Task<decimal> CixisKesintisiPreviewAsync(int isciId, int il, int ay);

        /// <summary>
        /// Toplu hesablama preview üçün — əvvəlki ayın maaşı hesablandıqdan
        /// sonra yaradılan xəstəlik/məzuniyyət qeydləri əsasında cari ayın maaşına
        /// tətbiq olunacaq korreksiyanı qaytarır.
        ///   - Kesinti: artıq ödənilmiş günlərə görə əvvəlki ay gündəlik dərəcəsi ilə kəsinti
        ///   - Gelir:   həmin günlər üçün xəstəlik haqqı və/və ya məzuniyyət ödənişi
        ///   - Aciq:    insan-oxuna izahat (UI-də göstərmək üçün)
        /// </summary>
        Task<(decimal Kesinti, decimal Gelir, string? Aciq)> EvvelkiAyKorreksiyasiPreviewAsync(
            int isciId, int il, int ay);
    }
}