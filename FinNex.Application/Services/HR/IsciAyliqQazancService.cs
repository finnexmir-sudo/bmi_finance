using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Maas;
using FinNex.Application.Interfaces.HR;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.HR
{
    public class IsciAyliqQazancService : IIsciAyliqQazancService
    {
        private readonly IUnitOfWork _unitOfWork;
        public IsciAyliqQazancService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<IList<IsciAyliqQazancDto>>> GetByIsciAsync(int isciId)
        {
            try
            {
                var list = await _unitOfWork.Repository<IsciAyliqQazanc>()
                    .Query()
                    .Where(x => x.IsciId == isciId && !x.Silinib)
                    .OrderByDescending(x => x.Il * 12 + x.Ay)
                    .ToListAsync();

                var dtos = list.Select(x => new IsciAyliqQazancDto
                {
                    Id = x.Id,
                    IsciId = x.IsciId,
                    Il = x.Il,
                    Ay = x.Ay,
                    Qazanc = x.Qazanc,
                    ElIleDaxilEdilib = x.ElIleDaxilEdilib,
                    Qeyd = x.Qeyd,
                    YaradilmaTarixi = x.YaradilmaTarixi
                }).ToList();

                return Result<IList<IsciAyliqQazancDto>>.Ok(dtos);
            }
            catch (Exception ex)
            {
                return Result<IList<IsciAyliqQazancDto>>.Fail($"Xəta: {ex.Message}");
            }
        }

        public async Task<Result> AddOrUpdateAsync(int isciId, int il, int ay, decimal qazanc, bool elIle, string? qeyd = null)
        {
            try
            {
                if (qazanc < 0)
                    return Result.Fail("Qazanc mənfi ola bilməz.");

                if (ay < 1 || ay > 12)
                    return Result.Fail("Ay 1-12 aralığında olmalıdır.");

                // Mövcud qeyd yoxla
                var movcud = await _unitOfWork.Repository<IsciAyliqQazanc>()
                    .Query()
                    .FirstOrDefaultAsync(x => x.IsciId == isciId && x.Il == il && x.Ay == ay && !x.Silinib);

                if (movcud != null)
                {
                    movcud.Qazanc = qazanc;
                    movcud.ElIleDaxilEdilib = elIle;
                    movcud.Qeyd = qeyd;
                    await _unitOfWork.YaddaSaxlaAsync();
                    return Result.Ok("Qeyd yeniləndi.");
                }

                // Yeni yarat
                var entity = new IsciAyliqQazanc
                {
                    IsciId = isciId,
                    Il = il,
                    Ay = ay,
                    Qazanc = qazanc,
                    ElIleDaxilEdilib = elIle,
                    Qeyd = qeyd
                };
                await _unitOfWork.Repository<IsciAyliqQazanc>().YaratAsync(entity);
                await _unitOfWork.YaddaSaxlaAsync();

                return Result.Ok("Qeyd əlavə edildi.");
            }
            catch (Exception ex)
            {
                return Result.Fail($"Xəta: {ex.Message}");
            }
        }

        public async Task<Result> AutoInsertFromMaasAsync(int isciId, int il, int ay, decimal qazanc)
        {
            // Aktiv qeydi yoxla — manual olarsa toxunma.
            var movcud = await _unitOfWork.Repository<IsciAyliqQazanc>()
                .Query()
                .FirstOrDefaultAsync(x => x.IsciId == isciId && x.Il == il && x.Ay == ay && !x.Silinib);

            if (movcud != null && movcud.ElIleDaxilEdilib)
                return Result.Ok("Əl ilə daxil edilmiş qeyd saxlanıldı.");

            if (movcud != null)
            {
                movcud.Qazanc = qazanc;
                movcud.Qeyd = $"Sistem tərəfindən {DateTime.Now:dd.MM.yyyy HH:mm} avtomatik yeniləndi";
                await _unitOfWork.YaddaSaxlaAsync();
                return Result.Ok("Qeyd yeniləndi.");
            }

            // LegvEdildi sonrası Silinib=1 qeyd qalır — unique constraint-i pozmamaq üçün
            // yeni INSERT etmək əvəzinə həmin qeydi bərpa et.
            var silinmis = await _unitOfWork.Repository<IsciAyliqQazanc>()
                .Query()
                .FirstOrDefaultAsync(x => x.IsciId == isciId && x.Il == il && x.Ay == ay && x.Silinib);

            if (silinmis != null && silinmis.ElIleDaxilEdilib)
                return Result.Ok("Əl ilə daxil edilmiş silinmiş qeyd saxlanıldı.");

            if (silinmis != null)
            {
                silinmis.Silinib = false;
                silinmis.SilinmeTarixi = null;
                silinmis.Qazanc = qazanc;
                silinmis.Qeyd = $"Sistem tərəfindən {DateTime.Now:dd.MM.yyyy HH:mm} bərpa edildi";
                await _unitOfWork.YaddaSaxlaAsync();
                return Result.Ok("Silinmiş qeyd bərpa edildi.");
            }

            return await AddOrUpdateAsync(isciId, il, ay, qazanc, elIle: false,
                qeyd: $"Sistem tərəfindən {DateTime.Now:dd.MM.yyyy HH:mm} avtomatik əlavə edildi");
        }

        public async Task<Result> DeleteAsync(int id)
        {
            try
            {
                var entity = await _unitOfWork.Repository<IsciAyliqQazanc>()
                    .Query()
                    .FirstOrDefaultAsync(x => x.Id == id && !x.Silinib);

                if (entity == null)
                    return Result.Fail("Qeyd tapılmadı.");

                entity.Silinib = true;
                entity.SilinmeTarixi = DateTime.Now;
                await _unitOfWork.YaddaSaxlaAsync();

                return Result.Ok("Qeyd silindi.");
            }
            catch (Exception ex)
            {
                return Result.Fail($"Xəta: {ex.Message}");
            }
        }

        public async Task<decimal> Son12AyCemiQazancAsync(int isciId)
        {
            var list = await _unitOfWork.Repository<IsciAyliqQazanc>()
                .Query()
                .Where(x => x.IsciId == isciId && !x.Silinib)
                .OrderByDescending(x => x.Il * 12 + x.Ay)
                .Take(12)
                .ToListAsync();

            return list.Sum(x => x.Qazanc);
        }

        public async Task<int> Son12AyQeydSayiAsync(int isciId)
        {
            return await _unitOfWork.Repository<IsciAyliqQazanc>()
                .Query()
                .Where(x => x.IsciId == isciId && !x.Silinib)
                .CountAsync();
        }

    }
}
