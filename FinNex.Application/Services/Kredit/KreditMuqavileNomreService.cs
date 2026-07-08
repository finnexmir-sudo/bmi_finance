using System.Globalization;
using FinNex.Application.DTOs.Kredit.Muqavile;
using FinNex.Application.Interfaces.Kredit;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;

namespace FinNex.Application.Services.Kredit;

/// <summary>
/// Müqavilə nömrələri (odb.muqavile_nomreleri) və məktub jurnalı (odb.xaric_mektub).
///
/// ⚠️ Bu, Oracle-a YAZAN yeganə servisdir. CLAUDE.md istisnası: yalnız bu iki cədvəl.
/// KreditMuqavile:NomreYaz = false (default) → preview, HEÇ NƏ yazılmır.
/// KreditMuqavile:NomreYaz = true → sayğaclar atomik (SELECT ... FOR UPDATE) artırılır.
///
/// Nömrə semantikası BMI-nin Menzil.cs (muq_no_at) məntiqindən götürülüb:
///   kr_zaminlik → kredit müqaviləsi nömrəsi ({k_mno})
///   kr_menzil   → ipoteka müqaviləsi nömrəsi ({i_mno})
///   kr_zaminler → zaminlik running nömrəsi ({zmno1})
/// </summary>
public class KreditMuqavileNomreService : IKreditMuqavileNomreService
{
    private readonly string _connectionString;
    private readonly bool _nomreYaz;

    public KreditMuqavileNomreService(IConfiguration config)
    {
        _connectionString = config["Oracle:ConnectionString"]
            ?? throw new InvalidOperationException("Oracle:ConnectionString konfiqurasiya edilməyib.");
        _nomreYaz = config.GetValue("KreditMuqavile:NomreYaz", false);
    }

