using FinNex.Application.DTOs.HR.Ezamiyyet;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.HR
{
    public class EzamiyyetService : IEzamiyyetService
    {
        private readonly IUnitOfWork _uow;

        public EzamiyyetService(IUnitOfWork uow) => _uow = uow;

        // ── Siyahı əməliyyatları ─────────────────────────────

        public async Task<IList<EzamiyyetMuracietListDto>> IsciMuracietleriAsync(int isciId)
        {
            var list = await _uow.Repository<EzamiyyetMuraciet>()
                .HamisiniGetirAsync(
                    x => x.IsciId == isciId && !x.Silinib,
                    include: q => q.Include(x => x.Mekan)
                                   .Include(x => x.Rehber),
                    izlemeden: true);
            return list.OrderByDescending(x => x.BaslamaTarixi).Select(Map).ToList();
        }

        public async Task<IList<EzamiyyetMuracietListDto>> HamisiniGetirAsync(EzamiyyetFiltrDto? filtr = null)
        {
            var query = _uow.Repository<EzamiyyetMuraciet>()
                .Query()
                .AsNoTracking()
                .Where(x => !x.Silinib)
                .Include(x => x.Isci)
                    .ThenInclude(i => i.IsciTeyinatlari.Where(t => !t.Silinib))
                    .ThenInclude(t => t.Departament)
                .Include(x => x.Mekan)
                .Include(x => x.Rehber)
                .AsQueryable();

            if (filtr != null)
            {
                if (filtr.IsciId.HasValue)
                    query = query.Where(x => x.IsciId == filtr.IsciId.Value);
                if (filtr.MekanId.HasValue)
                    query = query.Where(x => x.MekanId == filtr.MekanId.Value);
                if (filtr.Status.HasValue)
                    query = query.Where(x => x.Status == filtr.Status.Value);
                if (filtr.BaslangicTarix.HasValue)
                    query = query.Where(x => x.BaslamaTarixi.Date >= filtr.BaslangicTarix.Value.Date);
                if (filtr.SonTarix.HasValue)
                    query = query.Where(x => x.BitmeTarixi.Date <= filtr.SonTarix.Value.Date);
                if (filtr.DepartamentId.HasValue)
                    query = query.Where(x => x.Isci.IsciTeyinatlari
                        .Any(t => !t.Silinib && t.DepartamentId == filtr.DepartamentId.Value));
            }

            var list = await query.OrderByDescending(x => x.BaslamaTarixi).ToListAsync();
            return list.Select(Map).ToList();
        }

        public async Task<IList<EzamiyyetMuracietListDto>> GozleyenlerAsync()
        {
            var list = await _uow.Repository<EzamiyyetMuraciet>()
                .HamisiniGetirAsync(
                    x => !x.Silinib && x.Status == EzamiyyetStatus.Gozleyir,
                    include: q => q.Include(x => x.Isci)
                                   .Include(x => x.Mekan),
                    izlemeden: true);
            return list.OrderBy(x => x.BaslamaTarixi).Select(Map).ToList();
        }

        public async Task<EzamiyyetMuracietListDto?> DetayAsync(int id)
        {
            var entity = await _uow.Repository<EzamiyyetMuraciet>()
                .Query()
                .AsNoTracking()
                .Include(x => x.Isci)
                .Include(x => x.Mekan)
                .Include(x => x.Rehber)
                .FirstOrDefaultAsync(x => x.Id == id && !x.Silinib);
            return entity == null ? null : Map(entity);
        }

        // ── Yarat ────────────────────────────────────────────

        public async Task<(bool ok, string? error, int id)> YaratAsync(
            int isciId,
            EzamiyyetMuracietCreateDto dto,
            string dmsRoot)
        {
            if (string.IsNullOrWhiteSpace(dto.Baslig))
                return (false, "Başlıq boş ola bilməz.", 0);
            if (dto.BaslamaTarixi.Date > dto.BitmeTarixi.Date)
                return (false, "Başlama tarixi bitmə tarixindən böyük ola bilməz.", 0);

            int mekanId;
            if (dto.MekanId.HasValue && dto.MekanId.Value > 0)
            {
                mekanId = dto.MekanId.Value;
            }
            else if (!string.IsNullOrWhiteSpace(dto.YeniMekanAd))
            {
                var yeni = await YeniMekanYaratAsync(dto.YeniMekanAd.Trim());
                mekanId = yeni!.Id;
            }
            else
            {
                return (false, "Məkan seçin və ya yeni məkan adı daxil edin.", 0);
            }

            // Sənəd
            string? senedYolu = null, senedAd = null;
            if (dto.Sened != null && dto.Sened.Length > 0)
            {
                var dir = Path.Combine(dmsRoot, "ezamiyyet");
                Directory.CreateDirectory(dir);
                var ext = Path.GetExtension(dto.Sened.FileName);
                var fn = $"{Guid.NewGuid()}{ext}";
                await using var fs = new FileStream(Path.Combine(dir, fn), FileMode.Create);
                await dto.Sened.CopyToAsync(fs);
                senedYolu = $"ezamiyyet/{fn}";
                senedAd   = dto.Sened.FileName;
            }

            // Saat parse
            TimeSpan? baslamaSaati = ParseSaat(dto.BaslamaSaati);
            TimeSpan? bitisSaati   = ParseSaat(dto.BitisSaati);

            var entity = new EzamiyyetMuraciet
            {
                IsciId        = isciId,
                Baslig        = dto.Baslig.Trim(),
                MekanId       = mekanId,
                BaslamaTarixi = dto.BaslamaTarixi.Date,
                BitmeTarixi   = dto.BitmeTarixi.Date,
                BaslamaSaati  = baslamaSaati,
                BitisSaati    = bitisSaati,
                SenedYolu     = senedYolu,
                SenedAd       = senedAd,
                Qeyd          = dto.Qeyd?.Trim(),
                Status        = EzamiyyetStatus.Gozleyir
            };

            await _uow.Repository<EzamiyyetMuraciet>().YaratAsync(entity);
            await _uow.YaddaSaxlaAsync();
            return (true, null, entity.Id);
        }

        // ── Rəhbər təsdiq ────────────────────────────────────

        public async Task<(bool ok, string? error)> RehberTesdiqAsync(
            int id, bool tesdiq, string? qeyd, int rehberId)
        {
            var entity = await _uow.Repository<EzamiyyetMuraciet>()
                .Query()
                .FirstOrDefaultAsync(x => x.Id == id && !x.Silinib);
            if (entity == null)
                return (false, "Müraciət tapılmadı.");
            if (entity.Status != EzamiyyetStatus.Gozleyir)
                return (false, "Müraciət artıq cavablanıb.");

            entity.RehberTesdiq       = tesdiq;
            entity.RehberId           = rehberId;
            entity.RehberTesdiqTarixi = DateTime.Now;
            entity.RehberQeydi        = qeyd;
            entity.Status             = tesdiq ? EzamiyyetStatus.Tesdiqlendi : EzamiyyetStatus.Reddedildi;

            await _uow.Repository<EzamiyyetMuraciet>().YenileAsync(entity);
            await _uow.YaddaSaxlaAsync();
            return (true, null);
        }

        // ── Ləğv et ──────────────────────────────────────────

        public async Task<(bool ok, string? error)> LegvEtAsync(int id, int isciId)
        {
            var entity = await _uow.Repository<EzamiyyetMuraciet>()
                .Query()
                .FirstOrDefaultAsync(x => x.Id == id && x.IsciId == isciId && !x.Silinib);
            if (entity == null)
                return (false, "Müraciət tapılmadı.");
            if (entity.Status == EzamiyyetStatus.Tesdiqlendi &&
                entity.BaslamaTarixi.Date <= DateTime.Today)
                return (false, "Başlanmış ezamiyyəti ləğv etmək mümkün deyil.");

            entity.Status  = EzamiyyetStatus.Legvedildi;
            entity.Silinib = true;
            entity.SilinmeTarixi = DateTime.Now;
            await _uow.Repository<EzamiyyetMuraciet>().YenileAsync(entity);
            await _uow.YaddaSaxlaAsync();
            return (true, null);
        }

        // ── Məkan ─────────────────────────────────────────────

        public async Task<IList<EzamiyyetMekanListDto>> MekanlarAsync()
        {
            var mekanlar = await _uow.Repository<EzamiyyetMekan>()
                .Query()
                .AsNoTracking()
                .Where(x => !x.Silinib && x.Aktiv)
                .Select(x => new EzamiyyetMekanListDto
                {
                    Id   = x.Id,
                    Ad   = x.Ad,
                    Aktiv = x.Aktiv,
                    Sayi = x.Muracietler.Count(m => !m.Silinib)
                })
                .OrderBy(x => x.Ad)
                .ToListAsync();
            return mekanlar;
        }

        public async Task<EzamiyyetMekan?> YeniMekanYaratAsync(string ad)
        {
            var movcud = await _uow.Repository<EzamiyyetMekan>()
                .Query()
                .FirstOrDefaultAsync(x => x.Ad.ToLower() == ad.ToLower() && !x.Silinib);
            if (movcud != null) return movcud;

            var yeni = new EzamiyyetMekan { Ad = ad, Aktiv = true };
            await _uow.Repository<EzamiyyetMekan>().YaratAsync(yeni);
            await _uow.YaddaSaxlaAsync();
            return yeni;
        }

        // ── ADMS inteqrasiyası ────────────────────────────────

        public async Task<EzamiyyetMuraciet?> BugunAktivEzamiyyetAsync(int isciId, DateTime tarix)
        {
            return await _uow.Repository<EzamiyyetMuraciet>()
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.IsciId == isciId &&
                    !x.Silinib &&
                    x.Status == EzamiyyetStatus.Tesdiqlendi &&
                    x.BaslamaTarixi.Date <= tarix.Date &&
                    x.BitmeTarixi.Date   >= tarix.Date);
        }

        // ── Statistika ────────────────────────────────────────

        public async Task<IList<EzamiyyetStatistikDto>> StatistikaAsync(EzamiyyetFiltrDto filtr)
        {
            var query = _uow.Repository<EzamiyyetMuraciet>()
                .Query()
                .AsNoTracking()
                .Where(x => !x.Silinib)
                .Include(x => x.Isci)
                    .ThenInclude(i => i.IsciTeyinatlari.Where(t => !t.Silinib))
                    .ThenInclude(t => t.Departament)
                .Include(x => x.Mekan)
                .AsQueryable();

            if (filtr.BaslangicTarix.HasValue)
                query = query.Where(x => x.BaslamaTarixi.Date >= filtr.BaslangicTarix.Value.Date);
            if (filtr.SonTarix.HasValue)
                query = query.Where(x => x.BitmeTarixi.Date <= filtr.SonTarix.Value.Date);
            if (filtr.DepartamentId.HasValue)
                query = query.Where(x => x.Isci.IsciTeyinatlari
                    .Any(t => !t.Silinib && t.DepartamentId == filtr.DepartamentId.Value));

            var list = await query.ToListAsync();

            return list
                .GroupBy(x => x.IsciId)
                .Select(g =>
                {
                    var isci = g.First().Isci;
                    var dep  = isci.IsciTeyinatlari.FirstOrDefault()?.Departament?.Ad;
                    var enCoxMekan = g
                        .GroupBy(x => x.Mekan.Ad)
                        .OrderByDescending(mg => mg.Count())
                        .Select(mg => mg.Key)
                        .FirstOrDefault();

                    return new EzamiyyetStatistikDto
                    {
                        IsciId       = g.Key,
                        IsciTamAd    = isci.TamAd,
                        Departament  = dep,
                        CemiMuraciet = g.Count(),
                        Tesdiqlendi  = g.Count(x => x.Status == EzamiyyetStatus.Tesdiqlendi),
                        Reddedildi   = g.Count(x => x.Status == EzamiyyetStatus.Reddedildi),
                        Gozleyir     = g.Count(x => x.Status == EzamiyyetStatus.Gozleyir),
                        CemiGun      = g.Where(x => x.Status == EzamiyyetStatus.Tesdiqlendi)
                                        .Sum(x => (x.BitmeTarixi.Date - x.BaslamaTarixi.Date).Days + 1),
                        EnCoxMekan   = enCoxMekan
                    };
                })
                .OrderByDescending(x => x.CemiGun)
                .ToList();
        }

        // ── Helpers ───────────────────────────────────────────

        private static TimeSpan? ParseSaat(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            return TimeSpan.TryParse(s, out var ts) ? ts : null;
        }

        private static EzamiyyetMuracietListDto Map(EzamiyyetMuraciet x)
        {
            var teyinat = x.Isci?.IsciTeyinatlari?.FirstOrDefault(t => !t.Silinib);
            return new EzamiyyetMuracietListDto
            {
                Id                  = x.Id,
                IsciId              = x.IsciId,
                IsciTamAd           = x.Isci?.TamAd ?? "",
                IsciVezife          = teyinat?.Vezife?.Ad,
                Baslig              = x.Baslig,
                MekanId             = x.MekanId,
                MekanAd             = x.Mekan?.Ad ?? "",
                BaslamaTarixi       = x.BaslamaTarixi,
                BitmeTarixi         = x.BitmeTarixi,
                BaslamaSaati        = x.BaslamaSaati,
                BitisSaati          = x.BitisSaati,
                SenedYolu           = x.SenedYolu,
                SenedAd             = x.SenedAd,
                Qeyd                = x.Qeyd,
                Status              = x.Status,
                RehberTesdiq        = x.RehberTesdiq,
                RehberTamAd         = x.Rehber?.TamAd,
                RehberTesdiqTarixi  = x.RehberTesdiqTarixi,
                RehberQeydi         = x.RehberQeydi,
                YaradilmaTarixi     = x.YaradilmaTarixi
            };
        }
    }
}
