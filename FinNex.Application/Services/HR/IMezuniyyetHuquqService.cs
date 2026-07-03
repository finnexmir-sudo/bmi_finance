using FinNex.Application.DTOs.HR.Mezuniyyet;

namespace FinNex.Application.Services.HR
{
    /// <summary>
    /// İllik məzuniyyət hüququnu (əsas + staj + uşaq əlavələri) avtomatik hesablayır.
    /// Yalnız OXUMA — bazaya yazmır. Balans mühərriki və yoxlama səhifəsi bunu istifadə edir.
    /// </summary>
    public interface IMezuniyyetHuquqService
    {
        /// <summary>Bütün aktiv işçilər üçün verilən tarixə görə illik hüququ hesablayır.</summary>
        Task<IList<MezuniyyetHuquqDto>> HesablaAsync(DateTime? tarix = null);
    }
}