    public async Task<MenzilNomreleriDto> MenzilNomreleriAyirAsync(int zaminSayi, CancellationToken ct = default)
    {
        await using var con = new OracleConnection(_connectionString);
        await con.OpenAsync(ct);

        var il = await CariIlAsync(con, ct);

        await using var tx = _nomreYaz ? (OracleTransaction)await con.BeginTransactionAsync(ct) : null;

        // Sətir yoxdursa — yalnız yazı rejimində seed (sıfırlarla) əlavə et
        if (_nomreYaz && !await SetirVarAsync(con, il, ct))
            await SeedElaveEtAsync(con, il, ct);

        // Cari nömrələri oxu (yazı rejimində FOR UPDATE ilə kilidlə)
        var forUpdate = _nomreYaz ? " FOR UPDATE" : "";
        int krZaminlik = 1, krMenzil = 1, krZaminler = 0;

        await using (var read = new OracleCommand(
            $"SELECT kr_zaminlik, kr_menzil, kr_zaminler FROM odb.muqavile_nomreleri WHERE IL = :il{forUpdate}", con)
            { BindByName = true })
        {
            read.Parameters.Add("il", il);
            await using var dr = await read.ExecuteReaderAsync(ct);
            if (await dr.ReadAsync(ct))
            {
                krZaminlik = ToInt(dr[0], 1);
                krMenzil = ToInt(dr[1], 1);
                krZaminler = ToInt(dr[2], 0);
            }
        }

        // Sıfır/mənfi qorunma — brend-yeni ildə (real datada BMI artıq müsbət saxlayır)
        if (krZaminlik <= 0) krZaminlik = 1;
        if (krMenzil <= 0) krMenzil = 1;
        if (krZaminler < 0) krZaminler = 0;

        var netice = new MenzilNomreleriDto
        {
            KreditNo = krZaminlik,
            IpotekaNo = krMenzil,
            Yazildi = false
        };
        for (var i = 1; i <= zaminSayi; i++)
            netice.ZaminNolar.Add(krZaminler + i);

        if (_nomreYaz)
        {
            await using var upd = new OracleCommand(@"
                UPDATE odb.muqavile_nomreleri
                   SET kr_zaminlik = :kr,
                       kr_menzil   = :men,
                       kr_zaminler = :zam
                 WHERE IL = :il", con)
            { BindByName = true };
            upd.Parameters.Add("kr", krZaminlik + 1);
            upd.Parameters.Add("men", krMenzil + 1);
            upd.Parameters.Add("zam", krZaminler + zaminSayi);
            upd.Parameters.Add("il", il);
            await upd.ExecuteNonQueryAsync(ct);
            await tx!.CommitAsync(ct);
            netice.Yazildi = true;
        }

        return netice;
    }

    public async Task<MenzilNomreleriDto> ZaminlikNomreleriAyirAsync(int zaminSayi, CancellationToken ct = default)
    {
        await using var con = new OracleConnection(_connectionString);
        await con.OpenAsync(ct);

        var il = await CariIlAsync(con, ct);

        await using var tx = _nomreYaz ? (OracleTransaction)await con.BeginTransactionAsync(ct) : null;

        // Sətir yoxdursa — yalnız yazı rejimində seed (sıfırlarla) əlavə et
        if (_nomreYaz && !await SetirVarAsync(con, il, ct))
            await SeedElaveEtAsync(con, il, ct);

        // Yalnız zaminlik kreditinin sayğacları — ipoteka (kr_menzil) TOXUNULMUR
        var forUpdate = _nomreYaz ? " FOR UPDATE" : "";
        int krZaminlik = 1, krZaminler = 0;

        await using (var read = new OracleCommand(
            $"SELECT kr_zaminlik, kr_zaminler FROM odb.muqavile_nomreleri WHERE IL = :il{forUpdate}", con)
            { BindByName = true })
        {
            read.Parameters.Add("il", il);
            await using var dr = await read.ExecuteReaderAsync(ct);
            if (await dr.ReadAsync(ct))
            {
                krZaminlik = ToInt(dr[0], 1);
                krZaminler = ToInt(dr[1], 0);
            }
        }

        if (krZaminlik <= 0) krZaminlik = 1;
        if (krZaminler < 0) krZaminler = 0;

        var netice = new MenzilNomreleriDto
        {
            KreditNo = krZaminlik,
            IpotekaNo = 0,   // zaminlik kreditində ipoteka yoxdur
            Yazildi = false
        };
        for (var i = 1; i <= zaminSayi; i++)
            netice.ZaminNolar.Add(krZaminler + i);

        if (_nomreYaz)
        {
            await using var upd = new OracleCommand(@"
                UPDATE odb.muqavile_nomreleri
                   SET kr_zaminlik = :kr,
                       kr_zaminler = :zam
                 WHERE IL = :il", con)
            { BindByName = true };
            upd.Parameters.Add("kr", krZaminlik + 1);
            upd.Parameters.Add("zam", krZaminler + zaminSayi);
            upd.Parameters.Add("il", il);
            await upd.ExecuteNonQueryAsync(ct);
            await tx!.CommitAsync(ct);
            netice.Yazildi = true;
        }

        return netice;
    }

    public async Task<string> MektubQeydiyyatiAsync(DateTime tarix, string icraci, CancellationToken ct = default)
    {
        await using var con = new OracleConnection(_connectionString);
        await con.OpenAsync(ct);

        var il = await CariIlAsync(con, ct);

        if (_nomreYaz)
        {
            await using var ins = new OracleCommand(@"
                INSERT INTO odb.xaric_mektub x (x.gon_yer, x.tarix, x.qisa_mez, x.icraci, x.il)
                VALUES ('Mənzil', TO_DATE(:tar, 'dd-MM-yyyy'), 'mənzil gir sal', :icraci, :il)", con)
            { BindByName = true };
            ins.Parameters.Add("tar", tarix.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture));
            ins.Parameters.Add("icraci", icraci ?? "");
            ins.Parameters.Add("il", il);
            await ins.ExecuteNonQueryAsync(ct);
        }

        // Məktub nömrəsi — BMI məntiqi (mektubElaveEt)
        var mekNoArti = 0;
        await using (var max = new OracleCommand(
            "SELECT MAX(-TO_NUMBER(SUBSTR(t.qey_nom, 5, 5))) mn FROM odb.xaric_mektub t WHERE t.il = :il", con)
            { BindByName = true })
        {
            max.Parameters.Add("il", il);
            var r = await max.ExecuteScalarAsync(ct);
            mekNoArti = ToInt(r, 0);
        }

        return $"{il}-{mekNoArti}";
    }

    private static async Task<string> CariIlAsync(OracleConnection con, CancellationToken ct)
    {
        await using var cmd = new OracleCommand("SELECT TO_CHAR(SYSDATE, 'YYYY') FROM dual", con);
        var r = await cmd.ExecuteScalarAsync(ct);
        return r?.ToString() ?? DateTime.Now.Year.ToString(CultureInfo.InvariantCulture);
    }

    private static async Task<bool> SetirVarAsync(OracleConnection con, string il, CancellationToken ct)
    {
        await using var cmd = new OracleCommand(
            "SELECT COUNT(*) FROM odb.muqavile_nomreleri WHERE IL = :il", con) { BindByName = true };
        cmd.Parameters.Add("il", il);
        var r = await cmd.ExecuteScalarAsync(ct);
        return ToInt(r, 0) > 0;
    }

    private static async Task SeedElaveEtAsync(OracleConnection con, string il, CancellationToken ct)
    {
        await using var cmd = new OracleCommand(@"
            INSERT INTO odb.muqavile_nomreleri
                (IL, KR_SERENCAM, KR_ZAMINLIK, KR_MENZIL, KR_AVTOMOBIL, KR_ZAMINLER, DEPOZIT, KR_KART, KART_ZAMIN, KR_QIZIL)
            VALUES (:il, 0, 0, 0, 0, 0, 0, 0, 0, 0)", con) { BindByName = true };
        cmd.Parameters.Add("il", il);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static int ToInt(object? v, int fallback)
    {
        if (v is null || v is DBNull) return fallback;
        return int.TryParse(v.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var res) ? res : fallback;
    }
}
