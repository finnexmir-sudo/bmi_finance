using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Reyting;

public interface IReytingService
{
    // ── Kateqoriyalar ─────────────────────────────────────────────
    Task<IList<ReytingKateqoriyasiDto>> KateqoriyalariGetirAsync();
    Task<Result> KateqoriyaSaxlaAsync(ReytingKateqoriyasiSaveDto dto);
    Task<Result> KateqoriyaSilAsync(int id);

    // ── Parametrlər ───────────────────────────────────────────────
    Task<IList<ReytingParametriDto>> ParametrləriGetirAsync();
    Task<Result> ParametrSaxlaAsync(ReytingParametriSaveDto dto);

    // ── Hesablama ─────────────────────────────────────────────────
    // Bir işçi üçün müəyyən ay hesabla (və ya cari ay)
    Task<Result> ReytinqHesablaAsync(int isciId, int il, int ay);

    // Bütün aktiv işçilər üçün toplu hesablama (aylıq batch)
    Task<Result> TopluHesablaAsync(int il, int ay);

    // ── Manual əlavə ─────────────────────────────────────────────
    Task<Result> ManualXalElavEtAsync(ManualReytingElavEtDto dto, int elavEdenUserId);

    // ── Sorğular ─────────────────────────────────────────────────
    Task<IList<IsciReytinqiDto>> IsciReytinqleriGetirAsync(int il, int ay);
    Task<IsciReytinqiDto?> IsciReytinqiGetirAsync(int isciId, int il, int ay);

    // İşçinin cari (ən son) reytinqi — Cüzdanım üçün
    Task<IsciCariReytinqDto?> IsciCariReytinqiniGetirAsync(int isciId);

    // Redim zamanı əmsal almaq
    Task<(decimal pulAmsali, decimal saatAmsali)> IsciAmsallariGetirAsync(int isciId);
}
