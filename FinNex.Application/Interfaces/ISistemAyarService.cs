using FinNex.Domain.Entities;

namespace FinNex.Application.Interfaces;

public interface ISistemAyarService
{
    Task<SistemAyar> GetirAsync();
}
