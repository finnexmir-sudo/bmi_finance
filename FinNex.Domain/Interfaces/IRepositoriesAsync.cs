using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace FinNex.Domain.Interfaces
{
    public interface IRepositoryAsync<T> where T : BaseEntity
    {
        Task<IList<T>> GetAllAsync(Expression<Func<T, bool>>? predicate = null,
               Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null,
               bool izlemeden = false);

        Task<T?> GetByIdAsync(int id);

        Task<T?> GetirAsync(Expression<Func<T, bool>> predicate,
               Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null,
               bool izlemeden = false);

        Task<T> AddAsync(T entity);

        Task<T> UpdateAsync(T entity);

        // Soft Delete üçün bunu DisableAsync əvəzinə belə adlandıra bilərik
        Task<bool> SoftDeleteAsync(int id);

        // Əgər bazadan tamamilə silmək lazım olsa (nadir hallarda)
        Task DeleteAsync(int id);

        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);

        IQueryable<T> SorguHazirla(
               Expression<Func<T, bool>>? predicate = null,
               Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null,
               bool izlemeden = false);

        Task<int> SayAsync(Expression<Func<T, bool>>? predicate = null);
        IQueryable<T> Query();
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);


    }
}
