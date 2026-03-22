using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Dashboard;
using FinNex.Application.Interfaces;
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
                            .Include(i => i.Departament)
                            .Include(i => i.Vezife)
                            .Include(i => i.Maliye)
                            .Include(i => i.mezuniyyetBalans),
                        izlemeden: true);

                if (isci == null)
                    return Result<UserDashboardDto>.Fail("İşçi profili tapılmadı.");

                var dto = new UserDashboardDto
                {
                    IsciId = isci.Id,
                    TamAd = isci.TamAd,
                    VezifeAdi = isci.Vezife?.Ad ?? "-",
                    SobeAdi = isci.Departament?.Ad ?? "-",
                    IsheBaslamaTarixi = isci.IsheBaslamaTarixi,
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
                dto.IcazeliGun = davamiyyetler.Count(x => x.Status == DavamiyyetStatus.Icazeli);

                // Hələ qeydə alınmamış iş günləri (gələcək)
                var isGunleri = GetAyinIsGunleri(buIl, buAy);
                dto.IsGunuSayi = isGunleri.Count;
                dto.GozlenilenGun = isGunleri.Count(g => g > DateTime.Today
                    && !davamiyyetler.Any(d => d.Tarix.Date == g.Date));

                // ── 3. Davamiyyət təqvimi ────────────────────────────────
                var aydakiButunGunler = Enumerable
                    .Range(1, DateTime.DaysInMonth(buIl, buAy))
                    .Select(d => new DateTime(buIl, buAy, d))
                    .Where(d => d.DayOfWeek != DayOfWeek.Saturday
                             && d.DayOfWeek != DayOfWeek.Sunday)
                    .ToList();

                dto.DavamiyyetTakvim = aydakiButunGunler.Select(gun =>
                {
                    var qeyd = davamiyyetler.FirstOrDefault(d => d.Tarix.Date == gun.Date);
                    return new DashboardDavamiyyetGunDto
                    {
                        Tarix = gun,
                        Status = qeyd?.Status ?? DavamiyyetStatus.Isde
                    };
                }).ToList();

                // ── 4. Məzuniyyət balansı ────────────────────────────────
                // İllik — MezuniyyetBalans-dan
                var balans = isci.mezuniyyetBalans;
                if (balans != null && balans.Il == buIl)
                {
                    dto.IllikToplamGun = balans.ToplamGun;
                    dto.IllikIstifadeGun = balans.IstifadeOlunanGun;
                }

                // Xəstəlik və Ezamiyyət — Mezuniyyet cədvəlindən hesabla
                var tesdiqlenmisMezuniyyetler = await _unitOfWork.Repository<Mezuniyyet>()
                    .HamisiniGetirAsync(
                        x => x.IsciId == isci.Id
                          && x.Status == MezuniyyetStatus.Tesdiqlenib
                          && x.BaslamaTarixi.Year == buIl,
                        izlemeden: true);

                dto.XestelikIstifadeGun = tesdiqlenmisMezuniyyetler
                    .Where(x => x.Nov == MezuniyyetNovu.Xestelik)
                    .Sum(x => x.IsGunlerininSayi);

                dto.EzamiyyetIstifadeGun = tesdiqlenmisMezuniyyetler
                    .Where(x => x.Nov == MezuniyyetNovu.Ezamiyyet)
                    .Sum(x => x.IsGunlerininSayi);

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
                          && x.Status != MezuniyyetStatus.ImtinaEdildi,
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
                          && x.Status != IcazeStatus.ImtinaEdildi,
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

            // İllik məzuniyyət 5 gündən az qalıbsa
            if (dto.IllikQaligGun <= 5 && dto.IllikQaligGun >= 0)
            {
                list.Add(new DashboardBildiriDto
                {
                    Metn = $"İllik məzuniyyətinizdən cəmi {dto.IllikQaligGun} gün qalıb",
                    Tarix = DateTime.Now.AddDays(-2),
                    Nov = BildiriNovu.Xeberdarliq
                });
            }

            return list.Take(5).ToList();
        }
    }
}