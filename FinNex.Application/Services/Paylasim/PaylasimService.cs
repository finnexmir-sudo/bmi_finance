using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Paylasim;
using FinNex.Application.Interfaces.Paylasim;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Ganss.Xss;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.Paylasim
{
    public class PaylasimService : IPaylasimService
    {
        private readonly IUnitOfWork _uow;
        private readonly HtmlSanitizer _sanitizer;

        public PaylasimService(IUnitOfWork uow)
        {
            _uow = uow;

            // Quill-in çıxardığı HTML-i təmizləyir — <script>/onerror və s. atılır,
            // qalın/kursiv/siyahı/başlıq/link/şəkil (yalnız src, alt) saxlanılır.
            _sanitizer = new HtmlSanitizer();
            _sanitizer.AllowedTags.Add("img");
            _sanitizer.AllowedAttributes.Add("src");
            _sanitizer.AllowedAttributes.Add("alt");
        }

        public async Task<List<PaylasimListDto>> HamisiniGetirAsync()
        {
            var lista = await _uow.Repository<Melumat>().Query().AsNoTracking()
                .Include(x => x.GonderenAppUser)
                .Include(x => x.Kateqoriya)
                .OrderByDescending(x => x.YaradilmaTarixi)
                .ToListAsync();

            var hedefSaylari = await _uow.Repository<MelumatHedef>().Query().AsNoTracking()
                .GroupBy(x => x.MelumatId)
                .Select(g => new { MelumatId = g.Key, Say = g.Count() })
                .ToDictionaryAsync(g => g.MelumatId, g => g.Say);

            return lista.Select(m => Map(m, hedefSaylari.GetValueOrDefault(m.Id))).ToList();
        }

        public async Task<List<PaylasimListDto>> AktivLentAsync(int isciId)
        {
            var lista = await GorunenlerSorgusu(isciId)
                .Include(x => x.GonderenAppUser)
                .Include(x => x.Kateqoriya)
                .OrderByDescending(x => x.YaradilmaTarixi)
                .Take(50)
                .ToListAsync();

            return lista.Select(m => Map(m)).ToList();
        }

        /// <summary>
        /// Bir işçiyə görünən (aktiv, qaralama olmayan, hədəflənmiş/"hamısına") postların
        /// ortaq filtri — `AktivLentAsync` VƏ `YeniSayAsync` eyni şərti TƏKRARLAMASIN deyə
        /// bura çıxarıldı (bax CLAUDE.md "Bir Kəmiyyətə Üç Yazıcı" prinsipi).
        /// </summary>
        private IQueryable<Melumat> GorunenlerSorgusu(int isciId)
        {
            // Hədəf sətri YOXDURSA → "Hamısına" (geriyə uyğun). Varsa → yalnız bu Isci.
            var hedefRepo = _uow.Repository<MelumatHedef>();

            return _uow.Repository<Melumat>().Query().AsNoTracking()
                .Where(x => x.Aktivdir && !x.Qaralamadir &&
                    (!hedefRepo.Query().Any(h => h.MelumatId == x.Id) ||
                     hedefRepo.Query().Any(h => h.MelumatId == x.Id && h.IsciId == isciId)));
        }

        public async Task<int> YeniSayAsync(int isciId, int appUserId)
        {
            var baxis = await _uow.Repository<PaylasimBaxis>().Query().AsNoTracking()
                .FirstOrDefaultAsync(x => x.AppUserId == appUserId);
            var sonBaxis = baxis?.SonBaxisTarixi ?? DateTime.MinValue;

            return await GorunenlerSorgusu(isciId)
                .Where(x => x.YaradilmaTarixi > sonBaxis)
                .CountAsync();
        }

        public async Task GorulduIsaretleAsync(int appUserId)
        {
            var movcud = await _uow.Repository<PaylasimBaxis>().Query()
                .FirstOrDefaultAsync(x => x.AppUserId == appUserId);

            if (movcud != null)
            {
                movcud.SonBaxisTarixi = DateTime.Now;
                await _uow.Repository<PaylasimBaxis>().YenileAsync(movcud);
            }
            else
            {
                await _uow.Repository<PaylasimBaxis>().YaratAsync(new PaylasimBaxis
                {
                    AppUserId = appUserId,
                    SonBaxisTarixi = DateTime.Now
                });
            }

            await _uow.YaddaSaxlaAsync();
        }

        public async Task<PaylasimListDto?> DetailGetirAsync(int id)
        {
            var m = await _uow.Repository<Melumat>().Query().AsNoTracking()
                .Include(x => x.GonderenAppUser)
                .Include(x => x.Kateqoriya)
                .FirstOrDefaultAsync(x => x.Id == id);

            return m == null ? null : Map(m);
        }

        public async Task<Result<int>> YaratAsync(PaylasimYaratDto dto, int appUserId)
        {
            var xeta = Yoxla(dto);
            if (xeta != null) return Result<int>.Fail(xeta);

            var melumat = new Melumat
            {
                Basliq = dto.Basliq.Trim(),
                MetnHtml = _sanitizer.Sanitize(dto.MetnHtml),
                GonderenAppUserId = appUserId,
                KateqoriyaId = dto.KateqoriyaId,
                Qaralamadir = dto.QaralamaOlaraqSaxla,
                Aktivdir = !dto.QaralamaOlaraqSaxla
            };

            await _uow.Repository<Melumat>().YaratAsync(melumat);
            await _uow.YaddaSaxlaAsync(); // melumat.Id lazımdır — MelumatHedef FK-sı üçün

            await HedefleriTetbiqEtAsync(melumat.Id, dto, appUserId);
            await _uow.YaddaSaxlaAsync();

            return Result<int>.Ok(melumat.Id, "Paylaşım yaradıldı.");
        }

        public async Task<Result> YenileAsync(int id, PaylasimYaratDto dto, int appUserId)
        {
            var xeta = Yoxla(dto);
            if (xeta != null) return Result.Fail(xeta);

            var melumat = await _uow.Repository<Melumat>().IdIleGetirAsync(id);
            if (melumat == null) return Result.Fail("Paylaşım tapılmadı.");

            melumat.Basliq = dto.Basliq.Trim();
            melumat.MetnHtml = _sanitizer.Sanitize(dto.MetnHtml);
            melumat.KateqoriyaId = dto.KateqoriyaId;

            // Yalnız HƏLƏ QARALAMA olan post Paylaş/Qaralama saxla ilə vəziyyət dəyişə bilər —
            // artıq bir dəfə paylaşılmış (Qaralamadir=false) postun Aktivdir-i YALNIZ
            // Göstər/Gizlət düyməsi ilə idarə olunur, redaktə buna toxunmur.
            if (melumat.Qaralamadir)
            {
                melumat.Qaralamadir = dto.QaralamaOlaraqSaxla;
                if (!dto.QaralamaOlaraqSaxla) melumat.Aktivdir = true;
            }

            await _uow.Repository<Melumat>().YenileAsync(melumat);

            await HedefleriTetbiqEtAsync(id, dto, appUserId);
            await _uow.YaddaSaxlaAsync();

            return Result.Ok("Paylaşım yeniləndi.");
        }

        public async Task<Result> AktivlikDeyisAsync(int id)
        {
            var melumat = await _uow.Repository<Melumat>().IdIleGetirAsync(id);
            if (melumat == null) return Result.Fail("Paylaşım tapılmadı.");

            melumat.Aktivdir = !melumat.Aktivdir;
            await _uow.Repository<Melumat>().YenileAsync(melumat);
            await _uow.YaddaSaxlaAsync();

            return Result.Ok(melumat.Aktivdir ? "Lentdə göstərilir." : "Lentdən gizlədildi.");
        }

        public async Task<Result> SilAsync(int id)
        {
            var melumat = await _uow.Repository<Melumat>().IdIleGetirAsync(id);
            if (melumat == null) return Result.Fail("Paylaşım tapılmadı.");

            await _uow.Repository<Melumat>().YumshakSilAsync(id);
            await _uow.YaddaSaxlaAsync();

            return Result.Ok("Paylaşım silindi.");
        }

        public async Task<List<PaylasimIsciSecimDto>> AktivIscilerAsync()
        {
            // "İşçi Siyahıları — Sıralama və Filtr Qaydası" (CLAUDE.md): Sira/Ad/Soyad,
            // Status==Aktiv (Query() onsuz da !Silinib tətbiq edir).
            return await _uow.Repository<Isci>().Query()
                .Where(x => x.Status == IsciStatus.Aktiv)
                .OrderBy(x => x.Sira).ThenBy(x => x.Ad).ThenBy(x => x.Soyad)
                .Select(x => new PaylasimIsciSecimDto { Id = x.Id, Ad = x.Ad, Soyad = x.Soyad })
                .ToListAsync();
        }

        public async Task<List<int>> HedefIsciIdleriAsync(int melumatId)
        {
            return await _uow.Repository<MelumatHedef>().Query().AsNoTracking()
                .Where(x => x.MelumatId == melumatId)
                .Select(x => x.IsciId)
                .ToListAsync();
        }

        public async Task<List<int>> SonHedefKitlesiAsync(int adminAppUserId)
        {
            var setir = await _uow.Repository<PaylasimSonHedefKitlesi>().Query().AsNoTracking()
                .FirstOrDefaultAsync(x => x.AdminAppUserId == adminAppUserId);

            return ParseCsv(setir?.IsciIdlerCsv);
        }

        public async Task<List<PaylasimKateqoriyaDto>> AktivKateqoriyalarAsync()
        {
            return await _uow.Repository<PaylasimKateqoriyasi>().Query()
                .Where(x => x.Aktivdir)
                .OrderBy(x => x.Ad)
                .Select(x => new PaylasimKateqoriyaDto { Id = x.Id, Ad = x.Ad })
                .ToListAsync();
        }

        public async Task<Result<PaylasimKateqoriyaDto>> KateqoriyaYaratAsync(string ad)
        {
            var temiz = (ad ?? "").Trim();
            if (string.IsNullOrEmpty(temiz))
                return Result<PaylasimKateqoriyaDto>.Fail("Kateqoriya adı boş ola bilməz.");
            if (temiz.Length > 150)
                return Result<PaylasimKateqoriyaDto>.Fail("Kateqoriya adı 150 simvoldan uzun ola bilməz.");

            // Eyni adda aktiv kateqoriya varsa dublikat yaratmır, mövcudu qaytarır.
            var movcud = await _uow.Repository<PaylasimKateqoriyasi>().Query()
                .FirstOrDefaultAsync(x => x.Aktivdir && x.Ad == temiz);
            if (movcud != null)
                return Result<PaylasimKateqoriyaDto>.Ok(new PaylasimKateqoriyaDto { Id = movcud.Id, Ad = movcud.Ad });

            var yeni = new PaylasimKateqoriyasi { Ad = temiz, Aktivdir = true };
            await _uow.Repository<PaylasimKateqoriyasi>().YaratAsync(yeni);
            await _uow.YaddaSaxlaAsync();

            return Result<PaylasimKateqoriyaDto>.Ok(new PaylasimKateqoriyaDto { Id = yeni.Id, Ad = yeni.Ad });
        }

        public async Task<Result> PaylaAsync(int id)
        {
            var melumat = await _uow.Repository<Melumat>().IdIleGetirAsync(id);
            if (melumat == null) return Result.Fail("Paylaşım tapılmadı.");
            if (!melumat.Qaralamadir) return Result.Fail("Bu paylaşım artıq dərc olunub.");

            melumat.Qaralamadir = false;
            melumat.Aktivdir = true;
            await _uow.Repository<Melumat>().YenileAsync(melumat);
            await _uow.YaddaSaxlaAsync();

            return Result.Ok("Paylaşım dərc edildi.");
        }

        /// <summary>
        /// Köhnə hədəf sətirlərini təmizləyib (YenileAsync-də təkrarlandıqda köhnələr
        /// qalmasın) yenilərini yazır; "Hamısına" seçilibsə heç bir sətir qalmır. Seçilmiş
        /// işçilər varsa, həm də "son seçilmişlər" keşini bu admin üçün yeniləyir.
        /// </summary>
        private async Task HedefleriTetbiqEtAsync(int melumatId, PaylasimYaratDto dto, int appUserId)
        {
            var movcudHedefler = await _uow.Repository<MelumatHedef>().Query()
                .Where(x => x.MelumatId == melumatId)
                .ToListAsync();
            foreach (var h in movcudHedefler)
                await _uow.Repository<MelumatHedef>().YumshakSilAsync(h.Id);

            if (dto.HamisinaGonder) return;

            var secililer = (dto.SeciliIsciIdler ?? new List<int>()).Distinct().ToList();
            foreach (var isciId in secililer)
                await _uow.Repository<MelumatHedef>().YaratAsync(new MelumatHedef
                {
                    MelumatId = melumatId,
                    IsciId = isciId
                });

            await SonHedefKitlesiniSaxlaAsync(appUserId, secililer);
        }

        private async Task SonHedefKitlesiniSaxlaAsync(int adminAppUserId, List<int> isciIdler)
        {
            var csv = string.Join(",", isciIdler);
            var movcud = await _uow.Repository<PaylasimSonHedefKitlesi>().Query()
                .FirstOrDefaultAsync(x => x.AdminAppUserId == adminAppUserId);

            if (movcud != null)
            {
                movcud.IsciIdlerCsv = csv;
                await _uow.Repository<PaylasimSonHedefKitlesi>().YenileAsync(movcud);
            }
            else
            {
                await _uow.Repository<PaylasimSonHedefKitlesi>().YaratAsync(new PaylasimSonHedefKitlesi
                {
                    AdminAppUserId = adminAppUserId,
                    IsciIdlerCsv = csv
                });
            }
        }

        private static List<int> ParseCsv(string? csv)
        {
            if (string.IsNullOrWhiteSpace(csv)) return new List<int>();
            return csv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => int.TryParse(x, out var n) ? n : (int?)null)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .ToList();
        }

        private static string? Yoxla(PaylasimYaratDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Basliq))
                return "Başlıq boş ola bilməz.";
            if (dto.Basliq.Trim().Length > 300)
                return "Başlıq 300 simvoldan uzun ola bilməz.";
            if (string.IsNullOrWhiteSpace(dto.MetnHtml) || dto.MetnHtml.Trim() == "<p><br></p>")
                return "Məzmun boş ola bilməz.";
            if (!dto.HamisinaGonder && (dto.SeciliIsciIdler == null || dto.SeciliIsciIdler.Count == 0))
                return "Ən azı bir işçi seçin, yaxud \"Hamısına göndər\" seçimini işarələyin.";
            return null;
        }

        private static PaylasimListDto Map(Melumat m, int hedefSayi = 0) => new()
        {
            Id = m.Id,
            Basliq = m.Basliq,
            MetnHtml = m.MetnHtml,
            GonderenAdi = $"{m.GonderenAppUser.Ad} {m.GonderenAppUser.Soyad}",
            YaradilmaTarixi = m.YaradilmaTarixi,
            Aktivdir = m.Aktivdir,
            HedefSayi = hedefSayi,
            KateqoriyaId = m.KateqoriyaId,
            KateqoriyaAdi = m.Kateqoriya?.Ad,
            Qaralamadir = m.Qaralamadir
        };
    }
}
