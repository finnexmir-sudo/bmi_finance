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

        return await CehdEtAsync(async token =>
        {
            var result = new List<Dictionary<string, object?>>();

            await using var con = new OracleConnection(_connectionString);
            await con.OpenAsync(token);

            await using var cmd = new OracleCommand(sql, con)
            {
                CommandTimeout = 30
            };

            await using var reader = await cmd.ExecuteReaderAsync(token);
            var count = 0;
            while (count < maxRows && await reader.ReadAsync(token))
            {
                var row = new Dictionary<string, object?>();
                for (var i = 0; i < reader.FieldCount; i++)
                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                result.Add(row);
                count++;
            }

            return result;
        }, ct);
    }

    public async Task<OracleNetice> SelectXamAsync(string sql, int maxRows = 1000, CancellationToken ct = default)
    {
        sql = YalnizSelect(sql);

        return await CehdEtAsync(async token =>
        {
            var netice = new OracleNetice();

            await using var con = new OracleConnection(_connectionString);
            await con.OpenAsync(token);

            await using var cmd = new OracleCommand(sql, con)
            {
                CommandTimeout = 60
            };

            await using var reader = await cmd.ExecuteReaderAsync(token);

            // Sütun adları — sorğudakı sıra ilə (eyni adlı sütunlar belə ayrı saxlanır)
            for (var i = 0; i < reader.FieldCount; i++)
                netice.Sutunlar.Add(reader.GetName(i));

            var count = 0;
            while (count < maxRows && await reader.ReadAsync(token))
            {
                var row = new object?[reader.FieldCount];
                for (var i = 0; i < reader.FieldCount; i++)
                    row[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                netice.Setirler.Add(row);
                count++;
            }

            return netice;
        }, ct);
    }

    // ── KEÇİCİ ŞƏBƏKƏ XƏTASINDA TƏKRAR CƏHD (18.08.2026) ──────────────────
    //
    // Real hadisə: Mühasibat → Balans İcmalı bəzən «ORA-12570: TNS:packet reader
    // failure» verirdi, bir azdan isə eyni səhifə normal açılırdı. Bu, sorğunun
    // və ya kodun səhvi DEYİL — Oracle ilə TCP sessiyası qırılır:
    //   • hovuzdan (pool) gələn bağlantının sessiyasını aradakı firewall/NAT
    //     boşdayanmaya görə səssizcə bağlayıb, ADO.NET isə bunu bilmir;
    //   • uzun sorğu zamanı şəbəkə anlıq kəsilir.
    //
    // Belə xətalarda YENİ bağlantı ilə təkrar cəhd praktiki olaraq həmişə keçir.
    // Təkrar TƏHLÜKƏSİZDİR: bu servis yalnız SELECT icra edir (YalnizSelect) —
    // Oracle-a yazı layihədə qəti qadağandır, ona görə sorğunun iki dəfə getməsi
    // heç nəyi dəyişmir.
    //
    // DİQQƏT: siyahıya YALNIZ şəbəkə/bağlantı xətaları salınır. Sintaksis (ORA-00904),
    // hüquq (ORA-00942) və vaxt aşımı (ORA-01013) təkrarlanmır — onları təkrarlamaq
    // xətanı gizlədib istifadəçini 3 dəfə uzun gözlətməkdən başqa nəyə yaramaz.
    private static readonly int[] KeciciXetalar =
    {
        12570,  // TNS:packet reader failure
        12571,  // TNS:packet writer failure
        12537,  // TNS:connection closed
        12152,  // TNS:unable to send break message
         3113,  // end-of-file on communication channel
         3114,  // not connected to ORACLE
        12547,  // TNS:lost contact
        12560   // TNS:protocol adapter error
    };

    private const int MaxCehd = 3;

    private static async Task<T> CehdEtAsync<T>(Func<CancellationToken, Task<T>> emeliyyat, CancellationToken ct)
    {
        OracleException? sonXeta = null;

        for (var cehd = 1; cehd <= MaxCehd; cehd++)
        {
            try
            {
                return await emeliyyat(ct);
            }
            catch (OracleException ex) when (KeciciXetalar.Contains(ex.Number) && cehd < MaxCehd)
            {
                sonXeta = ex;
                // Ölü bağlantı hovuzda qalmasın — növbəti cəhd təmiz bağlantı alsın.
                OracleConnection.ClearAllPools();
                await Task.Delay(200 * cehd, ct);   // 200 ms, 400 ms
            }
        }

        // Bura yalnız bütün cəhdlər keçici xəta ilə bitəndə düşür.
        // `??` ilə yazmaq OLMAZ: `OracleException` və `InvalidOperationException`
        // arasında ortaq tip çıxarıla bilmir (CS0019) — ayrıca `throw` lazımdır.
        if (sonXeta != null) throw sonXeta;
        throw new InvalidOperationException("Oracle sorğusu icra edilmədi.");
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
