using AutoMapper;
using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.IsciStrukturRolu;
using FinNex.Application.Interfaces;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.HR
{
    public class IsciStrukturRoluService
        : ServiceAsync<IsciStrukturRolu, IsciStrukturRoluDto, IsciStrukturRoluCreateDto, IsciStrukturRoluUpdateDto>,
          IIsciStrukturRoluService
    {
        public IsciStrukturRoluService(IUnitOfWork unitOfWork, IMapper mapper)
            : base(unitOfWork, mapper) { }

        public async Task<Result<IList<IsciStrukturRoluDto>>> GetByIsciIdAsync(int isciId)
        {
            try
            {
                var list = await _unitOfWork.Repository<IsciStrukturRolu>()
                    .HamisiniGetirAsync(
                        x => x.IsciId == isciId,
                        q => q.Include(x => x.Isci)
                              .Include(x => x.Departament),
                        true);

                return Result<IList<IsciStrukturRoluDto>>.Ok(_mapper.Map<IList<IsciStrukturRoluDto>>(list));
            }
            catch
            {
                return Result<IList<IsciStrukturRoluDto>>.Fail("Struktur rolları gətirilərkən xəta baş verdi.");
            }
        }

        public async Task<Result<IList<IsciStrukturRoluDto>>> GetByRolAsync(StrukturRolTipi rolTipi)
        {
            try
            {
                var list = await _unitOfWork.Repository<IsciStrukturRolu>()
                    .HamisiniGetirAsync(
                        x => x.RolTipi == rolTipi && x.Aktivdir,
                        q => q.Include(x => x.Isci)
                              .Include(x => x.Departament),
                        true);

                return Result<IList<IsciStrukturRoluDto>>.Ok(_mapper.Map<IList<IsciStrukturRoluDto>>(list));
            }
            catch
            {
                return Result<IList<IsciStrukturRoluDto>>.Fail("Rol üzrə məlumatlar gətirilərkən xəta baş verdi.");
            }
        }

        public async Task<Result<IList<IsciStrukturRoluDto>>> GetByDepartamentRolAsync(int departamentId, StrukturRolTipi rolTipi)
        {
            try
            {
                var list = await _unitOfWork.Repository<IsciStrukturRolu>()
                    .HamisiniGetirAsync(
                        x => x.DepartamentId == departamentId && x.RolTipi == rolTipi && x.Aktivdir,
                        q => q.Include(x => x.Isci)
                              .Include(x => x.Departament),
                        true);

                return Result<IList<IsciStrukturRoluDto>>.Ok(_mapper.Map<IList<IsciStrukturRoluDto>>(list));
            }
            catch
            {
                return Result<IList<IsciStrukturRoluDto>>.Fail("Departament rol məlumatı gətirilərkən xəta baş verdi.");
            }
        }
    }
}