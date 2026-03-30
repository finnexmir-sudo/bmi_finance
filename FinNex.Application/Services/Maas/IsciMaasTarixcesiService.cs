using AutoMapper;
using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Isci;
using FinNex.Application.DTOs.HR.Maas;
using FinNex.Application.Interfaces.HR;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;

namespace FinNex.Application.Services.HR
{
    public class IsciMaasTarixcesiService
        : ServiceAsync<IsciMaasTarixcesi, DTOs.HR.Isci.IsciMaasTarixcesiDto, CreateIsciMaasTarixcesiDto, UpdateIsciMaasTarixcesiDto>,
          IIsciMaasTarixcesiService
    {
        public IsciMaasTarixcesiService(IUnitOfWork unitOfWork, IMapper mapper)
            : base(unitOfWork, mapper)
        {
        }

        public async Task<Result<IList<DTOs.HR.Isci.IsciMaasTarixcesiDto>>> IsciyeGoreGetirAsync(int isciId)
        {
            try
            {
                var entities = await _unitOfWork.Repository<IsciMaasTarixcesi>()
                    .HamisiniGetirAsync(x => x.IsciId == isciId, izlemeden: true);

                var dtos = _mapper.Map<IList<DTOs.HR.Isci.IsciMaasTarixcesiDto>>(entities);
                return Result<IList<DTOs.HR.Isci.IsciMaasTarixcesiDto>>.Ok(dtos);
            }
            catch
            {
                return Result<IList<DTOs.HR.Isci.IsciMaasTarixcesiDto>>.Fail("İşçi maaş tarixçəsi gətirilərkən xəta baş verdi.");
            }
        }
    }
}