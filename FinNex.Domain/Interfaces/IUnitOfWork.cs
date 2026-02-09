namespace FinNex.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepositoryAsync<T> Repository<T>() where T : BaseEntity;
    Task<int> YaddaSaxlaAsync(); // Bu, SaveChangesAsync-i çağıracaq
}
