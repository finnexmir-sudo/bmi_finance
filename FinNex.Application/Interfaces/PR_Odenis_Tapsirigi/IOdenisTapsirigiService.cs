using FinNex.Application.DTOs.PR_Odenis_Tapsirigi.OdenisTapsirigi;
using FinNex.Domain.Entities.PR_Odenis_Tapsirigi;

namespace FinNex.Application.Interfaces.PR_Odenis_Tapsirigi
{
    public interface IOdenisTapsirigiService : IServiceAsync<OdenisTapsirigi, OdenisTapsirigiDetailDto, OdenisTapsirigiCreateDto, OdenisTapsirigiUpdateDto>
    {
    }
}
