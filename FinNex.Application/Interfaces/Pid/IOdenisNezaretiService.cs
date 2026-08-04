using FinNex.Application.DTOs.Pid;
using FinNex.Domain.Entities.Pid;

namespace FinNex.Application.Interfaces.Pid;

public interface IOdenisNezaretiService
{
    // Oracle-dan canlı "Ödənişə Nəzarət" siyahısı (Aktiv Müştərilər + ARH_DD son ödəniş)
    Task<OdenisNezaretSiyahiDto> OracleSiyahiAsync();

    // Gray list — ARH_DD (debet 45019…, kredit 89150…) ödənişləri, müştəri üzrə qruplu
    Task<GrayOdenisSiyahiDto> GraySiyahiAsync();

    Task<IList<OdenisNezaretiDto>> HamisiniGetirAsync(BalansNovu? balans = null, string? axtaris = null);
    Task<OdenisNezaretiDto?> IdIleGetirAsync(int id);
    Task<int> YaratAsync(OdenisNezaretiCreateDto dto, int isciId);
    Task<bool> YenileAsync(OdenisNezaretiUpdateDto dto, int isciId);
    Task<bool> SilAsync(int id, int isciId);
}
