using FinNex.Application.Interfaces.Oracle;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;

namespace FinNex.Application.Services.Oracle;

public class OracleService : IOracleService
{
    private readonly string _connectionString;

    public OracleService(IConfiguration config)
    {
        _connectionString = config["Oracle:ConnectionString"]
            ?? throw new InvalidOperationException("Oracle:ConnectionString konfiqurasiya edilməyib.");
    }

    public async Task<List<Dictionary<string, object?>>> SelectAsync(string sql, CancellationToken ct = default)
    {
        var trimmed = sql.TrimStart();
        if (!trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)
            && !trimmed.StartsWith("WITH", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Oracle-da yalnız SELECT sorğusuna icazə var.");

        var result = new List<Dictionary<string, object?>>();

        await using var con = new OracleConnection(_connectionString);
        await con.OpenAsync(ct);

        await using var cmd = new OracleCommand(sql, con)
        {
            CommandTimeout = 30
        };

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var row = new Dictionary<string, object?>();
            for (var i = 0; i < reader.FieldCount; i++)
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            result.Add(row);
        }

        return result;
    }
}
