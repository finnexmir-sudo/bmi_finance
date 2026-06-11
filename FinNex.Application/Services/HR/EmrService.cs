using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;

namespace FinNex.Application.Services.HR
{
    public class EmrService : IEmrService
    {
        private readonly IUnitOfWork _uow;

        public EmrService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // (Nov, Il) sayğacını artırır. Sayğac yalnız artır, mövcud Emr sətirlərinə
        // baxmır — ona görə silinmə-təkrarı problemi yoxdur. İl dəyişəndə avtomatik
        // 1-dən başlayır (həmin il üçün sayğac sətri olmadığı üçün).
        public async Task<(int nomre, int il)> NovbetiNomreAsync(EmrNovu nov, DateTime? tarix = null)
        {
            int il = (tarix ?? DateTime.Now).Year;
            var repo = _uow.Repository<EmrSayghaci>();

            var saygac = (await repo.HamisiniGetirAsync())
                .FirstOrDefault(x => x.Nov == nov && x.Il == il);

            if (saygac == null)
            {
                saygac = new EmrSayghaci { Nov = nov, Il = il, SonNomre = 1 };
                await repo.YaratAsync(saygac);
            }
            else
            {
                saygac.SonNomre++;
                await repo.YenileAsync(saygac);
            }

            await _uow.YaddaSaxlaAsync();
            return (saygac.SonNomre, il);
        }

        public async Task<Emr> YeniEmrAsync(EmrNovu nov, int? elaqeliEntityId = null,
            DateTime? tarix = null, string? metn = null, int? verenIsciId = null)
        {
            var t = tarix ?? DateTime.Now;
            var (nomre, il) = await NovbetiNomreAsync(nov, t);

            var emr = new Emr
            {
                Nov             = nov,
                Nomre           = nomre,
                Il              = il,
                Tarix           = t,
                ElaqeliEntityId = elaqeliEntityId,
                Metn            = metn,
                VerenIsciId     = verenIsciId
            };
            await _uow.Repository<Emr>().YaratAsync(emr);
            await _uow.YaddaSaxlaAsync();
            return emr;
        }

        // HR köhnə əmrlərin davamı üçün son nömrəni təyin edir.
        // Növbəti generasiya (SonNomre + 1) buradan davam edir.
        public async Task SonNomreTeyinEtAsync(EmrNovu nov, int il, int sonNomre)
        {
            if (sonNomre < 0) sonNomre = 0;
            var repo = _uow.Repository<EmrSayghaci>();

            var saygac = (await repo.HamisiniGetirAsync())
                .FirstOrDefault(x => x.Nov == nov && x.Il == il);

            if (saygac == null)
            {
                saygac = new EmrSayghaci { Nov = nov, Il = il, SonNomre = sonNomre };
                await repo.YaratAsync(saygac);
            }
            else
            {
                saygac.SonNomre = sonNomre;
                await repo.YenileAsync(saygac);
            }

            await _uow.YaddaSaxlaAsync();
        }

        public async Task<IList<EmrSayghaci>> SayghaclarAsync()
        {
            var list = await _uow.Repository<EmrSayghaci>().HamisiniGetirAsync();
            return list.OrderByDescending(x => x.Il).ThenBy(x => x.Nov).ToList();
        }
    }
}
