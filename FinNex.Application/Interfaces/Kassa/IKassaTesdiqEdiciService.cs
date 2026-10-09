using FinNex.Domain.Entities.Kassa;

namespace FinNex.Application.Interfaces.Kassa
{
    /// <summary>
    /// Kassa valyuta kursunu təsdiqləyə/imtina edə bilən işçilərin siyahısını
    /// idarə edir. Admin işçi seçib bu siyahıya əlavə edir — yalnız siyahıda
    /// olan və aktiv dövrü olan işçilər təsdiq/imtina edə bilər.
    /// </summary>
    public interface IKassaTesdiqEdiciService
    {
        Task<IList<KassaTesdiqEdici>> HamisiniGetirAsync();
        Task<bool> TesdiqEdeBilerMiAsync(int isciId);
        Task<KassaTesdiqEdici> TeyinEtAsync(int isciId, string? qeyd, int? yaradanIcraciId);
        Task LegvEtAsync(int tesdiqEdiciId, int? legvEdenIcraciId);
    }
}
