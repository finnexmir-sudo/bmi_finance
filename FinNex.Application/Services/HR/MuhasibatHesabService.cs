using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;

namespace FinNex.Application.Services.HR
{
    public class MuhasibatHesabService : IMuhasibatHesabService
    {
        private readonly IUnitOfWork _uow;

        public MuhasibatHesabService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<string?> HesabAlAsync(string acar)
        {
            var hesab = (await _uow.Repository<MuhasibatHesabi>().HamisiniGetirAsync())
                .FirstOrDefault(x => x.Acar == acar && x.Aktiv && !x.Silinib);
            return string.IsNullOrWhiteSpace(hesab?.HesabNomresi) ? null : hesab!.HesabNomresi.Trim();
        }

        public async Task<IList<MuhasibatHesabi>> HamisiAsync()
        {
            var list = await _uow.Repository<MuhasibatHesabi>().HamisiniGetirAsync();
            return list.Where(x => !x.Silinib).OrderBy(x => x.Acar).ToList();
        }

        public async Task UpsertAsync(string acar, string ad, string hesabNomresi, bool aktiv = true)
        {
            var repo = _uow.Repository<MuhasibatHesabi>();
            var mevcud = (await repo.HamisiniGetirAsync())
                .FirstOrDefault(x => x.Acar == acar && !x.Silinib);

            if (mevcud == null)
            {
                await repo.YaratAsync(new MuhasibatHesabi
                {
                    Acar = acar.Trim(),
                    Ad = ad?.Trim() ?? acar,
                    HesabNomresi = hesabNomresi?.Trim() ?? "",
                    Aktiv = aktiv
                });
            }
            else
            {
                mevcud.Ad = ad?.Trim() ?? mevcud.Ad;
                mevcud.HesabNomresi = hesabNomresi?.Trim() ?? "";
                mevcud.Aktiv = aktiv;
                await repo.YenileAsync(mevcud);
            }
            await _uow.YaddaSaxlaAsync();
        }
    }
}
