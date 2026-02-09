using AutoMapper;
using FinNex.Application.DTOs.PR_Odenis_Tapsirigi.Bank;
using FinNex.Application.DTOs.PR_Odenis_Tapsirigi.BankHesabi;
using FinNex.Application.DTOs.PR_Odenis_Tapsirigi.Musteri;
using FinNex.Application.DTOs.PR_Odenis_Tapsirigi.MusteriHesabi;
using FinNex.Application.DTOs.PR_Odenis_Tapsirigi.OdenisTapsirigi;
using FinNex.Application.Interfaces.PR_Odenis_Tapsirigi;
using FinNex.Domain.Entities.PR_Odenis_Tapsirigi;
using FinNex.Domain.Interfaces;

namespace FinNex.Application.Services.PR_Odenis_Tapsirigi
{
    public class BankService : ServiceAsync<Bank, BankDetailDto, BankCreateDto, BankUpdateDto>, IBankService
    {
        public BankService(IUnitOfWork unitOfWork, IMapper mapper) : base(unitOfWork, mapper) { }
    }
    public class BankHesabiService : ServiceAsync<BankHesabi, BankHesabiDetailDto, BankHesabiCreateDto, BankHesabiUpdateDto>, IBankHesabiService
    {
        public BankHesabiService(IUnitOfWork unitOfWork, IMapper mapper) : base(unitOfWork, mapper) { }
    }
    public class MusteriService : ServiceAsync<Musteri, MusteriDetailDto, MusteriCreateDto, MusteriUpdateDto>, IMusteriService
    {
        public MusteriService(IUnitOfWork unitOfWork, IMapper mapper) : base(unitOfWork, mapper) { }
    }

    public class MusteriHesabiService : ServiceAsync<MusteriHesabi, MusteriHesabiDetailDto, MusteriHesabiCreateDto, MusteriHesabiUpdateDto>, IMusteriHesabiService
    {
        public MusteriHesabiService(IUnitOfWork unitOfWork, IMapper mapper) : base(unitOfWork, mapper) { }
    }
    public class OdenisTapsirigiService : ServiceAsync<OdenisTapsirigi, OdenisTapsirigiDetailDto, OdenisTapsirigiCreateDto, OdenisTapsirigiUpdateDto>, IOdenisTapsirigiService
    {
        public OdenisTapsirigiService(IUnitOfWork unitOfWork, IMapper mapper) : base(unitOfWork, mapper) { }

        // Nümunə: Əgər xüsusi bir məntiq lazımdırsa bura əlavə edilir
        // public async Task TesdiqleAsync(int id) { ... }
    }
}
