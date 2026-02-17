using AutoMapper;
using FinNex.Application.Common.Results;
using FinNex.Application.Interfaces;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace FinNex.Application.Services
{
    public class ServiceAsync<TEntity, TDto, TCreateDto, TUpdateDto> 
        : IServiceAsync<TEntity, TDto, TCreateDto, TUpdateDto>
        where TEntity : BaseEntity
        where TDto : class
    {
        protected readonly IUnitOfWork _unitOfWork;
        protected readonly IMapper _mapper;

        public ServiceAsync(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Result<IList<TDto>>> GetAllAsync()
        {
            try
            {
                var entities = await _unitOfWork
                    .Repository<TEntity>()
                    .GetAllAsync(izlemeden: true);

                var data = _mapper.Map<IList<TDto>>(entities);

                return Result<IList<TDto>>.Ok(data);
            }
            catch (Exception ex)
            {
                return Result<IList<TDto>>.Fail("Məlumatlar gətirilərkən xəta baş verdi.");
            }
        }

        public async Task<Result<IList<TDto>>> GetAllAsync(
            Expression<Func<TEntity, bool>>? predicate = null,
            Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
            bool izlemeden = true)
        {
            try
            {
                var entities = await _unitOfWork
                    .Repository<TEntity>()
                    .GetAllAsync(predicate, include, izlemeden);

                var data = _mapper.Map<IList<TDto>>(entities);

                return Result<IList<TDto>>.Ok(data);
            }
            catch
            {
                return Result<IList<TDto>>.Fail("Filtrləmə zamanı xəta baş verdi.");
            }
        }

        public async Task<Result<TDto?>> GetByIdAsync(int id)
        {
            try
            {
                var entity = await _unitOfWork
                    .Repository<TEntity>()
                    .GetByIdAsync(id);

                if (entity == null)
                    return Result<TDto?>.Fail("Tapılmadı.");

                var data = _mapper.Map<TDto>(entity);

                return Result<TDto?>.Ok(data);
            }
            catch
            {
                return Result<TDto?>.Fail("Məlumat gətirilərkən xəta baş verdi.");
            }
        }

        public virtual async Task<Result<TDto>> AddAsync(TCreateDto dto)
        {
            try
            {
                var entity = _mapper.Map<TEntity>(dto);

                await _unitOfWork.Repository<TEntity>().AddAsync(entity);
                await _unitOfWork.YaddaSaxlaAsync();

                var data = _mapper.Map<TDto>(entity);

                return Result<TDto>.Ok(data, "Uğurla yaradıldı.");
            }
            catch
            {
                return Result<TDto>.Fail("Yaradılma zamanı xəta baş verdi.");
            }
        }

        public async Task<Result> UpdateAsync(TUpdateDto dto)
        {
            try
            {
                var entity = _mapper.Map<TEntity>(dto);

                await _unitOfWork.Repository<TEntity>().UpdateAsync(entity);
                await _unitOfWork.YaddaSaxlaAsync();

                return Result.Ok("Uğurla yeniləndi.");
            }
            catch
            {
                return Result.Fail("Yenilənmə zamanı xəta baş verdi.");
            }
        }

        public async Task<Result> SoftDeleteAsync(int id)
        {
            try
            {
                await _unitOfWork.Repository<TEntity>().SoftDeleteAsync(id);
                await _unitOfWork.YaddaSaxlaAsync();

                return Result.Ok("Uğurla silindi.");
            }
            catch
            {
                return Result.Fail("Silinmə zamanı xəta baş verdi.");
            }
        }

        public async Task<Result<bool>> ExistsAsync(Expression<Func<TEntity, bool>> predicate)
        {
            try
            {
                var exists = await _unitOfWork
                    .Repository<TEntity>()
                    .ExistsAsync(predicate);

                return Result<bool>.Ok(exists);
            }
            catch
            {
                return Result<bool>.Fail("Yoxlama zamanı xəta baş verdi.");
            }
        }
    }
}
