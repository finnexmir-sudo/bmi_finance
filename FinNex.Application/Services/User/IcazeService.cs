using AutoMapper;
using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Icaze;
using FinNex.Application.Interfaces;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FinNex.Domain;

namespace FinNex.Application.Services
{
    public class IcazeService : IIcazeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;
        private readonly IMapper _mapper;

        public IcazeService(IUnitOfWork unitOfWork, UserManager<AppUser> userManager, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _mapper = mapper;
        }

        public async Task<Result<IList<IcazeListDto>>> GetIsciIcazeleriAsync(int isciId)
        {
            try
            {
                var list = await _unitOfWork.Repository<Icaze>()
                    .HamisiniGetirAsync(
                        x => x.IsciId == isciId,
                        include: q => q
                            .Include(i => i.Isci)
    .ThenInclude(i => i.IsciTeyinatlari)
                            .Include(i => i.EvezEdenIsci),
                        izlemeden: true);

                var dtos = list
                    .OrderByDescending(x => x.IcazeTarixi)
                    .Select(MapToListDto)
                    .ToList();

                return Result<IList<IcazeListDto>>.Ok(dtos);
            }
            catch (Exception ex)
            {
                return Result<IList<IcazeListDto>>.Fail($"Icazeler getirilmedi: {ex.Message}");
            }
        }

        public async Task<Result<IcazeListDto>> YaratAsync(IcazeCreateDto dto)
        {
            try
            {
                // Saat yoxlaması
                if (dto.BitisSaati <= dto.BaslamaSaati)
                    return Result<IcazeListDto>.Fail("Bitme saati baslama saatindan sonra olmalidir.");

                var entity = new Icaze
                {
                    IsciId = dto.IsciId,
                    EvezEdenIsciId = dto.EvezEdenIsciId,
                    IcazeTarixi = dto.IcazeTarixi,
                    BaslamaSaati = dto.BaslamaSaati,
                    BitisSaati = dto.BitisSaati,
                    Sebeb = dto.Sebeb,
                    Status = IcazeStatus.Gozlemede
                };

                await _unitOfWork.Repository<Icaze>().YaratAsync(entity);
                await _unitOfWork.YaddaSaxlaAsync();

                // Navigation property-ləri yüklə
                var saved = await _unitOfWork.Repository<Icaze>()
                    .GetirAsync(
                        x => x.Id == entity.Id,
                        include: q => q
                            .Include(i => i.Isci)
    .ThenInclude(i => i.IsciTeyinatlari)
                            .Include(i => i.EvezEdenIsci),
                        izlemeden: true);

                return Result<IcazeListDto>.Ok(MapToListDto(saved!), "Icaze muracietiniz gonderildi.");
            }
            catch (Exception ex)
            {
                return Result<IcazeListDto>.Fail($"Yaradilmadi: {ex.Message}");
            }
        }

        public async Task<Result> LegvEtAsync(int icazeId, int isciId)
        {
            try
            {
                var icaze = await _unitOfWork.Repository<Icaze>().IdIleGetirAsync(icazeId);

                if (icaze == null)
                    return Result.Fail("Icaze tapilmadi.");

                if (icaze.IsciId != isciId)
                    return Result.Fail("Bu icaze size aid deyil.");

                if (icaze.Status != IcazeStatus.Gozlemede)
                    return Result.Fail("Yalniz 'Gozlemede' statusundaki icaze legv edile biler.");

                await _unitOfWork.Repository<Icaze>().YumshakSilAsync(icazeId);
                await _unitOfWork.YaddaSaxlaAsync();

                return Result.Ok("Icaze legv edildi.");
            }
            catch (Exception ex)
            {
                return Result.Fail($"Legv zamani xeta: {ex.Message}");
            }
        }

