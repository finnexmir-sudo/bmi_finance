using AutoMapper;
using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Isci;
using FinNex.Application.Services;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

public class IsciService : ServiceAsync<Isci, IsciListDto, IsciCreateDto, IsciUpdateDto>, IIsciService
{
    public IsciService(IUnitOfWork unitOfWork, IMapper mapper) : base(unitOfWork, mapper) { }

    public override async Task<Result<IList<IsciListDto>>> HamisiniGetirAsync()
    {
        try
        {
            // Burada repository-nin Include dəstəkləyən metodundan istifadə edirik
            var entities = await _unitOfWork.Repository<Isci>()
                .HamisiniGetirAsync(
                    izlemeden: true,
                    include: x => x.Include(x => x.IsciTeyinatlari)
    .ThenInclude(t => t.Departament)
.Include(x => x.IsciTeyinatlari)
    .ThenInclude(t => t.Vezife)
                                   .Include(i => i.Maliye)
                );

            var data = _mapper.Map<IList<IsciListDto>>(entities);

            return Result<IList<IsciListDto>>.Ok(data);
        }
        catch (Exception)
        {
            return Result<IList<IsciListDto>>.Fail("İşçi siyahısı gətirilərkən xəta baş verdi.");
        }
    }
    public async Task<IsciDetailDto?> GetIsciDetailsAsync(int id)
    {
        var entity = await _unitOfWork.Repository<Isci>().GetirAsync(
            x => x.Id == id,
            include: q => q.Include(x => x.IsciTeyinatlari)
    .ThenInclude(t => t.Departament)
.Include(x => x.IsciTeyinatlari)
    .ThenInclude(t => t.Vezife).Include(x => x.Maliye)
        );
        return _mapper.Map<IsciDetailDto>(entity);
    }

    public async Task<IList<IsciListDto>> GetIscilerBySobeIdAsync(int sobeId)
    {
        var entities = await _unitOfWork.Repository<Isci>().HamisiniGetirAsync(
            x => x.IsciTeyinatlari.Any(t => t.Aktivdir && t.DepartamentId == sobeId),
            include: q => q.Include(x => x.IsciTeyinatlari)
    .ThenInclude(t => t.Departament)
.Include(x => x.IsciTeyinatlari)
    .ThenInclude(t => t.Vezife).Include(x => x.Maliye),
            izlemeden: true
        );
        return _mapper.Map<IList<IsciListDto>>(entities);
    }

    public async Task<bool> CheckFinExistsAsync(string fin) =>
        await _unitOfWork.Repository<Isci>().MovcuddurmuAsync(x => x.FIN == fin);

    public async Task<Result<List<IsciListDto>>> SearchIscilerByFinAsync(string fin)
    {
        var entities = await _unitOfWork.Repository<Isci>().HamisiniGetirAsync(
            x => x.FIN.Contains(fin),
            include: q => q.Include(x => x.Maliye).Include(x => x.IsciTeyinatlari)
    .ThenInclude(t => t.Departament),
            izlemeden: true
        );
        return Result<List<IsciListDto>>.Ok(_mapper.Map<List<IsciListDto>>(entities));
    }

    public async Task<Result> UpdateSalaryWithHistoryAsync(int isciId, decimal yeniMaas, string emrNo)
    {
        var isci = await _unitOfWork.Repository<Isci>().GetirAsync(x => x.Id == isciId, include: q => q.Include(x => x.Maliye));
        if (isci == null) return Result.Fail("İşçi tapılmadı.");

        var tarixce = new IsciMaasTarixcesi
        {
            IsciId = isciId,
            KohneMaas = isci.Maliye.CariMaas,
            YeniMaas = yeniMaas,
            DeyismeTarixi = DateTime.Now,
            EmrinNomresi = emrNo
        };
        isci.Maliye.CariMaas = yeniMaas;
        await _unitOfWork.Repository<IsciMaasTarixcesi>().YaratAsync(tarixce);
        return await _unitOfWork.YaddaSaxlaAsync() > 0 ? Result.Ok() : Result.Fail("Xəta!");
    }
}