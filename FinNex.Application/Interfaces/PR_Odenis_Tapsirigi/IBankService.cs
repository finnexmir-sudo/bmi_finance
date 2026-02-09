using FinNex.Application.DTOs.PR_Odenis_Tapsirigi.Bank;
using FinNex.Application.DTOs.PR_Odenis_Tapsirigi.BankHesabi;
using FinNex.Application.DTOs.PR_Odenis_Tapsirigi.Musteri;
using FinNex.Application.DTOs.PR_Odenis_Tapsirigi.MusteriHesabi;
using FinNex.Application.DTOs.PR_Odenis_Tapsirigi.OdenisTapsirigi;
using FinNex.Domain.Entities.PR_Odenis_Tapsirigi;

namespace FinNex.Application.Interfaces.PR_Odenis_Tapsirigi
{
    public interface IBankService : IServiceAsync<Bank, BankDetailDto, BankCreateDto, BankUpdateDto>
    {
    }
    public interface IBankHesabiService : IServiceAsync<BankHesabi, BankHesabiDetailDto, BankHesabiCreateDto, BankHesabiUpdateDto>
    {
    }
    public interface IMusteriService : IServiceAsync<Musteri, MusteriDetailDto, MusteriCreateDto, MusteriUpdateDto>
    {
    }
    public interface IMusteriHesabiService : IServiceAsync<MusteriHesabi, MusteriHesabiDetailDto, MusteriHesabiCreateDto, MusteriHesabiUpdateDto>
    {
    }
    public interface IOdenisTapsirigiService : IServiceAsync<OdenisTapsirigi, OdenisTapsirigiDetailDto, OdenisTapsirigiCreateDto, OdenisTapsirigiUpdateDto>
    {
    }
}
