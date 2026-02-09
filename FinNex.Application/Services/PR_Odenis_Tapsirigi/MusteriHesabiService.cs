using AutoMapper;
using FinNex.Application.DTOs.PR_Odenis_Tapsirigi.MusteriHesabi;
using FinNex.Application.Interfaces.PR_Odenis_Tapsirigi;
using FinNex.Domain.Entities.PR_Odenis_Tapsirigi;
using FinNex.Domain.Interfaces;

namespace FinNex.Application.Services.PR_Odenis_Tapsirigi
{
    public class MusteriHesabiService : ServiceAsync<MusteriHesabi, MusteriHesabiDetailDto, MusteriHesabiCreateDto, MusteriHesabiUpdateDto>, IMusteriHesabiService
    {
        public MusteriHesabiService(IUnitOfWork unitOfWork, IMapper mapper) : base(unitOfWork, mapper) { }
    }
}
