using AutoMapper;
using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.IsciMaliyeDtos;
using FinNex.Application.Interfaces.HR;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.HR
{
    public class IsciMaliyeService
        : ServiceAsync<IsciMaliye, IsciMaliyeDto, CreateIsciMaliyeDto, UpdateIsciMaliyeDto>,
          IIsciMaliyeService
    {
        public IsciMaliyeService(IUnitOfWork unitOfWork, IMapper mapper)
            : base(unitOfWork, mapper)
        {
        }

        public async Task<Result<IsciMaliyeDto?>> IsciIdIleGetirAsync(int isciId)
        {
            try
            {
                var entity = await _unitOfWork.Repository<IsciMaliye>()
                    .GetirAsync(x => x.IsciId == isciId, izlemeden: true);

                if (entity == null)
                    return Result<IsciMaliyeDto?>.Fail("İşçi maliyyə məlumatı tapılmadı.");

                var dto = _mapper.Map<IsciMaliyeDto>(entity);
                return Result<IsciMaliyeDto?>.Ok(dto);
            }
            catch
            {
                return Result<IsciMaliyeDto?>.Fail("İşçi maliyyə məlumatı gətirilərkən xəta baş verdi.");
            }
        }
    }
}