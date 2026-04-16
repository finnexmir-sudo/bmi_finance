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
        private const int MAX_QEYD_SAYI = 12;

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

                // Sliding window: 12-dən çoxsa ən köhnələri sil
                await SlidingWindowTetbiqEtAsync(isciId);

                return Result.Ok("Qeyd əlavə edildi.");
            }
            catch (Exception ex)
            {
                return Result.Fail($"Xəta: {ex.Message}");
            }
        }

        public async Task<Result> AutoInsertFromMaasAsync(int isciId, int il, int ay, decimal qazanc)
        {
            // Avtomatik əlavə — eyni metoddan istifadə edir, sadəcə elIle = false
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

        // ─────────────────────────────────────────────────────────
        // SLIDING WINDOW: ən köhnələri silir ki 12 qeyd qalsın
        // ─────────────────────────────────────────────────────────
        private async Task SlidingWindowTetbiqEtAsync(int isciId)
        {
            var list = await _unitOfWork.Repository<IsciAyliqQazanc>()
                .Query()
                .Where(x => x.IsciId == isciId && !x.Silinib)
                .OrderByDescending(x => x.Il * 12 + x.Ay)
                .ToListAsync();

            if (list.Count <= MAX_QEYD_SAYI) return;

            var silinecekler = list.Skip(MAX_QEYD_SAYI).ToList();
            foreach (var item in silinecekler)
            {
                item.Silinib = true;
                item.SilinmeTarixi = DateTime.Now;
            }
            await _unitOfWork.YaddaSaxlaAsync();
        }
    }
}
