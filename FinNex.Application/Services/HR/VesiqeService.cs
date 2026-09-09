using FinNex.Application.DTOs.HR.Vesiqe;
using FinNex.Application.Interfaces.Oracle;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Entities.Sorgular;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.HR
{
    /// <summary>
    /// Şəxsiyyət vəsiqəsinin bitmə tarixi — BMI (Oracle) `regnom` cədvəlindən
    /// FİN üzrə oxunur və işçi siyahısı ilə birləşdirilir.
    ///
    /// ⚠️ YAZMA YOXDUR — nə Oracle-a (qadağandır), nə öz bazamıza (istifadəçi
    /// qərarı: «sadəcə göstəririk, heç yerə yazmayacağıq»). Ona görə burada
    /// `YaddaSaxlaAsync` çağırışı YOXDUR və olmamalıdır.
    ///
    /// Sorğu `OracleSorgular` cədvəlində saxlanılır (SorguAdi = VESIQE_BITME) —
    /// kodda sabit SQL yoxdur, layihə qaydası budur.
    /// Mətn: `docs/sql/hr/Vesiqe_Bitme_OracleSorgu.sql`.
    /// </summary>
    public class VesiqeService : IVesiqeService
    {
        private readonly IUnitOfWork _uow;
        private readonly IOracleService _oracle;

        public VesiqeService(IUnitOfWork uow, IOracleService oracle)
        {
            _uow = uow;
            _oracle = oracle;
        }

        /// <summary>`OracleSorgular.SorguAdi` — dəyişsə səhifə heç nə göstərməz.</summary>
        private const string SorguAdi = "VESIQE_BITME";

        // Sorğunun qaytarmalı olduğu sütun adları (alias-lar). Ad dəyişsə dəyər
        // səssizcə `null` olardı — ona görə açıq yoxlanılır.
        private const string SutunFin   = "FIN";
        private const string SutunTarix = "VESIQE_BIT_TARIXI";

        public async Task<VesiqeNeticesi> SiyahiAsync(CancellationToken ct = default)
        {
            var netice = new VesiqeNeticesi();

            // ── 1. İşçilər ────────────────────────────────────────────────
            // Şərt «İşçilər» və «Müqavilə Bitmə» səhifələri ilə EYNİDİR:
            // `Status != IshtenCixib`. `== Aktiv` YAZMA — məzuniyyətdəki işçi
            // hələ işləyir və onun vəsiqəsi də bitir.
            var teyinatlar = await _uow.Repository<IsciTeyinat>()
                .Query()
                .AsNoTracking()
                .Include(t => t.Isci)
                .Include(t => t.Departament)
                .Include(t => t.Vezife)
                .Where(t => !t.Silinib && t.Aktivdir
                         && !t.Isci.Silinib
                         && t.Isci.Status != IsciStatus.IshtenCixib)
                .ToListAsync(ct);

            // Bir işçidə birdən çox aktiv təyinat ola bilər — sətir ikiləşməsin.
            var isciler = teyinatlar
                .GroupBy(t => t.IsciId)
                .Select(g => g.OrderByDescending(t => t.BaslamaTarixi).First())
                .ToList();

            if (isciler.Count == 0)
            {
                netice.Ugurlu = true;
                return netice;
            }

            // ── 2. Saxlanmış Oracle sorğusu ───────────────────────────────
            var sorgu = (await _uow.Repository<OracleSorgu>()
                    .HamisiniGetirAsync(x => !x.Silinib && x.Aktiv, izlemeden: true))
                .FirstOrDefault(x => string.Equals((x.SorguAdi ?? "").Trim(), SorguAdi,
                    StringComparison.OrdinalIgnoreCase));

            if (sorgu == null || string.IsNullOrWhiteSpace(sorgu.SorguMetni))
            {
                netice.Xeta = $"«{SorguAdi}» adlı Oracle sorğusu quraşdırılmayıb. " +
                              "Admin → Oracle Sorğuları bölməsinə əlavə edin " +
                              "(mətn: docs/sql/hr/Vesiqe_Bitme_OracleSorgu.sql).";
                return netice;
            }

            // FİN-lər sorğuya yapışdırılırsa MÜTLƏQ təmizlənir — yoxsa inyeksiyaya
            // açıq olardı. FİN yalnız hərf/rəqəmdir.
            var sql = sorgu.SorguMetni;
            if (sql.Contains("{FINLER}", StringComparison.OrdinalIgnoreCase))
            {
                var finler = isciler
                    .Select(t => FinTemizle(t.Isci.FIN))
                    .Where(f => f.Length > 0)
                    .Distinct()
                    .ToList();

                var siyahi = finler.Count > 0
                    ? string.Join(",", finler.Select(f => $"'{f}'"))
                    : "''";
                sql = sql.Replace("{FINLER}", siyahi, StringComparison.OrdinalIgnoreCase);
            }

            // ── 3. Oracle-dan oxu ─────────────────────────────────────────
            List<Dictionary<string, object?>> setirler;
            try
            {
                // Sorğuda {FINLER} yoxdursa bütün `regnom` gəlir və süzgəc
                // yaddaşda işləyir — ona görə limit geniş verilir.
                setirler = await _oracle.SelectAsync(sql, 200_000, ct);
            }
            catch (InvalidOperationException ex)
            {
                // ⚠️ Bunu «bağlantı xətası» kimi yazma — `OracleService.YalnizSelect`
                // sorğunun SELECT/WITH ilə BAŞLAMASINI tələb edir. Saxlanmış mətn
                // `--` şərh sətri ilə başlayırsa sorğu Oracle-a heç getmir.
                // Real hadisə (09.09.2026): sənəd faylı bütöv yapışdırıldı, mətn
                // «-- TÖVSİYƏ OLUNAN VARİANT…» ilə başladı, ekran isə «BMI-yə
                // bağlanmaq alınmadı» yazdı və şəbəkədə səbəb axtarıldı.
                netice.Xeta = $"Sorğu icra olunmadı: {ex.Message} " +
                              "Saxlanmış mətn birbaşa «select» sözü ilə başlamalıdır — " +
                              "əvvəldəki `--` şərh sətirlərini silin.";
                return netice;
            }
            catch (Exception ex)
            {
                netice.Xeta = $"BMI-yə bağlanmaq alınmadı: {ex.Message}";
                return netice;
            }

            // FİN → tarix. Eyni FİN birdən çox sətirdə olarsa ƏN SON (ən böyük)
            // tarix götürülür — köhnə vəsiqə sətri yenisini üstələməsin.
            var bmi = new Dictionary<string, DateTime>(StringComparer.Ordinal);
            bool sutunTapildi = setirler.Count == 0;

            foreach (var s in setirler)
            {
                if (!sutunTapildi)
                    sutunTapildi = SutunVarmi(s, SutunFin) && SutunVarmi(s, SutunTarix);

                var fin = FinTemizle(Metn(s, SutunFin));
                if (fin.Length == 0) continue;

                var tarix = Tarix(s, SutunTarix);
                if (tarix == null) continue;

                if (!bmi.TryGetValue(fin, out var movcud) || tarix.Value > movcud)
                    bmi[fin] = tarix.Value;
            }

            if (!sutunTapildi)
            {
                netice.Xeta = $"Sorğunun sütun adları uyğun deyil — «{SutunFin}» və " +
                              $"«{SutunTarix}» olmalıdır (alias ilə adlandırın).";
                return netice;
            }

            // ── 4. Birləşdir ──────────────────────────────────────────────
            var bugun = DateTime.Today;

            netice.Setirler = isciler
                .Select(t =>
                {
                    var fin = FinTemizle(t.Isci.FIN);
                    DateTime? bitme = fin.Length > 0 && bmi.TryGetValue(fin, out var d) ? d : null;

                    return new VesiqeSetriDto
                    {
                        IsciId        = t.IsciId,
                        Sira          = t.Isci.Sira,
                        Ad            = t.Isci.Ad,
                        Soyad         = t.Isci.Soyad,
                        AtaAdi        = t.Isci.AtaAdi,
                        FIN           = t.Isci.FIN ?? "",
                        DepartamentId = t.DepartamentId,
                        DepartamentAd = t.Departament?.Ad ?? "—",
                        VezifeAd      = t.Vezife?.Ad ?? "—",
                        BitmeTarixi   = bitme,
                        QalanGun      = bitme == null ? null : (bitme.Value.Date - bugun).Days
                    };
                })
                // ⚠️ SIRALAMA `Sira`-YA GÖRƏDİR, qalan günə görə YOX (istifadəçi
                // qərarı 09.09.2026: «işçi sıralamasından götürülməlidir»).
                // HR «İşçi Sıralaması» səhifəsində drag-and-drop ilə təyin edir;
                // ad/soyad əlifbası yalnız eyni `Sira` daxilində işləyir.
                // Təcililik siyahının SIRASINDA yox, «Qalan gün» sütununun
                // RƏNGİNDƏ və yuxarıdakı KPI kartlarında görünür.
                .OrderBy(r => r.Sira)
                .ThenBy(r => r.Ad)
                .ThenBy(r => r.Soyad)
                .ToList();

            netice.Ugurlu = true;
            return netice;
        }

        // ── Köməkçilər ────────────────────────────────────────────────────

        /// <summary>
        /// FİN normallaşdırması — boşluqsuz, BÖYÜK hərf, yalnız hərf/rəqəm.
        /// Oxuyan da, tutuşduran da EYNİ bu metoddan keçir; biri keçməsə
        /// «5ab2cd1» ilə «5AB2CD1» iki ayrı şəxs sayılardı və işçi «BMI-də
        /// yoxdur» görünərdi. Eyni zamanda SQL-ə yapışdırılan dəyəri təmizləyir.
        /// </summary>
        private static string FinTemizle(string? fin)
            => new string((fin ?? "").Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();

        private static bool SutunVarmi(IDictionary<string, object?> s, string sutun)
            => s.Keys.Any(k => string.Equals(k, sutun, StringComparison.OrdinalIgnoreCase));

        private static string Metn(IDictionary<string, object?> s, string sutun)
        {
            var acar = s.Keys.FirstOrDefault(k => string.Equals(k, sutun, StringComparison.OrdinalIgnoreCase));
            return acar == null ? "" : (s[acar]?.ToString() ?? "");
        }

        /// <summary>
        /// Oracle DATE sütunu — TİPİ BİRBAŞA götürülür, mətnə çevrilmir.
        /// `ToString()` + `Parse` mədəniyyət qarışığı verir (CLAUDE.md).
        /// Tanınmayan tip `null` qaytarır — səhv tarix göstərməkdənsə boş yaxşıdır.
        /// </summary>
        private static DateTime? Tarix(IDictionary<string, object?> s, string sutun)
        {
            var acar = s.Keys.FirstOrDefault(k => string.Equals(k, sutun, StringComparison.OrdinalIgnoreCase));
            if (acar == null) return null;

            return s[acar] switch
            {
                DateTime d       => d.Date,
                DateTimeOffset o => o.Date,
                _                => null
            };
        }
    }
}
