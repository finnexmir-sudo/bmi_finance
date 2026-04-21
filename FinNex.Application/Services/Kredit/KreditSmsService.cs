using FinNex.Application.Interfaces.Kredit;
using FinNex.Domain.Entities.Kredit;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.Kredit
{
    public class KreditSmsService : IKreditSmsService
    {
        private readonly IUnitOfWork _uow;

        public KreditSmsService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<KreditSmsLog> GonderAsync(int? muracietId, string telefon, string metn,
                                                      string? sablon, int gonderenIsciId)
        {
            // Hazırda gateway qoşulmayıb — sadəcə log yazılır, status 'Gozleyir' qalır.
            // Gateway əlavə edildikdə bu metod içində HTTP çağırışı və status yenilənməsi olacaq.
            var log = new KreditSmsLog
            {
                KreditMuracietId = muracietId,
                Telefon = telefon?.Trim() ?? string.Empty,
                Metn = metn,
                Sablon = sablon,
                Status = KreditSmsStatus.Gozleyir,
                GonderenIsciId = gonderenIsciId,
                YaradanIcraciId = gonderenIsciId,
                YaradilmaTarixi = DateTime.Now
            };
            await _uow.Repository<KreditSmsLog>().YaratAsync(log);
            await _uow.YaddaSaxlaAsync();
            return log;
        }

        public async Task<IList<KreditSmsLog>> MuracietUzreGetirAsync(int muracietId)
        {
            return await _uow.Repository<KreditSmsLog>().Query()
                .Where(x => !x.Silinib && x.KreditMuracietId == muracietId)
                .OrderByDescending(x => x.YaradilmaTarixi)
                .ToListAsync();
        }

        public async Task<IList<KreditSmsLog>> SonGonderilenler(int say = 100)
        {
            return await _uow.Repository<KreditSmsLog>().Query()
                .Where(x => !x.Silinib)
                .OrderByDescending(x => x.YaradilmaTarixi)
                .Take(say)
                .Include(x => x.KreditMuraciet)
                .Include(x => x.GonderenIsci)
                .ToListAsync();
        }
    }
}
