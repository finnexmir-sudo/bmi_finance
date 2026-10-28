using FinNex.Application.Interfaces.Kassa;
using FinNex.Domain.Entities.Kassa;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.Kassa
{
    public class KassaBildirisAlaniService : IKassaBildirisAlaniService
    {
        private readonly IUnitOfWork _uow;

        public KassaBildirisAlaniService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<IList<KassaBildirisAlani>> HamisiniGetirAsync()
        {
            var repo = _uow.Repository<KassaBildirisAlani>();
            return await repo.Query()
                .Where(x => !x.Silinib)
                .Include(x => x.Isci)
                .OrderByDescending(x => x.AktivdirFrom)
                .ToListAsync();
        }

        public async Task<IList<int>> AktivIsciIdleriniGetirAsync()
        {
            var indi = DateTime.Now;
            return await _uow.Repository<KassaBildirisAlani>().Query()
                .Where(x => !x.Silinib
                         && x.AktivdirFrom <= indi
                         && (x.AktivdirTo == null || x.AktivdirTo >= indi))
                .Select(x => x.IsciId)
                .ToListAsync();
        }

        public async Task<KassaBildirisAlani> TeyinEtAsync(int isciId, string? qeyd, int? yaradanIcraciId)
        {
            var movcud = await _uow.Repository<KassaBildirisAlani>().Query()
                .FirstOrDefaultAsync(x => !x.Silinib && x.IsciId == isciId
                                       && (x.AktivdirTo == null || x.AktivdirTo >= DateTime.Now));
            if (movcud != null) return movcud;

            var entity = new KassaBildirisAlani
            {
                IsciId = isciId,
                AktivdirFrom = DateTime.Now,
                Qeyd = qeyd,
                YaradanIcraciId = yaradanIcraciId,
                YaradilmaTarixi = DateTime.Now
            };
            await _uow.Repository<KassaBildirisAlani>().YaratAsync(entity);
            await _uow.YaddaSaxlaAsync();
            return entity;
        }

        public async Task LegvEtAsync(int id, int? legvEdenIcraciId)
        {
            var entity = await _uow.Repository<KassaBildirisAlani>().IdIleGetirAsync(id);
            if (entity == null || entity.Silinib) return;

            entity.AktivdirTo = DateTime.Now;
            entity.Silinib = true;
            entity.SilinmeTarixi = DateTime.Now;
            entity.SilenIcraciId = legvEdenIcraciId;
            await _uow.Repository<KassaBildirisAlani>().YenileAsync(entity);
            await _uow.YaddaSaxlaAsync();
        }
    }
}
