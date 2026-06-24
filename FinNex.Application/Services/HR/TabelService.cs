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
                            t.Isci.Status != IsciStatus.IshtenCixib &&   // çıxmış işçilər tabeldə göstərilmir
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

            // ── Günlük saatları əvvəlcədən hesabla ──────────────────────────────────
            // Normal işçi : hər iş günü 8 saat; bayram ərəfəsi → 7 saat
            // Əlil işçi   : tam 5 günlük həftə → 8+7+7+7+7 = 36 saat (natamam həftə → 7);
            //               bayram ərəfəsi → 7 saat (əlavə azalma yoxdur)
            var gunNormalSaat = new Dictionary<int, int>(); // gün nömrəsi → normal işçi saatı
            var gunElilSaat   = new Dictionary<int, int>(); // gün nömrəsi → əlil işçi saatı
            var bayramErtesiGunler = new HashSet<int>();    // bayram ərəfəsi iş günlərinin nömrəsi

            for (int d = 1; d <= gunSayi; d++)
            {
                var gun   = new DateTime(il, ay, d);
                bool wend = gun.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
                bool bayr = bayramTarihleri.Contains(gun.Date);
                bool igün = isGunuTarihleri.Contains(gun.Date);
                if ((!(!wend || igün)) || bayr) continue; // iş günü deyil

                bool ertesi = bayramTarihleri.Contains(gun.AddDays(1).Date);
                if (ertesi) bayramErtesiGunler.Add(d);

                // Normal işçi: 8; bayram ərəfəsi → 7
                gunNormalSaat[d] = ertesi ? 7 : 8;

                // Əlil işçi: həftədəki iş günlərini say
                int dow  = gun.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)gun.DayOfWeek;
                var mon  = gun.AddDays(-(dow - 1));
                int wCnt = 0, dIdx = -1, idx = 0;
                for (int wd = 0; wd < 5; wd++)
                {
                    var wday = mon.AddDays(wd);
                    bool wb  = bayramTarihleri.Contains(wday.Date);
                    bool wig = isGunuTarihleri.Contains(wday.Date);
                    bool wwe = wday.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
                    if ((!wwe || wig) && !wb)
                    {
                        if (wday.Date == gun.Date) dIdx = idx;
                        wCnt++; idx++;
                    }
                }
                // Tam 5 günlük həftə: 5-ci iş günü=4, qalanı=8; natamam həftə: 8
                int elilBase = (wCnt == 5 && dIdx == 4) ? 4 : 8;
                // Bayram ərəfəsi: max 7 (əlil işçi üçün əlavə azalma yoxdur)
                gunElilSaat[d] = ertesi ? Math.Min(elilBase, 7) : elilBase;
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

            // ── Cədvəl qur ──
            var satirlar = new List<TabelIsciSatiri>();

            foreach (var t in isciler)
            {
                var isci   = t.Isci;
                bool isElil = elilIsciIds.Contains(isci.Id);
                var saatCedveli = isElil ? gunElilSaat : gunNormalSaat;

                var satir = new TabelIsciSatiri
                {
                    IsciAd      = isci.TamAd,
                    Vezife      = t.Vezife?.Ad ?? "",
                    Departament = t.Departament?.Ad ?? "",
                };

                int isGunSayi = 0, isSaatSayi = 0, mezGun = 0, xestGun = 0;

                for (int d = 1; d <= gunSayi; d++)
                {
                    var gun = new DateTime(il, ay, d);

                    bool isWeekend    = gun.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
                    bool isBayram     = bayramTarihleri.Contains(gun.Date);
                    bool isIsGunu     = isGunuTarihleri.Contains(gun.Date);
                    bool isWorkingDay = (!isWeekend || isIsGunu) && !isBayram;

                    // Jetonla ödənilmiş məzuniyyət "M" sayılmır — jeton mükafatı günü ödəyir,
                    // ona görə adi iş günü kimi (saat yazılır) qalır. Yalnız adi məzuniyyət → "M".
                    bool hasMez = mezuniyyetler.Any(m => m.IsciId == isci.Id &&
                        !m.JetonIleOdendi &&
                        gun.Date >= m.BaslamaTarixi.Date && gun.Date <= m.BitmeTarixi.Date);

                    string kod;
                    if (isBayram)
                    {
                        // Bayram (qeyri-iş) günü məzuniyyətdən ÜSTÜNDÜR — bayram günü məzuniyyət
                        // sayılmır: "B" yazılır və Məz. sayğacına daxil edilmir.
                        kod = "B";
                    }
                    else if (hasMez)
                    {
                        kod = "M"; mezGun++;
                    }
                    else if (!isWorkingDay)
                    {
                        kod = "İ"; // bayram artıq yuxarıda yoxlanıldı — burada yalnız həftə sonu (İstirahət)
                    }
                    else
                    {
                        bool hasXest = xestelikler.Any(x => x.IsciId == isci.Id &&
                            gun.Date >= x.BaslamaTarixi.Date && gun.Date <= x.BitmeTarixi.Date);

                        if (hasXest)
                        {
                            kod = "X"; xestGun++;
                        }
                        else
                        {
                            int saat = saatCedveli[d];
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
                satir.EzamiyyetGun  = 0;
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
