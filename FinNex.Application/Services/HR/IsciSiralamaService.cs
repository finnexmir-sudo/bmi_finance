using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Isci;
using FinNex.Application.Interfaces.HR;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.HR
{
    public class IsciSiralamaService : IIsciSiralamaService
    {
        private readonly IUnitOfWork _unitOfWork;

        public IsciSiralamaService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IList<IsciSiraDto>> GetSiyahiAsync()
        {
            var isciler = await _unitOfWork.Repository<Isci>()
                .Query()
                .Where(x => x.Status == IsciStatus.Aktiv && !x.Silinib)
                .Include(x => x.IsciTeyinatlari.Where(t => t.BitmeTarixi == null))
                    .ThenInclude(t => t.Vezife)
                .Include(x => x.IsciTeyinatlari.Where(t => t.BitmeTarixi == null))
                    .ThenInclude(t => t.Departament)
                .OrderBy(x => x.Sira)
                .ThenBy(x => x.Ad).ThenBy(x => x.Soyad)
                .ToListAsync();

            return isciler.Select(i =>
            {
                var teyin = i.IsciTeyinatlari.FirstOrDefault();
                return new IsciSiraDto
                {
                    IsciId        = i.Id,
                    AdSoyad       = $"{i.Ad} {i.Soyad}",
                    VezifeAd      = teyin?.Vezife?.Ad,
                    DepartamentAd = teyin?.Departament?.Ad,
                    Sira          = i.Sira
                };
            }).ToList();
        }

        public async Task<Result<int>> SaxlaAsync(IList<int> isciIdSirasi)
        {
            if (isciIdSirasi == null || isciIdSirasi.Count == 0)
                return Result<int>.Ok(0, "Dəyişiklik olmadı.");

            try
            {
                var idSet = isciIdSirasi.Where(x => x > 0).ToList();
                if (idSet.Count == 0)
                    return Result<int>.Ok(0, "Düzgün işçi ID-si yoxdur.");

                var repo = _unitOfWork.Repository<Isci>();
                var isciler = await repo.Query()
                    .Where(x => idSet.Contains(x.Id) && !x.Silinib)
                    .ToListAsync();

                // Sırayı 1-dən başlat — yeni gələn işçilər (Sira=0) avtomatik
                // mövcudların qarşısında qalmasın deyə hamısına yeni dəyər veririk.
                int deyisen = 0;
                for (int i = 0; i < idSet.Count; i++)
                {
                    var isci = isciler.FirstOrDefault(x => x.Id == idSet[i]);
                    if (isci == null) continue;
                    var yeniSira = i + 1;
                    if (isci.Sira != yeniSira)
                    {
                        isci.Sira = yeniSira;
                        await repo.YenileAsync(isci);
                        deyisen++;
                    }
                }

                if (deyisen > 0) await _unitOfWork.YaddaSaxlaAsync();
                return Result<int>.Ok(deyisen);
            }
            catch (Exception ex)
            {
                return Result<int>.Fail($"Xəta: {ex.Message}");
            }
        }
    }
}
