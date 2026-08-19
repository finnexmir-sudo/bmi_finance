// Infrastructure/BackgroundJobs/XatirlatmaBackgroundService.cs
using FinNex.Application.DTOs.Communication;
using FinNex.Application.Interfaces;
using FinNex.Application.Interfaces.Communication;
using FinNex.DataAccess.Contexts;
using FinNex.Domain;
using FinNex.Domain.Entities.Avtopark;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Entities.HR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FinNex.Infrastructure.BackgroundJobs
{
    /// <summary>
    /// Hər 1 saatda bir işə düşür:
    ///  1) Tapşırıq son tarixi sabah olan işçilərə xatırlatma yaradır
    ///  2) Görüş sabah olan iştirakçılara xatırlatma yaradır
    ///  3) Məzuniyyət bitmə tarixinə 10 iş günü qalmış — HR-a bildiriş
    ///  4) Təyinat/müqavilə bitmə tarixinə 10 iş günü qalmış — HR-a bildiriş
    ///  5) Vaxtı keçmiş xatırlatmaları "göndərildi" kimi işarələyir
    /// </summary>
    public class XatirlatmaBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<XatirlatmaBackgroundService> _logger;
        private static readonly TimeSpan _interval = TimeSpan.FromHours(1);
        private const int BILDIRISH_IS_GUNU = 10;

        public XatirlatmaBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<XatirlatmaBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("XatirlatmaBackgroundService başladı.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await IsleAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "XatirlatmaBackgroundService xətası.");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }

        private async Task IsleAsync()
        {
            using var scope = _scopeFactory.CreateScope();

            var xatirlatmaService = scope.ServiceProvider
                .GetRequiredService<IXatirlatmaService>();
            var tapshiriqService = scope.ServiceProvider
                .GetRequiredService<ITapshiriqService>();
            var gorushService = scope.ServiceProvider
                .GetRequiredService<IGorushService>();
            var db = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();
            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<AppUser>>();
            // Avtopark müddət xəbərdarlığı — zəng ikonuna düşən bildiriş.
            var bildirisRouter = scope.ServiceProvider
                .GetRequiredService<IBildirisRouter>();

            var bugun = DateTime.Today;
            var sabahinTarixi = bugun.AddDays(1);

            // ── 1) Tapşırıq xatırlatmaları ───────────────────
            var tapshiriqlar = await tapshiriqService.GetYaxinlasanlarAsync(sabahinTarixi);
            if (tapshiriqlar.Success && tapshiriqlar.Data != null)
            {
                foreach (var t in tapshiriqlar.Data)
                {
                    await xatirlatmaService.SistemXatirlatmasiYaratAsync(new XatirlatmaSistemCreateDto
                    {
                        IsciId = t.TeyinOlunanIsciId,
                        Bashliq = $"Tapşırıq sabah bitir: {t.Bashliq}",
                        Qeyd = $"Son tarix: {t.SonTarix:dd.MM.yyyy}",
                        XatirlatmaTarixi = DateTime.Now,
                        EntityTipi = XatirlatmaEntityTipi.Tapshiriq,
                        EntityId = t.Id
                    });
                }
            }

            // ── 2) Görüş xatırlatmaları ───────────────────────
            var gorushler = await gorushService.GetYaxinlasanlarAsync(sabahinTarixi);
            if (gorushler.Success && gorushler.Data != null)
            {
                foreach (var g in gorushler.Data)
                {
                    foreach (var ishtirakci in g.Ishtirakcılar
                        .Where(i => i.Status != IshtirakciStatus.Redd))
                    {
                        await xatirlatmaService.SistemXatirlatmasiYaratAsync(new XatirlatmaSistemCreateDto
                        {
                            IsciId = ishtirakci.IsciId,
                            Bashliq = $"Görüş sabah: {g.Bashliq}",
                            Qeyd = $"{g.Tarix:dd.MM.yyyy} — {g.BaslamaSaati:hh\\:mm}",
                            XatirlatmaTarixi = DateTime.Now,
                            EntityTipi = XatirlatmaEntityTipi.Gorush,
                            EntityId = g.Id
                        });
                    }
                }
            }

            // ── 3) Məzuniyyət bitmə bildirişi — HR TƏLƏBİ İLƏ DAYANDIRILIB ──
            // HR-a məzuniyyətin bitmə tarixi barədə xatırlatma artıq göndərilmir.
            // (mezSayi 0 qalır — aşağıdakı yekun statistika logunda istifadə olunur.)
            int mezSayi = 0;

            // ── 4) Təyinat/müqavilə bitmə — 10 iş günü qalmış, HR-a bildiriş ──
            int teyinatSayi = 0;
            try
            {
                var bayramlar = await GetBayramTarixleriAsync(db, bugun.Year);
                var hedeftarix = IsGunuHesabla(bugun, BILDIRISH_IS_GUNU, bayramlar);

                var bitmekUzreTeyinatlar = await db.Set<IsciTeyinat>()
                    .Include(t => t.Isci)
                    .Include(t => t.Departament)
                    .Include(t => t.Vezife)
                    .Where(t => !t.Silinib
                        && t.Aktivdir
                        && t.BitmeTarixi.HasValue
                        && t.BitmeTarixi.Value.Date == hedeftarix.Date)
                    .ToListAsync();

                var hrIscileri = await GetHrIsciIdleriAsync(db, userManager);

                foreach (var tey in bitmekUzreTeyinatlar)
                {
                    foreach (var hrIsciId in hrIscileri)
                    {
                        var movcud = await db.Set<Xatirlatma>().AnyAsync(x =>
                            x.EntityTipi == XatirlatmaEntityTipi.Teyinat
                            && x.EntityId == tey.Id
                            && x.IsciId == hrIsciId
                            && !x.Silinib);

                        if (!movcud)
                        {
                            await xatirlatmaService.SistemXatirlatmasiYaratAsync(new XatirlatmaSistemCreateDto
                            {
                                IsciId = hrIsciId,
                                Bashliq = $"Müqavilə/təyinat bitir: {tey.Isci.Ad} {tey.Isci.Soyad}",
                                Qeyd = $"Bitmə tarixi: {tey.BitmeTarixi:dd.MM.yyyy}. " +
                                       $"Departament: {tey.Departament?.Ad}, Vəzifə: {tey.Vezife?.Ad}. 10 iş günü qalıb.",
                                XatirlatmaTarixi = DateTime.Now,
                                EntityTipi = XatirlatmaEntityTipi.Teyinat,
                                EntityId = tey.Id
                            });
                            teyinatSayi++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Təyinat xatırlatması xətası.");
            }

            // ── 6) Məzuniyyət yeniləmə tarixi 7 günə qalmış — HR-a bildiriş ──
            int yenilenSayi = 0;
            try
            {
                var hrIscileriYen = await GetHrIsciIdleriAsync(db, userManager);

                var aktifIsciler = await db.Isciler
                    .Where(i => !i.Silinib && i.Status == IsciStatus.Aktiv)
                    .Select(i => new { i.Id, i.Ad, i.Soyad, i.IsheQebulTarixi })
                    .ToListAsync();

                foreach (var isci in aktifIsciler)
                {
                    var next = NextRenewDate(isci.IsheQebulTarixi, bugun);
                    var qalan = (next - bugun).Days;

                    if (qalan < 0 || qalan > 7) continue;

                    foreach (var hrIsciId in hrIscileriYen)
                    {
                        var movcud = await db.Set<Xatirlatma>().AnyAsync(x =>
                            x.EntityTipi == XatirlatmaEntityTipi.MezuniyyetYenilenme
                            && x.EntityId == isci.Id
                            && x.IsciId == hrIsciId
                            && x.XatirlatmaTarixi.Year == bugun.Year
                            && !x.Silinib);

                        if (!movcud)
                        {
                            await xatirlatmaService.SistemXatirlatmasiYaratAsync(new XatirlatmaSistemCreateDto
                            {
                                IsciId           = hrIsciId,
                                Bashliq          = $"Məzuniyyət yeniləməsi: {isci.Ad} {isci.Soyad}",
                                Qeyd             = $"Yeniləmə tarixi: {next:dd.MM.yyyy}. " +
                                                   $"{(qalan == 0 ? "Bugün!" : $"{qalan} gün qalıb.")}",
                                XatirlatmaTarixi = DateTime.Now,
                                EntityTipi       = XatirlatmaEntityTipi.MezuniyyetYenilenme,
                                EntityId         = isci.Id
                            });
                            yenilenSayi++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Məzuniyyət yeniləmə xatırlatması xətası.");
            }

            // ── 7) Avtopark: sığorta / texniki baxış / yağ dəyişmə müddəti ──
            // ALICILAR ROLA GÖRƏ HESABLANMIR — Admin `AvtoparkXeberdarliqAlicilari`
            // cədvəlində işçiləri bir-bir seçir (19.08.2026 qərarı). Siyahı boşdursa
            // xəbərdarlıq heç kimə getmir; bu, səssiz qalmasın deyə loga yazılır.
            int avtoparkSayi = 0;
            try
            {
                var alicilar = await db.Set<AvtoparkXeberdarliqAlicisi>()
                    .Include(a => a.Isci)
                    .Where(a => !a.Silinib && a.Aktivdir
                             && a.Isci.Status == IsciStatus.Aktiv && !a.Isci.Silinib)
                    .Select(a => a.IsciId)
                    .Distinct()
                    .ToListAsync();

                // Xəbərdarlıq pəncərəsi hər sətrin ÖZ `XeberdarliqGun`-una görədir,
                // ona görə sabit tarixlə filtrləyə bilmirik: ən böyük mümkün
                // pəncərə (365 gün) qədər gətirib şərti yaddaşda tətbiq edirik.
                // Sətir sayı azdır (maşın × növ), bu, real yük yaratmır.
                var namizedler = await db.Set<MasinMuddet>()
                    .Include(m => m.Masin)
                    .Include(m => m.Nov)
                    .Where(m => !m.Silinib && m.Aktivdir
                             && m.SonTarix <= bugun.AddDays(365))
                    .ToListAsync();

                if (namizedler.Count > 0 && alicilar.Count == 0)
                    _logger.LogWarning(
                        "Avtopark: {Say} müddət qeydi xəbərdarlıq tələb edir, amma ALICI SİYAHISI BOŞDUR — " +
                        "Avtopark → Müddətlər → Xəbərdarlıq alıcıları ekranından işçi əlavə edin.",
                        namizedler.Count);

                foreach (var m in namizedler)
                {
                    var qalan = (m.SonTarix.Date - bugun).Days;
                    if (qalan > m.XeberdarliqGun) continue;   // hələ pəncərəyə düşməyib

                    // Bir dəfə göndərilib. Vaxtı KEÇMİŞSƏ həftədə bir təkrarlanır —
                    // yoxsa uzadılmayan sığorta bir bildirişdən sonra unudulardı.
                    if (m.XeberdarliqGonderilib)
                    {
                        var kecib = qalan < 0;
                        var sonGonderis = m.XeberdarliqTarixi ?? DateTime.MinValue;
                        if (!kecib || sonGonderis > DateTime.Now.AddDays(-7)) continue;
                    }

                    var veziyyet = qalan < 0
                        ? $"{-qalan} gün ƏVVƏL bitib"
                        : qalan == 0 ? "BU GÜN bitir" : $"{qalan} gün qalıb";

                    var metn = $"{m.Masin?.DovletNomresi} — {m.Nov?.Ad}: " +
                               $"son tarix {m.SonTarix:dd.MM.yyyy} ({veziyyet}).";

                    // Bildirişlər ARDICIL yazılır (`Task.WhenAll` YOX) —
                    // `DbContext` thread-safe deyil (CLAUDE.md).
                    foreach (var isciId in alicilar)
                    {
                        try
                        {
                            await bildirisRouter.NotifyIsciAsync(
                                isciId,
                                BildirisNovu.MasinMuddetXeberdarliq,
                                qalan < 0 ? "Maşın müddəti BİTİB" : "Maşın müddəti yaxınlaşır",
                                metn,
                                "/Avtopark/Muddet/Yaxinlasanlar");
                        }
                        catch (Exception bex)
                        {
                            _logger.LogWarning(bex,
                                "Avtopark xəbərdarlığı göndərilmədi — isciId={IsciId}, muddetId={MuddetId}",
                                isciId, m.Id);
                        }
                    }

                    // Bayraq ALICI OLMASA DA yazılır? XEYR — yazılsaydı alıcı
                    // sonradan əlavə ediləndə xəbərdarlıq heç vaxt getməzdi.
                    if (alicilar.Count > 0)
                    {
                        m.XeberdarliqGonderilib = true;
                        m.XeberdarliqTarixi = DateTime.Now;
                        avtoparkSayi++;
                    }
                }

                if (avtoparkSayi > 0) await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Avtopark müddət xəbərdarlığı xətası.");
            }

            // ── 5) Göndərilməmiş keçmiş xatırlatmaları işarələ ──
            var gonderilemeyenler = await xatirlatmaService.GetGonderilemeyenlerAsync();
            if (gonderilemeyenler.Success && gonderilemeyenler.Data != null)
            {
                foreach (var x in gonderilemeyenler.Data)
                {
                    await xatirlatmaService.GonderildiIsareEtAsync(x.Id);
                }
            }

            _logger.LogInformation(
                "Xatırlatma dövrü tamamlandı. Tapşırıq: {t}, Görüş: {g}, Məzuniyyət: {m}, Təyinat: {te}, Yeniləmə: {y}, Avtopark: {a}",
                tapshiriqlar.Data?.Count ?? 0,
                gorushler.Data?.Count ?? 0,
                mezSayi,
                teyinatSayi,
                yenilenSayi,
                avtoparkSayi);
        }

        private static DateTime NextRenewDate(DateTime hireDate, DateTime today)
        {
            int year  = today.Year;
            int month = hireDate.Month;
            int day   = Math.Min(hireDate.Day, DateTime.DaysInMonth(year, month));
            var thisYear = new DateTime(year, month, day);

            if (thisYear < today)
            {
                year++;
                day = Math.Min(hireDate.Day, DateTime.DaysInMonth(year, month));
                return new DateTime(year, month, day);
            }
            return thisYear;
        }

        /// <summary>
        /// Bugündən başlayaraq N iş günü irəli hesablayır.
        /// Şənbə, bazar və bayram günlərini atlayır.
        /// </summary>
        private static DateTime IsGunuHesabla(DateTime baslangic, int isGunSayi, HashSet<DateTime> bayramlar)
        {
            var tarix = baslangic;
            int sayilmis = 0;

            while (sayilmis < isGunSayi)
            {
                tarix = tarix.AddDays(1);

                // Şənbə (6) və Bazar (0) günlərini atla
                if (tarix.DayOfWeek == DayOfWeek.Saturday || tarix.DayOfWeek == DayOfWeek.Sunday)
                    continue;

                // Bayram günlərini atla
                if (bayramlar.Contains(tarix.Date))
                    continue;

                sayilmis++;
            }

            return tarix;
        }

        /// <summary>
        /// Verilmiş il üçün bayram tarixlərini çəkir.
        /// HerIlTeyinOlunur olan bayramlar da daxildir.
        /// </summary>
        private static async Task<HashSet<DateTime>> GetBayramTarixleriAsync(AppDbContext db, int il)
        {
            var bayramlar = await db.BayramGunleri
                .Where(b => !b.Silinib && (b.Tarix.Year == il || b.HerIlTeyinOlunur))
                .ToListAsync();

            var tarixler = new HashSet<DateTime>();
            foreach (var b in bayramlar)
            {
                if (b.HerIlTeyinOlunur)
                {
                    // Sabit bayram — cari il üçün tarix yarat
                    tarixler.Add(new DateTime(il, b.Tarix.Month, b.Tarix.Day));
                }
                else
                {
                    tarixler.Add(b.Tarix.Date);
                }
            }

            return tarixler;
        }

        /// <summary>
        /// HR və Admin roluna sahib bütün işçilərin IsciId-lərini qaytarır.
        /// </summary>
        private static async Task<List<int>> GetHrIsciIdleriAsync(AppDbContext db, UserManager<AppUser> userManager)
        {
            var hrUsers = await userManager.GetUsersInRoleAsync(RoleNames.HR);
            var adminUsers = await userManager.GetUsersInRoleAsync(RoleNames.Admin);

            var allUsers = hrUsers.Concat(adminUsers).DistinctBy(u => u.Id).ToList();

            var isciIdler = new List<int>();
            foreach (var user in allUsers)
            {
                if (user.IsciId.HasValue)
                {
                    isciIdler.Add(user.IsciId.Value);
                }
            }

            // Əgər heç bir HR işçisi yoxdursa, admin user-in özünü əlavə et
            if (!isciIdler.Any())
            {
                var adminIsci = await db.Isciler
                    .Where(i => !i.Silinib && i.AppUserId != null)
                    .FirstOrDefaultAsync();
                if (adminIsci != null)
                    isciIdler.Add(adminIsci.Id);
            }

            return isciIdler;
        }
    }
}
