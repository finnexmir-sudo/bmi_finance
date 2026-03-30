using AutoMapper;
using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.SenedDovriyyesi.Icaze;
using FinNex.Application.Interfaces.SenedDovriyyesi;
using FinNex.Domain.Entities.SenedDovriyyesi;
using FinNex.Domain.Interfaces;

namespace FinNex.Application.Services.SenedDovriyyesi
{
    public class SenedDovriyyesiIstifadeciIcazesiService
        : ServiceAsync<
            SenedDovriyyesiIstifadeciIcazesi,
            SenedDovriyyesiIstifadeciIcazesiDto,
            SenedDovriyyesiIstifadeciIcazesiCreateDto,
            SenedDovriyyesiIstifadeciIcazesiUpdateDto>,
          ISenedDovriyyesiIstifadeciIcazesiService
    {
        public SenedDovriyyesiIstifadeciIcazesiService(IUnitOfWork unitOfWork, IMapper mapper)
            : base(unitOfWork, mapper)
        {
        }

        public async Task<Result<IList<SenedDovriyyesiIstifadeciIcazesiDto>>> IstifadeciyeGoreGetirAsync(int istifadeciId)
        {
            try
            {
                var entities = await _unitOfWork
                    .Repository<SenedDovriyyesiIstifadeciIcazesi>()
                    .HamisiniGetirAsync(x => x.IstifadeciId == istifadeciId && !x.Silinib);

                var data = _mapper.Map<IList<SenedDovriyyesiIstifadeciIcazesiDto>>(entities);

                return Result<IList<SenedDovriyyesiIstifadeciIcazesiDto>>.Ok(data);
            }
            catch
            {
                return Result<IList<SenedDovriyyesiIstifadeciIcazesiDto>>.Fail("İstifadəçi icazələri gətirilərkən xəta baş verdi.");
            }
        }

        public async Task<Result<bool>> BaxaBilermiAsync(int istifadeciId, int sobeId)
        {
            try
            {
                var varmi = await _unitOfWork
                    .Repository<SenedDovriyyesiIstifadeciIcazesi>()
                    .MovcuddurmuAsync(x =>
                        x.IstifadeciId == istifadeciId &&
                        x.SobeId == sobeId &&
                        !x.Silinib &&
                        (x.IcazeNovu == IcazeNovu.View || x.IcazeNovu == IcazeNovu.Full));

                return Result<bool>.Ok(varmi);
            }
            catch
            {
                return Result<bool>.Fail("Baxış icazəsi yoxlanılarkən xəta baş verdi.");
            }
        }

        public async Task<Result<bool>> TamIcazesiVarmiAsync(int istifadeciId, int sobeId)
        {
            try
            {
                var varmi = await _unitOfWork
                    .Repository<SenedDovriyyesiIstifadeciIcazesi>()
                    .MovcuddurmuAsync(x =>
                        x.IstifadeciId == istifadeciId &&
                        x.SobeId == sobeId &&
                        !x.Silinib &&
                        x.IcazeNovu == IcazeNovu.Full);

                return Result<bool>.Ok(varmi);
            }
            catch
            {
                return Result<bool>.Fail("Tam icazə yoxlanılarkən xəta baş verdi.");
            }
        }
    }
}