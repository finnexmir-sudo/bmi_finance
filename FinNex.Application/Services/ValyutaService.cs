
using AutoMapper;
using FinNex.Application.DTOs;
using FinNex.Application.Interfaces.PR_Odenis_Tapsirigi;
using FinNex.Domain.Entities;
using FinNex.Domain.Interfaces;

namespace FinNex.Application.Services
{
    public class ValyutaService : IValyutaService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ValyutaService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<List<ValyutaListDto>> GetAktivAsync()
        {
            var list = await _unitOfWork.Repository<Valyuta>()
                .GetAllAsync(x => x.Silinib == false);

            return _mapper.Map<List<ValyutaListDto>>(list);
        }

    }

}
