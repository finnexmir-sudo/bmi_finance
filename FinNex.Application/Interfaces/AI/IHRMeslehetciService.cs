using FinNex.Domain.Entities.AI;

namespace FinNex.Application.Interfaces.AI;

public interface IHRMeslehetciService
{
    /// <summary>
    /// Mərhələ 1: Sualın hansı qanun kateqoriyasına aid olduğunu təyin edir.
    /// Qaytarır: "emek" | "vergi" | "dsmf" | "itss" | "hamisi"
    /// </summary>
    Task<string> KateqoriyaTapAsync(string sual);

    /// <summary>
    /// Mərhələ 2: Sualı cavablandırır. Qanun konteksti varsa onu əsas götürür.
    /// </summary>
    Task<string> SualSorAsync(
        string sual,
        List<HRSohbetMesaj> tarixce,
        string? qanunKonteksti = null,
        string? menbeAdi = null);
}
