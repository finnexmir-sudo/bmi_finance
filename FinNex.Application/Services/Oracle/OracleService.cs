using FinNex.Application.DTOs.Oracle;
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

    public async Task<List<Dictionary<string, object?>>> SelectAsync(string sql, int maxRows = 1000, CancellationToken ct = default)
    {
        sql = YalnizSelect(sql);

        var result = new List<Dictionary<string, object?>>();

        await using var con = new OracleConnection(_connectionString);
        await con.OpenAsync(ct);

        await using var cmd = new OracleCommand(sql, con)
        {
            CommandTimeout = 30
        };

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var count = 0;
        while (count < maxRows && await reader.ReadAsync(ct))
        {
            var row = new Dictionary<string, object?>();
            for (var i = 0; i < reader.FieldCount; i++)
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            result.Add(row);
            count++;
        }

        return result;
    }

    public async Task<OracleNetice> SelectXamAsync(string sql, int maxRows = 1000, CancellationToken ct = default)
    {
        sql = YalnizSelect(sql);

        var netice = new OracleNetice();

        await using var con = new OracleConnection(_connectionString);
        await con.OpenAsync(ct);

        await using var cmd = new OracleCommand(sql, con)
        {
            CommandTimeout = 60
        };

        await using var reader = await cmd.ExecuteReaderAsync(ct);

        // Sütun adları — sorğudakı sıra ilə (eyni adlı sütunlar belə ayrı saxlanır)
        for (var i = 0; i < reader.FieldCount; i++)
            netice.Sutunlar.Add(reader.GetName(i));

        var count = 0;
        while (count < maxRows && await reader.ReadAsync(ct))
        {
            var row = new object?[reader.FieldCount];
            for (var i = 0; i < reader.FieldCount; i++)
                row[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            netice.Setirler.Add(row);
            count++;
        }

        return netice;
    }

    // Yalnız SELECT / WITH icazəlidir — Oracle yazma (DML/DDL) qəti qadağandır.
    private static string YalnizSelect(string sql)
    {
        sql = sql.Trim().TrimEnd(';').TrimEnd();
        var trimmed = sql.TrimStart();
        if (!trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)
            && !trimmed.StartsWith("WITH", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Oracle-da yalnız SELECT sorğusuna icazə var.");
        return sql;
    }
}