        public async Task<Result<IcazeDetailDto>> GetDetayAsync(int icazeId)
        {
            try
            {
                var icaze = await _unitOfWork.Repository<Icaze>()
                    .GetirAsync(
                        x => x.Id == icazeId,
                        include: q => q
                            .Include(i => i.Isci)
    .ThenInclude(i => i.IsciTeyinatlari)
                            .Include(i => i.EvezEdenIsci),
                        izlemeden: true);

                if (icaze == null)
                    return Result<IcazeDetailDto>.Fail("Tapilmadi.");

                var dto = new IcazeDetailDto
                {
                    Id = icaze.Id,
                    IsciAdSoyad = icaze.Isci.TamAd,
                    SobeAdi = icaze.Isci.IsciTeyinatlari
                    .Where(t => t.Aktivdir)
                    .Select(t => t.Departament.Ad)
                    .FirstOrDefault() ?? "-",
                    EvezEdenAdSoyad = icaze.EvezEdenIsci.TamAd,
                    IcazeTarixi = icaze.IcazeTarixi,
                    BaslamaSaati = icaze.BaslamaSaati,
                    BitisSaati = icaze.BitisSaati,
                    IcazeSaati = icaze.IcazeSaati,
                    Sebeb = icaze.Sebeb,
                    Status = icaze.Status,
                    ImtinaSebebi = icaze.ImtinaSebebi,
                    SobeReisiTesdiq = icaze.SobeReisiTesdiq,
                    SobeReisiTesdiqTarixi = icaze.SobeReisiTesdiqTarixi,
                    RehberTesdiq = icaze.RehberTesdiq,
                    RehberTesdiqTarixi = icaze.RehberTesdiqTarixi,
                    HrTesdiq = icaze.HrTesdiq,
                    HrTesdiqTarixi = icaze.HrTesdiqTarixi,
                };

                return Result<IcazeDetailDto>.Ok(dto);
            }
            catch (Exception ex)
            {
                return Result<IcazeDetailDto>.Fail($"Xeta: {ex.Message}");
            }
        }

        public async Task<Result<IList<IcazeListDto>>> GetAllAsync()
        {
            try
            {
                var list = await _unitOfWork.Repository<Icaze>()
                    .HamisiniGetirAsync(
                        include: q => q
                            .Include(i => i.Isci)
                                .ThenInclude(i => i.IsciTeyinatlari)
                            .Include(i => i.EvezEdenIsci),
                        izlemeden: true);

                var dtos = list
                    .OrderByDescending(x => x.IcazeTarixi)
                    .Select(MapToListDto)
                    .ToList();

                return Result<IList<IcazeListDto>>.Ok(dtos);
            }
            catch (Exception ex)
            {
                return Result<IList<IcazeListDto>>.Fail($"Icazeler getirilmedi: {ex.Message}");
            }
        }

        public async Task<Result<IList<IcazeListDto>>> GetGozlemededeAsync()
        {
            try
            {
                var list = await _unitOfWork.Repository<Icaze>()
                    .HamisiniGetirAsync(
                        predicate: x => x.Status == IcazeStatus.Gozlemede,
                        include: q => q
                            .Include(i => i.Isci)
                                .ThenInclude(i => i.IsciTeyinatlari)
                            .Include(i => i.EvezEdenIsci),
                        izlemeden: true);

                var dtos = list
                    .OrderByDescending(x => x.IcazeTarixi)
                    .Select(MapToListDto)
                    .ToList();

                return Result<IList<IcazeListDto>>.Ok(dtos);
            }
            catch (Exception ex)
            {
                return Result<IList<IcazeListDto>>.Fail($"Gözləmədə icazələr gətirilmədi: {ex.Message}");
            }
        }

        public async Task<Result<IList<IcazeListDto>>> GetRehberTesdiqindeAsync()
        {
            try
            {
                var list = await _unitOfWork.Repository<Icaze>()
                    .HamisiniGetirAsync(
                        predicate: x => x.Status == IcazeStatus.RehberTesdiqinde,
                        include: q => q
                            .Include(i => i.Isci)
                                .ThenInclude(i => i.IsciTeyinatlari)
                            .Include(i => i.EvezEdenIsci),
                        izlemeden: true);

                var dtos = list
                    .OrderByDescending(x => x.IcazeTarixi)
                    .Select(MapToListDto)
                    .ToList();

                return Result<IList<IcazeListDto>>.Ok(dtos);
            }
            catch (Exception ex)
            {
                return Result<IList<IcazeListDto>>.Fail($"Rəhbər təsdiqindəki icazələr gətirilmədi: {ex.Message}");
            }
        }

