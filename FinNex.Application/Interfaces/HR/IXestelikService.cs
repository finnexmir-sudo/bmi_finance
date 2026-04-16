using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Maas;
using FinNex.Domain.Entities.HR;

namespace FinNex.Application.Interfaces.HR
{
    /// <summary>
    /// Xəstəlik bülletənləri və ödənişlərini idarə edir.
    /// HR sənəd əsasında yazır, sistem avtomatik şirkət payını hesablayır.
    /// </summary>
    public interface IXestelikService
    {
        Task<Result<IList<XestelikDto>>> GetListAsync();
        Task<Result<XestelikDto?>> GetByIdAsync(int id);
        Task<Result<int>> CreateAsync(XestelikCreateDto input, int? hrIsciId);
        Task<Result> DeleteAsync(int id);

        /// <summary>
        /// Real-time hesablama preview — HR forma doldurarkən göstərilir.
        /// </summary>
        Task<Result<XestelikPreviewDto>> PreviewAsync(int isciId, DateTime baslama, DateTime bitme);

        /// <summary>
        /// Müəyyən ay ərzində işçinin xəstəlik ödənişlərini gətirir
        /// (MaasHesablamaService bu metoddan istifadə edir).
        /// </summary>
        Task<List<Xestelik>> AyUzreXestelikleriGetirAsync(int isciId, int il, int ay);
    }
}
