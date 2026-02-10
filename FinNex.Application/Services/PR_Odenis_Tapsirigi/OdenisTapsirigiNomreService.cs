using FinNex.Application.Interfaces.PR_Odenis_Tapsirigi;
using FinNex.Domain.Entities.PR_Odenis_Tapsirigi;
using FinNex.Domain.Interfaces;

namespace FinNex.Application.Services.PR_Odenis_Tapsirigi
{
    public class OdenisTapsirigiNomreService : IOdenisTapsirigiNomreService
    {
        private readonly IUnitOfWork _unitOfWork;

        public OdenisTapsirigiNomreService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<int> NovbetiNomreAlAsync()
        {
            var repo = _unitOfWork.Repository<OdenisTapsirigiNomresi>();

            var saygac = (await repo.HamisiniGetirAsync())
                            .FirstOrDefault();

            if (saygac == null)
            {
                saygac = new OdenisTapsirigiNomresi
                {
                    SonNomre = 1
                };

                await repo.YaratAsync(saygac);
            }
            else
            {
                saygac.SonNomre++;
                await repo.YenileAsync(saygac);
            }

            await _unitOfWork.YaddaSaxlaAsync();

            return saygac.SonNomre;
        }
    }

}
