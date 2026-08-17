using AutoMapper;
using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Structur;
using FinNex.Application.DTOs.User;
using FinNex.Application.Interfaces.Structur;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Entities.Structure;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.Structur
{
    public class DepartmentService
    : ServiceAsync<Departament, DepartmentListDto, DepartmentCreateDto, DepartmentUpdateDto>,
      IDepartmentService
    {
        public DepartmentService(IUnitOfWork unitOfWork, IMapper mapper)
            : base(unitOfWork, mapper)
        {
        }

        public async Task AssignAsync(UserDepartmentCreateDto dto)
        {
            var exists = await _unitOfWork
                .Repository<UserDepartment>()
                .AnyAsync(x =>
                    x.UserId == dto.UserId &&
                    x.DepartmentId == dto.DepartmentId);

            if (exists)
                throw new Exception("User already assigned to this department.");

            var entity = _mapper.Map<UserDepartment>(dto);
            await _unitOfWork.Repository<UserDepartment>().YaratAsync(entity);
            await _unitOfWork.YaddaSaxlaAsync();
        }

        // ── İŞÇİ SAYI: AKTİV TƏYİNAT = `Aktivdir` (17.08.2026) ───────────────
        // Əvvəl şərt `BitmeTarixi == null` idi. `IsciService.TeyinatRedakteEtAsync`
        // redaktədə `BitmeTarixi`-ni formadan yazır, `Aktivdir`-ə toxunmur → sətir
        // `Aktivdir=1` VƏ `BitmeTarixi=<tarix>` qalır. Bazada 29 təyinatın 22-si belə
        // idi → departamentlərin çoxu "0 işçi" görünürdü, halbuki İşçilər siyahısı
        // (Aktivdir ilə işləyir) hamısını göstərirdi. `BitmeTarixi` planlaşdırılmış
        // bitmə tarixidir — sətrin bitdiyini SÜBUT ETMİR.
        //
        // Şərtin dörd hissəsi də lazımdır:
        //   t.Aktivdir            → cari təyinat (köhnəsi say=ikiqat olmasın)
        //   !t.Silinib            → yumşaq silinmiş təyinat sayılmasın (əvvəl YOX idi)
        //   t.Isci.Status==Aktiv  → çıxmış işçi sayılmasın (təyinat avtomatik bağlanmır)
        //   !t.Isci.Silinib       → silinmiş işçi sayılmasın
        // Eyni şərt OrganizasiyaController və VezifeService-də təkrarlanır — birini
        // dəyişəndə o birilərini də tutuşdur (say = siyahı qaydası).

        // Parametrsiz HamisiniGetirAsync — IsciSayi IsciTeyinat-dan hesablanır
        public override async Task<Result<IList<DepartmentListDto>>> HamisiniGetirAsync()
        {
            var data = await _unitOfWork
                .Repository<Departament>()
                .Query()
                .Where(d => !d.Silinib)
                .Select(d => new DepartmentListDto
                {
                    Id = d.Id,
                    Ad = d.Ad,
                    Aciqlama = d.Aciqlama,
                    IsciSayi = d.IsciTeyinatlar
                        .Count(t => t.Aktivdir && !t.Silinib
                                 && t.Isci.Status == IsciStatus.Aktiv && !t.Isci.Silinib)
                })
                .ToListAsync();

            return Result<IList<DepartmentListDto>>.Ok(data);
        }

        // Predicate ilə HamisiniGetirAsync — icazəli şöbə filtri üçün
        public override async Task<Result<IList<DepartmentListDto>>> HamisiniGetirAsync(
            System.Linq.Expressions.Expression<Func<Departament, bool>>? predicate = null,
            Func<IQueryable<Departament>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Departament, object>>? include = null,
            bool izlemeden = true)
        {
            var query = _unitOfWork
                .Repository<Departament>()
                .Query()
                .Where(d => !d.Silinib);

            if (predicate != null)
                query = query.Where(predicate);

            var data = await query
                .Select(d => new DepartmentListDto
                {
                    Id = d.Id,
                    Ad = d.Ad,
                    Aciqlama = d.Aciqlama,
                    IsciSayi = d.IsciTeyinatlar
                        .Count(t => t.Aktivdir && !t.Silinib
                                 && t.Isci.Status == IsciStatus.Aktiv && !t.Isci.Silinib)
                })
                .ToListAsync();

            return Result<IList<DepartmentListDto>>.Ok(data);
        }

        public async Task<Result<List<DepartmentListDto>>> GetAllWithEmployeeCountAsync()
        {
            var data = await _unitOfWork
                .Repository<Departament>()
                .Query()
                .Where(d => !d.Silinib)
                .Select(d => new DepartmentListDto
                {
                    Id = d.Id,
                    Ad = d.Ad,
                    Aciqlama = d.Aciqlama,
                    IsciSayi = d.IsciTeyinatlar
                        .Count(t => t.Aktivdir && !t.Silinib
                                 && t.Isci.Status == IsciStatus.Aktiv && !t.Isci.Silinib)
                })
                .ToListAsync();

            return Result<List<DepartmentListDto>>.Ok(data);
        }
    }
}