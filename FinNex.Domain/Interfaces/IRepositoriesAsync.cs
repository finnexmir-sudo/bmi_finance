using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace FinNex.Domain.Interfaces
{
    public interface IRepositoryAsync<T> where T : BaseEntity
    {
        Task<IList<T>> HamisiniGetirAsync(Expression<Func<T, bool>>? predicate = null,
               Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null,
               bool izlemeden = false);

        Task<T?> IdIleGetirAsync(int id);

        Task<T?> GetirAsync(Expression<Func<T, bool>> predicate,
               Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null,
               bool izlemeden = false);

        Task<T> YaratAsync(T entity);

        Task<T> YenileAsync(T entity);

        // Soft Delete üçün bunu DisableAsync əvəzinə belə adlandıra bilərik
        Task<bool> YumshakSilAsync(int id);

        // Əgər bazadan tamamilə silmək lazım olsa (nadir hallarda)
        Task DeleteAsync(int id);

        Task<bool> MovcuddurmuAsync(Expression<Func<T, bool>> predicate);

        IQueryable<T> SorguHazirla(
               Expression<Func<T, bool>>? predicate = null,
               Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null,
               bool izlemeden = false);

        Task<int> SayAsync(Expression<Func<T, bool>>? predicate = null);
    }
}
