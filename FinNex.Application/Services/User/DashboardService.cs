using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Dashboard;
using FinNex.Application.Interfaces;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FinNex.Domain;

namespace FinNex.Application.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;

        public DashboardService(IUnitOfWork unitOfWork, UserManager<AppUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<Result<UserDashboardDto>> GetDashboardAsync(string username)
        {
            try
            {
                // ── 1. AppUser → Isci tap ────────────────────────────────
                var appUser = await _userManager.FindByNameAsync(username);
                if (appUser == null)
                    return Result<UserDashboardDto>.Fail("İstifadəçi tapılmadı.");

                var isci = await _unitOfWork.Repository<Isci>()
                    .GetirAsync(
                        x => x.AppUserId == appUser.Id,
                        include: q => q
    .Include(i => i.IsciTeyinatlari.Where(t => t.Aktivdir))
        .ThenInclude(t => t.Departament)
    .Include(i => i.IsciTeyinatlari.Where(t => t.Aktivdir))
        .ThenInclude(t => t.Vezife)
    .Include(i => i.Maliye)
    .Include(i => i.MezuniyyetBalanslari),
                        izlemeden: true);

                if (isci == null)
                    return Result<UserDashboardDto>.Fail("İşçi profili tapılmadı.");

                var dto = new UserDashboardDto
                {
                    IsciId = isci.Id,
                    TamAd = isci.TamAd,
                    VezifeAdi = isci.IsciTeyinatlari
    .Where(t => t.Aktivdir)
    .Select(t => t.Vezife.Ad)
    .FirstOrDefault() ?? "-",

                    SobeAdi = isci.IsciTeyinatlari
    .Where(t => t.Aktivdir)
    .Select(t => t.Departament.Ad)
    .FirstOrDefault() ?? "-",
                    IsheBaslamaTarixi = isci.IsheQebulTarixi,
                };

                // ── 2. Davamiyyət (cari ay) ──────────────────────────────
                var buAy = DateTime.Now.Month;
                var buIl = DateTime.Now.Year;

                var davamiyyetler = await _unitOfWork.Repository<Davamiyyet>()
                    .HamisiniGetirAsync(
                        x => x.IsciId == isci.Id
                          && x.Tarix.Month == buAy
                          && x.Tarix.Year == buIl,
                        izlemeden: true);

                dto.IslenanGun = davamiyyetler.Count(x => x.Status == DavamiyyetStatus.Isde
                                                          || x.Status == DavamiyyetStatus.Gecikme);
                dto.QaibGun = davamiyyetler.Count(x => x.Status == DavamiyyetStatus.Qayib);
                dto.IcazeliGun = davamiyyetler.Count(x => x.Status == DavamiyyetStatus.Icazeli
                                                          || x.Status == DavamiyyetStatus.Xestelik
                                                          || x.Status == DavamiyyetStatus.Ezamiyyet);

                // Hələ qeydə alınmamış iş günləri (gələcək)
                var isGunleri = GetAyinIsGunleri(buIl, buAy);
                dto.IsGunuSayi = isGunleri.Count;
                dto.GozlenilenGun = isGunleri.Count(g => g > DateTime.Today
                    && !davamiyyetler.Any(d => d.Tarix.Date == g.Date));

                // ── 3. Davamiyyət təqvimi — ayın bütün günləri ──────────
                var aydakiButunGunler = Enumerable
                    .Range(1, DateTime.DaysInMonth(buIl, buAy))
                    .Select(d => new DateTime(buIl, buAy, d))
                    .ToList();

                // Bayram günləri
                var bayramlar = await _unitOfWork.Repository<BayramGunu>()
                    .HamisiniGetirAsync(b => !b.Silinib, izlemeden: true);

                bool IsHoliday(DateTime g, out string? ad)
                {
                    foreach (var b in bayramlar)
                    {
                        if (b.HerIlTeyinOlunur)
                        {
                            if (b.Tarix.Month == g.Month && b.Tarix.Day == g.Day)
                            { ad = b.Ad; return true; }
                        }
                        else
                        {
                            if (b.Tarix.Date == g.Date) { ad = b.Ad; return true; }
                        }
                    }
                    ad = null;
                    return false;
                }

                // Görüş (offline tədbir) səbəbli gecikmələri təqvimdə də bağışla —
                // kart məntiqi ilə (aşağıda ── 6d) EYNİ: iştirakçı olub, girişi tədbir
                // bitmə saatı + tolerans içindədirsə, həmin gün "Gecikmə" (qırmızı) deyil,
                // normal iş günü (yaşıl) kimi göstərilir.
                var izParam = await _unitOfWork.Repository<IsParametri>()
                    .GetirAsync(x => !x.Silinib);
                var gecikTolerans = TimeSpan.FromMinutes(izParam?.GecikmeToleransDeqiqe ?? 5);

                var buAyTedbirBitisleri = new Dictionary<DateTime, TimeSpan>();
                try
                {
                    var buAyIshtiraklar = await _unitOfWork.Repository<GorushIshtirakci>()
                        .HamisiniGetirAsync(
                            x => x.IsciId == isci.Id && !x.Silinib
                              && x.Gorush.Nov == GorushNovu.Offline
                              && x.Gorush.Status != GorushStatus.LegvEdildi
                              && x.Status != IshtirakciStatus.Redd
                              && x.Status != IshtirakciStatus.IshtiraketmeyecekBildirib
                              && x.Gorush.Tarix.Year == buIl
                              && x.Gorush.Tarix.Month == buAy
                              && x.Gorush.BitisSaati != null,
                            include: q => q.Include(gi => gi.Gorush),
                            izlemeden: true);
                    foreach (var t in buAyIshtiraklar)
                    {
                        var d = t.Gorush.Tarix.Date;
                        var bit = t.Gorush.BitisSaati!.Value;
                        if (!buAyTedbirBitisleri.TryGetValue(d, out var cur) || bit > cur)
                            buAyTedbirBitisleri[d] = bit;
                    }
                }
                catch { /* Görüş cədvəli yoxdursa keç */ }

                bool BuAyGecikmeBagislanir(Davamiyyet r) =>
                    r.GirisVaxti.HasValue
                    && buAyTedbirBitisleri.TryGetValue(r.Tarix.Date, out var bit)
                    && r.GirisVaxti.Value.TimeOfDay <= bit + gecikTolerans;

                dto.DavamiyyetTakvim = aydakiButunGunler.Select(gun =>
                {
                    var qeyd = davamiyyetler.FirstOrDefault(d => d.Tarix.Date == gun.Date);
                    var bayramdir = IsHoliday(gun, out var bayramAdi);
                    var status = qeyd?.Status ?? DavamiyyetStatus.Isde;
                    // Tədbir səbəbli bağışlanan gecikmə təqvimdə normal iş günü kimi görünür.
                    if (status == DavamiyyetStatus.Gecikme && qeyd != null && BuAyGecikmeBagislanir(qeyd))
                        status = DavamiyyetStatus.Isde;
                    return new DashboardDavamiyyetGunDto
                    {
                        Tarix = gun,
                        Status = status,
                        Bayramdir = bayramdir,
                        BayramAdi = bayramAdi
                    };
                }).ToList();

                // ── 4. Məzuniyyət balansı (BÜTÜN illərin CƏMİ — keçmiş illərdən
                // qalan günlər də cari ilə əlavə olunur, beləliklə işçi əvvəl
                // istifadə etmədiyi günləri görür və müraciət edə bilər) ─
                var bAll = isci.MezuniyyetBalanslari?.ToList() ?? new List<MezuniyyetBalans>();

                var illikBalanslar = bAll.Where(b => b.Nov == MezuniyyetNovu.Illik).ToList();
                if (illikBalanslar.Any())
                {
                    dto.IllikToplamGun = illikBalanslar.Sum(b => b.ToplamGun);
                    dto.IllikIstifadeGun = illikBalanslar.Sum(b => b.IstifadeOlunanGun);
                }

                var xestelikBalanslar = bAll.Where(b => b.Nov == MezuniyyetNovu.Xestelik).ToList();
                if (xestelikBalanslar.Any())
                {
                    dto.XestelikToplamGun = xestelikBalanslar.Sum(b => b.ToplamGun);
                    dto.XestelikIstifadeGun = xestelikBalanslar.Sum(b => b.IstifadeOlunanGun);
                }

                var ezamiyyetBalanslar = bAll.Where(b => b.Nov == MezuniyyetNovu.Ezamiyyet).ToList();
                if (ezamiyyetBalanslar.Any())
                {
                    dto.EzamiyyetToplamGun = ezamiyyetBalanslar.Sum(b => b.ToplamGun);
                    dto.EzamiyyetIstifadeGun = ezamiyyetBalanslar.Sum(b => b.IstifadeOlunanGun);
                }

                // ── 5. Son ödənişlər (son 3 ay) ──────────────────────────
                var sonMaaslar = await _unitOfWork.Repository<Maas>()
                    .HamisiniGetirAsync(
                        x => x.IsciId == isci.Id,
                        izlemeden: true);

                dto.SonOdenisler = sonMaaslar
                    .OrderByDescending(x => x.Il).ThenByDescending(x => x.Ay)
                    .Take(3)
                    .Select(m => new DashboardMaasDto
                    {
                        Il = m.Il,
                        Ay = m.Ay,
                        NetMebleg = m.NetMebleg,
                        Status = m.Status
                    }).ToList();

                // ── 6. Aktiv məzuniyyət müraciətləri ─────────────────────
                var aktivMuracietler = await _unitOfWork.Repository<Mezuniyyet>()
                    .HamisiniGetirAsync(
                        x => x.IsciId == isci.Id
                          && x.Status != MezuniyyetStatus.ImtinaEdildi
                          && x.Status != MezuniyyetStatus.Tesdiqlenib,
                        include: q => q.Include(m => m.EvezEdenIsci),
                        izlemeden: true);

                dto.AktivMuracietler = aktivMuracietler
                    .OrderByDescending(x => x.YaradilmaTarixi)
                    .Take(5)
                    .Select(m => new DashboardMezuniyyetDto
                    {
                        Id = m.Id,
                        Nov = m.Nov,
                        Status = m.Status,
                        BaslamaTarixi = m.BaslamaTarixi,
                        BitmeTarixi = m.BitmeTarixi,
                        IsGunlerininSayi = m.IsGunlerininSayi,
                        EvezEdenAdSoyad = m.EvezEdenIsci?.TamAd
                    }).ToList();

                // ── 6b. Aktiv icazə müraciətləri ─────────────────────────
                var aktivIcazeler = await _unitOfWork.Repository<Icaze>()
                    .HamisiniGetirAsync(
                        x => x.IsciId == isci.Id
                          && x.Status != IcazeStatus.ImtinaEdildi
                          && x.Status != IcazeStatus.Tesdiqlenib,
                        include: q => q.Include(i => i.EvezEdenIsci),
                        izlemeden: true);

                dto.AktivIcazeler = aktivIcazeler
                    .OrderByDescending(x => x.IcazeTarixi)
                    .Take(5)
                    .Select(i => new DashboardIcazeDto
                    {
                        Id = i.Id,
                        IcazeTarixi = i.IcazeTarixi,
                        BaslamaSaati = i.BaslamaSaati,
                        BitisSaati = i.BitisSaati,
                        IcazeSaati = i.IcazeSaati,
                        EvezEdenAdSoyad = i.EvezEdenIsci?.TamAd,
                        Sebeb = i.Sebeb,
                        Status = i.Status
                    }).ToList();

                // ── 6c. İcazə saat balansı (illik 36 saat) ───────────────
                // İstifadə = cari təqvim ilində təsdiqlənmiş adi icazə saatları.
                // Jeton ödənən saat (bonus, sayılmır) və istifadə edilməyən nahar
                // çıxılır — IcazeService.EfektivSaat ilə EYNİ qayda.
                // (izParam yuxarıda ── 3-də yüklənib.)
                var naharBas = izParam?.NaharBaslamaSaati ?? new TimeSpan(13, 0, 0);
                var naharDeq = izParam?.NaharMuddetDeqiqe ?? 45;
                var cariIl = DateTime.Today.Year;

                var ilinIcazeleri = await _unitOfWork.Repository<Icaze>()
                    .HamisiniGetirAsync(
                        x => x.IsciId == isci.Id && !x.Silinib
                          && x.Status == IcazeStatus.Tesdiqlenib
                          && x.IcazeTarixi.Year == cariIl,
                        izlemeden: true);

                // Nahar çıxılması REAL kəsişmə əsaslıdır (icazə pəncərəsi ∩ nahar) — IcazeService ilə eyni.
                dto.IcazeIstifadeSaat = Math.Round(
                    ilinIcazeleri.Sum(x => Math.Max(0,
                        x.IcazeSaati
                        - (double)x.JetonOdenenSaat
                        - (x.NaharNezereAlinmasin
                            ? IcazeService.NaharKesishmeSaat(x.BaslamaSaati, x.BitisSaati, naharBas, naharDeq)
                            : 0))), 2);

                // ── 6d. Gecikmə balansı (cari il) ────────────────────────
                // Gün = Status==Gecikme; toplam saat = (faktiki giriş − standart giriş) cəmi.
                var standartGiris = izParam?.StandartGirisVaxti ?? new TimeSpan(9, 0, 0);
                var ilinGecikmeleri = await _unitOfWork.Repository<Davamiyyet>()
                    .HamisiniGetirAsync(
                        x => x.IsciId == isci.Id && !x.Silinib
                          && x.Status == DavamiyyetStatus.Gecikme
                          && x.Tarix.Year == cariIl,
                        izlemeden: true);

                // Tədbir (offline görüş) səbəbli gecikmələri bağışla — həmin gün iştirakçı olub
                // girişi tədbir bitmə saatı + tolerans içindədirsə gecikmə sayılmır (HR/ADMS ilə eyni).
                // (gecikTolerans yuxarıda ── 3-də təyin olunub.)
                var tedbirBitisleri = new Dictionary<DateTime, TimeSpan>();
                try
                {
                    var tedbirIshtiraklar = await _unitOfWork.Repository<GorushIshtirakci>()
                        .HamisiniGetirAsync(
                            x => x.IsciId == isci.Id && !x.Silinib
                              && x.Gorush.Nov == GorushNovu.Offline
                              && x.Gorush.Status != GorushStatus.LegvEdildi
                              && x.Status != IshtirakciStatus.Redd
                              && x.Status != IshtirakciStatus.IshtiraketmeyecekBildirib
                              && x.Gorush.Tarix.Year == cariIl
                              && x.Gorush.BitisSaati != null,
                            include: q => q.Include(gi => gi.Gorush),
                            izlemeden: true);
                    foreach (var t in tedbirIshtiraklar)
                    {
                        var d = t.Gorush.Tarix.Date;
                        var bit = t.Gorush.BitisSaati!.Value;
                        if (!tedbirBitisleri.TryGetValue(d, out var cur) || bit > cur)
                            tedbirBitisleri[d] = bit;
                    }
                }
                catch { /* Görüş cədvəli yoxdursa keç */ }

                bool GecikmeBagislanir(Davamiyyet r) =>
                    r.GirisVaxti.HasValue
                    && tedbirBitisleri.TryGetValue(r.Tarix.Date, out var bit)
                    && r.GirisVaxti.Value.TimeOfDay <= bit + gecikTolerans;

                var realGecikmeler = ilinGecikmeleri.Where(x => !GecikmeBagislanir(x)).ToList();

                dto.GecikmeGunSayi = realGecikmeler.Count;
                dto.GecikmeToplamSaat = Math.Round(
                    realGecikmeler
                        .Where(x => x.GirisVaxti.HasValue)
                        .Sum(x => Math.Max(0, (x.GirisVaxti!.Value.TimeOfDay - standartGiris).TotalHours)), 2);
                dto.GecikmeSonTarix = realGecikmeler.Count > 0
                    ? realGecikmeler.Max(x => x.Tarix)
                    : (DateTime?)null;

                // ── 7. Bildirişlər ────────────────────────────────────────
                // Sadə qaydalar: son maaş, imtina, workflow dəyişikliyi
                dto.Bildiriler = BuildBildiriler(dto);

                return Result<UserDashboardDto>.Ok(dto);
            }
            catch (Exception ex)
            {
                return Result<UserDashboardDto>.Fail($"Dashboard yüklənərkən xəta: {ex.Message}");
            }
        }

        // ── Köməkçi: ayın iş günlərini qaytarır (şənbə/bazar xaric) ──
        private static List<DateTime> GetAyinIsGunleri(int il, int ay)
        {
            return Enumerable
                .Range(1, DateTime.DaysInMonth(il, ay))
                .Select(d => new DateTime(il, ay, d))
                .Where(d => d.DayOfWeek != DayOfWeek.Saturday
                         && d.DayOfWeek != DayOfWeek.Sunday)
                .ToList();
        }

        // ── Köməkçi: bildirişləri avtomatik yarat ────────────────────────
        private static List<DashboardBildiriDto> BuildBildiriler(UserDashboardDto dto)
        {
            var list = new List<DashboardBildiriDto>();

            // Son maaş ödənilibsə
            var sonMaas = dto.SonOdenisler.FirstOrDefault();
            if (sonMaas != null && sonMaas.Status == MaasStatus.Odenildi)
            {
                list.Add(new DashboardBildiriDto
                {
                    Metn = $"{sonMaas.AyAd} maaşı hesabınıza köçürüldü",
                    Tarix = DateTime.Now.AddDays(-1),
                    Nov = BildiriNovu.Ugur
                });
            }

            // Aktiv müraciət varsa
            foreach (var m in dto.AktivMuracietler.Where(x =>
                x.Status != MezuniyyetStatus.Tesdiqlenib))
            {
                list.Add(new DashboardBildiriDto
                {
                    Metn = $"{m.NovAd} müraciətiniz — {m.WorkflowMerhele}",
                    Tarix = DateTime.Now.AddHours(-3),
                    Nov = BildiriNovu.Xeberdarliq
                });
            }

            // Əmək məzuniyyəti 5 gündən az qalıbsa
            if (dto.IllikQaligGun <= 5 && dto.IllikQaligGun >= 0)
            {
                list.Add(new DashboardBildiriDto
                {
                    Metn = $"Əmək məzuniyyətinizdən cəmi {dto.IllikQaligGun} gün qalıb",
                    Tarix = DateTime.Now.AddDays(-2),
                    Nov = BildiriNovu.Xeberdarliq
                });
            }

            return list.Take(5).ToList();
        }
    }
}