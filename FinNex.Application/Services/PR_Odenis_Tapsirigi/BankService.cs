using AutoMapper;
using FinNex.Application.DTOs.PR_Odenis_Tapsirigi.Bank;
using FinNex.Application.Interfaces.PR_Odenis_Tapsirigi;
using FinNex.Domain.Entities.PR_Odenis_Tapsirigi;
using FinNex.Domain.Interfaces;

namespace FinNex.Application.Services.PR_Odenis_Tapsirigi
{
    public class BankService : ServiceAsync<Bank, BankDetailDto, BankCreateDto, BankUpdateDto>, IBankService
    {
        public BankService(IUnitOfWork unitOfWork, IMapper mapper) : base(unitOfWork, mapper) { }
    }

}
