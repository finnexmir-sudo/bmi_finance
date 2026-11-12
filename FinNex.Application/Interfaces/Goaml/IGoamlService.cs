using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Goaml;

namespace FinNex.Application.Interfaces.Goaml
{
    /// <summary>GOAML — "Əməliyyatlar" tabı.</summary>
    public interface IGoamlService
    {
        /// <summary>Oracle-dan (DOC_DNA_ALL_TAB+RISK) namizəd əməliyyatları oxuyur.
        /// Oracle-a HEÇ NƏ yazılmır.</summary>
        Task<Result<List<GoamlNamizedSetirDto>>> NamizedAxtarAsync(GoamlAxtarisDto dto, CancellationToken ct = default);

        /// <summary>"Risk" tabı — ölkə/qara siyahı/hesab-sahibi vətəndaşlığı əsaslı
        /// namizəd əməliyyatları `ODB.DOC_DNA_ALL_RISK`-dən oxuyur (AYSEL-in
        /// staging cədvəlindən ASILI DEYİL — 10-11.11.2026-da canlı doğrulanıb).
        /// Nəticə eyni <see cref="GoamlNamizedSetirDto"/> formasındadır, ona görə
        /// "Təsdiqlə" axını Əməliyyatlar ilə EYNİDİR (paylaşılır).</summary>
        Task<Result<List<GoamlNamizedSetirDto>>> RiskNamizedAxtarAsync(GoamlAxtarisDto dto, CancellationToken ct = default);

        /// <summary>Operator seçdiyi namizədi təsdiqləyir — bizim ÖZ SQL Server-imizə
        /// yazılır (Oracle-a YOX). Eyni namizəd (filial+tarix+recnum) ikinci dəfə
        /// təsdiqlənə bilməz.</summary>
        Task<Result> TesdiqleAsync(GoamlTesdiqDto dto, int isciId);

        Task<Result<List<GoamlEmeliyyatListDto>>> HamisiniGetirAsync();

        /// <summary>Təsdiqlənmiş TƏK qeydin tam (KYC + audit) detalı — "Bax" ekranı.</summary>
        Task<Result<GoamlEmeliyyatDetalDto>> DetalGetirAsync(int id);

        /// <summary>Təsdiqlənmiş qeydi yumşaq silir — YALNIZ hələ ixrac edilməmiş
        /// (Status==Qaralama) qeydlər üçün (09.11.2026, istifadəçi qərarı: operator
        /// səhvən seçdiyi/artıq lazım olmayan sətri geri götürə bilsin). Silinmiş
        /// qeyd (Candidate açarı) MovcuddurmuAsync-dən çıxır — eyni namizəd lazım
        /// olsa YENİDƏN təsdiqlənə bilər.</summary>
        Task<Result> SilAsync(int id);

        /// <summary>Təsdiqlə formasının GET-i açılanda çağırılır (09.11.2026, istifadəçi
        /// qərarı: "sistem gətirə bilmirsə, işçi doldurur"; 12.11.2026-da HƏR İKİ
        /// tərəfi MÜSTƏQİL qaytaracaq şəkildə düzəldildi — bax <see cref="GoamlHesabSahibiKycHerIkiTerefDto"/>.
        /// Debet VƏ Kredit hesablarının HƏR İKİSİ üçün — hansı BİZİM bankın hesabıdırsa
        /// (BALSCHT_FIZ/BALSCHT_UR üzvlüyü) — sahibinin KYC-sini Oracle-dan (YALNIZ
        /// OXUYUR) tapıb qaytarır. Heç biri bizim deyilsə (xarici/başqa bank) və ya
        /// Oracle əlçatmazdırsa həmin tərəf `null` qalır — operator formanı özü doldurur.
        /// Hansı tərəfin NƏTİCƏDƏ tətbiq olunacağını çağıran tərəf (Tip-in istiqamətinə
        /// görə) qərar verir — bu metod YALNIZ xam datanı tapır, seçim etmir.</summary>
        Task<GoamlHesabSahibiKycHerIkiTerefDto> HesabSahibiKycTapAsync(int filial, string? debet, string? kredit, CancellationToken ct = default);

        /// <summary>Export to JSON (#15, 12.11.2026) — verilmiş tarix aralığında (+ istəyə
        /// bağlı Tip/ReportCode filtri) `Status==Qaralama` olan TƏSDİQLƏNMİŞ qeydləri
        /// goAML-in rəsmi JSON sxeminə (bax `docs/sql/goaml/97_Goaml_JSON_MMX_IN_
        /// Tam_Metn.sql`) uyğun mətnə çevirir və onları `IxracEdilib`-ə keçirir —
        /// NÖVBƏTİ ixrac eyni qeydi TƏKRAR GÖTÜRMƏYƏCƏK. Oracle-a YALNIZ OXU (malumatin_novu,
        /// kurval) aparılır, heç nə yazılmır. Fayla YAZMAQ bu metodun işi DEYİL —
        /// nəticə mətn kimi qaytarılır, DMS-ə yazmaq/yükləmək controller-in işidir
        /// (Kassa/Exchange-in `SablonYolu()` presedentinə uyğun).</summary>
        Task<Result<GoamlJsonIxracNeticeDto>> JsonIxracAsync(GoamlJsonIxracFiltrDto filtr, CancellationToken ct = default);
    }
}
