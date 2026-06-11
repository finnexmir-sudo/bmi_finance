using FinNex.Domain.Entities.HR;

namespace FinNex.Application.Services.HR
{
    // Mühasibat (proводka) hesabları — açar→hesab. Genişlənən ayar.
    public interface IMuhasibatHesabService
    {
        // Açara görə aktiv hesab nömrəsi (yoxdursa null).
        Task<string?> HesabAlAsync(string acar);

        // Bütün hesablar (idarəetmə üçün).
        Task<IList<MuhasibatHesabi>> HamisiAsync();

        // Açar üzrə hesabı yenilə/yarat.
        Task UpsertAsync(string acar, string ad, string hesabNomresi, bool aktiv = true);
    }
}
