using FinNex.Application.Interfaces.Kredit;
using FinNex.Domain.Entities.Kredit;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.Kredit
{
    public class KreditQerarService : IKreditQerarService
    {
        private readonly IUnitOfWork _uow;

        public KreditQerarService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<KreditQerar?> MuracietUzreGetirAsync(int muracietId)
        {
            return await _uow.Repository<KreditQerar>().Query()
                .Where(x => !x.Silinib && x.KreditMuracietId == muracietId)
                .Include(x => x.DaxilEdenIsci)
                .Include(x => x.Imzalar.Where(i => !i.Silinib))
                    .ThenInclude(i => i.KomiteUzvu)
                        .ThenInclude(u => u.Isci)
                .FirstOrDefaultAsync();
        }

        public async Task<IList<KreditQerar>> HamisiniGetirAsync(KreditQerarTipi? yalnizBu = null)
        {
            var q = _uow.Repository<KreditQerar>().Query().Where(x => !x.Silinib);
            if (yalnizBu.HasValue) q = q.Where(x => x.Qerar == yalnizBu.Value);

            return await q.Include(x => x.KreditMuraciet)
                          .Include(x => x.DaxilEdenIsci)
                          .OrderByDescending(x => x.QerarTarixi)
                          .ToListAsync();
        }

        public async Task<KreditQerar> QebulEtAsync(KreditQerar qerar, IList<int> imzalayanKomiteUzvuIds,
                                                      int daxilEdenIsciId)
        {
            if (qerar.KreditMuracietId <= 0)
                throw new InvalidOperationException("KreditMuracietId təyin olunmayıb.");
            if (imzalayanKomiteUzvuIds == null || imzalayanKomiteUzvuIds.Count == 0)
                throw new InvalidOperationException("Ən azı bir imzalayan üzv seçilməlidir.");

            // Müraciəti yoxla və kilidlə — başqa yerdə də var, amma burada audit üçün
            var muraciet = await _uow.Repository<KreditMuraciet>().IdIleGetirAsync(qerar.KreditMuracietId)
                ?? throw new InvalidOperationException("Müraciət tapılmadı.");

            // Aktiv üzvlər arasından seçilməlidir
            var indi = DateTime.Now;
            var aktivUzvIds = await _uow.Repository<KomiteUzvu>().Query()
                .Where(x => !x.Silinib
                         && x.AktivdirFrom <= indi
                         && (x.AktivdirTo == null || x.AktivdirTo >= indi))
                .Select(x => x.Id)
                .ToListAsync();

            var legitimId = imzalayanKomiteUzvuIds.Where(id => aktivUzvIds.Contains(id)).Distinct().ToList();
            if (legitimId.Count == 0)
                throw new InvalidOperationException("Seçilən imzalayanlar aktiv komitə üzvü deyil.");

            using var trx = await _uow.BeginTransactionAsync();
            try
            {
                qerar.DaxilEdenIsciId = daxilEdenIsciId;
                qerar.YaradanIcraciId = daxilEdenIsciId;
                qerar.YaradilmaTarixi = DateTime.Now;
                await _uow.Repository<KreditQerar>().YaratAsync(qerar);
                await _uow.YaddaSaxlaAsync();

                foreach (var uzvId in legitimId)
                {
                    var imza = new KreditQerarImza
                    {
                        KreditQerarId = qerar.Id,
                        KomiteUzvuId = uzvId,
                        YaradanIcraciId = daxilEdenIsciId,
                        YaradilmaTarixi = DateTime.Now
                    };
                    await _uow.Repository<KreditQerarImza>().YaratAsync(imza);
                }

                // Müraciət statusu
                muraciet.Status = qerar.Qerar == KreditQerarTipi.Tesdiqlenib
                    ? KreditMuracietStatus.Tesdiqlenib
                    : KreditMuracietStatus.ReddEdilib;
                muraciet.YenileyenIcraciId = daxilEdenIsciId;
                muraciet.YenilenmeTarixi = DateTime.Now;
                await _uow.Repository<KreditMuraciet>().YenileAsync(muraciet);

                await _uow.YaddaSaxlaAsync();
                await trx.CommitAsync();

                return qerar;
            }
            catch
            {
                await trx.RollbackAsync();
                throw;
            }
        }

        public async Task YenileAsync(KreditQerar qerar, IList<int>? yeniImzalayanlar, int? yenileyenIcraciId)
        {
            qerar.YenileyenIcraciId = yenileyenIcraciId;
            qerar.YenilenmeTarixi = DateTime.Now;
            await _uow.Repository<KreditQerar>().YenileAsync(qerar);

            if (yeniImzalayanlar != null)
            {
                // Köhnə imzaları soft-sil
                var kohne = await _uow.Repository<KreditQerarImza>().Query()
                    .Where(x => !x.Silinib && x.KreditQerarId == qerar.Id)
                    .ToListAsync();
                foreach (var k in kohne)
                {
                    k.Silinib = true;
                    k.SilinmeTarixi = DateTime.Now;
                    k.SilenIcraciId = yenileyenIcraciId;
                    await _uow.Repository<KreditQerarImza>().YenileAsync(k);
                }

                foreach (var uzvId in yeniImzalayanlar.Distinct())
                {
                    var imza = new KreditQerarImza
                    {
                        KreditQerarId = qerar.Id,
                        KomiteUzvuId = uzvId,
                        YaradanIcraciId = yenileyenIcraciId,
                        YaradilmaTarixi = DateTime.Now
                    };
                    await _uow.Repository<KreditQerarImza>().YaratAsync(imza);
                }
            }

            await _uow.YaddaSaxlaAsync();
        }
    }
}
