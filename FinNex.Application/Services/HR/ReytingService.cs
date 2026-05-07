using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Reyting;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.HR
{
    public class ReytingService : IReytingService
    {
        private readonly IUnitOfWork _uow;
        private readonly UserManager<AppUser> _userManager;

        public ReytingService(IUnitOfWork uow, UserManager<AppUser> userManager)
        {
            _uow = uow;
            _userManager = userManager;
        }

        // ── Kateqoriyalar ────────────────────────────────────────

        public async Task<IList<ReytingKateqoriyasiDto>> KateqoriyalariGetirAsync()
        {
            var list = await _uow.Repository<ReytingKateqoriyasi>()
                .Query().OrderBy(x => x.Sira).ToListAsync();
            return list.Select(MapKat).ToList();
        }

        public async Task<Result> KateqoriyaSaxlaAsync(ReytingKateqoriyasiSaveDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Ad))
                return Result.Fail("Ad boş ola bilməz.");
            if (dto.MinXal > dto.MaxXal)
                return Result.Fail("Min xal Max xaldan böyük ola bilməz.");
            if (dto.PulAmsali <= 0 || dto.SaatAmsali <= 0)
                return Result.Fail("Əmsal müsbət olmalıdır.");

            if (dto.Id == 0)
            {
                var kat = new ReytingKateqoriyasi
                {
                    Ad = dto.Ad, RengKodu = dto.RengKodu,
                    MinXal = dto.MinXal, MaxXal = dto.MaxXal,
                    PulAmsali = dto.PulAmsali, SaatAmsali = dto.SaatAmsali,
                    Sira = dto.Sira, Aktivdir = dto.Aktivdir
                };
                await _uow.Repository<ReytingKateqoriyasi>().YaratAsync(kat);
            }
            else
            {
                var kat = await _uow.Repository<ReytingKateqoriyasi>()
                    .Query().FirstOrDefaultAsync(x => x.Id == dto.Id);
                if (kat == null) return Result.Fail("Kateqoriya tapılmadı.");
                kat.Ad = dto.Ad; kat.RengKodu = dto.RengKodu;
                kat.MinXal = dto.MinXal; kat.MaxXal = dto.MaxXal;
                kat.PulAmsali = dto.PulAmsali; kat.SaatAmsali = dto.SaatAmsali;
                kat.Sira = dto.Sira; kat.Aktivdir = dto.Aktivdir;
                await _uow.Repository<ReytingKateqoriyasi>().YenileAsync(kat);
            }

            await _uow.YaddaSaxlaAsync();
            return Result.Ok("Saxlanıldı.");
        }

        public async Task<Result> KateqoriyaSilAsync(int id)
        {
            var kat = await _uow.Repository<ReytingKateqoriyasi>()
                .Query().FirstOrDefaultAsync(x => x.Id == id);
            if (kat == null) return Result.Fail("Kateqoriya tapılmadı.");

            var istifadeVarmi = await _uow.Repository<IsciReytinqi>()
                .Query().AnyAsync(x => x.KateqoriyaId == id);
            if (istifadeVarmi)
                return Result.Fail("Bu kateqoriya işçi reytinqlərində istifadə edilir, silinə bilməz.");

            await _uow.Repository<ReytingKateqoriyasi>().YumshakSilAsync(id);
            await _uow.YaddaSaxlaAsync();
            return Result.Ok("Silindi.");
        }

        // ── Parametrlər ──────────────────────────────────────────

        public async Task<IList<ReytingParametriDto>> ParametrləriGetirAsync()
        {
            var list = await _uow.Repository<ReytingParametri>()
                .Query().OrderBy(x => x.HadiseNovu).ToListAsync();
            return list.Select(MapParam).ToList();
        }

        public async Task<Result> ParametrSaxlaAsync(ReytingParametriSaveDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Ad))
                return Result.Fail("Ad boş ola bilməz.");

            if (dto.Id == 0)
            {
                var p = new ReytingParametri
                {
                    HadiseNovu = dto.HadiseNovu, Ad = dto.Ad,
                    XalDeyeri = dto.XalDeyeri, Aktivdir = dto.Aktivdir
                };
                await _uow.Repository<ReytingParametri>().YaratAsync(p);
            }
            else
            {
                var p = await _uow.Repository<ReytingParametri>()
                    .Query().FirstOrDefaultAsync(x => x.Id == dto.Id);
                if (p == null) return Result.Fail("Parametr tapılmadı.");
                p.HadiseNovu = dto.HadiseNovu; p.Ad = dto.Ad;
                p.XalDeyeri = dto.XalDeyeri; p.Aktivdir = dto.Aktivdir;
                await _uow.Repository<ReytingParametri>().YenileAsync(p);
            }

            await _uow.YaddaSaxlaAsync();
            return Result.Ok("Saxlanıldı.");
        }

        // ── Hesablama ────────────────────────────────────────────

        public async Task<Result> ReytinqHesablaAsync(int isciId, int il, int ay)
        {
            var isci = await _uow.Repository<Isci>()
                .Query().FirstOrDefaultAsync(x => x.Id == isciId);
            if (isci == null) return Result.Fail("İşçi tapılmadı.");

            var parametrlər = await _uow.Repository<ReytingParametri>()
                .Query().Where(x => x.Aktivdir).ToListAsync();

            var avtXal = await AvtomatikXalHesablaAsync(isciId, il, ay, parametrlər);

            var cari = await _uow.Repository<IsciReytinqi>()
                .Query()
                .Include(x => x.ManualQeydler)
                .FirstOrDefaultAsync(x => x.IsciId == isciId && x.Il == il && x.Ay == ay);

            int manualXal = cari?.ManualXal ?? 0;
            int cemiXal = avtXal + manualXal;

            var kateqoriya = await KateqoriyaTapAsync(cemiXal);

            if (cari == null)
            {
                cari = new IsciReytinqi
                {
                    IsciId = isciId, Il = il, Ay = ay,
                    AvtomatikXal = avtXal, ManualXal = manualXal,
                    CemiXal = cemiXal, KateqoriyaId = kateqoriya?.Id,
                    HesablamaTarixi = DateTime.Now
                };
                await _uow.Repository<IsciReytinqi>().YaratAsync(cari);
            }
            else
            {
                cari.AvtomatikXal = avtXal;
                cari.CemiXal = cemiXal;
                cari.KateqoriyaId = kateqoriya?.Id;
                cari.HesablamaTarixi = DateTime.Now;
                await _uow.Repository<IsciReytinqi>().YenileAsync(cari);
            }

            await _uow.YaddaSaxlaAsync();
            return Result.Ok("Reytinq hesablandı.");
        }

        public async Task<Result> TopluHesablaAsync(int il, int ay)
        {
            var isciler = await _uow.Repository<Isci>()
                .Query().Where(x => !x.Silinib).Select(x => x.Id).ToListAsync();

            foreach (var isciId in isciler)
                await ReytinqHesablaAsync(isciId, il, ay);

            return Result.Ok($"{isciler.Count} işçi üçün reytinq hesablandı.");
        }

        // ── Manual əlavə ─────────────────────────────────────────

        public async Task<Result> ManualXalElavEtAsync(ManualReytingElavEtDto dto, int elavEdenUserId)
        {
            if (string.IsNullOrWhiteSpace(dto.Sebeb))
                return Result.Fail("Səbəb boş ola bilməz.");
            if (dto.Xal == 0)
                return Result.Fail("Xal 0 ola bilməz.");

            var isci = await _uow.Repository<Isci>()
                .Query().FirstOrDefaultAsync(x => x.Id == dto.IsciId);
            if (isci == null) return Result.Fail("İşçi tapılmadı.");

            // Snapshot yoxdursa yarat
            var reytinq = await _uow.Repository<IsciReytinqi>()
                .Query()
                .FirstOrDefaultAsync(x => x.IsciId == dto.IsciId && x.Il == dto.Il && x.Ay == dto.Ay);

            if (reytinq == null)
            {
                reytinq = new IsciReytinqi
                {
                    IsciId = dto.IsciId, Il = dto.Il, Ay = dto.Ay,
                    AvtomatikXal = 0, ManualXal = 0, CemiXal = 0,
                    HesablamaTarixi = DateTime.Now
                };
                await _uow.Repository<IsciReytinqi>().YaratAsync(reytinq);
                await _uow.YaddaSaxlaAsync();
            }

            var qeyd = new ManualReytingQeydi
            {
                IsciReytinqiId = reytinq.Id,
                Xal = dto.Xal, Sebeb = dto.Sebeb,
                ElavEdenUserId = elavEdenUserId,
                Tarix = DateTime.Now
            };
            await _uow.Repository<ManualReytingQeydi>().YaratAsync(qeyd);

            reytinq.ManualXal += dto.Xal;
            reytinq.CemiXal = reytinq.AvtomatikXal + reytinq.ManualXal;
            reytinq.KateqoriyaId = (await KateqoriyaTapAsync(reytinq.CemiXal))?.Id;
            await _uow.Repository<IsciReytinqi>().YenileAsync(reytinq);

            await _uow.YaddaSaxlaAsync();
            return Result.Ok("Xal əlavə edildi.");
        }

        // ── Sorğular ─────────────────────────────────────────────

        public async Task<IList<IsciReytinqiDto>> IsciReytinqleriGetirAsync(int il, int ay)
        {
            var list = await _uow.Repository<IsciReytinqi>()
                .Query()
                .Include(x => x.Isci)
                    .ThenInclude(i => i.IsciTeyinatlari.Where(t => t.Aktivdir))
                        .ThenInclude(t => t.Departament)
                .Include(x => x.Isci)
                    .ThenInclude(i => i.IsciTeyinatlari.Where(t => t.Aktivdir))
                        .ThenInclude(t => t.Vezife)
                .Include(x => x.Kateqoriya)
                .Include(x => x.ManualQeydler)
                .Where(x => x.Il == il && x.Ay == ay)
                .ToListAsync();

            var userIds = list.SelectMany(r => r.ManualQeydler.Select(q => q.ElavEdenUserId)).Distinct().ToList();
            var users = await _userManager.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();
            var userMap = users.ToDictionary(u => u.Id, u => u.Ad + " " + u.Soyad);

            return list.Select(r => MapReytinq(r, userMap)).ToList();
        }

        public async Task<IsciReytinqiDto?> IsciReytinqiGetirAsync(int isciId, int il, int ay)
        {
            var r = await _uow.Repository<IsciReytinqi>()
                .Query()
                .Include(x => x.Isci)
                    .ThenInclude(i => i.IsciTeyinatlari.Where(t => t.Aktivdir))
                        .ThenInclude(t => t.Departament)
                .Include(x => x.Isci)
                    .ThenInclude(i => i.IsciTeyinatlari.Where(t => t.Aktivdir))
                        .ThenInclude(t => t.Vezife)
                .Include(x => x.Kateqoriya)
                .Include(x => x.ManualQeydler)
                .FirstOrDefaultAsync(x => x.IsciId == isciId && x.Il == il && x.Ay == ay);

            if (r == null) return null;

            var userIds = r.ManualQeydler.Select(q => q.ElavEdenUserId).Distinct().ToList();
            var users = await _userManager.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();
            var userMap = users.ToDictionary(u => u.Id, u => u.Ad + " " + u.Soyad);

            return MapReytinq(r, userMap);
        }

        public async Task<IsciCariReytinqDto?> IsciCariReytinqiniGetirAsync(int isciId)
        {
            var r = await _uow.Repository<IsciReytinqi>()
                .Query()
                .Include(x => x.Kateqoriya)
                .Include(x => x.ManualQeydler)
                .Where(x => x.IsciId == isciId)
                .OrderByDescending(x => x.Il).ThenByDescending(x => x.Ay)
                .FirstOrDefaultAsync();

            if (r == null) return null;

            var userIds = r.ManualQeydler.Select(q => q.ElavEdenUserId).Distinct().ToList();
            var users = await _userManager.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();
            var userMap = users.ToDictionary(u => u.Id, u => u.Ad + " " + u.Soyad);

            return new IsciCariReytinqDto
            {
                CemiXal = r.CemiXal,
                KateqoriyaAd = r.Kateqoriya?.Ad,
                KateqoriyaReng = r.Kateqoriya?.RengKodu,
                PulAmsali = r.Kateqoriya?.PulAmsali ?? 1.00m,
                SaatAmsali = r.Kateqoriya?.SaatAmsali ?? 1.00m,
                Il = r.Il, Ay = r.Ay,
                SonQeydler = r.ManualQeydler
                    .OrderByDescending(q => q.Tarix).Take(5)
                    .Select(q => new ManualQeydDto
                    {
                        Id = q.Id, Xal = q.Xal, Sebeb = q.Sebeb,
                        ElavEden = userMap.GetValueOrDefault(q.ElavEdenUserId, "—"),
                        Tarix = q.Tarix
                    }).ToList()
            };
        }

        public async Task<(decimal pulAmsali, decimal saatAmsali)> IsciAmsallariGetirAsync(int isciId)
        {
            var r = await _uow.Repository<IsciReytinqi>()
                .Query()
                .Include(x => x.Kateqoriya)
                .Where(x => x.IsciId == isciId)
                .OrderByDescending(x => x.Il).ThenByDescending(x => x.Ay)
                .FirstOrDefaultAsync();

            var pul = r?.Kateqoriya?.PulAmsali ?? 1.00m;
            var saat = r?.Kateqoriya?.SaatAmsali ?? 1.00m;
            return (pul, saat);
        }

        // ── Köməkçi metodlar ─────────────────────────────────────

        private async Task<int> AvtomatikXalHesablaAsync(int isciId, int il, int ay,
            IList<ReytingParametri> parametrlər)
        {
            int xal = 0;
            var ayBaslangic = new DateTime(il, ay, 1);
            var aySonu = ayBaslangic.AddMonths(1);

            // Müsbət jeton
            var musXal = parametrlər.FirstOrDefault(p => p.HadiseNovu == ReytingHadiseNovu.MusbatJetonAldi);
            if (musXal != null)
            {
                var sayi = await _uow.Repository<IsciJetonu>()
                    .Query()
                    .Include(j => j.JetonTeyinati)
                    .CountAsync(j => j.IsciId == isciId
                        && j.JetonTeyinati.Nov == JetonNovu.Musbat
                        && j.QazanmaTarixi >= ayBaslangic
                        && j.QazanmaTarixi < aySonu
                        && j.Status == IsciJetonuStatus.Aktiv);
                xal += sayi * musXal.XalDeyeri;
            }

            // Qara jeton
            var qaraXal = parametrlər.FirstOrDefault(p => p.HadiseNovu == ReytingHadiseNovu.QaraJetonAldi);
            if (qaraXal != null)
            {
                var sayi = await _uow.Repository<IsciJetonu>()
                    .Query()
                    .Include(j => j.JetonTeyinati)
                    .CountAsync(j => j.IsciId == isciId
                        && j.JetonTeyinati.Nov == JetonNovu.Menfi
                        && j.QazanmaTarixi >= ayBaslangic
                        && j.QazanmaTarixi < aySonu
                        && j.Status == IsciJetonuStatus.Aktiv);
                xal += sayi * qaraXal.XalDeyeri;
            }

            // Gecikmə
            var gecXal = parametrlər.FirstOrDefault(p => p.HadiseNovu == ReytingHadiseNovu.Gecikme);
            if (gecXal != null)
            {
                var sayi = await _uow.Repository<Davamiyyet>()
                    .Query()
                    .CountAsync(d => d.IsciId == isciId
                        && d.Status == DavamiyyetStatus.Gecikme
                        && d.Tarix >= ayBaslangic
                        && d.Tarix < aySonu);
                xal += sayi * gecXal.XalDeyeri;
            }

            // İcazəsiz qayıb
            var qayibXal = parametrlər.FirstOrDefault(p => p.HadiseNovu == ReytingHadiseNovu.IcazesizQayib);
            if (qayibXal != null)
            {
                var sayi = await _uow.Repository<Davamiyyet>()
                    .Query()
                    .CountAsync(d => d.IsciId == isciId
                        && d.Status == DavamiyyetStatus.Qayib
                        && d.MaasdanKes
                        && d.Tarix >= ayBaslangic
                        && d.Tarix < aySonu);
                xal += sayi * qayibXal.XalDeyeri;
            }

            return xal;
        }

        private async Task<ReytingKateqoriyasi?> KateqoriyaTapAsync(int cemiXal)
        {
            return await _uow.Repository<ReytingKateqoriyasi>()
                .Query()
                .Where(k => k.Aktivdir && cemiXal >= k.MinXal && cemiXal <= k.MaxXal)
                .OrderBy(k => k.Sira)
                .FirstOrDefaultAsync();
        }

        // ── Mapper-lər ───────────────────────────────────────────

        private static ReytingKateqoriyasiDto MapKat(ReytingKateqoriyasi k) => new()
        {
            Id = k.Id, Ad = k.Ad, RengKodu = k.RengKodu,
            MinXal = k.MinXal, MaxXal = k.MaxXal,
            PulAmsali = k.PulAmsali, SaatAmsali = k.SaatAmsali,
            Sira = k.Sira, Aktivdir = k.Aktivdir
        };

        private static ReytingParametriDto MapParam(ReytingParametri p) => new()
        {
            Id = p.Id, HadiseNovu = p.HadiseNovu,
            HadiseAdi = HadiseAdGetir(p.HadiseNovu),
            Ad = p.Ad, XalDeyeri = p.XalDeyeri, Aktivdir = p.Aktivdir
        };

        private static IsciReytinqiDto MapReytinq(IsciReytinqi r,
            Dictionary<int, string> userMap)
        {
            var teyinat = r.Isci.IsciTeyinatlari.FirstOrDefault(t => t.Aktivdir);
            return new IsciReytinqiDto
            {
                Id = r.Id, IsciId = r.IsciId,
                IsciAd = r.Isci.Ad + " " + r.Isci.Soyad,
                Vezife = teyinat?.Vezife?.Ad,
                Departament = teyinat?.Departament?.Ad,
                Il = r.Il, Ay = r.Ay,
                AyAd = AyAdGetir(r.Ay),
                AvtomatikXal = r.AvtomatikXal,
                ManualXal = r.ManualXal,
                CemiXal = r.CemiXal,
                KateqoriyaAd = r.Kateqoriya?.Ad,
                KateqoriyaReng = r.Kateqoriya?.RengKodu,
                PulAmsali = r.Kateqoriya?.PulAmsali ?? 1.00m,
                SaatAmsali = r.Kateqoriya?.SaatAmsali ?? 1.00m,
                HesablamaTarixi = r.HesablamaTarixi,
                ManualQeydler = r.ManualQeydler
                    .OrderByDescending(q => q.Tarix)
                    .Select(q => new ManualQeydDto
                    {
                        Id = q.Id, Xal = q.Xal, Sebeb = q.Sebeb,
                        ElavEden = userMap.GetValueOrDefault(q.ElavEdenUserId, "—"),
                        Tarix = q.Tarix
                    }).ToList()
            };
        }

        private static string HadiseAdGetir(ReytingHadiseNovu nov) => nov switch
        {
            ReytingHadiseNovu.MusbatJetonAldi => "Müsbət jeton aldı",
            ReytingHadiseNovu.QaraJetonAldi   => "Qara jeton aldı",
            ReytingHadiseNovu.Gecikme          => "Gecikdi",
            ReytingHadiseNovu.IcazesizQayib   => "İcazəsiz qayıb",
            _                                  => nov.ToString()
        };

        private static string AyAdGetir(int ay) => ay switch
        {
            1 => "Yan", 2 => "Fev", 3 => "Mar", 4 => "Apr",
            5 => "May", 6 => "İyn", 7 => "İyl", 8 => "Avq",
            9 => "Sep", 10 => "Okt", 11 => "Noy", 12 => "Dek",
            _ => ay.ToString()
        };
    }
}
