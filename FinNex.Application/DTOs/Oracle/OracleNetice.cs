namespace FinNex.Application.DTOs.Oracle;

// Raw SELECT nəticəsi — sütun adları (sorğudakı sıra ilə) + sətir dəyərləri.
// Dictionary deyil: eyni adlı sütunlar belə itmir, sütun sırası dəqiq qorunur (Excel export üçün).
public sealed class OracleNetice
{
    public List<string> Sutunlar { get; set; } = new();
    public List<object?[]> Setirler { get; set; } = new();
}
