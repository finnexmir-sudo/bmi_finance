using AutoMapper;
using FinNex.Application.Common.Paged;
using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.SenedDovriyyesi;
using FinNex.Application.DTOs.SenedDovriyyesi.Fayl;
using FinNex.Application.DTOs.SenedDovriyyesi.Sened;
using FinNex.Application.DTOs.Structur;
using FinNex.Application.Interfaces;
using FinNex.Application.Interfaces.SenedDovriyyesi;
using FinNex.Application.Interfaces.Structur;
using FinNex.DataAccess.UnitOfWorks;
using FinNex.Domain.Entities.SenedDovriyyesi;
using FinNex.Domain.Entities.Structure;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client.Extensions.Msal;
using System.Security.Cryptography;

namespace FinNex.Application.Services.SenedDovriyyesi
{
    public class SenedService : ServiceAsync<Sened, SenedDetailDto, SenedCreateDto, SenedUpdateDto>, ISenedService
    {

        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IFileStorageService _storage;
        private readonly IAuditLogService _audit;
        private readonly IDepartmentService _departmentService;

        public SenedService(IUnitOfWork uow,
            IMapper mapper,
            IFileStorageService storage,
            IAuditLogService audit,
            IDepartmentService departmentService) : base(uow, mapper)
        {
            _uow = uow;
            _mapper = mapper;
            _storage = storage;
            _audit = audit;
            _departmentService = departmentService;

        }

