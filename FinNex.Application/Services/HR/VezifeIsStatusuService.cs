using FinNex.Application.DTOs.HR.VezifeIsStatusu;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.HR
{
    public class VezifeIsStatusuService : IVezifeIsStatusuService
    {
        private readonly IUnitOfWork _unitOfWork;

        public VezifeIsStatusuService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IList<VezifeIsStatusuDto>> SiyahiAsync()
        {
            // Vəzifə cədvəli DEPARTAMENT üzrə sətirlənir — eyni ad bir neçə
            // sətirdə ola bilər (CLAUDE.md: "Aktiv Təyinat" ilə bənzər tələ).
            // Bu siyahı isə ADINA görə TƏKRARSIZ olmalıdır (istifadəçi tələbi).
            var adlar = await _unitOfWork.Repository<Vezife>()
                .Query()
                .AsNoTracking()
                .Where(v => v.Aktivdir && !v.Silinib)
                .Select(v => v.Ad)
                .Distinct()
                .OrderBy(a => a)
                .ToListAsync();

            var statuslar = await _unitOfWork.Repository<VezifeIsStatusu>()
                .Query()
                .AsNoTracking()
                .Where(s => !s.Silinib)
                .ToListAsync();

            var statusDict = statuslar.ToDictionary(
                s => s.VezifeAdi, s => s.Status, StringComparer.OrdinalIgnoreCase);

            return adlar.Select(ad => new VezifeIsStatusuDto
            {
                VezifeAdi = ad,
                Status = statusDict.TryGetValue(ad, out var st) ? st : VezifeIsStatusuTipi.Icrachi,
                Qurulub = statusDict.ContainsKey(ad)
            }).ToList();
        }

        public async Task SaxlaAsync(IEnumerable<VezifeIsStatusuSetDto> setirler, int? icraciId)
        {
            var repo = _unitOfWork.Repository<VezifeIsStatusu>();
            var movcudlar = await repo.Query().Where(s => !s.Silinib).ToListAsync();
            var movcudDict = movcudlar.ToDictionary(s => s.VezifeAdi, StringComparer.OrdinalIgnoreCase);

            foreach (var setir in setirler)
            {
                if (string.IsNullOrWhiteSpace(setir.VezifeAdi)) continue;
                var ad = setir.VezifeAdi.Trim();

                if (movcudDict.TryGetValue(ad, out var movcud))
                {
                    if (movcud.Status != setir.Status)
                    {
                        movcud.Status = setir.Status;
                        movcud.YenileyenIcraciId = icraciId;
                        movcud.YenilenmeTarixi = DateTime.Now;
                    }
                }
                else
                {
                    await repo.YaratAsync(new VezifeIsStatusu
                    {
                        VezifeAdi = ad,
                        Status = setir.Status,
                        YaradanIcraciId = icraciId
                    });
                }
            }

            await _unitOfWork.YaddaSaxlaAsync();
        }

        public async Task<VezifeIsStatusuTipi> TapAsync(string? vezifeAdi)
        {
            if (string.IsNullOrWhiteSpace(vezifeAdi)) return VezifeIsStatusuTipi.Icrachi;

            var qeyd = await _unitOfWork.Repository<VezifeIsStatusu>()
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(s => !s.Silinib && s.VezifeAdi == vezifeAdi.Trim());

            return qeyd?.Status ?? VezifeIsStatusuTipi.Icrachi;
        }
    }
}
