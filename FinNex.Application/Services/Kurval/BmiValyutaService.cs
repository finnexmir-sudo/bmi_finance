using System.Globalization;
using FinNex.Application.DTOs.Kurval;
using FinNex.Application.Interfaces.Oracle;
using FinNex.Application.Interfaces.Kurval;
using FinNex.Domain.Entities.Sorgular;
using FinNex.Domain.Interfaces;

namespace FinNex.Application.Services.Kurval;

public class BmiValyutaService : IBmiValyutaService
{
    private readonly IUnitOfWork _uow;
    private readonly IOracleService _oracle;

    // Oracle sorğusu Admin → Oracle Sorğular-da saxlanılır (layihə qaydası).
    // Seed: docs/sql/valyuta/Valyuta_OracleSorgu.sql
    private const string SorguAdi = "VALYUTA_SIYAHISI";

    // Ehtiyat siyahı — Oracle əlçatmaz olanda işə düşür.
    //
    // NİYƏ VAR: bu olmasa Oracle bağlantısı kəsiləndə həvalə forması valyutasız
    // qalar və istifadəçi ümumiyyətlə qeyd yarada bilməz.
    // NİYƏ TƏHLÜKƏSİZDİR: dəyərlər `kurval`-dakı ilə eynidir (13.08.2026
    // yoxlaması) və saxlanılan dəyər yenə KODDUR — ehtiyat siyahı ilə yaradılan
    // qeyd normal qeyddən fərqlənmir.
    // BMI-yə yeni valyuta əlavə olunsa burada görünməz; amma o halda Oracle
    // onsuz da işləyirsə canlı siyahı gəlir — ehtiyat yalnız qəza halıdır.
    private static readonly BmiValyutaDto[] Ehtiyat =
    {
        new() { Kod = "00", Ad = "AZƏRBAYCAN MANATI" },
        new() { Kod = "01", Ad = "ABŞ DOLLARI" },
        new() { Kod = "02", Ad = "AVRO" },
        new() { Kod = "03", Ad = "RUS RUBLU" },
        new() { Kod = "04", Ad = "İRAN RİALI" },
        new() { Kod = "05", Ad = "BƏƏ DİRHƏMİ" },
    };

    public BmiValyutaService(IUnitOfWork uow, IOracleService oracle)
    {
        _uow = uow;
        _oracle = oracle;
    }

    public async Task<IList<BmiValyutaDto>> SiyahiAsync(CancellationToken ct = default)
    {
        try
        {
            var sorgu = (await _uow.Repository<OracleSorgu>()
                    .HamisiniGetirAsync(x => !x.Silinib && x.Aktiv, izlemeden: true))
                .FirstOrDefault(x => string.Equals((x.SorguAdi ?? "").Trim(), SorguAdi,
                    StringComparison.OrdinalIgnoreCase));

            if (sorgu == null || string.IsNullOrWhiteSpace(sorgu.SorguMetni))
                return Ehtiyat.ToList();

            var setirler = await _oracle.SelectAsync(sorgu.SorguMetni, 100, ct);

            var siyahi = setirler
                .Select(s => new BmiValyutaDto
                {
                    Kod = Metn(s, "SOKNAMEVALUT"),
                    Ad  = Metn(s, "NAMEVALUTI")
                })
                .Where(v => !string.IsNullOrWhiteSpace(v.Kod))
                .OrderBy(v => v.Kod, StringComparer.Ordinal)
                .ToList();

            return siyahi.Count > 0 ? siyahi : Ehtiyat.ToList();
        }
        catch
        {
            // Oracle əlçatmazdır — forma işləməyə davam etsin
            return Ehtiyat.ToList();
        }
    }

    // ══ VALYUTA KURSU (20 000 USD limiti üçün) ════════════════════════════

    private const string KursSorguAdi = "VALYUTA_KURSU";

    public async Task<decimal?> KursAsync(string valyutaKodu, DateTime tarix, CancellationToken ct = default)
    {
        // Kod yalnız RƏQƏMDİR («00», «01»…) — sorğuya birbaşa yapışdırılır,
        // ona görə burada TƏMİZLƏNİR. Təmizlənməsə inyeksiyaya açıq olardı.
        var kod = new string((valyutaKodu ?? "").Where(char.IsDigit).ToArray());
        if (kod.Length is 0 or > 4) return null;

        try
        {
            var sorgu = (await _uow.Repository<OracleSorgu>()
                    .HamisiniGetirAsync(x => !x.Silinib && x.Aktiv, izlemeden: true))
                .FirstOrDefault(x => string.Equals((x.SorguAdi ?? "").Trim(), KursSorguAdi,
                    StringComparison.OrdinalIgnoreCase));

            if (sorgu == null || string.IsNullOrWhiteSpace(sorgu.SorguMetni))
                return null;   // sorğu quraşdırılmayıb — səssiz 0 YOX

            var sql = sorgu.SorguMetni
                .Replace("{KOD}", kod)
                .Replace("{TARIX}", tarix.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture));

            var setirler = await _oracle.SelectAsync(sql, 1, ct);
            if (setirler.Count == 0) return null;

            var kurs = Reqem(setirler[0], "KURS");

            // 0 və mənfi kurs REAL DEYİL — bölmədə istifadə olunsa nəticə
            // ya sonsuz, ya mənfi çıxar. Belə dəyəri «alınmadı» sayırıq.
            return kurs is > 0 ? kurs : null;
        }
        catch
        {
            return null;   // Oracle əlçatmazdır — çağıran tərəf bloklayacaq
        }
    }

    /// <summary>
    /// Oracle NUMBER sütunu — TİPİ BİRBAŞA götürülür.
    ///
    /// ⚠️ `ToString()` + `Parse` DƏQİQ 100× SƏHV verir: `120.58m.ToString()`
    /// cari mədəniyyətdə «120,58» olur, `TryParse(NumberStyles.Any, Invariant)`
    /// isə vergülü MİN AYIRICISI sayıb 12058 qaytarır (CLAUDE.md, 19.08.2026
    /// VM 98.2.1 hadisəsi). Ona görə burada çevirmə YOXDUR.
    /// </summary>
    private static decimal? Reqem(IDictionary<string, object?> s, string sutun)
    {
        var acar = s.Keys.FirstOrDefault(k => string.Equals(k, sutun, StringComparison.OrdinalIgnoreCase));
        if (acar == null) return null;

        return s[acar] switch
        {
            null      => null,
            decimal d => d,
            double dd => (decimal)dd,
            float f   => (decimal)f,
            int i     => i,
            long l    => l,
            short sh  => sh,
            // Sütun həqiqətən MƏTN gəlsə: NumberStyles.Float — `Any` İŞLƏTMƏ,
            // o, min ayırıcısına icazə verir və eyni 100× səhvi qaytarır.
            string st => decimal.TryParse(st, NumberStyles.Float, CultureInfo.InvariantCulture, out var m1) ? m1
                       : decimal.TryParse(st, NumberStyles.Float, CultureInfo.CurrentCulture, out var m2) ? m2
                       : null,
            _         => null
        };
    }

    // Sütun adı böyük/kiçik hərflə gələ bilər — müqayisə həssas deyil.
    private static string Metn(IDictionary<string, object?> s, string sutun)
    {
        var acar = s.Keys.FirstOrDefault(k => string.Equals(k, sutun, StringComparison.OrdinalIgnoreCase));
        if (acar == null || s[acar] == null) return "";
        return (Convert.ToString(s[acar], CultureInfo.InvariantCulture) ?? "").Trim();
    }
}
