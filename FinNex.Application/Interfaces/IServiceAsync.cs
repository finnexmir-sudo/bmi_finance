
using FinNex.Application.Common.Results;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace FinNex.Application.Interfaces
{
    public interface IServiceAsync<TEntity, TDto, TCreateDto, TUpdateDto>
    where TEntity : class
    where TDto : class
    {
        Task<Result<IList<TDto>>> GetAllAsync();
        Task<Result<IList<TDto>>> GetAllAsync(
    Expression<Func<TEntity, bool>>? predicate = null,
    Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
    bool izlemeden = true);
        Task<Result<TDto?>> GetByIdAsync(int id);
        Task<Result< TDto>> AddAsync(TCreateDto dto);
        Task<Result> UpdateAsync(TUpdateDto dto);
        Task<Result> SoftDeleteAsync(int id);  
        Task<Result<bool>> ExistsAsync(Expression<Func<TEntity, bool>> predicate);

    }
}
