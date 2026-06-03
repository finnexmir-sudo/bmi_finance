using FinNex.Application.DTOs.HR.Tabel;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.HR
{
    public class TabelService : ITabelService
    {
        private readonly IUnitOfWork _uow;
        public TabelService(IUnitOfWork uow) => _uow = uow;

        public async Task<TabelAyDto> GenerateTabelAsync(int il, int ay)
        {
            var ayBaslangic = new DateTime(il, ay, 1);
            var ayBitis     = new DateTime(il, ay, DateTime.DaysInMonth(il, ay));
            int gunSayi     = DateTime.DaysInMonth(il, ay);

            // ── Bayramlar (bir gün əlavə: ayın son gününün sabahını da yoxlamaq üçün) ──
            var bayramlar = await _uow.Repository<BayramGunu>()
                .Query().AsNoTracking()
                .Where(b => !b.Silinib &&
                            b.Tarix.Date >= ayBaslangic.Date &&
                            b.Tarix.Date <= ayBitis.AddDays(1).Date)
                .ToListAsync();

            var bayramTarihleri = new HashSet<DateTime>(
                bayramlar.Where(b => b.Tip == GunTipi.Bayram).Select(b => b.Tarix.Date));
            var isGunuTarihleri = new HashSet<DateTime>(
                bayramlar.Where(b => b.Tip == GunTipi.IsGunu).Select(b => b.Tarix.Date));

            // ── Aktiv işçilər + teyinat ──
            var teyinatlar = await _uow.Repository<IsciTeyinat>()
                .Query().AsNoTracking()
                .Include(t => t.Isci)
                .Include(t => t.Departament)
                .Include(t => t.Vezife)
                .Where(t => !t.Silinib && t.Aktivdir &&
                            t.BaslamaTarixi.Date <= ayBitis.Date &&
                            (t.BitmeTarixi == null || t.BitmeTarixi.Value.Date >= ayBaslangic.Date))
                .ToListAsync();

            var isciler = teyinatlar
                .GroupBy(t => t.IsciId)
                .Select(g => g.OrderByDescending(t => t.BaslamaTarixi).First())
                .OrderBy(t => t.Departament?.Ad ?? "")
                .ThenBy(t => t.Isci.TamAd)
                .ToList();

            // ── Əlillik güzəştləri ──
            var elilGuzestler = await _uow.Repository<IsciGuzest>()
                .Query().AsNoTracking()
                .Include(ig => ig.Guzest)
                .Where(ig => !ig.Silinib &&
                             ig.Guzest.Ad.Contains("Əlil") &&
                             ig.BaslamaTarixi.Date <= ayBitis.Date &&
                             (ig.BitmeTarixi == null || ig.BitmeTarixi.Value.Date >= ayBaslangic.Date))
                .ToListAsync();
            var elilIsciIds = new HashSet<int>(elilGuzestler.Select(ig => ig.IsciId));

            // ── Məzuniyyətlər (tam təsdiqlənmiş) ──
            var mezuniyyetler = await _uow.Repository<Mezuniyyet>()
                .Query().AsNoTracking()
                .Where(m => !m.Silinib &&
                            m.Status == MezuniyyetStatus.Tesdiqlenib &&
                            m.BaslamaTarixi.Date <= ayBitis.Date &&
                            m.BitmeTarixi.Date  >= ayBaslangic.Date)
                .ToListAsync();

            // ── Xəstəliklər ──
            var xestelikler = await _uow.Repository<Xestelik>()
                .Query().AsNoTracking()
                .Where(x => !x.Silinib &&
                            x.Status == XestelikStatus.Tesdiqlenib &&
                            x.BaslamaTarixi.Date <= ayBitis.Date &&
                            x.BitmeTarixi.Date  >= ayBaslangic.Date)
                .ToListAsync();

            // ── Ezamiyyətlər ──
            var ezamiyyetler = await _uow.Repository<EzamiyyetMuraciet>()
                .Query().AsNoTracking()
                .Where(e => !e.Silinib &&
                            e.Status == EzamiyyetStatus.Tesdiqlendi &&
                            e.BaslamaTarixi.Date <= ayBitis.Date &&
                            e.BitmeTarixi.Date  >= ayBaslangic.Date)
                .ToListAsync();

            // ── Cədvəl qur ──
            var satirlar = new List<TabelIsciSatiri>();

            foreach (var t in isciler)
            {
                var isci      = t.Isci;
                bool isElil   = elilIsciIds.Contains(isci.Id);
                int normalSaat = isElil ? 7 : 8;

                var satir = new TabelIsciSatiri
                {
                    IsciAd      = isci.TamAd,
                    Vezife      = t.Vezife?.Ad ?? "",
                    Departament = t.Departament?.Ad ?? "",
                };

                int isGunSayi = 0, isSaatSayi = 0, mezGun = 0, ezamGun = 0, xestGun = 0;

                for (int d = 1; d <= gunSayi; d++)
                {
                    var gun = new DateTime(il, ay, d);

                    bool isWeekend = gun.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
                    bool isBayram  = bayramTarihleri.Contains(gun.Date);
                    bool isIsGunu  = isGunuTarihleri.Contains(gun.Date); // şənbə/bazar iş günü

                    // İş günüdür əgər: (həftəiçi YAXUD iş günü override) VƏ bayram deyil
                    bool isWorkingDay = (!isWeekend || isIsGunu) && !isBayram;

                    string kod;
                    if (!isWorkingDay)
                    {
                        kod = isBayram ? "B" : "İ";
                    }
                    else
                    {
                        bool hasMez  = mezuniyyetler.Any(m => m.IsciId == isci.Id &&
                            gun.Date >= m.BaslamaTarixi.Date && gun.Date <= m.BitmeTarixi.Date);
                        bool hasXest = xestelikler.Any(x => x.IsciId == isci.Id &&
                            gun.Date >= x.BaslamaTarixi.Date && gun.Date <= x.BitmeTarixi.Date);
                        bool hasEzam = ezamiyyetler.Any(e => e.IsciId == isci.Id &&
                            gun.Date >= e.BaslamaTarixi.Date && gun.Date <= e.BitmeTarixi.Date);

                        if      (hasMez)  { kod = "M"; mezGun++;  }
                        else if (hasXest) { kod = "X"; xestGun++; }
                        else if (hasEzam) { kod = "E"; ezamGun++; }
                        else
                        {
                            // Bayram ərəfəsi: sabah bayramdırsa → 1 saat az
                            bool bayramErtesi = bayramTarihleri.Contains(gun.AddDays(1).Date);
                            int saat = bayramErtesi ? normalSaat - 1 : normalSaat;
                            kod = saat.ToString();
                            isGunSayi++;
                            isSaatSayi += saat;
                        }
                    }

                    satir.GunKodlari.Add(kod);
                }

                satir.IsGunSayi     = isGunSayi;
                satir.IsSaatSayi    = isSaatSayi;
                satir.MezuniyyetGun = mezGun;
                satir.EzamiyyetGun  = ezamGun;
                satir.XestelikGun   = xestGun;

                satirlar.Add(satir);
            }

            return new TabelAyDto { Il = il, Ay = ay, GunSayi = gunSayi, Satirlar = satirlar };
        }
    }
}
