using FinNex.Domain.Entities.Kredit;

namespace FinNex.Application.Interfaces.Kredit
{
    /// <summary>
    /// Kredit komitəsi üzvlərini idarə edir. Admin komitə üzvlərini təyin edir,
    /// rol verir (sədr/üzv). Komitə qərarını imzalayan üzvlər bu siyahıdan
    /// seçilir.
    /// </summary>
    public interface IKomiteUzvuService
    {
        Task<IList<KomiteUzvu>> HamisiniGetirAsync(bool yalnizAktiv = true);
        Task<bool> IsciKomiteUzvuMu(int isciId);
        Task<KomiteUzvu> TeyinEtAsync(int isciId, KomiteUzvuRolu rol, string? qeyd, int? yaradanIcraciId);
        Task RolDeyisAsync(int komiteUzvuId, KomiteUzvuRolu yeniRol, int? yenileyenIcraciId);
        Task LegvEtAsync(int komiteUzvuId, int? legvEdenIcraciId);
    }
}
