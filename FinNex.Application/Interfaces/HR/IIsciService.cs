using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Isci;
using FinNex.Application.Interfaces;
using FinNex.Domain.Entities.HR;

public interface IIsciService : IServiceAsync<Isci, IsciListDto, IsciCreateDto, IsciUpdateDto>
{
    Task<IsciDetailDto?> GetIsciDetailsAsync(int id);
    Task<IList<IsciListDto>> GetIscilerBySobeIdAsync(int sobeId);
    Task<bool> CheckFinExistsAsync(string fin);
    Task<Result<List<IsciListDto>>> SearchIscilerByFinAsync(string fin);
    Task<Result> UpdateSalaryWithHistoryAsync(int isciId, decimal yeniMaas, string? emrNo);
    Task<Result<IList<IsciMaasTarixcesiDto>>> GetMaasTarixcesiAsync(int isciId);
    Task<Result> TeyinatDeyisAsync(int isciId, int departamentId, int vezifeId, DateTime baslamaTarixi);
    Task<Result> TeyinatRedakteEtAsync(int isciId, int departamentId, int vezifeId, DateTime? bitmeTarixi = null);
    Task<Result<int?>> GetAktivDepartamentIdAsync(int isciId);
    Task<decimal> GetCariMaasAsync(int isciId);

    /// <summary>
    /// İşçinin bank IBAN-ını yeniləyir (IsciMaliye.BankHesabNo).
    /// Boşluq və kiçik hərflər avtomatik təmizlənir. Format yoxlanılır:
    /// AZ + 2 rəqəm + 4 hərf + 20 rəqəm. Maliye sətri yoxdursa yaradılır.
    /// </summary>
    Task<Result> IbanYenileAsync(int isciId, string? iban);

    Task<IList<IsciHesabDto>> GetIsciHesabSiyahisiAsync();
    Task<Result> HesabYenileAsync(int isciId, string? hesab);

    /// <summary>
    /// Server-side səhifələmə və axtarış üçün.
    /// </summary>
    /// <param name="tab">"aktiv" və ya "cixmis"</param>
    /// <param name="search">Ad / soyad / FIN axtarışı (optional)</param>
    /// <param name="page">1-based səhifə nömrəsi</param>
    /// <param name="pageSize">Səhifə ölçüsü</param>
    Task<(IList<IsciListDto> Items, int TotalCount, int AktivCount, int CixmisCount)> GetPagedAsync(
        string tab, string? search, int page, int pageSize);
}
