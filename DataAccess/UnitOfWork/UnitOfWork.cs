using FinNex.DataAccess.Contexts;
using FinNex.DataAccess.Repositories;
using FinNex.Domain.Interfaces;
using FinNex.Domain.Entities; // BaseEntity üçün lazımdır
using System.Collections;

namespace FinNex.DataAccess.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private Hashtable _repositories; // Yaradılan repository-ləri keşdə saxlamaq üçün

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    // Repository-ni hər dəfə 'new' etmək əvəzinə, varsa keşdən veririk
    public IRepositoryAsync<T> Repository<T>() where T : BaseEntity
    {
        if (_repositories == null) _repositories = new Hashtable();

        var type = typeof(T).Name;

        if (!_repositories.ContainsKey(type))
        {
            var repositoryType = typeof(EfRepositoryAsync<>);
            var repositoryInstance = Activator.CreateInstance(repositoryType.MakeGenericType(typeof(T)), _context);
            _repositories.Add(type, repositoryInstance);
        }

        return (IRepositoryAsync<T>)_repositories[type]!;
    }

    // Sənin istədiyin Azərbaycanca metod adı
    public async Task<int> YaddaSaxlaAsync()
    {
        return await _context.SaveChangesAsync();
    }

    // Əgər interfeysdə SaveChangesAsync qalıbsa, onu da bura yönləndiririk
    public async Task<int> SaveChangesAsync()
    {
        return await YaddaSaxlaAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}