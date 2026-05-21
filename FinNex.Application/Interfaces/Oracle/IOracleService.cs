namespace FinNex.Application.Interfaces.Oracle;

public interface IOracleService
{
    /// <summary>
    /// Verilmiş SELECT sorğusunu Oracle-da icra edir.
    /// Yalnız SELECT icazəlidir — başqa DML/DDL qəbul edilmir.
    /// </summary>
    Task<List<Dictionary<string, object?>>> SelectAsync(string sql, CancellationToken ct = default);
}
