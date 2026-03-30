using AutoMapper;
using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Maas;  // ← yalnız bir dəfə
using FinNex.Application.DTOs.HR.MaasNovuDtos;
using FinNex.Application.Interfaces.HR;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.HR
{
    public class MaasDetayService
        : ServiceAsync<MaasDetay, DTOs.HR.Maas.MaasDetayDto, CreateMaasDetayDto, UpdateMaasDetayDto>,
          IMaasDetayService
    {
        public MaasDetayService(IUnitOfWork unitOfWork, IMapper mapper)
            : base(unitOfWork, mapper)
        {
        }

        public async Task<Result<IList<DTOs.HR.Maas.MaasDetayDto>>> MaasIdIleGetirAsync(int maasId)
        {
            try
            {
                var entities = await _unitOfWork.Repository<MaasDetay>()
                    .HamisiniGetirAsync(
                        x => x.MaasId == maasId,
                        q => q.Include(x => x.MaasNovu),
                        true);

                var dtos = _mapper.Map<IList<DTOs.HR.Maas.MaasDetayDto>>(entities);
                return Result<IList<DTOs.HR.Maas.MaasDetayDto>>.Ok(dtos);
            }
            catch
            {
                return Result<IList<DTOs.HR.Maas.MaasDetayDto>>.Fail("Maaş detalları gətirilərkən xəta baş verdi.");
            }
        }
    }
}