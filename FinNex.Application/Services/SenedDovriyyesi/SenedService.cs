using AutoMapper;
using FinNex.Application.Common.Paged;
using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.SenedDovriyyesi;
using FinNex.Application.DTOs.SenedDovriyyesi.Fayl;
using FinNex.Application.DTOs.SenedDovriyyesi.Sened;
using FinNex.Application.Interfaces.SenedDovriyyesi;
using FinNex.Domain.Entities.SenedDovriyyesi;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FinNex.Application.Services.SenedDovriyyesi
{
    public class SenedService : ISenedService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IFileStorageService _storage;
        private readonly IAuditLogService _audit;

        public SenedService(IUnitOfWork uow, IMapper mapper, IFileStorageService storage, IAuditLogService audit)
        {
            _uow = uow;
            _mapper = mapper;
            _storage = storage;
            _audit = audit;
        }

        public async Task<Result<int>> CreateAsync(SenedCreateDto dto, int userId, string? ip)
        {
            // Validations (biznes)
            if (string.IsNullOrWhiteSpace(dto.AcarSoz))
                return Result<int>.Fail("Açar söz boş ola bilməz.");

            // var olsa: Sobe, SenedNovu yoxla
            var sobe = await _uow.Repository<Sobe>().GetirAsync(x => x.Id == dto.SobeId && !x.Silinib);
            if (sobe is null) return Result<int>.Fail("Şöbə tapılmadı.");

            var nov = await _uow.Repository<SenedNovu>().GetirAsync(x => x.Id == dto.SenedNovuId && !x.Silinib);
            if (nov is null) return Result<int>.Fail("Sənəd növü tapılmadı.");

            var sened = new Sened
            {
                SobeId = dto.SobeId,
                SenedNovuId = dto.SenedNovuId,
                Basliq = dto.Basliq.Trim(),
                AcarSoz = dto.AcarSoz.Trim(),
                Status = SenedStatusu.Yeni,
                YaradanIcraciId = userId
            };

            await _uow.Repository<Sened>().YaratAsync(sened);
            await _uow.YaddaSaxlaAsync(); // burada Id yaranır

            // Tag maps
            if (dto.TagIds?.Count > 0)
            {
                foreach (var tagId in dto.TagIds.Distinct())
                {
                    var map = new SenedTagMap { SenedId = sened.Id, TagId = tagId };
                    await _uow.Repository<SenedTagMap>().YaratAsync(map);
                }
                await _uow.YaddaSaxlaAsync();
            }

            await _audit.WriteAsync(userId, "Create", sened.Id, ip, new { dto.SobeId, dto.SenedNovuId, dto.AcarSoz });

            return Result<int>.Ok(sened.Id, "Sənəd yaradıldı.");
        }

        public async Task<Result<PagedResult<SenedListDto>>> GetPagedAsync(
            PagedRequest req, int? sobeId, int? senedNovuId, SenedStatusu? status, string? search)
        {
            var query = _uow.Repository<Sened>().Query();

            query = query
                .Where(x => !x.Silinib)
                .Include(x => x.Sobe)
                .Include(x => x.SenedNovu)
                .Include(x => x.Fayllar);


            if (sobeId.HasValue) query = query.Where(x => x.SobeId == sobeId);
            if (senedNovuId.HasValue) query = query.Where(x => x.SenedNovuId == senedNovuId);
            if (status.HasValue) query = query.Where(x => x.Status == status);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(x => x.AcarSoz.Contains(s) || x.Basliq.Contains(s));
            }

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.YaradilmaTarixi)
                .Skip((req.Page - 1) * req.PageSize)
                .Take(req.PageSize)
                .ToListAsync();

            var dto = _mapper.Map<List<SenedListDto>>(items);

            return Result<PagedResult<SenedListDto>>.Ok(new PagedResult<SenedListDto>
            {
                Items = dto,
                TotalCount = total,
                Page = req.Page,
                PageSize = req.PageSize
            });
        }

        public async Task<Result<PagedResult<SenedListDto>>> GetDeletedPagedAsync(PagedRequest req)
        {
            var query = _uow.Repository<Sened>().Query()
                .IgnoreQueryFilters()
                .Where(x => x.Silinib)
                .Include(x => x.Sobe)
                .Include(x => x.SenedNovu)
                .Include(x => x.Fayllar);

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.SilinmeTarixi)
                .Skip((req.Page - 1) * req.PageSize)
                .Take(req.PageSize)
                .ToListAsync();

            var dto = _mapper.Map<List<SenedListDto>>(items);

            return Result<PagedResult<SenedListDto>>.Ok(new PagedResult<SenedListDto>
            {
                Items = dto,
                TotalCount = total,
                Page = req.Page,
                PageSize = req.PageSize
            });
        }

        public async Task<Result<SenedDetailDto>> GetDetailAsync(int senedId)
        {
            var sened = await _uow.Repository<Sened>().Query()
                .Include(x => x.Sobe)
                .Include(x => x.SenedNovu)
                .Include(x => x.Fayllar)
                .Include(x => x.SenedTagMaps).ThenInclude(m => m.Tag)
                .FirstOrDefaultAsync(x => x.Id == senedId && !x.Silinib);

            if (sened is null) return Result<SenedDetailDto>.Fail("Sənəd tapılmadı.");

            return Result<SenedDetailDto>.Ok(_mapper.Map<SenedDetailDto>(sened));
        }

        public async Task<Result<SenedDashboardDto>> GetDashboardAsync()
        {
            var query = _uow.Repository<Sened>().Query().Where(x => !x.Silinib);

            var umumi = await query.CountAsync();
            var yeni = await query.CountAsync(x => x.Status == SenedStatusu.Yeni);
            var yoxlanilir = await query.CountAsync(x => x.Status == SenedStatusu.Yoxlanilir);
            var tesdiq = await query.CountAsync(x => x.Status == SenedStatusu.Tesdiq);
            var arxiv = await query.CountAsync(x => x.Status == SenedStatusu.Arxiv);

            var sonSenedler = await query
                .Include(x => x.Sobe)
                .Include(x => x.SenedNovu)
                .Include(x => x.Fayllar)
                .OrderByDescending(x => x.YaradilmaTarixi)
                .Take(10)
                .ToListAsync();

            var dto = new SenedDashboardDto
            {
                UmumiSenedler = umumi,
                YeniSenedler = yeni,
                YoxlanilirSenedler = yoxlanilir,
                TesdiqSenedler = tesdiq,
                ArxivSenedler = arxiv,
                SonSenedler = _mapper.Map<List<SenedListDto>>(sonSenedler)
            };

            return Result<SenedDashboardDto>.Ok(dto);
        }

        public async Task<Result<int>> UploadNewVersionAsync(SenedFaylUploadDto dto, int userId, string? ip)
        {
            var sened = await _uow.Repository<Sened>().GetirAsync(x => x.Id == dto.SenedId && !x.Silinib);
            if (sened is null) return Result<int>.Fail("Sənəd tapılmadı.");

            // Aktiv versiyanı söndür
            var aktivler = await _uow.Repository<SenedFayl>().HamisiniGetirAsync(
                x => x.SenedId == dto.SenedId && x.AktivVersiya && !x.Silinib);

            foreach (var f in aktivler)
            {
                f.AktivVersiya = false;
                f.YenileyenIcraciId = userId;
                f.YenilenmeTarixi = DateTime.Now;
                await _uow.Repository<SenedFayl>().YenileAsync(f);
            }

            // versiya no
            var maxVersiya = await _uow.Repository<SenedFayl>().Query()
                .Where(x => x.SenedId == dto.SenedId && !x.Silinib)
                .Select(x => (int?)x.VersiyaNo)
                .MaxAsync() ?? 0;

            var nextVersiya = maxVersiya + 1;

            // storage
            var (storedName, path, sha256) = await _storage.SaveAsync(dto.Stream, dto.OriginalAd, dto.ContentType);

            var entity = new SenedFayl
            {
                SenedId = dto.SenedId,
                VersiyaNo = nextVersiya,
                OriginalAd = dto.OriginalAd,
                StoredAd = storedName,
                ContentType = dto.ContentType,
                OlcuBytes = dto.OlcuBytes,
                Sha256 = sha256,
                Yol = path,
                AktivVersiya = true,
                YaradanIcraciId = userId
            };

            await _uow.Repository<SenedFayl>().YaratAsync(entity);
            await _uow.YaddaSaxlaAsync();

            await _audit.WriteAsync(userId, "Upload", dto.SenedId, ip, new { nextVersiya, dto.OriginalAd, dto.OlcuBytes });

            return Result<int>.Ok(entity.Id, "Fayl yükləndi (yeni versiya).");
        }

        public async Task<Result> UpdateStatusAsync(int senedId, SenedStatusu status, int userId, string? ip)
        {
            var sened = await _uow.Repository<Sened>().GetirAsync(x => x.Id == senedId && !x.Silinib);
            if (sened is null) return Result.Fail("Sənəd tapılmadı.");

            sened.Status = status;
            sened.YenileyenIcraciId = userId;
            sened.YenilenmeTarixi = DateTime.Now;

            await _uow.Repository<Sened>().YenileAsync(sened);
            await _uow.YaddaSaxlaAsync();

            await _audit.WriteAsync(userId, "Status", senedId, ip, new { status });

            return Result.Ok("Status yeniləndi.");
        }

        public async Task<Result> SoftDeleteAsync(int senedId, int userId, string? ip)
        {
            var sened = await _uow.Repository<Sened>().GetirAsync(x => x.Id == senedId && !x.Silinib);
            if (sened is null) return Result.Fail("Sənəd tapılmadı.");

            sened.Silinib = true;
            sened.SilenIcraciId = userId;
            sened.SilinmeTarixi = DateTime.Now;

            await _uow.Repository<Sened>().YenileAsync(sened);
            await _uow.YaddaSaxlaAsync();

            await _audit.WriteAsync(userId, "SoftDelete", senedId, ip);

            return Result.Ok("Sənəd silindi (soft delete).");
        }

        public async Task<Result> RestoreAsync(int senedId, int userId, string? ip)
        {
            var sened = await _uow.Repository<Sened>().GetirAsync(x => x.Id == senedId && x.Silinib);
            if (sened is null) return Result.Fail("Bərpa ediləcək sənəd tapılmadı.");

            sened.Silinib = false;
            sened.SilenIcraciId = null;
            sened.SilinmeTarixi = null;
            sened.YenileyenIcraciId = userId;
            sened.YenilenmeTarixi = DateTime.Now;

            await _uow.Repository<Sened>().YenileAsync(sened);
            await _uow.YaddaSaxlaAsync();

            await _audit.WriteAsync(userId, "Restore", senedId, ip);

            return Result.Ok("Sənəd bərpa edildi.");
        }

        public async Task<Result<SenedFayl>> GetFileEntityAsync(int faylId)
        {
            var fayl = await _uow.Repository<SenedFayl>().GetirAsync(x => x.Id == faylId && !x.Silinib);
            if (fayl is null) return Result<SenedFayl>.Fail("Fayl tapılmadı.");
            return Result<SenedFayl>.Ok(fayl);
        }

    }
    public class AuditLogService : IAuditLogService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public AuditLogService(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task WriteAsync(int userId, string action, int? senedId, string? ip, object? details = null)
        {
            string? json = null;

            if (details != null)
            {
                json = JsonSerializer.Serialize(details);
            }

            var log = new AuditLog
            {
                UserId = userId,
                Action = action,
                SenedId = senedId,
                Ip = ip,
                DetailsJson = json,
                YaradanIcraciId = userId
            };

            await _uow.Repository<AuditLog>().YaratAsync(log);
            await _uow.YaddaSaxlaAsync();
        }

        public async Task<List<AuditLogDto>> GetBySenedIdAsync(int senedId)
        {
            var logs = await _uow.Repository<AuditLog>().Query()
                .Where(x => x.SenedId == senedId && !x.Silinib)
                .OrderByDescending(x => x.YaradilmaTarixi)
                .ToListAsync();

            return _mapper.Map<List<AuditLogDto>>(logs);
        }

    }
}
