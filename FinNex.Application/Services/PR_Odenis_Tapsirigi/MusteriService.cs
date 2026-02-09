using AutoMapper;
using FinNex.Application.DTOs.PR_Odenis_Tapsirigi.Musteri;
using FinNex.Application.Interfaces.PR_Odenis_Tapsirigi;
using FinNex.Domain.Entities.PR_Odenis_Tapsirigi;
using FinNex.Domain.Interfaces;

namespace FinNex.Application.Services.PR_Odenis_Tapsirigi
{
    public class MusteriService : ServiceAsync<Musteri, MusteriDetailDto, MusteriCreateDto, MusteriUpdateDto>, IMusteriService
    {
        public MusteriService(IUnitOfWork unitOfWork, IMapper mapper) : base(unitOfWork, mapper) { }
    }
}
