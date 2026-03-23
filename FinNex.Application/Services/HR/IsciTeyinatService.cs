using AutoMapper;
using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.IsciTeyinat;
using FinNex.Application.Interfaces;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.HR
{
    public class IsciTeyinatService
        : ServiceAsync<IsciTeyinat, IsciTeyinatDto, IsciTeyinatCreateDto, IsciTeyinatUpdateDto>,
          IIsciTeyinatService
    {
        public IsciTeyinatService(IUnitOfWork unitOfWork, IMapper mapper)
            : base(unitOfWork, mapper) { }

        public async Task<Result<IList<IsciTeyinatDto>>> GetByIsciIdAsync(int isciId)
        {
            try
            {
                var list = await _unitOfWork.Repository<IsciTeyinat>()
                    .HamisiniGetirAsync(
                        x => x.IsciId == isciId,
                        q => q.Include(x => x.Isci)
                              .Include(x => x.Departament)
                              .Include(x => x.Vezife),
                        true);

                return Result<IList<IsciTeyinatDto>>.Ok(_mapper.Map<IList<IsciTeyinatDto>>(list));
            }
            catch
            {
                return Result<IList<IsciTeyinatDto>>.Fail("İşçi təyinatları gətirilərkən xəta baş verdi.");
            }
        }

        public async Task<Result<IsciTeyinatDto?>> GetAktivTeyinatAsync(int isciId)
        {
            try
            {
                var entity = await _unitOfWork.Repository<IsciTeyinat>()
                    .GetirAsync(
                        x => x.IsciId == isciId && x.Aktivdir,
                        q => q.Include(x => x.Isci)
                              .Include(x => x.Departament)
                              .Include(x => x.Vezife));

                if (entity == null)
                    return Result<IsciTeyinatDto?>.Fail("Aktiv təyinat tapılmadı.");

                return Result<IsciTeyinatDto?>.Ok(_mapper.Map<IsciTeyinatDto>(entity));
            }
            catch
            {
                return Result<IsciTeyinatDto?>.Fail("Aktiv təyinat gətirilərkən xəta baş verdi.");
            }
        }
    }
}