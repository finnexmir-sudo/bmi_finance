using System.Globalization;
using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Kredit.Muqavile;
using FinNex.Application.Interfaces.Kredit;
using FinNex.Application.Interfaces.Oracle;
using FinNex.Domain.Entities.Kredit;
using FinNex.Domain.Entities.Sorgular;
using FinNex.Domain.Interfaces;

namespace FinNex.Application.Services.Kredit;

public class MuqavileSayghacImportService : IMuqavileSayghacImportService
{
    private readonly IUnitOfWork _uow;
    private readonly IOracleService _oracle;
    private readonly IMuqavileSayghacService _saygac;

    // Oracle sorğusu Admin → Oracle Sorğular-da saxlanılır (layihə qaydası).
    // Seed: docs/sql/kredit/Muqavile_Sayghac_OracleSorgu.sql
    private const string SorguAdi = "KREDIT_SAYGAC_ORACLE";

    public MuqavileSayghacImportService(
        IUnitOfWork uow, IOracleService oracle, IMuqavileSayghacService saygac)
    {
        _uow = uow;
        _oracle = oracle;
        _saygac = saygac;
    }

    public async Task<Result<MuqavileSayghacKocurmeDto>> VeziyyetAsync(CancellationToken ct = default)
    {
        try
        {
            var setirler = await OracleSetirlerAsync(ct);

            // FinNex tərəfi — mövcud sayğaclar (növ+il → SonNomre)
            var movcud = (await _uow.Repository<MuqavileSayghaci>()
                    .HamisiniGetirAsync(x => !x.Silinib, izlemeden: true))
                .ToList();

            var netice = new MuqavileSayghacKocurmeDto();

            foreach (var s in setirler)
            {
                var il = Tam(s, "IL");
                if (il == null) continue;   // ilsiz sayğac sətri mənasızdır

                foreach (var (novu, meta) in MuqavileSayghacService.Novler)
                {
                    // Sütun sorğuda yoxdursa həmin sayğac sadəcə göstərilmir —
                    // idxal sətir-sətir gedir, bir sütunun olmaması digərlərini dayandırmır.
                    // (Sütun adı böyük/kiçik hərflə gələ bilər — müqayisə həssas deyil.)
                    if (!s.Keys.Contains(meta.OracleSutun, StringComparer.OrdinalIgnoreCase)) continue;

                    var xam = Tam(s, meta.OracleSutun) ?? 0;
                    if (xam < 0) xam = 0;

                    // Çevrilmə: Oracle "növbəti" saxlayırsa 1 çıxılır (FinNex "sonuncu" saxlayır)
                    var yeni = meta.OracleNovbetiSaxlayir ? Math.Max(xam - 1, 0) : xam;

                    var fin = movcud.FirstOrDefault(x => x.Novu == novu && x.Il == il.Value);

                    netice.Setirler.Add(new MuqavileSayghacKocurmeSetirDto
                    {
                        Novu                  = novu,
                        NovuAdi               = meta.Ad,
                        OracleSutun           = meta.OracleSutun,
                        Il                    = il.Value,
                        OracleDeyer           = xam,
                        OracleNovbetiSaxlayir = meta.OracleNovbetiSaxlayir,
                        YeniSonNomre          = yeni,
                        FinNexSonNomre        = fin?.SonNomre
                    });
                }
            }

            netice.Setirler = netice.Setirler
                .OrderByDescending(x => x.Il).ThenBy(x => x.Novu).ToList();

            return Result<MuqavileSayghacKocurmeDto>.Ok(netice);
        }
        catch (Exception ex)
        {
            return Result<MuqavileSayghacKocurmeDto>.Fail($"Vəziyyət alınmadı: {ex.Message}");
        }
    }

    public async Task<Result<MuqavileSayghacKocurmeNeticeDto>> IlKocurAsync(
        int il, int? istifadeciId, CancellationToken ct = default)
    {
        try
        {
            var veziyyet = await VeziyyetAsync(ct);
            if (!veziyyet.Success || veziyyet.Data == null)
                return Result<MuqavileSayghacKocurmeNeticeDto>.Fail(veziyyet.Message ?? "Vəziyyət alınmadı.");

            var setirler = veziyyet.Data.Setirler.Where(x => x.Il == il).ToList();
            if (setirler.Count == 0)
                return Result<MuqavileSayghacKocurmeNeticeDto>.Fail($"{il} ili üçün Oracle-da sayğac tapılmadı.");

            var netice = new MuqavileSayghacKocurmeNeticeDto { Il = il };

            foreach (var s in setirler)
            {
                ct.ThrowIfCancellationRequested();

                // Artıq eyni dəyərdədirsə toxunma — təkrar basmaq təhlükəsiz olsun
                if (s.Kocurulub) { netice.Kecilen++; continue; }

                await _saygac.SonNomreTeyinEtAsync(s.Novu, il, s.YeniSonNomre, istifadeciId);
                netice.Yazilan++;
            }

            var metn = $"{il}: {netice.Yazilan} sayğac yazıldı, {netice.Kecilen} keçildi.";
            return Result<MuqavileSayghacKocurmeNeticeDto>.Ok(netice, metn);
        }
        catch (Exception ex)
        {
            return Result<MuqavileSayghacKocurmeNeticeDto>.Fail($"Köçürmə xətası: {ex.Message}");
        }
    }

    private async Task<IList<Dictionary<string, object?>>> OracleSetirlerAsync(CancellationToken ct)
    {
        var sorgu = (await _uow.Repository<OracleSorgu>()
                .HamisiniGetirAsync(x => !x.Silinib && x.Aktiv, izlemeden: true))
            .FirstOrDefault(x => string.Equals((x.SorguAdi ?? "").Trim(), SorguAdi,
                StringComparison.OrdinalIgnoreCase));

        if (sorgu == null || string.IsNullOrWhiteSpace(sorgu.SorguMetni))
            throw new InvalidOperationException(
                $"Oracle sorğusu tapılmadı: «{SorguAdi}». " +
                "Admin → Oracle Sorğular-da yaradılmalıdır " +
                "(seed skripti: docs/sql/kredit/Muqavile_Sayghac_OracleSorgu.sql).");

        var setirler = await _oracle.SelectAsync(sorgu.SorguMetni, 200, ct);

        // IL olmadan heç nə etmək olmaz — açıq xəta ver, səssiz keçmə
        if (setirler.Count > 0 &&
            !setirler[0].Keys.Contains("IL", StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"«{SorguAdi}» nəticəsində IL sütunu yoxdur. Sorğu redaktə olunub? Bazaya heç nə yazılmadı.");

        return setirler;
    }

    private static int? Tam(IDictionary<string, object?> s, string sutun)
    {
        // Sütun adı böyük/kiçik hərf fərqi ilə gələ bilər — açarı adına görə tap
        var acar = s.Keys.FirstOrDefault(k => string.Equals(k, sutun, StringComparison.OrdinalIgnoreCase));
        if (acar == null || s[acar] == null) return null;
        try { return Convert.ToInt32(s[acar], CultureInfo.InvariantCulture); }
        catch { return null; }
    }
}
