using FinNex.Application.Interfaces.Kredit;
using FinNex.Domain.Entities.Kredit;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.Kredit
{
    public class KreditBaxanIsciService : IKreditBaxanIsciService
    {
        private readonly IUnitOfWork _uow;

        public KreditBaxanIsciService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<IList<KreditBaxanIsci>> HamisiniGetirAsync()
        {
            var repo = _uow.Repository<KreditBaxanIsci>();
            return await repo.Query()
                .Where(x => !x.Silinib)
                .Include(x => x.Isci)
                .OrderByDescending(x => x.AktivdirFrom)
                .ToListAsync();
        }

        public async Task<bool> BaxaBilerMiAsync(int isciId)
        {
            var indi = DateTime.Now;
            return await _uow.Repository<KreditBaxanIsci>().Query()
                .AnyAsync(x => !x.Silinib
                            && x.IsciId == isciId
                            && x.AktivdirFrom <= indi
                            && (x.AktivdirTo == null || x.AktivdirTo >= indi));
        }

        public async Task<KreditBaxanIsci> TeyinEtAsync(int isciId, string? qeyd, int? yaradanIcraciId)
        {
            // Həmin işçi üçün aktiv qeyd artıq varsa, yenidən yaratma
            var movcud = await _uow.Repository<KreditBaxanIsci>().Query()
                .FirstOrDefaultAsync(x => !x.Silinib && x.IsciId == isciId
                                       && (x.AktivdirTo == null || x.AktivdirTo >= DateTime.Now));
            if (movcud != null) return movcud;

            var entity = new KreditBaxanIsci
            {
                IsciId = isciId,
                AktivdirFrom = DateTime.Now,
                Qeyd = qeyd,
                YaradanIcraciId = yaradanIcraciId,
                YaradilmaTarixi = DateTime.Now
            };
            await _uow.Repository<KreditBaxanIsci>().YaratAsync(entity);
            await _uow.YaddaSaxlaAsync();
            return entity;
        }

        public async Task LegvEtAsync(int baxanIsciId, int? legvEdenIcraciId)
        {
            var entity = await _uow.Repository<KreditBaxanIsci>().IdIleGetirAsync(baxanIsciId);
            if (entity == null || entity.Silinib) return;

            entity.AktivdirTo = DateTime.Now;
            entity.Silinib = true;
            entity.SilinmeTarixi = DateTime.Now;
            entity.SilenIcraciId = legvEdenIcraciId;
            await _uow.Repository<KreditBaxanIsci>().YenileAsync(entity);
            await _uow.YaddaSaxlaAsync();
        }
    }
}
