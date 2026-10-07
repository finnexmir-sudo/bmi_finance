using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Imtiyaz;
using FinNex.Application.Interfaces.HR;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.HR
{
    /// <summary>
    /// İşçiyə HR tərəfindən verilmiş fərdi imtiyaz/istisna qeydləri — YALNIZ
    /// GÖSTƏRİŞDİR (bax interfeys sənədi), heç bir hesablamaya müdaxilə etmir.
    /// </summary>
    public class IsciImtiyazService : IIsciImtiyazService
    {
        private readonly IUnitOfWork _uow;

        public IsciImtiyazService(IUnitOfWork uow) => _uow = uow;

        private static IsciImtiyazDto Map(IsciImtiyaz x) => new()
        {
            Id = x.Id,
            IsciId = x.IsciId,
            Baslik = x.Baslik,
            HeftaGunu = x.HeftaGunu,
            BaslamaSaati = x.BaslamaSaati,
            BitisSaati = x.BitisSaati,
            Aciqlama = x.Aciqlama,
            BaslamaTarixi = x.BaslamaTarixi,
            BitmeTarixi = x.BitmeTarixi,
            Aktivdir = x.Aktivdir
        };

        public async Task<IList<IsciImtiyazDto>> IsciUzreHamisiniGetirAsync(int isciId)
        {
            var list = await _uow.Repository<IsciImtiyaz>().Query().AsNoTracking()
                .Where(x => x.IsciId == isciId)
                .OrderByDescending(x => x.Aktivdir)
                .ThenByDescending(x => x.BaslamaTarixi)
                .ToListAsync();

            return list.Select(Map).ToList();
        }

        public async Task<IList<IsciImtiyazDto>> AktivImtiyazlarAsync(int isciId)
        {
            var bugun = DateTime.Today;

            var list = await _uow.Repository<IsciImtiyaz>().Query().AsNoTracking()
                .Where(x => x.IsciId == isciId
                            && x.Aktivdir
                            && x.BaslamaTarixi <= bugun
                            && (x.BitmeTarixi == null || x.BitmeTarixi >= bugun))
                .OrderBy(x => x.HeftaGunu == null ? 0 : 1) // "hər gün" qeydlər əvvəl
                .ThenBy(x => x.BaslamaSaati)
                .ToListAsync();

            return list.Select(Map).ToList();
        }

        private static Result? Yoxla(IsciImtiyazCreateDto dto)
        {
            if (dto.IsciId <= 0) return Result.Fail("İşçi seçilməlidir.");
            if (string.IsNullOrWhiteSpace(dto.Baslik)) return Result.Fail("Başlıq mütləqdir.");
            if (dto.Baslik.Trim().Length > 200) return Result.Fail("Başlıq 200 simvoldan uzun ola bilməz.");
            if (dto.Aciqlama is { Length: > 1000 }) return Result.Fail("Açıqlama 1000 simvoldan uzun ola bilməz.");

            if (dto.BaslamaSaati.HasValue && dto.BitisSaati.HasValue
                && dto.BitisSaati.Value <= dto.BaslamaSaati.Value)
                return Result.Fail("Bitiş saatı başlanğıc saatından sonra olmalıdır.");

            if (dto.BitmeTarixi.HasValue && dto.BitmeTarixi.Value.Date < dto.BaslamaTarixi.Date)
                return Result.Fail("Bitmə tarixi başlama tarixindən əvvəl ola bilməz.");

            return null;
        }

        public async Task<Result<int>> YaratAsync(IsciImtiyazCreateDto dto, int userId)
        {
            var xeta = Yoxla(dto);
            if (xeta != null) return Result<int>.Fail(xeta.Message ?? "Məlumat yanlışdır.");

            var isciVar = await _uow.Repository<Isci>().Query().AsNoTracking()
                .AnyAsync(x => x.Id == dto.IsciId && !x.Silinib);
            if (!isciVar) return Result<int>.Fail("İşçi tapılmadı.");

            var e = new IsciImtiyaz
            {
                IsciId = dto.IsciId,
                Baslik = dto.Baslik.Trim(),
                HeftaGunu = dto.HeftaGunu,
                BaslamaSaati = dto.BaslamaSaati,
                BitisSaati = dto.BitisSaati,
                Aciqlama = dto.Aciqlama?.Trim(),
                BaslamaTarixi = dto.BaslamaTarixi.Date,
                BitmeTarixi = dto.BitmeTarixi?.Date,
                Aktivdir = true,
                YaradanIcraciId = userId
            };

            await _uow.Repository<IsciImtiyaz>().YaratAsync(e);
            await _uow.YaddaSaxlaAsync();

            return Result<int>.Ok(e.Id, "İmtiyaz əlavə edildi.");
        }

        public async Task<Result> YenileAsync(IsciImtiyazCreateDto dto, int userId)
        {
            var xeta = Yoxla(dto);
            if (xeta != null) return xeta;

            var e = await _uow.Repository<IsciImtiyaz>().GetirAsync(x => x.Id == dto.Id && !x.Silinib);
            if (e == null) return Result.Fail("İmtiyaz qeydi tapılmadı.");

            e.Baslik = dto.Baslik.Trim();
            e.HeftaGunu = dto.HeftaGunu;
            e.BaslamaSaati = dto.BaslamaSaati;
            e.BitisSaati = dto.BitisSaati;
            e.Aciqlama = dto.Aciqlama?.Trim();
            e.BaslamaTarixi = dto.BaslamaTarixi.Date;
            e.BitmeTarixi = dto.BitmeTarixi?.Date;
            e.YenileyenIcraciId = userId;
            e.YenilenmeTarixi = DateTime.Now;

            await _uow.Repository<IsciImtiyaz>().YenileAsync(e);
            await _uow.YaddaSaxlaAsync();
            return Result.Ok("İmtiyaz yeniləndi.");
        }

        public async Task<Result> AktivlikDeyisAsync(int id, bool aktivdir, int userId)
        {
            var e = await _uow.Repository<IsciImtiyaz>().GetirAsync(x => x.Id == id && !x.Silinib);
            if (e == null) return Result.Fail("İmtiyaz qeydi tapılmadı.");

            e.Aktivdir = aktivdir;
            e.YenileyenIcraciId = userId;
            e.YenilenmeTarixi = DateTime.Now;

            await _uow.Repository<IsciImtiyaz>().YenileAsync(e);
            await _uow.YaddaSaxlaAsync();
            return Result.Ok(aktivdir ? "Aktivləşdirildi." : "Deaktiv edildi.");
        }

        public async Task<Result> SilAsync(int id, int userId)
        {
            var e = await _uow.Repository<IsciImtiyaz>().GetirAsync(x => x.Id == id && !x.Silinib);
            if (e == null) return Result.Fail("İmtiyaz qeydi tapılmadı.");

            e.Silinib = true;
            e.SilinmeTarixi = DateTime.Now;
            e.SilenIcraciId = userId;

            await _uow.Repository<IsciImtiyaz>().YenileAsync(e);
            await _uow.YaddaSaxlaAsync();
            return Result.Ok("İmtiyaz silindi.");
        }
    }
}
