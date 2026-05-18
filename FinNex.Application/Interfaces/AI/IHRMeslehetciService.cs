using FinNex.Domain.Entities.AI;

namespace FinNex.Application.Interfaces.AI;

public interface IHRMeslehetciService
{
    Task<string> SualSorAsync(string sual, List<HRSohbetMesaj> tarixce);
}
