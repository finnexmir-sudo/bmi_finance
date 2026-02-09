using AutoMapper;
using FinNex.Application.DTOs.PR_Odenis_Tapsirigi.Bank;
using FinNex.Application.DTOs.PR_Odenis_Tapsirigi.BankHesabi;
using FinNex.Application.DTOs.PR_Odenis_Tapsirigi.Musteri;
using FinNex.Application.DTOs.PR_Odenis_Tapsirigi.MusteriHesabi;
using FinNex.Application.DTOs.PR_Odenis_Tapsirigi.OdenisTapsirigi;
using FinNex.Domain.Entities.PR_Odenis_Tapsirigi;

namespace FinNex.Application
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Bank Mappings
            CreateMap<Bank, BankDetailDto>().ReverseMap();
            CreateMap<BankCreateDto, Bank>();
            CreateMap<BankUpdateDto, Bank>();

            // BankHesabi Mappings
            CreateMap<BankHesabi, BankHesabiDetailDto>().ReverseMap();
            CreateMap<BankHesabiCreateDto, BankHesabi>();
            CreateMap<BankHesabiUpdateDto, BankHesabi>();

            // Musteri Mappings
            CreateMap<Musteri, MusteriDetailDto>().ReverseMap();
            CreateMap<MusteriCreateDto, Musteri>();
            CreateMap<MusteriUpdateDto, Musteri>();

            // MusteriHesabi Mappings
            CreateMap<MusteriHesabi, MusteriHesabiDetailDto>().ReverseMap();
            CreateMap<MusteriHesabiCreateDto, MusteriHesabi>();
            CreateMap<MusteriHesabiUpdateDto, MusteriHesabi>();

            // OdenisTapsirigi Mappings
            CreateMap<OdenisTapsirigi, OdenisTapsirigiDetailDto>().ReverseMap();
            CreateMap<OdenisTapsirigiCreateDto, OdenisTapsirigi>();
            CreateMap<OdenisTapsirigiUpdateDto, OdenisTapsirigi>();
        }
    }
}
