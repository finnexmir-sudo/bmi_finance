using FinNex.Application.Interfaces.Kredit;
using FinNex.Domain.Entities.Kredit;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.Kredit
{
    public class KreditMuracietService : IKreditMuracietService
    {
        private readonly IUnitOfWork _uow;

        public KreditMuracietService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<KreditMuraciet?> IdIleGetirAsync(int id, bool includeAll = true)
        {
            var q = _uow.Repository<KreditMuraciet>().Query().Where(x => !x.Silinib && x.Id == id);

            if (includeAll)
            {
                q = q.Include(x => x.BaxanIsci)
                     .Include(x => x.MkrBaxanIsci)
                     .Include(x => x.AsanFinanceBaxanIsci)
                     .Include(x => x.Zaminler.Where(z => !z.Silinib))
                     .Include(x => x.Qerar!).ThenInclude(q => q.Imzalar.Where(i => !i.Silinib))
                         .ThenInclude(i => i.KomiteUzvu).ThenInclude(u => u.Isci)
                     .Include(x => x.Randevu)
                     .Include(x => x.SmsLoglar.Where(s => !s.Silinib));
            }
            return await q.FirstOrDefaultAsync();
        }

        public async Task<IList<KreditMuraciet>> SiyahiAsync(KreditMuracietStatus? status = null,
                                                               KreditMuracietMenbe? menbe = null,
                                                               int? baxanIsciId = null)
        {
            var q = _uow.Repository<KreditMuraciet>().Query().Where(x => !x.Silinib);
            if (status.HasValue) q = q.Where(x => x.Status == status.Value);
            if (menbe.HasValue) q = q.Where(x => x.Menbe == menbe.Value);
            if (baxanIsciId.HasValue) q = q.Where(x => x.BaxanIsciId == baxanIsciId.Value);

            return await q.Include(x => x.BaxanIsci)
                          .Include(x => x.Randevu)
                          .Include(x => x.SmsLoglar.Where(s => !s.Silinib))
                          .OrderByDescending(x => x.MuracietTarixi)
                          .ToListAsync();
        }

        public async Task<IList<KreditMuraciet>> FinUzreTarixceAsync(string? fin, int? istisnaMuracietId = null)
        {
            if (string.IsNullOrWhiteSpace(fin)) return new List<KreditMuraciet>();
            var normalize = fin.Trim().ToUpper();

            var q = _uow.Repository<KreditMuraciet>().Query()
                .Where(x => !x.Silinib && x.FIN != null && x.FIN.ToUpper() == normalize);
            if (istisnaMuracietId.HasValue)
                q = q.Where(x => x.Id != istisnaMuracietId.Value);

            return await q.OrderByDescending(x => x.MuracietTarixi).ToListAsync();
        }

        public async Task<bool> MailMessageIdVarsa(string messageId)
        {
            if (string.IsNullOrWhiteSpace(messageId)) return false;
            return await _uow.Repository<KreditMuraciet>().Query()
                .AnyAsync(x => x.MailMessageId == messageId);
        }

        public async Task<KreditMuraciet> YaratAsync(KreditMuraciet entity, int? yaradanIcraciId)
        {
            entity.Status = KreditMuracietStatus.Yeni;
            if (entity.MuracietTarixi == default)
                entity.MuracietTarixi = DateTime.Now;
            entity.YaradanIcraciId = yaradanIcraciId;
            entity.YaradilmaTarixi = DateTime.Now;
            await _uow.Repository<KreditMuraciet>().YaratAsync(entity);
            await _uow.YaddaSaxlaAsync();
            return entity;
        }

        public async Task BaxilmagaGoturAsync(int muracietId, int baxanIsciId, string? qeyd)
        {
            var m = await _uow.Repository<KreditMuraciet>().IdIleGetirAsync(muracietId)
                ?? throw new InvalidOperationException("Müraciət tapılmadı.");
            if (m.Silinib) throw new InvalidOperationException("Müraciət silinib.");

            m.BaxanIsciId = baxanIsciId;
            m.BaxilmaTarixi = DateTime.Now;
            if (!string.IsNullOrWhiteSpace(qeyd)) m.IlkinBaxisQeyd = qeyd;
            if (m.Status == KreditMuracietStatus.Yeni)
                m.Status = KreditMuracietStatus.Yoxlanilir;
            m.YenileyenIcraciId = baxanIsciId;
            m.YenilenmeTarixi = DateTime.Now;

            await _uow.Repository<KreditMuraciet>().YenileAsync(m);
            await _uow.YaddaSaxlaAsync();
        }

        public async Task MkrNeticesiYazAsync(int muracietId, string netice, int baxanIsciId)
        {
            var m = await _uow.Repository<KreditMuraciet>().IdIleGetirAsync(muracietId)
                ?? throw new InvalidOperationException("Müraciət tapılmadı.");

            m.MkrNeticesi = netice;
            m.MkrBaxilmaTarixi = DateTime.Now;
            m.MkrBaxanIsciId = baxanIsciId;
            m.YenileyenIcraciId = baxanIsciId;
            m.YenilenmeTarixi = DateTime.Now;
            await _uow.Repository<KreditMuraciet>().YenileAsync(m);
            await _uow.YaddaSaxlaAsync();
        }

        public async Task AsanFinanceNeticesiYazAsync(int muracietId, string netice, int baxanIsciId)
        {
            var m = await _uow.Repository<KreditMuraciet>().IdIleGetirAsync(muracietId)
                ?? throw new InvalidOperationException("Müraciət tapılmadı.");

            m.AsanFinanceNeticesi = netice;
            m.AsanFinanceBaxilmaTarixi = DateTime.Now;
            m.AsanFinanceBaxanIsciId = baxanIsciId;
            m.YenileyenIcraciId = baxanIsciId;
            m.YenilenmeTarixi = DateTime.Now;
            await _uow.Repository<KreditMuraciet>().YenileAsync(m);
            await _uow.YaddaSaxlaAsync();
        }

        public async Task KomiteyeGonderAsync(int muracietId, int gonderenIsciId, string? qeyd)
        {
            var m = await _uow.Repository<KreditMuraciet>().IdIleGetirAsync(muracietId)
                ?? throw new InvalidOperationException("Müraciət tapılmadı.");
            if (m.Status != KreditMuracietStatus.Yoxlanilir && m.Status != KreditMuracietStatus.Yeni)
                throw new InvalidOperationException(
                    "Yalnız 'Yeni' və ya 'Yoxlanılır' statusundakı müraciət komitəyə göndərilə bilər.");

            m.Status = KreditMuracietStatus.KomiteyeGonderildi;
            if (!string.IsNullOrWhiteSpace(qeyd)) m.Qeyd = qeyd;
            m.YenileyenIcraciId = gonderenIsciId;
            m.YenilenmeTarixi = DateTime.Now;
            await _uow.Repository<KreditMuraciet>().YenileAsync(m);
            await _uow.YaddaSaxlaAsync();
        }

        public async Task YenileAsync(KreditMuraciet entity, int? yenileyenIcraciId)
        {
            entity.YenileyenIcraciId = yenileyenIcraciId;
            entity.YenilenmeTarixi = DateTime.Now;
            await _uow.Repository<KreditMuraciet>().YenileAsync(entity);
            await _uow.YaddaSaxlaAsync();
        }

        public async Task SilAsync(int muracietId, int? silenIcraciId)
        {
            var m = await _uow.Repository<KreditMuraciet>().IdIleGetirAsync(muracietId);
            if (m == null || m.Silinib) return;
            m.Silinib = true;
            m.SilinmeTarixi = DateTime.Now;
            m.SilenIcraciId = silenIcraciId;
            await _uow.Repository<KreditMuraciet>().YenileAsync(m);
            await _uow.YaddaSaxlaAsync();
        }
    }
}
