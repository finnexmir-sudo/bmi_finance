using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Communication;
using FinNex.Domain.Entities.Communication;

namespace FinNex.Application.Interfaces.Communication
{
    public interface IBildirisService
    {
        Task<Result<IList<BildirisDto>>> GetIscibildirisleriAsync(int isciId);
        Task<Result> OxunduIsareEtAsync(int bildirisId, int isciId);
        Task<Result> HamisiniOxunduIsareEtAsync(int isciId);
        Task<Result<int>> OxunmamisSayiAsync(int isciId);
        Task<Result> YaratAsync(int isciId, BildirisNovu nov,
            string bashliq, string metn, string? redirectUrl = null,
            int? mezuniyyetId = null, int? icazeId = null, int? mesajId = null);

        /// <summary>
        /// Ləğv/silinmiş məzuniyyətə bağlı, hələ OXUNMAMIŞ bildirişləri yumşaq silir.
        /// Oxunmuşlara toxunulmur — onlar baş vermiş hadisənin tarixçəsidir; oxunmamış
        /// bildiriş isə "sənə iş gəlib" deməkdir və o iş artıq yoxdur.
        /// Qaytarır: silinən bildiriş sayı.
        /// </summary>
        Task<Result<int>> MezuniyyetBildirisleriniSilAsync(int mezuniyyetId);
    }
}
