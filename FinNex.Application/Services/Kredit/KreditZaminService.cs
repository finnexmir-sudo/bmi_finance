using FinNex.Application.Interfaces.Kredit;
using FinNex.Domain.Entities.Kredit;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.Kredit
{
    public class KreditZaminService : IKreditZaminService
    {
        private readonly IUnitOfWork _uow;

        public KreditZaminService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<IList<KreditZamin>> MuracietZaminleriniGetirAsync(int muracietId)
        {
            return await _uow.Repository<KreditZamin>().Query()
                .Where(x => !x.Silinib && x.KreditMuracietId == muracietId)
                .OrderBy(x => x.YaradilmaTarixi)
                .ToListAsync();
        }

        public async Task<IList<KreditZamin>> FinUzreTarixceAsync(string fin, int? istisnaMuracietId = null)
        {
            if (string.IsNullOrWhiteSpace(fin)) return new List<KreditZamin>();
            var normalize = fin.Trim().ToUpper();

            var q = _uow.Repository<KreditZamin>().Query()
                .Where(x => !x.Silinib && x.FIN != null && x.FIN.ToUpper() == normalize);
            if (istisnaMuracietId.HasValue)
                q = q.Where(x => x.KreditMuracietId != istisnaMuracietId.Value);

            return await q.Include(x => x.KreditMuraciet)
                          .OrderByDescending(x => x.YaradilmaTarixi)
                          .ToListAsync();
        }

        public async Task<KreditZamin> YaratAsync(KreditZamin entity, int? yaradanIcraciId)
        {
            entity.YaradanIcraciId = yaradanIcraciId;
            entity.YaradilmaTarixi = DateTime.Now;
            await _uow.Repository<KreditZamin>().YaratAsync(entity);
            await _uow.YaddaSaxlaAsync();
            return entity;
        }

        public async Task SilAsync(int zaminId, int? silenIcraciId)
        {
            var entity = await _uow.Repository<KreditZamin>().IdIleGetirAsync(zaminId);
            if (entity == null || entity.Silinib) return;
            entity.Silinib = true;
            entity.SilinmeTarixi = DateTime.Now;
            entity.SilenIcraciId = silenIcraciId;
            await _uow.Repository<KreditZamin>().YenileAsync(entity);
            await _uow.YaddaSaxlaAsync();
        }

        public async Task MkrNeticesiYazAsync(int zaminId, string netice, int baxanIsciId)
        {
            var z = await _uow.Repository<KreditZamin>().IdIleGetirAsync(zaminId)
                ?? throw new InvalidOperationException("Zamin tapılmadı.");
            if (z.Silinib) throw new InvalidOperationException("Zamin silinib.");

            z.MkrNeticesi = netice;
            z.MkrBaxilmaTarixi = DateTime.Now;
            z.MkrBaxanIsciId = baxanIsciId;
            z.YenileyenIcraciId = baxanIsciId;
            z.YenilenmeTarixi = DateTime.Now;
            await _uow.Repository<KreditZamin>().YenileAsync(z);
            await _uow.YaddaSaxlaAsync();
        }

        public async Task AsanFinanceNeticesiYazAsync(int zaminId, string netice, int baxanIsciId)
        {
            var z = await _uow.Repository<KreditZamin>().IdIleGetirAsync(zaminId)
                ?? throw new InvalidOperationException("Zamin tapılmadı.");
            if (z.Silinib) throw new InvalidOperationException("Zamin silinib.");

            z.AsanFinanceNeticesi = netice;
            z.AsanFinanceBaxilmaTarixi = DateTime.Now;
            z.AsanFinanceBaxanIsciId = baxanIsciId;
            z.YenileyenIcraciId = baxanIsciId;
            z.YenilenmeTarixi = DateTime.Now;
            await _uow.Repository<KreditZamin>().YenileAsync(z);
            await _uow.YaddaSaxlaAsync();
        }
    }
}
