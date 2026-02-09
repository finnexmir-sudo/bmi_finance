using FinNex.Application.DTOs.PR_Odenis_Tapsirigi.Bank;
using FinNex.Domain.Entities.PR_Odenis_Tapsirigi;

namespace FinNex.Application.Interfaces.PR_Odenis_Tapsirigi
{
    public interface IBankService : IServiceAsync<Bank, BankDetailDto, BankCreateDto, BankUpdateDto>
    {
    }
}
