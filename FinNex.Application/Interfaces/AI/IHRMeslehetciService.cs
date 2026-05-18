using FinNex.Domain.Entities.AI;

namespace FinNex.Application.Interfaces.AI;

public interface IHRMeslehetciService
{
    /// <summary>
    /// Mərhələ 1: Sualın hansı qanun kateqoriyasına aid olduğunu təyin edir.
    /// movcudKateqoriyalar boşdursa "hamisi" qaytarır.
    /// </summary>
    Task<string> KateqoriyaTapAsync(string sual, List<string> movcudKateqoriyalar);

    /// <summary>
    /// Mərhələ 2: Sualı cavablandırır. Qanun konteksti varsa onu əsas götürür.
    /// </summary>
    Task<string> SualSorAsync(
        string sual,
        List<HRSohbetMesaj> tarixce,
        string? qanunKonteksti = null,
        string? menbeAdi = null);
}
