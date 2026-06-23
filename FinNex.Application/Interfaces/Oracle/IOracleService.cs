using FinNex.Application.DTOs.Oracle;

namespace FinNex.Application.Interfaces.Oracle;

public interface IOracleService
{
    /// <summary>
    /// Verilmiş SELECT sorğusunu Oracle-da icra edir.
    /// Yalnız SELECT icazəlidir — başqa DML/DDL qəbul edilmir.
    /// maxRows: qaytarılacaq maksimum sətir sayı (default 1000).
    /// </summary>
    Task<List<Dictionary<string, object?>>> SelectAsync(string sql, int maxRows = 1000, CancellationToken ct = default);

    /// <summary>
    /// SELECT sorğusunu icra edib sütun adlarını (sorğudakı sıra ilə) və sətir dəyərlərini qaytarır.
    /// Raw / Excel export üçün — eyni adlı sütunlar itmir, sütun sırası və xam dəyər tipi qorunur.
    /// </summary>
    Task<OracleNetice> SelectXamAsync(string sql, int maxRows = 1000, CancellationToken ct = default);
}
