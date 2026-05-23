using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.AyliqElave;
using FinNex.Application.Interfaces.HR;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.HR
{
    public class AyliqElaveService : IAyliqElaveService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AyliqElaveService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // ── Səhifə üçün sətirlər ──────────────────────────────────────────────
        public async Task<IList<AyliqElaveSetirDto>> GetSetirlerAsync(int il, int ay)
        {
            // Aktiv işçilər
            var isciler = await _unitOfWork.Repository<Isci>()
                .Query()
                .Where(x => x.Status == IsciStatus.Aktiv && !x.Silinib)
                .OrderBy(x => x.Sira).ThenBy(x => x.Ad).ThenBy(x => x.Soyad)
                .ToListAsync();

            // Bu ay üçün mövcud qeydlər
            var qeydler = await _unitOfWork.Repository<AyliqElaveQeydi>()
                .Query()
                .Where(x => x.Il == il && x.Ay == ay && !x.Silinib)
                .ToListAsync();

            // Maaşı hesablanmış işçilər — sətir kilidli olacaq
            var kilidli = (await _unitOfWork.Repository<Maas>()
                .Query()
                .Where(x => x.Il == il && x.Ay == ay && !x.Silinib)
                .Select(x => x.IsciId)
                .ToListAsync())
                .ToHashSet();

            return isciler.Select(i =>
            {
                var q = qeydler.FirstOrDefault(x => x.IsciId == i.Id);
                return new AyliqElaveSetirDto
                {
                    IsciId   = i.Id,
                    AdSoyad  = $"{i.Ad} {i.Soyad}",
                    Bonus    = q?.Bonus ?? 0,
                    Overtime = q?.Overtime ?? 0,
                    Kilidli  = kilidli.Contains(i.Id)
                };
            }).ToList();
        }

        // ── Bulk upsert ───────────────────────────────────────────────────────
        public async Task<Result<int>> SaxlaAsync(int il, int ay, IList<AyliqElaveSetirDto> setirler)
        {
            if (setirler == null || setirler.Count == 0)
                return Result<int>.Ok(0, "Dəyişiklik olmadı.");

            try
            {
                // Kilidli işçilər — maaşı hesablanıb, dəyişməyə icazə yox
                var kilidli = (await _unitOfWork.Repository<Maas>()
                    .Query()
                    .Where(x => x.Il == il && x.Ay == ay && !x.Silinib)
                    .Select(x => x.IsciId)
                    .ToListAsync())
                    .ToHashSet();

                var repo = _unitOfWork.Repository<AyliqElaveQeydi>();
                var movcud = await repo.Query()
                    .Where(x => x.Il == il && x.Ay == ay && !x.Silinib)
                    .ToListAsync();

                int deyisen = 0;
                foreach (var s in setirler)
                {
                    if (s.IsciId <= 0 || kilidli.Contains(s.IsciId)) continue;

                    var bonus    = s.Bonus    < 0 ? 0 : s.Bonus;
                    var overtime = s.Overtime < 0 ? 0 : s.Overtime;
                    var q = movcud.FirstOrDefault(x => x.IsciId == s.IsciId);

                    // Hər ikisi sıfırdırsa — qeyd lazım deyil; varsa soft-delete
                    if (bonus == 0 && overtime == 0)
                    {
                        if (q != null)
                        {
                            q.Silinib = true;
                            await repo.YenileAsync(q);
                            deyisen++;
                        }
                        continue;
                    }

                    if (q == null)
                    {
                        await repo.YaratAsync(new AyliqElaveQeydi
                        {
                            IsciId   = s.IsciId,
                            Il       = il,
                            Ay       = ay,
                            Bonus    = bonus,
                            Overtime = overtime
                        });
                        deyisen++;
                    }
                    else if (q.Bonus != bonus || q.Overtime != overtime)
                    {
                        q.Bonus    = bonus;
                        q.Overtime = overtime;
                        await repo.YenileAsync(q);
                        deyisen++;
                    }
                }

                await _unitOfWork.YaddaSaxlaAsync();
                return Result<int>.Ok(deyisen);
            }
            catch (Exception ex)
            {
                return Result<int>.Fail($"Xəta: {ex.Message}");
            }
        }

        // ── TopluHesabla səhifəsi üçün ay üzrə xəritə ─────────────────────────
        public async Task<(IDictionary<int, decimal> Bonus, IDictionary<int, decimal> Overtime)> GetAyMapAsync(int il, int ay)
        {
            var list = await _unitOfWork.Repository<AyliqElaveQeydi>()
                .Query()
                .Where(x => x.Il == il && x.Ay == ay && !x.Silinib)
                .Select(x => new { x.IsciId, x.Bonus, x.Overtime })
                .ToListAsync();

            IDictionary<int, decimal> bonusMap    = list.ToDictionary(x => x.IsciId, x => x.Bonus);
            IDictionary<int, decimal> overtimeMap = list.ToDictionary(x => x.IsciId, x => x.Overtime);
            return (bonusMap, overtimeMap);
        }
    }
}
