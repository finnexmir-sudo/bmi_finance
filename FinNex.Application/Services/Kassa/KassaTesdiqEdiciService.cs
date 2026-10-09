using FinNex.Application.Interfaces.Kassa;
using FinNex.Domain.Entities.Kassa;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.Kassa
{
    public class KassaTesdiqEdiciService : IKassaTesdiqEdiciService
    {
        private readonly IUnitOfWork _uow;

        public KassaTesdiqEdiciService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<IList<KassaTesdiqEdici>> HamisiniGetirAsync()
        {
            var repo = _uow.Repository<KassaTesdiqEdici>();
            return await repo.Query()
                .Where(x => !x.Silinib)
                .Include(x => x.Isci)
                .OrderByDescending(x => x.AktivdirFrom)
                .ToListAsync();
        }

        public async Task<bool> TesdiqEdeBilerMiAsync(int isciId)
        {
            var indi = DateTime.Now;
            return await _uow.Repository<KassaTesdiqEdici>().Query()
                .AnyAsync(x => !x.Silinib
                            && x.IsciId == isciId
                            && x.AktivdirFrom <= indi
                            && (x.AktivdirTo == null || x.AktivdirTo >= indi));
        }

        public async Task<KassaTesdiqEdici> TeyinEtAsync(int isciId, string? qeyd, int? yaradanIcraciId)
        {
            var movcud = await _uow.Repository<KassaTesdiqEdici>().Query()
                .FirstOrDefaultAsync(x => !x.Silinib && x.IsciId == isciId
                                       && (x.AktivdirTo == null || x.AktivdirTo >= DateTime.Now));
            if (movcud != null) return movcud;

            var entity = new KassaTesdiqEdici
            {
                IsciId = isciId,
                AktivdirFrom = DateTime.Now,
                Qeyd = qeyd,
                YaradanIcraciId = yaradanIcraciId,
                YaradilmaTarixi = DateTime.Now
            };
            await _uow.Repository<KassaTesdiqEdici>().YaratAsync(entity);
            await _uow.YaddaSaxlaAsync();
            return entity;
        }

        public async Task LegvEtAsync(int tesdiqEdiciId, int? legvEdenIcraciId)
        {
            var entity = await _uow.Repository<KassaTesdiqEdici>().IdIleGetirAsync(tesdiqEdiciId);
            if (entity == null || entity.Silinib) return;

            entity.AktivdirTo = DateTime.Now;
            entity.Silinib = true;
            entity.SilinmeTarixi = DateTime.Now;
            entity.SilenIcraciId = legvEdenIcraciId;
            await _uow.Repository<KassaTesdiqEdici>().YenileAsync(entity);
            await _uow.YaddaSaxlaAsync();
        }
    }
}
