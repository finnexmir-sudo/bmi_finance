using FinNex.Application.Interfaces.Kredit;
using FinNex.Domain.Entities.Kredit;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.Kredit
{
    public class KomiteUzvuService : IKomiteUzvuService
    {
        private readonly IUnitOfWork _uow;

        public KomiteUzvuService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<IList<KomiteUzvu>> HamisiniGetirAsync(bool yalnizAktiv = true)
        {
            var indi = DateTime.Now;
            var q = _uow.Repository<KomiteUzvu>().Query().Where(x => !x.Silinib);
            if (yalnizAktiv)
            {
                q = q.Where(x => x.AktivdirFrom <= indi
                              && (x.AktivdirTo == null || x.AktivdirTo >= indi));
            }
            return await q.Include(x => x.Isci)
                          .OrderBy(x => x.Rol)
                          .ThenBy(x => x.Isci.Soyad)
                          .ToListAsync();
        }

        public async Task<bool> IsciKomiteUzvuMu(int isciId)
        {
            var indi = DateTime.Now;
            return await _uow.Repository<KomiteUzvu>().Query()
                .AnyAsync(x => !x.Silinib
                            && x.IsciId == isciId
                            && x.AktivdirFrom <= indi
                            && (x.AktivdirTo == null || x.AktivdirTo >= indi));
        }

        public async Task<KomiteUzvu> TeyinEtAsync(int isciId, KomiteUzvuRolu rol, string? qeyd, int? yaradanIcraciId)
        {
            var movcud = await _uow.Repository<KomiteUzvu>().Query()
                .FirstOrDefaultAsync(x => !x.Silinib && x.IsciId == isciId
                                       && (x.AktivdirTo == null || x.AktivdirTo >= DateTime.Now));
            if (movcud != null)
            {
                // Artıq üzvdür — rol dəyişdirmək lazım ola bilər
                if (movcud.Rol != rol)
                {
                    movcud.Rol = rol;
                    movcud.YenileyenIcraciId = yaradanIcraciId;
                    movcud.YenilenmeTarixi = DateTime.Now;
                    await _uow.Repository<KomiteUzvu>().YenileAsync(movcud);
                    await _uow.YaddaSaxlaAsync();
                }
                return movcud;
            }

            var entity = new KomiteUzvu
            {
                IsciId = isciId,
                Rol = rol,
                AktivdirFrom = DateTime.Now,
                Qeyd = qeyd,
                YaradanIcraciId = yaradanIcraciId,
                YaradilmaTarixi = DateTime.Now
            };
            await _uow.Repository<KomiteUzvu>().YaratAsync(entity);
            await _uow.YaddaSaxlaAsync();
            return entity;
        }

        public async Task RolDeyisAsync(int komiteUzvuId, KomiteUzvuRolu yeniRol, int? yenileyenIcraciId)
        {
            var entity = await _uow.Repository<KomiteUzvu>().IdIleGetirAsync(komiteUzvuId);
            if (entity == null || entity.Silinib) return;
            entity.Rol = yeniRol;
            entity.YenileyenIcraciId = yenileyenIcraciId;
            entity.YenilenmeTarixi = DateTime.Now;
            await _uow.Repository<KomiteUzvu>().YenileAsync(entity);
            await _uow.YaddaSaxlaAsync();
        }

        public async Task LegvEtAsync(int komiteUzvuId, int? legvEdenIcraciId)
        {
            var entity = await _uow.Repository<KomiteUzvu>().IdIleGetirAsync(komiteUzvuId);
            if (entity == null || entity.Silinib) return;
            entity.AktivdirTo = DateTime.Now;
            entity.Silinib = true;
            entity.SilinmeTarixi = DateTime.Now;
            entity.SilenIcraciId = legvEdenIcraciId;
            await _uow.Repository<KomiteUzvu>().YenileAsync(entity);
            await _uow.YaddaSaxlaAsync();
        }
    }
}
