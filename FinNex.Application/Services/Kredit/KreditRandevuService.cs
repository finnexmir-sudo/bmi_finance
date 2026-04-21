using FinNex.Application.Interfaces.Kredit;
using FinNex.Domain.Entities.Kredit;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.Kredit
{
    public class KreditRandevuService : IKreditRandevuService
    {
        private readonly IUnitOfWork _uow;

        public KreditRandevuService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<IList<KreditRandevu>> GunUzreGetirAsync(DateTime gun)
        {
            var gunBaslangic = gun.Date;
            var gunSon = gunBaslangic.AddDays(1);
            return await _uow.Repository<KreditRandevu>().Query()
                .Where(x => !x.Silinib && x.Tarix >= gunBaslangic && x.Tarix < gunSon)
                .Include(x => x.KreditMuraciet)
                .Include(x => x.TeyinEdenIsci)
                .OrderBy(x => x.Tarix)
                .ToListAsync();
        }

        public async Task<IList<KreditRandevu>> AraliqUzreGetirAsync(DateTime baslangic, DateTime son)
        {
            var s = baslangic.Date;
            var e = son.Date.AddDays(1);
            return await _uow.Repository<KreditRandevu>().Query()
                .Where(x => !x.Silinib && x.Tarix >= s && x.Tarix < e)
                .Include(x => x.KreditMuraciet)
                .OrderBy(x => x.Tarix)
                .ToListAsync();
        }

        public async Task<KreditRandevu?> MuracietUzreGetirAsync(int muracietId)
        {
            return await _uow.Repository<KreditRandevu>().Query()
                .Where(x => !x.Silinib && x.KreditMuracietId == muracietId)
                .FirstOrDefaultAsync();
        }

        public async Task<KreditRandevu> TeyinEtAsync(int muracietId, DateTime tarix, int muddetDeqiqe,
                                                       string? qeyd, int teyinEdenIsciId)
        {
            // Əvvəlki randevu varsa ləğv et (soft)
            var kohne = await MuracietUzreGetirAsync(muracietId);
            if (kohne != null)
            {
                kohne.Silinib = true;
                kohne.SilinmeTarixi = DateTime.Now;
                kohne.SilenIcraciId = teyinEdenIsciId;
                await _uow.Repository<KreditRandevu>().YenileAsync(kohne);
            }

            var yeni = new KreditRandevu
            {
                KreditMuracietId = muracietId,
                Tarix = tarix,
                MuddetDeqiqe = muddetDeqiqe,
                Qeyd = qeyd,
                Status = KreditRandevuStatus.Planli,
                TeyinEdenIsciId = teyinEdenIsciId,
                YaradanIcraciId = teyinEdenIsciId,
                YaradilmaTarixi = DateTime.Now
            };
            await _uow.Repository<KreditRandevu>().YaratAsync(yeni);
            await _uow.YaddaSaxlaAsync();
            return yeni;
        }

        public async Task DurumDeyisAsync(int randevuId, KreditRandevuStatus yeniDurum, int? yenileyenIcraciId)
        {
            var entity = await _uow.Repository<KreditRandevu>().IdIleGetirAsync(randevuId);
            if (entity == null || entity.Silinib) return;
            entity.Status = yeniDurum;
            if (yeniDurum == KreditRandevuStatus.Gelib)
                entity.GelmeTarixi = DateTime.Now;
            entity.YenileyenIcraciId = yenileyenIcraciId;
            entity.YenilenmeTarixi = DateTime.Now;
            await _uow.Repository<KreditRandevu>().YenileAsync(entity);
            await _uow.YaddaSaxlaAsync();
        }

        public async Task LegvEtAsync(int randevuId, int? legvEdenIcraciId)
        {
            var entity = await _uow.Repository<KreditRandevu>().IdIleGetirAsync(randevuId);
            if (entity == null || entity.Silinib) return;
            entity.Status = KreditRandevuStatus.Legv;
            entity.Silinib = true;
            entity.SilinmeTarixi = DateTime.Now;
            entity.SilenIcraciId = legvEdenIcraciId;
            await _uow.Repository<KreditRandevu>().YenileAsync(entity);
            await _uow.YaddaSaxlaAsync();
        }
    }
}
