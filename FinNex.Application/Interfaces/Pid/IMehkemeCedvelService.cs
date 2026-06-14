using FinNex.Application.DTOs.Pid;

namespace FinNex.Application.Interfaces.Pid;

public interface IMehkemeCedvelService
{
    Task<IList<MehkemeCedvelListDto>> HamisiniGetirAsync(string? axtaris = null);
    Task<(int isSayi, int iclasSayi)> ImportAsync(List<MehkemeCedvelImportDto> isler, int isciId);
    Task<bool> SilAsync(int id, int isciId);
}
