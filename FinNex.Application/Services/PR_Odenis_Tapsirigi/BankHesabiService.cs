using AutoMapper;
using FinNex.Application.DTOs.PR_Odenis_Tapsirigi.BankHesabi;
using FinNex.Application.Interfaces.PR_Odenis_Tapsirigi;
using FinNex.Domain.Entities.PR_Odenis_Tapsirigi;
using FinNex.Domain.Interfaces;

namespace FinNex.Application.Services.PR_Odenis_Tapsirigi
{
    public class BankHesabiService : ServiceAsync<BankHesabi, BankHesabiDetailDto, BankHesabiCreateDto, BankHesabiUpdateDto>, IBankHesabiService
    {
        public BankHesabiService(IUnitOfWork unitOfWork, IMapper mapper) : base(unitOfWork, mapper) { }
    }
}