        public async Task<Result<int>> CreateAsync(
    SenedCreateDto dto,
    SenedUploadDto uploadDto,
    int userId,
    string? ip)
        {
            // =========================
            // 1️⃣ Business Validations
            // =========================
            if (string.IsNullOrWhiteSpace(dto.Basliq))
                return Result<int>.Fail("Başlıq boş ola bilməz.");

            if (string.IsNullOrWhiteSpace(dto.AcarSoz))
                return Result<int>.Fail("Açar söz boş ola bilməz.");

            // Bir neçə fayl dəstəyi + köhnə tək-fayl yolu ilə geri uyğunluq
            var fayllar = new List<IFormFile>();
            if (uploadDto?.Fayllar != null && uploadDto.Fayllar.Count > 0)
                fayllar.AddRange(uploadDto.Fayllar.Where(f => f != null && f.Length > 0));
            else if (uploadDto?.Fayl != null && uploadDto.Fayl.Length > 0)
                fayllar.Add(uploadDto.Fayl);

            if (fayllar.Count == 0)
                return Result<int>.Fail("Ən azı bir fayl seçilməlidir.");

            var sobe = await _departmentService
                .HamisiniGetirAsync(x => x.Id == dto.SobeId && !x.Silinib);

            if (sobe is null)
                return Result<int>.Fail("Departament tapılmadı.");

            var nov = await _uow.Repository<SenedNovu>()
                .GetirAsync(x => x.Id == dto.SenedNovuId && !x.Silinib);

            if (nov is null)
                return Result<int>.Fail("Sənəd növü tapılmadı.");

            // =========================
            // 2️⃣ SENED CREATE
            // =========================

            // Sənəd nömrəsi yaradılır: {SenedNovu.Kod}-{İl}-{SıraNo:D3}
            var currentYear = DateTime.UtcNow.Year;
            var existingCount = await _uow.Repository<Sened>().Query()
                .CountAsync(x => x.SenedNovuId == dto.SenedNovuId
                    && x.YaradilmaTarixi.Year == currentYear);
            var senedNomresi = $"{nov.Kod}-{currentYear}-{(existingCount + 1):D3}";

            // SenedTarixi: istifadəçi boş saxlaya bilməz deyil — default bugün
            var senedTarixi = dto.SenedTarixi == default
                ? DateTime.Now.Date
                : dto.SenedTarixi.Date;

            var sened = new Sened
            {
                DepartmentId = dto.SobeId,
                SenedNovuId = dto.SenedNovuId,
                Basliq = dto.Basliq.Trim(),
                AcarSoz = dto.AcarSoz.Trim(),
                SenedNomresi = senedNomresi,
                SenedTarixi = senedTarixi,
                Status = SenedStatusu.Yeni,
                Mexfilik = MexfilikSeviyesi.Internal,
                YaradanIcraciId = userId,
                YaradilmaTarixi = DateTime.UtcNow
            };

            await _uow.Repository<Sened>().YaratAsync(sened);
            await _uow.YaddaSaxlaAsync(); // burada ID yaranır

            // =========================
            // 3️⃣ FILES SAVE (hər biri üçün ayrı versiya)
            // =========================
            var faylAdlari = new List<string>();
            var versiyaNo = 0;
            foreach (var formFile in fayllar)
            {
                versiyaNo++;

                using var stream = formFile.OpenReadStream();
                var (storedName, path, sha256) =
                    await _storage.SaveAsync(
                        stream,
                        formFile.FileName,
                        formFile.ContentType);

                var fayl = new SenedFayl
                {
                    SenedId = sened.Id,
                    VersiyaNo = versiyaNo,
                    OriginalAd = formFile.FileName,
                    ContentType = formFile.ContentType,
                    OlcuBytes = formFile.Length,
                    Sha256 = sha256,
                    // Yaradılışda hər fayl aktiv — istifadəçi birdən çox sənəd əlavə
                    // edəndə hamısı Detal səhifəsində görünsün deyə.
                    AktivVersiya = true,
                    Yol = path,
                    StoredAd = storedName,
                    YaradilmaTarixi = DateTime.UtcNow
                };

                await _uow.Repository<SenedFayl>().YaratAsync(fayl);
                faylAdlari.Add(formFile.FileName);
            }

            // =========================
            // 4️⃣ TAG MAP
            // =========================
            if (dto.TagIds?.Count > 0)
            {
                foreach (var tagId in dto.TagIds.Distinct())
                {
                    var map = new SenedTagMap
                    {
                        SenedId = sened.Id,
                        TagId = tagId
                    };

                    await _uow.Repository<SenedTagMap>().YaratAsync(map);
                }
            }

            // =========================
            // 5️⃣ CREATOR ACCESS
            // =========================
            var access = new SenedAccess
            {
                SenedId = sened.Id,
                PrincipalType = PrincipalType.User,
                PrincipalId = userId,
                Permission = AccessPermission.TamHuquq
            };

            await _uow.Repository<SenedAccess>().YaratAsync(access);

            // =========================
            // 6️⃣ AUDIT
            // =========================
            await _audit.WriteAsync(
                userId,
                "Create",
                sened.Id,
                ip,
                new
                {
                    action = "Create",
                    SobeId = dto.SobeId,
                    SenedNovuId = dto.SenedNovuId,
                    Basliq = dto.Basliq,
                    AcarSoz = dto.AcarSoz,
                    SenedTarixi = senedTarixi,
                    FaylAdlari = faylAdlari,
                    FaylSayi = faylAdlari.Count
                });

            // =========================
            // 7️⃣ SAVE ALL
            // =========================
            await _uow.YaddaSaxlaAsync();

            return Result<int>.Ok(sened.Id, "Sənəd yaradıldı.");
        }



        public async Task<Result<PagedResult<SenedListDto>>> GetPagedAsync(
    PagedRequest req,
    List<int> icazeliSobeIdleri,
    int? sobeId,
    int? senedNovuId,
    SenedStatusu? status,
    string? search,
    int? tagId = null,
    string? sortBy = null,
    string? sortDir = null)
        {
            var query = _uow.Repository<Sened>().Query();

            query = query
                 .Where(x => !x.Silinib )
                 .Include(x => x.Department)
                 .Include(x => x.SenedNovu)
                 .Include(x => x.Fayllar);

            query = query.Where(x => icazeliSobeIdleri.Contains(x.DepartmentId));

            if (sobeId.HasValue) query = query.Where(x => x.DepartmentId == sobeId);
            if (senedNovuId.HasValue) query = query.Where(x => x.SenedNovuId == senedNovuId);
            if (status.HasValue) query = query.Where(x => x.Status == status);
            if (tagId.HasValue)
                query = query.Where(x => x.SenedTagMaps.Any(m => m.TagId == tagId.Value && !m.Silinib));
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(x => x.AcarSoz.Contains(s) || x.Basliq.Contains(s));
            }