        public async Task<Result<IList<IcazeListDto>>> GetHrTesdiqindeAsync()
        {
            try
            {
                var list = await _unitOfWork.Repository<Icaze>()
                    .HamisiniGetirAsync(
                        predicate: x => x.Status == IcazeStatus.HrTesdiqinde,
                        include: q => q
                            .Include(i => i.Isci)
                                .ThenInclude(i => i.IsciTeyinatlari)
                            .Include(i => i.EvezEdenIsci),
                        izlemeden: true);

                var dtos = list
                    .OrderByDescending(x => x.IcazeTarixi)
                    .Select(MapToListDto)
                    .ToList();

                return Result<IList<IcazeListDto>>.Ok(dtos);
            }
            catch (Exception ex)
            {
                return Result<IList<IcazeListDto>>.Fail($"HR təsdiqindəki icazələr gətirilmədi: {ex.Message}");
            }
        }

        public async Task<Result> SobeReisiTesdiqAsync(int id, bool status, string? qeyd)
        {
            try
            {
                var icaze = await _unitOfWork.Repository<Icaze>().IdIleGetirAsync(id);
                if (icaze == null) return Result.Fail("İcazə tapılmadı.");

                icaze.SobeReisiTesdiq = status;
                icaze.SobeReisiTesdiqTarixi = DateTime.Now;
                icaze.Status = status ? IcazeStatus.RehberTesdiqinde : IcazeStatus.ImtinaEdildi;

                if (!status) icaze.ImtinaSebebi = qeyd;

                await _unitOfWork.YaddaSaxlaAsync();
                return Result.Ok("Şöbə rəisi qərarı qeydə alındı.");
            }
            catch (Exception ex)
            {
                return Result.Fail($"Xəta: {ex.Message}");
            }
        }

        public async Task<Result> RehberTesdiqAsync(int id, bool status, string? qeyd)
        {
            try
            {
                var icaze = await _unitOfWork.Repository<Icaze>().IdIleGetirAsync(id);
                if (icaze == null) return Result.Fail("İcazə tapılmadı.");

                icaze.RehberTesdiq = status;
                icaze.RehberTesdiqTarixi = DateTime.Now;
                icaze.Status = status ? IcazeStatus.HrTesdiqinde : IcazeStatus.ImtinaEdildi;

                if (!status) icaze.ImtinaSebebi = qeyd;

                await _unitOfWork.YaddaSaxlaAsync();
                return Result.Ok("Rəhbər qərarı qeydə alındı.");
            }
            catch (Exception ex)
            {
                return Result.Fail($"Xəta: {ex.Message}");
            }
        }

        public async Task<Result> HrTesdiqAsync(int id, bool status, string? qeyd)
        {
            try
            {
                var icaze = await _unitOfWork.Repository<Icaze>().IdIleGetirAsync(id);
                if (icaze == null) return Result.Fail("İcazə tapılmadı.");

                icaze.HrTesdiq = status;
                icaze.HrTesdiqTarixi = DateTime.Now;
                icaze.Status = status ? IcazeStatus.Tesdiqlenib : IcazeStatus.ImtinaEdildi;

                if (!status) icaze.ImtinaSebebi = qeyd;

                await _unitOfWork.YaddaSaxlaAsync();
                return Result.Ok("HR qərarı qeydə alındı.");
            }
            catch (Exception ex)
            {
                return Result.Fail($"Xəta: {ex.Message}");
            }
        }

        private static IcazeListDto MapToListDto(Icaze icaze) => new()
        {
            Id = icaze.Id,
            IsciAdSoyad = icaze.Isci.TamAd,
            SobeAdi = icaze.Isci.IsciTeyinatlari
    .Where(t => t.Aktivdir)
    .Select(t => t.Departament.Ad)
    .FirstOrDefault() ?? "-",
            EvezEdenAdSoyad = icaze.EvezEdenIsci.TamAd,
            IcazeTarixi = icaze.IcazeTarixi,
            BaslamaSaati = icaze.BaslamaSaati,
            BitisSaati = icaze.BitisSaati,
            IcazeSaati = icaze.IcazeSaati,
            Sebeb = icaze.Sebeb,
            Status = icaze.Status,
        };
    }
}