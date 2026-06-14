using FinNex.Application.DTOs.Pid;
using FinNex.Domain.Entities.Pid;

namespace FinNex.Application.Interfaces.Pid;

public interface IOdenisNezaretiService
{
    Task<IList<OdenisNezaretiDto>> HamisiniGetirAsync(BalansNovu? balans = null, string? axtaris = null);
    Task<OdenisNezaretiDto?> IdIleGetirAsync(int id);
    Task<int> YaratAsync(OdenisNezaretiCreateDto dto, int isciId);
    Task<bool> YenileAsync(OdenisNezaretiUpdateDto dto, int isciId);
    Task<bool> SilAsync(int id, int isciId);
}
