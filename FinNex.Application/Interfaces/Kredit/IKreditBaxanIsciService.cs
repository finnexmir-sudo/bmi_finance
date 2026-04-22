using FinNex.Domain.Entities.Kredit;

namespace FinNex.Application.Interfaces.Kredit
{
    /// <summary>
    /// Kredit müraciətlərinə baxa bilən işçilərin siyahısını idarə edir.
    /// Admin istifadəçi işçi seçib bu siyahıya əlavə edir — yalnız siyahıda
    /// olan və aktiv dövrü olan işçilər Müraciət bölməsinə girə bilər.
    /// </summary>
    public interface IKreditBaxanIsciService
    {
        Task<IList<KreditBaxanIsci>> HamisiniGetirAsync();
        Task<bool> BaxaBilerMiAsync(int isciId);
        Task<KreditBaxanIsci> TeyinEtAsync(int isciId, string? qeyd, int? yaradanIcraciId);
        Task LegvEtAsync(int baxanIsciId, int? legvEdenIcraciId);
    }
}
