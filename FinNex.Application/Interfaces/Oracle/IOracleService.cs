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

    /// <summary>16.11.2026, DİAQNOSTİKA — tətbiqin RUNTIME-da İSTİFADƏ ETDİYİ
    /// connection string-in "Data Source=..." hissəsini (host:port/servis)
    /// şifrəsiz qaytarır. "GOAML Oracle Diaqnostikası" səhifəsi istifadəçinin
    /// `appsettings.json`/User Secrets-də GÖRDÜYÜ ilə tətbiqin ƏSLİNDƏ
    /// BAĞLANDIĞI ünvanın EYNİ olduğunu sübut etmək üçün (env variable/başqa
    /// config mənbəyi dəyəri ÜSTƏLƏYƏ bilər — bu, həmin riski bağlayır).</summary>
    string MaskedConnectionInfo { get; }
}
