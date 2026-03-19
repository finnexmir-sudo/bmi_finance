
using FinNex.Application.Common.Results;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace FinNex.Application.Interfaces
{
    public interface IServiceAsync<TEntity, TDto, TCreateDto, TUpdateDto>
    where TEntity : class
    where TDto : class
    {
        Task<Result<IList<TDto>>> HamisiniGetirAsync();
      Task<Result<IList<TDto>>> HamisiniGetirAsync(
    Expression<Func<TEntity, bool>>? predicate = null,
    Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
    bool izlemeden = true);
        Task<Result<TDto?>> IdIleGetirAsync(int id);
        Task<Result< TDto>> YaratAsync(TCreateDto dto);
        Task<Result> YenileAsync(TUpdateDto dto);
        Task<Result> SilAsync(int id);  
        Task<Result<bool>> MovcuddurmuAsync(Expression<Func<TEntity, bool>> predicate);

    }
}
