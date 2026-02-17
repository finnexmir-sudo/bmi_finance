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
            int cariIl = DateTime.Now.Year;

            // ⚠️ Cari il üçün sayğacı tap
            var saygac = (await repo.GetAllAsync())
                            .FirstOrDefault(x => x.Il == cariIl);

            if (saygac == null)
            {
                // 🔹 İl dəyişibsə → 1-dən başla
                saygac = new OdenisTapsirigiNomresi
                {
                    Il = cariIl,
                    SonNomre = 1
                };

                await repo.AddAsync(saygac);
            }
            else
            {
                // 🔹 Eyni il → artır
                saygac.SonNomre++;
                await repo.UpdateAsync(saygac);
            }

            await _unitOfWork.YaddaSaxlaAsync();

            return saygac.SonNomre;
        }

    }

}
