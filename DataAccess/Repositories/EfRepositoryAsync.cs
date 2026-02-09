using FinNex.Domain.Entities;
using FinNex.DataAccess.Contexts;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace FinNex.DataAccess.Repositories;

public class EfRepositoryAsync<T> : IRepositoryAsync<T> where T : BaseEntity
{
    protected readonly AppDbContext _context;
    private readonly DbSet<T> _dbSet;

    public EfRepositoryAsync(AppDbContext context)
    {
        _context = context;
        _dbSet = _context.Set<T>();
    }

    public async Task<IList<T>> HamisiniGetirAsync(Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null,
        bool izlemeden = false)
    {
        IQueryable<T> sorgu = _dbSet.Where(x => !x.Silinib);

        if (izlemeden) sorgu = sorgu.AsNoTracking();
        if (include != null) sorgu = include(sorgu);
        if (predicate != null) sorgu = sorgu.Where(predicate);

        return await sorgu.ToListAsync();
    }

    public async Task<T?> IdIleGetirAsync(int id)
    {
        return await _dbSet.FirstOrDefaultAsync(x => x.Id == id && !x.Silinib);
    }

    public async Task<T?> GetirAsync(Expression<Func<T, bool>> predicate,
        Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null,
        bool izlemeden = false)
    {
        IQueryable<T> sorgu = _dbSet.Where(x => !x.Silinib);

        if (izlemeden) sorgu = sorgu.AsNoTracking();
        if (include != null) sorgu = include(sorgu);

        return await sorgu.FirstOrDefaultAsync(predicate);
    }

    public async Task<T> YaratAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        return entity;
    }

    public async Task<T> YenileAsync(T entity)
    {
        _dbSet.Update(entity);
        return await Task.FromResult(entity);
    }

    public async Task<bool> YumshakSilAsync(int id)
    {
        var entity = await _dbSet.FindAsync(id);
        if (entity == null) return false;

        entity.Silinib = true;
        entity.SilinmeTarixi = DateTime.Now;

        _dbSet.Update(entity);
        return true;
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _dbSet.FindAsync(id);
        if (entity != null)
        {
            _dbSet.Remove(entity);
        }
    }

    public async Task<bool> MovcuddurmuAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.AnyAsync(predicate);
    }

    public IQueryable<T> SorguHazirla(Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null,
        bool izlemeden = false)
    {
        IQueryable<T> sorgu = _dbSet.Where(x => !x.Silinib);

        if (izlemeden) sorgu = sorgu.AsNoTracking();
        if (include != null) sorgu = include(sorgu);
        if (predicate != null) sorgu = sorgu.Where(predicate);

        return sorgu;
    }

    public async Task<int> SayAsync(Expression<Func<T, bool>>? predicate = null)
    {
        if (predicate != null)
            return await _dbSet.CountAsync(predicate);

        return await _dbSet.CountAsync(x => !x.Silinib);
    }
}