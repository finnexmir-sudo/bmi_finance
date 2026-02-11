using AutoMapper;
using FinNex.Application.DTOs.PR_Odenis_Tapsirigi.Bank;
using FinNex.Application.DTOs.PR_Odenis_Tapsirigi.BankHesabi;
using FinNex.Application.DTOs.PR_Odenis_Tapsirigi.Musteri;
using FinNex.Application.DTOs.PR_Odenis_Tapsirigi.MusteriHesabi;
using FinNex.Application.DTOs.PR_Odenis_Tapsirigi.OdenisTapsirigi;
using FinNex.Domain.Entities.PR_Odenis_Tapsirigi;

namespace FinNex.Application.MappingProfile.PR_Odenis_Tapsirigi
{
    public class Mapping:Profile
    {
        // Bank
        public Mapping()
        {
            // Bank
            CreateMap<Bank, BankDetailDto>().ReverseMap();
            CreateMap<Bank, BankListDto>();
            CreateMap<BankCreateDto, Bank>();
            CreateMap<BankUpdateDto, Bank>();

            // BankHesabi
            CreateMap<BankHesabi, BankHesabiDetailDto>().ReverseMap();
            CreateMap<BankHesabiCreateDto, BankHesabi>();
            CreateMap<BankHesabiUpdateDto, BankHesabi>();

            // Musteri
            CreateMap<Musteri, MusteriDetailDto>().ReverseMap();
            CreateMap<Musteri, MusteriHesabiDto>()
                .ForMember(d => d.Hesablar, o => o.MapFrom(s => s.MusteriHesablari));
            CreateMap<MusteriCreateDto, Musteri>();
            CreateMap<MusteriUpdateDto, Musteri>();

            // MusteriHesabi
            CreateMap<MusteriHesabi, MusteriHesabiDto>().ReverseMap();
            CreateMap<MusteriHesabi, MusteriHesabiListDto>();
            CreateMap<MusteriHesabiCreateDto, MusteriHesabi>();
            CreateMap<MusteriHesabiUpdateDto, MusteriHesabi>();

            // OdenisTapsirigi
            CreateMap<OdenisTapsirigi, OdenisTapsirigiDetailDto>().ReverseMap();
            CreateMap<OdenisTapsirigi, OdenisTapsirigiListDto>();
            CreateMap<OdenisTapsirigiCreateDto, OdenisTapsirigi>();
            CreateMap<OdenisTapsirigiUpdateDto, OdenisTapsirigi>();
        }
    }
}
