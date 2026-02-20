using FinNex.Application.DTOs.PR_Odenis_Tapsirigi.BankHesabi;
using FinNex.Domain.Entities.PR_Odenis_Tapsirigi;

namespace FinNex.Application.Interfaces.PR_Odenis_Tapsirigi
{
    public interface IBankHesabiService : IServiceAsync<BankHesabi, BankHesabiDetailDto, BankHesabiCreateDto, BankHesabiUpdateDto>
    {
    }
}
