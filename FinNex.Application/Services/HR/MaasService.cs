using AutoMapper;
using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Maas;
using FinNex.Application.Services;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

public class MaasService : ServiceAsync<Maas, MaasListDto, MaasCreateDto, MaasUpdateDto>, IMaasService
{
    public MaasService(IUnitOfWork unitOfWork, IMapper mapper) : base(unitOfWork, mapper) { }

    public async Task<Result> CalculateMonthlyPayrollAsync(int isciId, int il, int ay)
    {
        var isci = await _unitOfWork.Repository<Isci>().GetirAsync(x => x.Id == isciId, include: q => q.Include(x => x.Maliye));
        if (isci == null) return Result.Fail("İşçi tapılmadı.");

        var maasNovleri = await _unitOfWork.Repository<MaasNovu>().HamisiniGetirAsync(x => x.Aktivdir);
        var maasMaster = new Maas { IsciId = isciId, Il = il, Ay = ay, Status = MaasStatus.Layihe, Detallar = new List<MaasDetay>() };

        var esasMaas = maasNovleri.First(x => x.Ad == "Əsas Əməkhaqqı");
        maasMaster.Detallar.Add(new MaasDetay { MaasNovuId = esasMaas.Id, Mebleg = isci.Maliye.CariMaas });
        maasMaster.NetMebleg = isci.Maliye.CariMaas;

        await _unitOfWork.Repository<Maas>().YaratAsync(maasMaster);
        return await _unitOfWork.YaddaSaxlaAsync() > 0 ? Result.Ok() : Result.Fail("Xəta!");
    }
    public async Task<Result<IList<MaasDto>>> IsciyeGoreGetirAsync(int isciId)
    {
        try
        {
            var entities = await _unitOfWork.Repository<Maas>()
                .HamisiniGetirAsync(
                    x => x.IsciId == isciId,
                    q => q.Include(x => x.Detallar),
                    true);

            var dtos = _mapper.Map<IList<MaasDto>>(entities);
            return Result<IList<MaasDto>>.Ok(dtos);
        }
        catch
        {
            return Result<IList<MaasDto>>.Fail("İşçinin maaş məlumatları gətirilərkən xəta baş verdi.");
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
                return Result<MaasDto?>.Fail("Bu ay üçün maaş tapılmadı.");

            var dto = _mapper.Map<MaasDto>(entity);
            return Result<MaasDto?>.Ok(dto);
        }
        catch
        {
            return Result<MaasDto?>.Fail("Maaş məlumatı gətirilərkən xəta baş verdi.");
        }
    }

    public async Task<Result> StatusDeyisAsync(int maasId, MaasStatus yeniStatus)
    {
        try
        {
            var repo = _unitOfWork.Repository<Maas>();
            var entity = await repo.IdIleGetirAsync(maasId);

            if (entity == null)
                return Result.Fail("Maaş tapılmadı.");

            entity.Status = yeniStatus;

            if (yeniStatus == MaasStatus.Tesdiqlendi)
                entity.TesdiqTarixi = DateTime.UtcNow;

            if (yeniStatus == MaasStatus.Odenildi)
                entity.OdenisTarixi = DateTime.UtcNow;

            await repo.YenileAsync(entity);
            await _unitOfWork.YaddaSaxlaAsync();

            return Result.Ok("Maaş statusu uğurla dəyişdirildi.");
        }
        catch
        {
            return Result.Fail("Status dəyişdirilərkən xəta baş verdi.");
        }
    }
}