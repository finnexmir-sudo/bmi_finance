using FinNex.Application.Common.Results;
using FinNex.Domain.Entities.HR;

namespace FinNex.Application.Interfaces.HR
{
    /// <summary>
    /// Qabaqcadan ödənilmiş məzuniyyətin BİRDƏN ÇOX aya düşən hissəsi üçün
    /// avtomatik "əvəzləşmə" (reconciliation) Avans qeydləri (26.11.2026).
    ///
    /// NİYƏ VAR: qabaqcadan ödəniş TAM MƏBLƏĞİ ödəniş ayında (ilk ay) bank
    /// köçürməsi ilə işçiyə verilir, AMMA tutulma/vergi bazası
    /// (MezuniyyetAvansAyPaylariAsync) həmin məbləği MƏZUNİYYƏT GÜNLƏRİNİN
    /// düşdüyü AYLARA bölür. Payroll-un sonrakı ay(lar)ın hesablaması bu
    /// pulun artıq verildiyini BİLMİR — mühasib bunu indiyədək HƏR sonrakı ay
    /// üçün əl ilə bir "Avans" sətri yazıb NET-dən çıxdıraraq düzəldirdi (real
    /// hadisə: Fuad Qulu Zadə, noyabr ayı 242.20 ₼ fərq). Bu servis həmin əl
    /// işini avtomatlaşdırır.
    ///
    /// TƏK çağırış nöqtəsi: <see cref="EvezlesmeAvanslariniHazirlaAsync"/>.
    /// Həm `HR/MezuniyyetOdenisController.Planla` (yeni qeydlər, MƏCBURİ addım),
    /// həm manual bərpa endpoint-i (köhnə, bu funksiyadan ƏVVƏL `Planla` olunmuş
    /// qeydlər üçün) bunu çağırır — iki yerdə ayrıca hesablama YAZILMIR.
    /// </summary>
    public interface IMezuniyyetAvansService
    {
        /// <summary>
        /// Verilmiş Mezuniyyet üçün (əgər leave birdən çox təqvim ayına
        /// düşürsə) HƏR sonrakı ay üçün YENİ (hələ bazaya YAZILMAMIŞ,
        /// `YaratAsync` ilə stage edilmiş) bir əvəzləşmə Avans qeydi
        /// yaradır/qaytarır.
        ///
        /// <paramref name="odenenNet"/> — faktiki ödənilən (bank köçürməsi)
        /// NET məbləğ; <c>MezuniyyetAvansAyPaylariAsync</c>-in `hedefNet`
        /// parametrinə ötürülür ki, ay-ay netlərin cəmi bu rəqəmə qəpiyinə
        /// bağlansın (Detail səhifəsinin işlətdiyi EYNİ qayda).
        ///
        /// Leave YALNIZ bir aya düşürsə — boş, uğurlu nəticə qaytarır (heç nə
        /// yaradılmır, bu, XƏTA DEYİL, ən çox rast gəlinən haldır).
        ///
        /// İDEMPOTENT: eyni (MezuniyyetId, Il, Ay) üçün artıq bir əvəzləşmə
        /// qeydi varsa, onu TƏKRAR yaratmır (sakitcə ötürür) — manual bərpa
        /// endpoint-inin təkrar klikindən qorunma.
        ///
        /// Qaytarılan siyahı YALNIZ STAGE edilib (`YaratAsync` çağırılıb,
        /// `SaveChangesAsync` ÇAĞIRILMAYIB) — ÇAĞIRAN TƏRƏF öz dəyişiklikləri
        /// ilə BİRLİKDƏ, TƏK bir `YaddaSaxlaAsync()` ilə commit etməlidir ki,
        /// Mezuniyyet statusunun dəyişməsi VƏ əvəzləşmə qeydlərinin yaranması
        /// ATOMİK olsun (biri uğursuz olsa, o biri də bazaya düşməsin).
        /// </summary>
        Task<Result<List<Avans>>> EvezlesmeAvanslariniHazirlaAsync(
            int mezuniyyetId, decimal odenenNet, int? muhasibIsciId);
    }
}
