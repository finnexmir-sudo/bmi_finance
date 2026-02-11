using FinNex.Application.DTOs.PR_Document;
using FinNex.Application.DTOs.SenedDovriyyesi;

namespace FinNex.Application.Interfaces.PR_Document
{
    public interface ISenedIdareetmeService
    {
        Task<SenedDashboardDto> DashboardGetirAsync();

        Task<IList<SenedListDto>> SiyahiGetirAsync(
            int? sobeId = null,
            int? senedNovuId = null,
            int? status = null,
            string? acarSoz = null,
            int sehife = 1,
            int sehifeOlcusu = 10);

        Task<int> SayGetirAsync(
            int? sobeId = null,
            int? senedNovuId = null,
            int? status = null,
            string? acarSoz = null);

        Task<SenedDetailDto?> DetayGetirAsync(int id);

        Task<int> YukleAsync(SenedUploadDto dto);

        Task<(byte[] bytes, string contentType, string fileName)?> FaylYukleAsync(int faylId);
    }
}
