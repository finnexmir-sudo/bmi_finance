using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Maas;

namespace FinNex.Application.Interfaces.HR
{
    /// <summary>
    /// İşçinin son 12 ay qazanc tarixçəsini idarə edir.
    /// 2026 məzuniyyət ödənişi formulu üçün lazımdır.
    /// </summary>
    public interface IIsciAyliqQazancService
    {
        /// <summary>
        /// İşçinin son 12 ayını gətirir (ən yenidən köhnəyə doğru).
        /// </summary>
        Task<Result<IList<IsciAyliqQazancDto>>> GetByIsciAsync(int isciId);

        /// <summary>
        /// Tək qeyd əlavə edir və ya yeniləyir.
        /// Sliding window: əgər həmin işçi üçün artıq 12 qeyd varsa,
        /// ən köhnə qeyd avtomatik silinir.
        /// </summary>
        Task<Result> AddOrUpdateAsync(int isciId, int il, int ay, decimal qazanc, bool elIle, string? qeyd = null,
            decimal dsmfIsci = 0, decimal dsmfIsegoturen = 0);

        /// <summary>
        /// Sistem hesablama nəticəsindən avtomatik qeyd əlavə edir.
        /// HR maaş hesabladığı zaman çağrılır.
        /// Qazanc — məzuniyyət üçün; DSMF məbləğləri xəstəlik düsturu üçün.
        /// </summary>
        Task<Result> AutoInsertFromMaasAsync(int isciId, int il, int ay, decimal qazanc,
            decimal dsmfIsci = 0, decimal dsmfIsegoturen = 0);

        /// <summary>
        /// Tək qeydi silir.
        /// </summary>
        Task<Result> DeleteAsync(int id);

        /// <summary>
        /// Məzuniyyət ödənişi üçün son 12 ayın CƏMİ qazancını qaytarır.
        /// </summary>
        Task<decimal> Son12AyCemiQazancAsync(int isciId);

        /// <summary>
        /// Son 12 ay neçə qeyd var (preview üçün lazımdır).
        /// </summary>
        Task<int> Son12AyQeydSayiAsync(int isciId);
    }
}
