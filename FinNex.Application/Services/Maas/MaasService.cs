using AutoMapper;
using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Maas;
using FinNex.Application.Interfaces.HR;
using FinNex.Application.Interfaces.Maas_If;
using FinNex.Application.Services;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class MaasService : ServiceAsync<Maas, MaasListDto, MaasCreateDto, MaasUpdateDto>, IMaasService
{
    private readonly ILogger<MaasService> _logger;
    private readonly IIsciAyliqQazancService _ayliqQazancService;

    public MaasService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<MaasService> logger,
        IIsciAyliqQazancService ayliqQazancService) : base(unitOfWork, mapper)
    {
        _logger = logger;
        _ayliqQazancService = ayliqQazancService;
    }

    public async Task<Result<IList<MaasDto>>> IsciyeGoreGetirAsync(int isciId)
    {
        try
        {
            var entities = await _unitOfWork.Repository<Maas>()
                .HamisiniGetirAsync(
                    x => x.IsciId == isciId,
                    q => q.Include(x => x.Detallar).ThenInclude(d => d.MaasNovu),
                    true);

            var dtos = _mapper.Map<IList<MaasDto>>(entities);
            return Result<IList<MaasDto>>.Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "İşçi maaş məlumatları gətirilirkən xəta: IsciId={IsciId}", isciId);
            return Result<IList<MaasDto>>.Fail("Isci maas melumatları getirilirken xeta bas verdi.");
        }
    }

    public async Task<Result<MaasDto?>> IsciAyUzreGetirAsync(int isciId, int il, int ay)
    {
        try
        {
            var entity = await _unitOfWork.Repository<Maas>()
                .GetirAsync(
                    x => x.IsciId == isciId && x.Il == il && x.Ay == ay,
                    q => q.Include(x => x.Detallar).ThenInclude(d => d.MaasNovu),
                    true);

            if (entity == null)
                return Result<MaasDto?>.Fail("Bu ay ucun maas tapilmadi.");

            var dto = _mapper.Map<MaasDto>(entity);
            return Result<MaasDto?>.Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Maaş məlumatı gətirilirkən xəta: IsciId={IsciId}, İl={Il}, Ay={Ay}", isciId, il, ay);
            return Result<MaasDto?>.Fail("Maas melumatı getirilirken xeta bas verdi.");
        }
    }

    public async Task<Result> StatusDeyisAsync(int maasId, MaasStatus yeniStatus)
    {
        try
        {
            var repo = _unitOfWork.Repository<Maas>();
            var entity = await repo.IdIleGetirAsync(maasId);

            if (entity == null)
                return Result.Fail("Maas tapilmadi.");

            if (entity.Status == MaasStatus.Tesdiqlendi && yeniStatus == MaasStatus.Layihe)
                return Result.Fail("Tesdiqlenmis maas Layihe statusuna qaytarila bilmez.");

            if (entity.Status == MaasStatus.Odenildi)
                return Result.Fail("Odenilmis maasda status deyisikliyine icaze verilmir.");

            // Ləğv = layihə birdəfəlik atılır: Maas + MaasDetay + AyliqQazanc silinir.
            // Silinib=true olduğu üçün bütün sorğulardan avtomatik çıxır —
            // növbəti hesablamada işçi "yeni" kimi görünür.
            if (yeniStatus == MaasStatus.LegvEdildi)
            {
                var now = DateTime.UtcNow;

                // 1. XestelikOdenisleri MaasId linkini qır (qeyd qalır, link NULL olur)
                var xestelikler = await _unitOfWork.Repository<XestelikOdenis>()
                    .Query()
                    .Where(x => x.MaasId == maasId)
                    .ToListAsync();
                foreach (var x in xestelikler)
                    x.MaasId = null;

                // 2. MaasDetay-ları sil
                var detallar = await _unitOfWork.Repository<MaasDetay>()
                    .Query()
                    .Where(d => d.MaasId == maasId)
                    .ToListAsync();
                foreach (var d in detallar)
                    await _unitOfWork.Repository<MaasDetay>().DeleteAsync(d.Id);

                // 3. AyliqQazanc qeydini sil (sistem tərəfindən yazılmışsa) — hard-delete,
                //    yenidən hesablamada INSERT unique constraint-i pozmasın.
                //    Manual (ElIleDaxilEdilib=true) qeydlərə toxunmur.
                var qazancQeyd = await _unitOfWork.Repository<IsciAyliqQazanc>()
                    .Query()
                    .FirstOrDefaultAsync(x => x.IsciId == entity.IsciId
                                           && x.Il == entity.Il
                                           && x.Ay == entity.Ay
                                           && !x.ElIleDaxilEdilib);
                if (qazancQeyd != null)
                    await _unitOfWork.Repository<IsciAyliqQazanc>().DeleteAsync(qazancQeyd.Id);

                // 4. Maaş qeydini hard-delete et (unique index conflict-in qarsisini al)
                await _unitOfWork.Repository<Maas>().DeleteAsync(entity.Id);

                await _unitOfWork.YaddaSaxlaAsync();
                return Result.Ok("Maaş ləğv edildi və bazadan silindi.");
            }

            entity.Status = yeniStatus;

            if (yeniStatus == MaasStatus.Tesdiqlendi)
                entity.TesdiqTarixi = DateTime.UtcNow;

            if (yeniStatus == MaasStatus.Odenildi)
                entity.OdenisTarixi = DateTime.UtcNow;

            await repo.YenileAsync(entity);
            await _unitOfWork.YaddaSaxlaAsync();

            return Result.Ok("Maas statusu ugurla deyisdirildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Maaş status dəyişdirmə xətası: MaasId={MaasId}, YeniStatus={Status}", maasId, yeniStatus);
            return Result.Fail("Status deyisdiriilirken xeta bas verdi.");
        }
    }
}
