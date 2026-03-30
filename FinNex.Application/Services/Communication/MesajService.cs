// FinNex.Application.Services.Communication/MesajService.cs
using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Communication;
using FinNex.Application.Interfaces.Communication;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.Communication
{
    public class MesajService : IMesajService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBildirisService _bildirisService;

        public MesajService(IUnitOfWork unitOfWork, IBildirisService bildirisService)
        {
            _unitOfWork = unitOfWork;
            _bildirisService = bildirisService;
        }

        public async Task<Result<IList<MesajListDto>>> GetGelenlerAsync(int isciId)
        {
            try
            {
                var list = await _unitOfWork.Repository<Mesaj>()
                    .HamisiniGetirAsync(
                        predicate: x => x.AlanIsciId == isciId && !x.Silinib
                                     && x.CavabVerdigiMesajId == null, // Yalnız ana mesajlar
                        include: q => q
                            .Include(m => m.GonderenIsci)
                            .Include(m => m.AlanIsci),
                        izlemeden: true);

                return Result<IList<MesajListDto>>.Ok(
                    list.OrderByDescending(x => x.YaradilmaTarixi)
                        .Select(m => ToListDto(m, isciId)).ToList());
            }
            catch (Exception ex)
            {
                return Result<IList<MesajListDto>>.Fail($"Xəta: {ex.Message}");
            }
        }

        public async Task<Result<IList<MesajListDto>>> GetGonderilenlerAsync(int isciId)
        {
            try
            {
                var list = await _unitOfWork.Repository<Mesaj>()
                    .HamisiniGetirAsync(
                        predicate: x => x.GonderenIsciId == isciId && !x.Silinib
                                     && x.CavabVerdigiMesajId == null,
                        include: q => q
                            .Include(m => m.GonderenIsci)
                            .Include(m => m.AlanIsci),
                        izlemeden: true);

                return Result<IList<MesajListDto>>.Ok(
                    list.OrderByDescending(x => x.YaradilmaTarixi)
                        .Select(m => ToListDto(m, isciId)).ToList());
            }
            catch (Exception ex)
            {
                return Result<IList<MesajListDto>>.Fail($"Xəta: {ex.Message}");
            }
        }

        public async Task<Result<MesajDetailDto>> GetDetayAsync(int mesajId, int isciId)
        {
            try
            {
                var m = await _unitOfWork.Repository<Mesaj>()
                    .GetirAsync(
                        predicate: x => x.Id == mesajId && !x.Silinib,
                        include: q => q
                            .Include(m => m.GonderenIsci)
                            .Include(m => m.AlanIsci)
                            .Include(m => m.Cavablar)
                                .ThenInclude(c => c.GonderenIsci));

                if (m == null) return Result<MesajDetailDto>.Fail("Tapılmadı.");

                // Oxundu işarəsi
                if (m.AlanIsciId == isciId && !m.Oxunub)
                {
                    m.Oxunub = true;
                    m.OxunmaTarixi = DateTime.Now;
                    await _unitOfWork.Repository<Mesaj>().YenileAsync(m);
                    await _unitOfWork.YaddaSaxlaAsync();
                }

                var dto = new MesajDetailDto
                {
                    Id = m.Id,
                    GonderenIsciId = m.GonderenIsciId,
                    AlanIsciId = m.AlanIsciId,
                    GonderenAd = m.GonderenIsci.TamAd,
                    AlanAd = m.AlanIsci.TamAd,
                    Movzu = m.Movzu,
                    Metn = m.Metn,
                    MetnOnizleme = m.Metn.Length > 80 ? m.Metn[..80] + "..." : m.Metn,
                    Oxunub = m.Oxunub,
                    OxunmaTarixi = m.OxunmaTarixi,
                    YaradilmaTarixi = m.YaradilmaTarixi,
                    MenimGonderdiyim = m.GonderenIsciId == isciId,
                    CavabVerdigiMesajId = m.CavabVerdigiMesajId,
                    Cavablar = m.Cavablar
                        .Where(c => !c.Silinib)
                        .OrderBy(c => c.YaradilmaTarixi)
                        .Select(c => ToListDto(c, isciId)).ToList()
                };

                return Result<MesajDetailDto>.Ok(dto);
            }
            catch (Exception ex)
            {
                return Result<MesajDetailDto>.Fail($"Xəta: {ex.Message}");
            }
        }

        public async Task<Result> GonderAsync(MesajCreateDto dto)
        {
            try
            {
                var entity = new Mesaj
                {
                    GonderenIsciId = dto.GonderenIsciId,
                    AlanIsciId = dto.AlanIsciId,
                    Movzu = dto.Movzu,
                    Metn = dto.Metn,
                    CavabVerdigiMesajId = dto.CavabVerdigiMesajId
                };

                await _unitOfWork.Repository<Mesaj>().YaratAsync(entity);
                await _unitOfWork.YaddaSaxlaAsync();

                // Alan işçiyə bildiriş göndər
                var gonderenIsci = await _unitOfWork.Repository<FinNex.Domain.Entities.HR.Isci>()
                    .IdIleGetirAsync(dto.GonderenIsciId);

                await _bildirisService.YaratAsync(
                    isciId: dto.AlanIsciId,
                    nov: BildirisNovu.YeniMesaj,
                    bashliq: "Yeni mesaj",
                    metn: $"{gonderenIsci?.TamAd ?? "Biri"} sizə mesaj göndərdi: {dto.Movzu}",
                    redirectUrl: $"/User/Inbox/Mesaj/{entity.Id}",
                    mesajId: entity.Id);

                return Result.Ok("Mesaj göndərildi.");
            }
            catch (Exception ex)
            {
                return Result.Fail($"Xəta: {ex.Message}");
            }
        }

        public async Task<Result> OxunduIsareEtAsync(int mesajId, int isciId)
        {
            var m = await _unitOfWork.Repository<Mesaj>()
                .GetirAsync(x => x.Id == mesajId && x.AlanIsciId == isciId);

            if (m == null) return Result.Fail("Tapılmadı.");

            m.Oxunub = true;
            m.OxunmaTarixi = DateTime.Now;
            await _unitOfWork.Repository<Mesaj>().YenileAsync(m);
            await _unitOfWork.YaddaSaxlaAsync();
            return Result.Ok();
        }

        public async Task<Result<int>> OxunmamisSayiAsync(int isciId)
        {
            var sayi = (await _unitOfWork.Repository<Mesaj>()
                .HamisiniGetirAsync(x => x.AlanIsciId == isciId && !x.Oxunub && !x.Silinib))
                .Count;

            return Result<int>.Ok(sayi);
        }

        private static MesajListDto ToListDto(Mesaj m, int isciId) => new()
        {
            Id = m.Id,
            GonderenAd = m.GonderenIsci?.TamAd ?? "",
            AlanAd = m.AlanIsci?.TamAd ?? "",
            Movzu = m.Movzu,
            MetnOnizleme = m.Metn.Length > 80 ? m.Metn[..80] + "..." : m.Metn,
            Oxunub = m.Oxunub,
            OxunmaTarixi = m.OxunmaTarixi,
            YaradilmaTarixi = m.YaradilmaTarixi,
            MenimGonderdiyim = m.GonderenIsciId == isciId,
            CavabVerdigiMesajId = m.CavabVerdigiMesajId
        };
    }
}