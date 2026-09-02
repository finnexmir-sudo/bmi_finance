using FinNex.Application.Interfaces.Kredit;
using FinNex.Domain.Entities.Kredit;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.Kredit
{
    /// <inheritdoc cref="IKreditReddSebebiService"/>
    public class KreditReddSebebiService : IKreditReddSebebiService
    {
        private readonly IUnitOfWork _uow;

        public KreditReddSebebiService(IUnitOfWork uow) => _uow = uow;

        public async Task<IList<KreditReddSebebi>> HamisiniGetirAsync()
            => await _uow.Repository<KreditReddSebebi>().Query()
                    .Where(x => !x.Silinib)
                    .OrderBy(x => x.Sira).ThenBy(x => x.Ad)
                    .AsNoTracking()
                    .ToListAsync();

        public async Task<IList<KreditReddSebebi>> AktivleriGetirAsync()
            => await _uow.Repository<KreditReddSebebi>().Query()
                    .Where(x => !x.Silinib && x.Aktivdir)
                    .OrderBy(x => x.Sira).ThenBy(x => x.Ad)
                    .AsNoTracking()
                    .ToListAsync();

        public async Task<KreditReddSebebi> YaratAsync(string ad, int sira, int? yaradanIcraciId)
        {
            if (string.IsNullOrWhiteSpace(ad))
                throw new InvalidOperationException("Səbəbin adı boş ola bilməz.");

            var temiz = ad.Trim();

            // Dublikat yoxlaması — iki eyni sətir hesabatı ikiyə bölər.
            // Deaktivləri də sayır: eyni ad yenidən yazılmasın, mövcudu aktivləşdirilsin.
            var varmi = await _uow.Repository<KreditReddSebebi>().Query()
                .AnyAsync(x => !x.Silinib && x.Ad.ToUpper() == temiz.ToUpper());
            if (varmi)
                throw new InvalidOperationException(
                    "Bu adda səbəb artıq var. Deaktivdirsə, siyahıdan aktivləşdirin.");

            var e = new KreditReddSebebi
            {
                Ad = temiz,
                Sira = sira,
                Aktivdir = true,
                YaradanIcraciId = yaradanIcraciId,
                YaradilmaTarixi = DateTime.Now
            };
            await _uow.Repository<KreditReddSebebi>().YaratAsync(e);
            await _uow.YaddaSaxlaAsync();
            return e;
        }

        public async Task YenileAsync(int id, string ad, int sira, int? yenileyenIcraciId)
        {
            if (string.IsNullOrWhiteSpace(ad))
                throw new InvalidOperationException("Səbəbin adı boş ola bilməz.");

            var e = await _uow.Repository<KreditReddSebebi>().IdIleGetirAsync(id)
                ?? throw new InvalidOperationException("Səbəb tapılmadı.");

            var temiz = ad.Trim();
            var varmi = await _uow.Repository<KreditReddSebebi>().Query()
                .AnyAsync(x => !x.Silinib && x.Id != id && x.Ad.ToUpper() == temiz.ToUpper());
            if (varmi)
                throw new InvalidOperationException("Bu adda başqa səbəb artıq var.");

            e.Ad = temiz;
            e.Sira = sira;
            e.YenileyenIcraciId = yenileyenIcraciId;
            e.YenilenmeTarixi = DateTime.Now;
            await _uow.Repository<KreditReddSebebi>().YenileAsync(e);
            await _uow.YaddaSaxlaAsync();
        }

        public async Task AktivliyiDeyisAsync(int id, bool aktivdir, int? yenileyenIcraciId)
        {
            var e = await _uow.Repository<KreditReddSebebi>().IdIleGetirAsync(id)
                ?? throw new InvalidOperationException("Səbəb tapılmadı.");

            e.Aktivdir = aktivdir;
            e.YenileyenIcraciId = yenileyenIcraciId;
            e.YenilenmeTarixi = DateTime.Now;
            await _uow.Repository<KreditReddSebebi>().YenileAsync(e);
            await _uow.YaddaSaxlaAsync();
        }
    }
}
