using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.IcraciNo;

namespace FinNex.Application.Interfaces.HR;

public interface IIcraciNoService
{
    // Bütün işçilər + təyin olunmuş icraçı nömrələri (İcraçı Nömrələri səhifəsi)
    Task<IList<IcraciNoSetirDto>> HamisiniGetirAsync();

    // Toplu təyinat — hər işçi üçün icraçı nömrəsi (boş/0 = silinir). Unikallıq yoxlanılır.
    Task<Result> TopluTeyinEtAsync(IList<IcraciNoTeyinDto> teyinler);
}
