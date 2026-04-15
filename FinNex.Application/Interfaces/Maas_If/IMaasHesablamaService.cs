using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Maas;
using FinNex.Application.DTOs.HR.Mezuniyyet;

namespace FinNex.Application.Interfaces.Maas_If
{
    public interface IMaasHesablamaService
    {
        Task<Result<MaasHesablaNeticesiDto>> FerdiHesablaAsync(FerdiHesablaInputDto input);
        Task<Result<TopluHesablamaNeticesiDto>> TopluHesablaAsync(TopluHesablaInputDto input);

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
        /// Verilmiş brüt məbləğ üçün bütün tutulmaları (gəlir vergisi, DSMF,
        /// İşsizlik, İTSS) və net məbləği hesablayır. FerdiHesablaAsync-ın
        /// istifadə etdiyi eyni VergiPille/MaasParametri mənbəyindən.
        /// </summary>
        Task<MezuniyyetTutulmaDto> TutulmalariHesablaAsync(decimal brut, DateTime tarix);

        /// <summary>
        /// Verilmiş tarixdə işçinin ştat maaşını IsciMaasTarixcesi-dən tapır.
        /// Məzuniyyət pulunun artım əmsalı (K_i = SonMaas / Ay_i_Maas) üçün
        /// istifadə olunur.
        /// </summary>
        Task<decimal> StatMaasiTarixeGoreTapAsync(int isciId, DateTime tarix);
    }
}