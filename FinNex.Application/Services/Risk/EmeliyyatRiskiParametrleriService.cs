using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Risk;
using FinNex.Application.Interfaces.Risk;
using FinNex.Domain.Entities.Risk;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.Risk;

public class EmeliyyatRiskiParametrleriService : IEmeliyyatRiskiParametrleriService
{
    private readonly Domain.Interfaces.IUnitOfWork _unitOfWork;

    public EmeliyyatRiskiParametrleriService(Domain.Interfaces.IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<EmeliyyatRiskiParametrleriDto> AlAsync()
    {
        var qeyd = await _unitOfWork.Repository<EmeliyyatRiskiParametrleri>()
            .Query()
            .AsNoTracking()
            .OrderBy(p => p.Id)
            .FirstOrDefaultAsync();

        qeyd ??= EmeliyyatRiskiParametrleri.Defolt;

        return new EmeliyyatRiskiParametrleriDto { TedbirZererHeddi = qeyd.TedbirZererHeddi };
    }

    public async Task<Result> SaxlaAsync(EmeliyyatRiskiParametrleriDto dto, int? icraciId)
    {
        if (dto.TedbirZererHeddi < 0)
            return Result.Fail("Zərər həddi mənfi ola bilməz.");

        var repo = _unitOfWork.Repository<EmeliyyatRiskiParametrleri>();
        var movcud = await repo.Query().OrderBy(p => p.Id).FirstOrDefaultAsync();

        if (movcud == null)
        {
            await repo.YaratAsync(new EmeliyyatRiskiParametrleri
            {
                TedbirZererHeddi = dto.TedbirZererHeddi,
                YaradanIcraciId = icraciId
            });
        }
        else
        {
            movcud.TedbirZererHeddi = dto.TedbirZererHeddi;
            movcud.YenileyenIcraciId = icraciId;
            movcud.YenilenmeTarixi = DateTime.Now;
        }

        await _unitOfWork.YaddaSaxlaAsync();
        return Result.Ok("Parametr yadda saxlanıldı.");
    }
}
