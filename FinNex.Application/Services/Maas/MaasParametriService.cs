using AutoMapper;
using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.MaasNovuDtos;
using FinNex.Application.Interfaces.HR;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;

namespace FinNex.Application.Services.HR
{
    public class MaasParametriService
        : ServiceAsync<MaasParametri, MaasParametriDto, CreateMaasParametriDto, UpdateMaasParametriDto>,
          IMaasParametriService
    {
        public MaasParametriService(IUnitOfWork unitOfWork, IMapper mapper)
            : base(unitOfWork, mapper)
        {
        }

        public async Task<Result<IList<MaasParametriDto>>> AktivOlanlariGetirAsync()
        {
            try
            {
                var entities = await _unitOfWork.Repository<MaasParametri>()
                    .HamisiniGetirAsync(x => x.Aktivdir, izlemeden: true);

                var dtos = _mapper.Map<IList<MaasParametriDto>>(entities);
                return Result<IList<MaasParametriDto>>.Ok(dtos);
            }
            catch
            {
                return Result<IList<MaasParametriDto>>.Fail("Aktiv maaş parametrləri gətirilərkən xəta baş verdi.");
            }
        }

        public async Task<Result<MaasParametriDto?>> NoveGoreGetirAsync(MaasParametrNovu nov, DateTime tarix)
        {
            try
            {
                var entity = await _unitOfWork.Repository<MaasParametri>()
                    .GetirAsync(x =>
                        x.Nov == nov &&
                        x.Aktivdir &&
                        x.BaslamaTarixi <= tarix &&
                        (x.BitmeTarixi == null || x.BitmeTarixi >= tarix),
                        izlemeden: true);

                if (entity == null)
                    return Result<MaasParametriDto?>.Fail("Uyğun maaş parametri tapılmadı.");

                var dto = _mapper.Map<MaasParametriDto>(entity);
                return Result<MaasParametriDto?>.Ok(dto);
            }
            catch
            {
                return Result<MaasParametriDto?>.Fail("Maaş parametri gətirilərkən xəta baş verdi.");
            }
        }
    }
}