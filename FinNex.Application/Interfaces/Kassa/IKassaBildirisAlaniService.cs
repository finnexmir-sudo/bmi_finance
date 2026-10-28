using FinNex.Domain.Entities.Kassa;

namespace FinNex.Application.Interfaces.Kassa
{
    public interface IKassaBildirisAlaniService
    {
        Task<IList<KassaBildirisAlani>> HamisiniGetirAsync();

        /// <summary>Kurs təsdiqləndikdə bildiriş göndərilməli, CARİ aktiv işçilərin Id-ləri.</summary>
        Task<IList<int>> AktivIsciIdleriniGetirAsync();

        Task<KassaBildirisAlani> TeyinEtAsync(int isciId, string? qeyd, int? yaradanIcraciId);
        Task LegvEtAsync(int id, int? legvEdenIcraciId);
    }
}
