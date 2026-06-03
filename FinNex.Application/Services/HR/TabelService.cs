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

            // ── Günlük saat cədvəlini əvvəlcədən hesabla (bütün işçilər üçün eynidir) ──
            // Tam 5 günlük həftə: ilk iş günü = 8, qalanı = 7  (8+7+7+7+7 = 36 saat)
            // Natamam həftə (bayram varsa): hər iş günü = 8
            var gunBaseSaatlari   = new Dictionary<int, int>(); // gün nömrəsi → saat
            var bayramErtesiGunler = new HashSet<int>();        // bayram ərəfəsi iş günlərinin nömrəsi

            for (int d = 1; d <= gunSayi; d++)
            {
                var gun_      = new DateTime(il, ay, d);
                bool wend_    = gun_.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
                bool bayr_    = bayramTarihleri.Contains(gun_.Date);
                bool isGunu_  = isGunuTarihleri.Contains(gun_.Date);
                if (!(!wend_ || isGunu_) || bayr_) continue; // iş günü deyil

                bool ertesi = bayramTarihleri.Contains(gun_.AddDays(1).Date);
                if (ertesi) bayramErtesiGunler.Add(d);

                // Bu günün həftəsindəki bütün iş günlərini say (Bazar ertəsi–Cümə)
                int dow_  = gun_.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)gun_.DayOfWeek;
                var mon_  = gun_.AddDays(-(dow_ - 1));
                int wCnt  = 0, dIdx = -1, idx_ = 0;
                for (int wd = 0; wd < 5; wd++)
                {
                    var wday  = mon_.AddDays(wd);
                    bool wb   = bayramTarihleri.Contains(wday.Date);
                    bool wig  = isGunuTarihleri.Contains(wday.Date);
                    bool wwe  = wday.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
                    if ((!wwe || wig) && !wb) { if (wday.Date == gun_.Date) dIdx = idx_; wCnt++; idx_++; }
                }
                int base_ = wCnt == 5 ? (dIdx == 0 ? 8 : 7) : 8;
                gunBaseSaatlari[d] = ertesi ? Math.Min(base_, 7) : base_;
            }

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
                var isci = t.Isci;

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

                    bool isWeekend    = gun.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
                    bool isBayram     = bayramTarihleri.Contains(gun.Date);
                    bool isIsGunu     = isGunuTarihleri.Contains(gun.Date);
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
                            // Saat əvvəlcədən hesablanmış cədvəldən götürülür
                            int saat = gunBaseSaatlari[d];
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

            return new TabelAyDto
            {
                Il = il, Ay = ay, GunSayi = gunSayi,
                BayramErtesiGunler = bayramErtesiGunler,
                Satirlar = satirlar
            };
        }
    }
}