            var total = await query.CountAsync();

            // Sıralama: default = sənəd tarixi (desc)
            var desc = !string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);
            var key = (sortBy ?? "senedTarixi").ToLowerInvariant();

            query = key switch
            {
                "yaradilmatarixi" => desc
                    ? query.OrderByDescending(x => x.YaradilmaTarixi)
                    : query.OrderBy(x => x.YaradilmaTarixi),
                _ => desc
                    ? query.OrderByDescending(x => x.SenedTarixi).ThenByDescending(x => x.YaradilmaTarixi)
                    : query.OrderBy(x => x.SenedTarixi).ThenBy(x => x.YaradilmaTarixi)
            };

            var items = await query
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

        public async Task<Result<PagedResult<SenedListDto>>> GetSilinmisPagedAsync(
                PagedRequest req, List<int> icazeliSobeIdleri, int? sobeId, int? senedNovuId, SenedStatusu? status, string? search)
        {
            var query = _uow.Repository<Sened>().QueryDeleted();

            query = query
                 .Where(x => x.Silinib)
                 .Include(x => x.Department)
                 .Include(x => x.SenedNovu)
                 .Include(x => x.Fayllar);

            query = query.Where(x => icazeliSobeIdleri.Contains(x.DepartmentId));

            if (sobeId.HasValue) query = query.Where(x => x.DepartmentId == sobeId);
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


        public async Task<Result<SenedDetailDto>> GetDetailAsync(
    int senedId,
    List<int> icazeliSobeIdleri,
    bool isAdmin)
        {
            var query = _uow.Repository<Sened>()
                .Query()
                .Include(x => x.Department)
                .Include(x => x.Fayllar)
                .Include(x => x.SenedTagMaps.Where(m => !m.Silinib))
                    .ThenInclude(m => m.Tag)
                .AsQueryable();

            if (!isAdmin)
                query = query.Where(x => icazeliSobeIdleri.Contains(x.DepartmentId));

            var sened = await query.FirstOrDefaultAsync(x => x.Id == senedId);

            if (sened == null)
                return Result<SenedDetailDto>.Fail("Sənəd tapılmadı və ya icazəniz yoxdur.");

            var logs = await _uow.Repository<AuditLog>().Query()
                .Include(x => x.User)
                .Where(x => x.SenedId == senedId)
                .OrderByDescending(x => x.YaradilmaTarixi)
                .ToListAsync();

            var dto = _mapper.Map<SenedDetailDto>(sened);
            dto.AuditLogs = _mapper.Map<List<AuditLogDto>>(logs);

            return Result<SenedDetailDto>.Ok(dto);
        }
        public async Task<Result<SenedDetailDto>> GetDetailSilinmisAsync(
    int senedId,
    List<int> icazeliSobeIdleri,
    bool isAdmin)
        {
            var query = _uow.Repository<Sened>()
                .QueryDeleted()
                .Include(x => x.Department)
                .Include(x => x.Fayllar)
                .AsQueryable();

            if (!isAdmin)
                query = query.Where(x => icazeliSobeIdleri.Contains(x.DepartmentId));

            var sened = await query.FirstOrDefaultAsync(x => x.Id == senedId);

            if (sened == null)
                return Result<SenedDetailDto>.Fail("Sənəd tapılmadı və ya icazəniz yoxdur.");

            var logs = await _uow.Repository<AuditLog>().Query()
                .Include(x => x.User)
                .Where(x => x.SenedId == senedId)
                .OrderByDescending(x => x.YaradilmaTarixi)
                .ToListAsync();

            var dto = _mapper.Map<SenedDetailDto>(sened);
            dto.AuditLogs = _mapper.Map<List<AuditLogDto>>(logs);

            return Result<SenedDetailDto>.Ok(dto);
        }

        public async Task<Result<SenedDetailDto>> silmeİCazeSorgusuAsync(
    int senedId,
    List<int> icazeliSobeIdleri,
    bool isAdmin)
        {
            var query = _uow.Repository<Sened>()
                .Query()
                .Include(x => x.Department)
                .Include(x => x.Fayllar)
                .AsQueryable();

            if (!isAdmin)
                query = query.Where(x => icazeliSobeIdleri.Contains(x.DepartmentId));

            var sened = await query.FirstOrDefaultAsync(x => x.Id == senedId);

            if (sened == null)
                return Result<SenedDetailDto>.Fail("Sənədi silmək icazəniz yoxdur !!!");

            return Result<SenedDetailDto>.Ok(_mapper.Map<SenedDetailDto>(sened));
        }

        public async Task<Result<SenedDashboardDto>> GetDashboardAsync()
        {
            var query = _uow.Repository<Sened>().Query().Where(x => !x.Silinib);

            var umumi = await query.CountAsync();
            var yeni = await query.CountAsync(x => x.YaradilmaTarixi.Date == DateTime.Now.Date);
            var yoxlanilir = await query.CountAsync(x => x.Status == SenedStatusu.Yoxlanilir);
            var tesdiq = await query.CountAsync(x => x.Status == SenedStatusu.Tesdiq);
            var arxiv = await query.CountAsync(x => x.Status == SenedStatusu.Arxiv);

            var sonSenedler = await query
                .Include(x => x.Department)
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

            await _audit.WriteAsync(userId, "Upload", dto.SenedId, ip, new
            {
                action = "UploadNewVersion",
                Versiya = nextVersiya,
                FaylAdi = dto.OriginalAd,
                Olcu = dto.OlcuBytes
            });

            return Result<int>.Ok(entity.Id, "Fayl yükləndi (yeni versiya).");
        }

        public async Task<Result> UpdateStatusAsync(int senedId, SenedStatusu status, int userId, string? ip)
        {
            var sened = await _uow.Repository<Sened>().GetirAsync(x => x.Id == senedId && !x.Silinib);
            if (sened is null) return Result.Fail("Sənəd tapılmadı.");

            var oldStatus = sened.Status;

            sened.Status = status;
            sened.YenileyenIcraciId = userId;
            sened.YenilenmeTarixi = DateTime.UtcNow;

            await _uow.Repository<Sened>().YenileAsync(sened);
            await _uow.YaddaSaxlaAsync();

            await _audit.WriteAsync(userId, "Status", senedId, ip, new
            {
                action = "StatusChange",
                old = new { Status = oldStatus.ToString() },
                @new = new { Status = status.ToString() }
            });

            return Result.Ok("Status yeniləndi.");
        }

        public async Task<Result> SoftDeleteAsync(int senedId, int userId, string? ip)
        {
            var sened = await _uow.Repository<Sened>().GetirAsync(x => x.Id == senedId && !x.Silinib);
            if (sened is null) return Result.Fail("Sənəd tapılmadı.");

            sened.Silinib = true;
            sened.SilenIcraciId = userId;
            sened.SilinmeTarixi = DateTime.UtcNow;

            await _uow.Repository<Sened>().YenileAsync(sened);

            await _audit.WriteAsync(
                userId,
                "SoftDelete",
                senedId,
                ip,
                new
                {
                    action = "SoftDelete",
                    Basliq = sened.Basliq,
                    Mesaj = "Sənəd arxivə göndərildi"
                }
            );

            await _uow.YaddaSaxlaAsync();

            return Result.Ok("Sənəd uğurla silindi.");
        }

        public async Task<Result> RestoreAsync(int senedId, int userId, string? ip)
        {
            var sened = await _uow.Repository<Sened>().SilinmisGetirAsync(x => x.Id == senedId && x.Silinib);
            if (sened is null) return Result.Fail("Bərpa ediləcək sənəd tapılmadı.");

            sened.Silinib = false;
            sened.SilenIcraciId = null;
            sened.SilinmeTarixi = null;
            sened.YenileyenIcraciId = userId;
            sened.YenilenmeTarixi = DateTime.Now;

            await _uow.Repository<Sened>().YenileAsync(sened);
            await _uow.YaddaSaxlaAsync();

            await _audit.WriteAsync(userId, "Restore", senedId, ip, new
            {
                action = "Restore",
                Basliq = sened.Basliq,
                Mesaj = "Sənəd bərpa edildi"
            });

            return Result.Ok("Sənəd bərpa edildi.");
        }

        public async Task<Result<SenedFayl>> GetFileEntityAsync(int faylId)
        {
            var fayl = await _uow.Repository<SenedFayl>().GetirAsync(x => x.Id == faylId && !x.Silinib);
            if (fayl is null) return Result<SenedFayl>.Fail("Fayl tapılmadı.");
            return Result<SenedFayl>.Ok(fayl);
        }

        public async Task<Result<int>> CreateWithFileAsync(
    SenedCreateDto dto,
    Stream stream,
    string originalName,
    string contentType,
    long size,
    int userId,
    string? ip)
        {
            await using var transaction = await _uow.BeginTransactionAsync();

            try
            {
                // ===== 1️⃣ VALIDATION =====

                if (string.IsNullOrWhiteSpace(dto.AcarSoz))
                    return Result<int>.Fail("Açar söz boş ola bilməz.");

                var sobe = await _departmentService.HamisiniGetirAsync(x => x.Id == dto.SobeId && !x.Silinib);

                if (sobe is null)
                    return Result<int>.Fail("Şöbə tapılmadı.");

                var nov = await _uow.Repository<SenedNovu>()
                    .GetirAsync(x => x.Id == dto.SenedNovuId && !x.Silinib);

                if (nov is null)
                    return Result<int>.Fail("Sənəd növü tapılmadı.");

                // ===== 2️⃣ SENED YARAT =====

                // Sənəd nömrəsi yaradılır: {SenedNovu.Kod}-{İl}-{SıraNo:D3}
                var currentYear = DateTime.UtcNow.Year;
                var existingCount = await _uow.Repository<Sened>().Query()
                    .CountAsync(x => x.SenedNovuId == dto.SenedNovuId
                        && x.YaradilmaTarixi.Year == currentYear);
                var senedNomresi = $"{nov.Kod}-{currentYear}-{(existingCount + 1):D3}";

                var sened = new Sened
                {
                    DepartmentId = dto.SobeId,
                    SenedNovuId = dto.SenedNovuId,
                    Basliq = dto.Basliq.Trim(),
                    AcarSoz = dto.AcarSoz.Trim(),
                    SenedNomresi = senedNomresi,
                    Status = SenedStatusu.Yeni,
                    YaradanIcraciId = userId
                };

                await _uow.Repository<Sened>().YaratAsync(sened);
                await _uow.YaddaSaxlaAsync();

                // ===== 3️⃣ STORAGE (FAYL YAZ) =====

                var (storedName, path, sha256) =
                    await _storage.SaveAsync(stream, originalName, contentType);

                // ===== 4️⃣ SENEDFAYL YARAT =====

                var senedFayl = new SenedFayl
                {
                    SenedId = sened.Id,
                    VersiyaNo = 1,
                    OriginalAd = originalName,
                    StoredAd = storedName,
                    ContentType = contentType,
                    OlcuBytes = size,
                    Sha256 = sha256,
                    Yol = path,
                    AktivVersiya = true,
                    YaradanIcraciId = userId
                };

                await _uow.Repository<SenedFayl>().YaratAsync(senedFayl);

                // ===== 5️⃣ TAG MAPS =====

                if (dto.TagIds?.Count > 0)
                {
                    foreach (var tagId in dto.TagIds.Distinct())
                    {
                        await _uow.Repository<SenedTagMap>()
                            .YaratAsync(new SenedTagMap
                            {
                                SenedId = sened.Id,
                                TagId = tagId
                            });
                    }
                }

                await _uow.YaddaSaxlaAsync();

                // ===== 6️⃣ AUDIT =====

                await _audit.WriteAsync(userId, "CreateWithFile", sened.Id, ip,
                    new
                    {
                        action = "Create",
                        SobeId = dto.SobeId,
                        SenedNovuId = dto.SenedNovuId,
                        Basliq = dto.Basliq,
                        AcarSoz = dto.AcarSoz,
                        FaylAdi = originalName,
                        Olcu = size
                    });

                await transaction.CommitAsync();

                return Result<int>.Ok(sened.Id, "Sənəd və fayl uğurla yaradıldı.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Result<int>.Fail("Xəta baş verdi: " + ex.Message);
            }
        }

        public async Task<Result> UpdateAsync(SenedUpdateDto dto, int userId, string? ip)
        {
            // 1. Sənədi bazadan tapırıq (və varsa əlaqəli cədvəlləri Include edirik)
            var sened = await _uow.Repository<Sened>().Query()
                .FirstOrDefaultAsync(x => x.Id == dto.Id && !x.Silinib);

            if (sened == null)
                return Result.Fail("Sənəd tapılmadı.");

            // 2. Biznes validasiyalar (Məsələn, açar söz boş ola bilməz)
            if (string.IsNullOrWhiteSpace(dto.AcarSoz))
                return Result.Fail("Açar söz boş ola bilməz.");

            // 3. Şöbənin mövcudluğunu yoxlayırıq (əgər dto-da gəlirsə)
            var sobe = await _departmentService.HamisiniGetirAsync(x => x.Id == dto.SobeId && !x.Silinib);
            if (sobe == null)
                return Result.Fail("Şöbə tapılmadı.");

            // 4. Köhnə dəyərləri yadda saxlayırıq (audit üçün)
            var oldValues = new
            {
                sened.Basliq,
                SobeId = sened.DepartmentId,
                sened.SenedNovuId,
                sened.AcarSoz
            };

            // 5. Məlumatları mapiyirik (və ya əllə mənimsədirik)
            sened.Basliq = dto.Basliq;
            sened.DepartmentId = dto.SobeId;
            sened.SenedNovuId = dto.SenedNovuId;
            sened.AcarSoz = dto.AcarSoz;
            sened.YenileyenIcraciId = userId;
            sened.YenilenmeTarixi = DateTime.UtcNow;

            // 6. Yadda saxlayırıq
            _uow.Repository<Sened>().YenileAsync(sened);

            // 6.5 Tag-ləri yenilə (köhnələri sil, yenilərini əlavə et)
            var existingMaps = await _uow.Repository<SenedTagMap>()
                .HamisiniGetirAsync(x => x.SenedId == dto.Id && !x.Silinib);

            var existingTagIds = existingMaps.Select(x => x.TagId).ToHashSet();
            var newTagIds = (dto.TagIds ?? new List<int>()).ToHashSet();

            // Silinəcək tag-lər
            foreach (var map in existingMaps.Where(m => !newTagIds.Contains(m.TagId)))
            {
                map.Silinib = true;
                map.SilenIcraciId = userId;
                map.SilinmeTarixi = DateTime.UtcNow;
                await _uow.Repository<SenedTagMap>().YenileAsync(map);
            }

            // Əlavə olunacaq tag-lər
            foreach (var tagId in newTagIds.Where(t => !existingTagIds.Contains(t)))
            {
                await _uow.Repository<SenedTagMap>().YaratAsync(new SenedTagMap
                {
                    SenedId = dto.Id,
                    TagId = tagId,
                    YaradanIcraciId = userId
                });
            }

            // 7. AUDIT — köhnə vs yeni dəyərləri JSON olaraq yazırıq
            await _audit.WriteAsync(
                userId,
                "Update",
                sened.Id,
                ip,
                new
                {
                    action = "Update",
                    old = oldValues,
                    @new = new
                    {
                        Basliq = dto.Basliq,
                        SobeId = dto.SobeId,
                        SenedNovuId = dto.SenedNovuId,
                        AcarSoz = dto.AcarSoz
                    }
                });

            // 8. Bazaya yazırıq
            await _uow.YaddaSaxlaAsync();

            return Result.Ok();
        }

        public override async Task<Result<SenedDetailDto?>> IdIleGetirAsync(int id)
        {
            var entity = await _unitOfWork
                .Repository<Sened>()
                .GetirAsync(x => x.Id == id,
                    include: q => q
                        .Include(x => x.Fayllar).Include(a => a.Department));


            if (entity == null)
                return Result<SenedDetailDto?>.Fail("Tapılmadı.");

            var dto = _mapper.Map<SenedDetailDto>(entity);

            return Result<SenedDetailDto?>.Ok(dto);
        }
    }
}

