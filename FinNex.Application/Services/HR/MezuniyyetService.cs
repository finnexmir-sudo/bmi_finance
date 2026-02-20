using AutoMapper;
using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Mezuniyyet;
using FinNex.Application.Services;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

public class MezuniyyetService : ServiceAsync<Mezuniyyet, MezuniyyetDto, MezuniyyetCreateDto, MezuniyyetUpdateDto>, IMezuniyyetService
{
    public MezuniyyetService(IUnitOfWork unitOfWork, IMapper mapper) : base(unitOfWork, mapper)
    {
    }

    public async Task<Result<IList<MezuniyyetListDto>>> GetListAsync()
    {
        var entities = await _unitOfWork.Repository<Mezuniyyet>()
            .HamisiniGetirAsync(include: x => x.Include(i => i.Isci), izlemeden: true);

        var data = _mapper.Map<IList<MezuniyyetListDto>>(entities);
        return Result<IList<MezuniyyetListDto>>.Ok(data);
    }

    public override async Task<Result<MezuniyyetDto>> YaratAsync(MezuniyyetCreateDto dto)
    {
        using (var transaction = await _unitOfWork.BeginTransactionAsync())
        {
            try
            {
                // 1. İş günlərini hesablayan məntiq (Bayramları çıxmaqla)
                int isGunu = await HesablaIsGunuAsync(dto.BaslamaTarixi, dto.BitmeTarixi);

                // 2. Balansı yoxla
                var balans = await _unitOfWork.Repository<MezuniyyetBalans>()
                    .GetirAsync(x => x.IsciId == dto.IsciId && x.Il == dto.BaslamaTarixi.Year);

                if (balans == null || balans.QaliqGun < isGunu)
                    return Result<MezuniyyetDto>.Fail("Kifayət qədər məzuniyyət balansınız yoxdur.");

                // 3. Entity-ni yarat
                var entity = _mapper.Map<Mezuniyyet>(dto);
                entity.IsGunlerininSayi = isGunu;
                entity.Status = MezuniyyetStatus.Gozlemede;

                await _unitOfWork.Repository<Mezuniyyet>().YaratAsync(entity);
                await _unitOfWork.YaddaSaxlaAsync();
                await transaction.CommitAsync();

                return Result<MezuniyyetDto>.Ok(_mapper.Map<MezuniyyetDto>(entity), "Müraciət uğurla göndərildi.");
            }
            catch
            {
                await transaction.RollbackAsync();
                return Result<MezuniyyetDto>.Fail("Xəta baş verdi.");
            }
        }
    }

    // Təsdiq Metodları (Workflow məntiqi)
    public async Task<Result> SobeReisiTesdiqAsync(int id, bool status, string? qeyd)
    {
        var m = await _unitOfWork.Repository<Mezuniyyet>().IdIleGetirAsync(id);
        if (m == null) return Result.Fail("Müraciət tapılmadı.");

        m.SobeReisiTesdiq = status;
        m.SobeReisiTesdiqTarixi = DateTime.Now;

        // Əgər rəis təsdiq etdisə, status "Rəhbər Təsdiqində" olur, etmədisə "İmtina"
        m.Status = status ? MezuniyyetStatus.RehberTesdiqinde : MezuniyyetStatus.ImtinaEdildi;

        if (!status) m.ImtinaSebebi = qeyd;

        await _unitOfWork.YaddaSaxlaAsync();
        return Result.Ok("Şöbə rəisi qərarı qeydə alındı.");
    }

    // --- Yardımçı Alqoritm: İş günlərini bayramlara görə hesablayır ---
    private async Task<int> HesablaIsGunuAsync(DateTime baslangic, DateTime bitis)
    {
        var bayramlar = await _unitOfWork.Repository<BayramGunu>()
            .HamisiniGetirAsync(x => x.Tarix >= baslangic && x.Tarix <= bitis);

        int count = 0;
        for (var date = baslangic; date <= bitis; date = date.AddDays(1))
        {
            // Şənbə (6) və Bazar (0) günləri deyilsə VƏ bayram günləri siyahısında yoxdursa
            if (date.DayOfWeek != DayOfWeek.Saturday &&
                date.DayOfWeek != DayOfWeek.Sunday &&
                !bayramlar.Any(b => b.Tarix.Date == date.Date))
            {
                count++;
            }
        }
        return count;
    }

    public async Task<Result> RehberTesdiqAsync(int id, bool status, string? qeyd)
    {
        var m = await _unitOfWork.Repository<Mezuniyyet>().IdIleGetirAsync(id);
        if (m == null) return Result.Fail("Müraciət tapılmadı.");

        m.RehberTesdiq = status;
        m.RehberTesdiqTarixi = DateTime.Now;

        // Rəhbər təsdiq etdisə, növbəti dayanacaq HR-dır
        m.Status = status ? MezuniyyetStatus.HrTesdiqinde : MezuniyyetStatus.ImtinaEdildi;

        if (!status) m.ImtinaSebebi = qeyd;

        await _unitOfWork.YaddaSaxlaAsync();
        return Result.Ok("Rəhbər qərarı qeydə alındı.");
    }
    // HrTesdiqAsync metodu
    public async Task<Result> HrTesdiqAsync(int id, bool status, string? qeyd)
    {
        var m = await _unitOfWork.Repository<Mezuniyyet>().IdIleGetirAsync(id);
        if (m == null) return Result.Fail("Müraciət tapılmadı.");

        m.HrTesdiq = status;
        m.HrTesdiqTarixi = DateTime.Now;
        m.Status = status ? MezuniyyetStatus.Tesdiqlenib : MezuniyyetStatus.ImtinaEdildi;

        if (!status) m.ImtinaSebebi = qeyd;

        await _unitOfWork.YaddaSaxlaAsync(); // İndi await var, xəbərdarlıq itəcək
        return Result.Ok("HR qərarı qeydə alındı.");
    }
}
