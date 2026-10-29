using FinNex.Application.Interfaces.Muhasibat;
using FinNex.Domain.Entities.Muhasibat;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.Muhasibat
{
    public class HesabatSecilmisService : IHesabatSecilmisService
    {
        private readonly IUnitOfWork _uow;

        public HesabatSecilmisService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<HashSet<string>> SeciliKodlarAsync(int appUserId)
        {
            var kodlar = await _uow.Repository<HesabatSecilmis>().Query()
                .AsNoTracking()
                .Where(x => x.AppUserId == appUserId && !x.Silinib)
                .Select(x => x.HesabatKod)
                .ToListAsync();

            return kodlar.ToHashSet();
        }

        public async Task<bool> SecVeyaCixarAsync(int appUserId, string hesabatKod)
        {
            if (string.IsNullOrWhiteSpace(hesabatKod))
                return false;

            var kod = hesabatKod.Trim();

            // ⚠️ Unikal indeks (AppUserId, HesabatKod) `Silinib`-i FİLTRLƏMİR (CLAUDE.md:
            // "Yumşaq Silinmiş Sətir + Unikal İndeks") — `Query()` avtomatik `!Silinib`
            // tətbiq edir, silinmiş sətri görmək üçün burada MƏCBURİ `QueryAll()` lazımdır,
            // yoxsa aşağıdakı "dirilt" qolu heç vaxt işə düşmür və unikal indeks ikinci
            // dəfə seçmək istəyəndə INSERT-i sındırır.
            var movcud = await _uow.Repository<HesabatSecilmis>().QueryAll()
                .FirstOrDefaultAsync(x => x.AppUserId == appUserId && x.HesabatKod == kod);

            if (movcud != null && !movcud.Silinib)
            {
                movcud.Silinib = true;
                movcud.SilinmeTarixi = DateTime.Now;
                await _uow.YaddaSaxlaAsync();
                return false;
            }

            if (movcud != null)
            {
                movcud.Silinib = false;
                movcud.SilinmeTarixi = null;
            }
            else
            {
                await _uow.Repository<HesabatSecilmis>().YaratAsync(new HesabatSecilmis
                {
                    AppUserId = appUserId,
                    HesabatKod = kod
                });
            }

            await _uow.YaddaSaxlaAsync();
            return true;
        }
    }
}
