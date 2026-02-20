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
}