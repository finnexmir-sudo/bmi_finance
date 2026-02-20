using AutoMapper;
using FinNex.Application.DTOs.PR_Odenis_Tapsirigi.OdenisTapsirigi;
using FinNex.Application.Interfaces.PR_Odenis_Tapsirigi;
using FinNex.Domain.Entities.PR_Odenis_Tapsirigi;
using FinNex.Domain.Interfaces;

namespace FinNex.Application.Services.PR_Odenis_Tapsirigi
{
    public class OdenisTapsirigiService : ServiceAsync<OdenisTapsirigi, OdenisTapsirigiDetailDto, OdenisTapsirigiCreateDto, OdenisTapsirigiUpdateDto>, IOdenisTapsirigiService
    {
        public OdenisTapsirigiService(IUnitOfWork unitOfWork, IMapper mapper) : base(unitOfWork, mapper) { }

        // Nümunə: Əgər xüsusi bir məntiq lazımdırsa bura əlavə edilir
        // public async Task TesdiqleAsync(int id) { ... }
    }
}
