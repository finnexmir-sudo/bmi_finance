using FinNex.Application.DTOs.HR.VezifeIsStatusu;
using FinNex.Domain.Entities.HR;

namespace FinNex.Application.Services.HR
{
    /// <summary>
    /// Vəzifə ADINA görə "İşdə statusu" (icraçı / məsul şəxs) təsnifatı —
    /// HR bunu "HR → Vəzifələr → İşdə statusları" səhifəsində bir dəfə qurur.
    /// </summary>
    public interface IVezifeIsStatusuService
    {
        /// <summary>
        /// Bütün TƏKRARSIZ (aktiv) Vəzifə adlarını, hər biri üçün cari statusla
        /// (qurulmayıbsa default İcraçı) qaytarır — idarəetmə səhifəsi üçün.
        /// </summary>
        Task<IList<VezifeIsStatusuDto>> SiyahiAsync();

        /// <summary>Siyahını bir dəfəyə (bulk) yaddaşa yazır — səhifədəki "Saxla" düyməsi.</summary>
        Task SaxlaAsync(IEnumerable<VezifeIsStatusuSetDto> setirler, int? icraciId);

        /// <summary>
        /// Bir vəzifə adının statusunu tapır. Qurulmayıbsa <see cref="VezifeIsStatusuTipi.Icrachi"/>
        /// (default) qaytarır — heç vaxt null olmur, çağıran tərəf "qurulmayıb" halını
        /// ayrıca yoxlamaq istəyirsə <see cref="SiyahiAsync"/>-dəki <c>Qurulub</c> bayrağından istifadə etsin.
        /// </summary>
        Task<VezifeIsStatusuTipi> TapAsync(string? vezifeAdi);
    }
}
