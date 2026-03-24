using AutoMapper;
using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.MaasNovuDtos;
using FinNex.Application.Interfaces.HR;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;

namespace FinNex.Application.Services.HR
{
    public class MaasNovuService
        : ServiceAsync<MaasNovu, MaasNovuDto, CreateMaasNovuDto, UpdateMaasNovuDto>,
          IMaasNovuService
    {
        public MaasNovuService(IUnitOfWork unitOfWork, IMapper mapper)
            : base(unitOfWork, mapper)
        {
        }

        public async Task<Result<IList<MaasNovuDto>>> AktivOlanlariGetirAsync()
        {
            try
            {
                var entities = await _unitOfWork.Repository<MaasNovu>()
                    .HamisiniGetirAsync(x => x.Aktivdir, izlemeden: true);

                var dtos = _mapper.Map<IList<MaasNovuDto>>(entities);
                return Result<IList<MaasNovuDto>>.Ok(dtos);
            }
            catch
            {
                return Result<IList<MaasNovuDto>>.Fail("Aktiv maaş növləri gətirilərkən xəta baş verdi.");
            }
        }
    }
}