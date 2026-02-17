using AutoMapper;
using FinNex.Application.DTOs.PR_Odenis_Tapsirigi.Musteri;
using FinNex.Application.DTOs.PR_Odenis_Tapsirigi.MusteriHesabi;
using FinNex.Application.Interfaces.PR_Odenis_Tapsirigi;
using FinNex.Domain.Entities.PR_Odenis_Tapsirigi;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.PR_Odenis_Tapsirigi
{
    public class MusteriService : ServiceAsync<Musteri, MusteriDetailDto, MusteriCreateDto, MusteriUpdateDto>, IMusteriService
    {
        public MusteriService(IUnitOfWork unitOfWork, IMapper mapper) : base(unitOfWork, mapper) { }

        public async Task<MusteriHesabiDto?> VoenleAxtar(string voen)
        {
            var list = await _unitOfWork.Repository<Musteri>()
                .GetAllAsync(a => a.Voen == voen.Trim(),
                include: q => q.Include(m => m.MusteriHesablari),
                izlemeden: true);
            
            var musteri = list.FirstOrDefault();
            return musteri == null ? null : _mapper.Map<MusteriHesabiDto>(musteri);
        }
        public async Task YaratAsync(MusteriCreateDto dto)
        {
            var musteri = await _unitOfWork.Repository<Musteri>()
                .GetirAsync(x => x.Voen == dto.Voen);

            if (musteri == null)
            {
                musteri = new Musteri
                {
                    Ad = dto.Ad,
                    Voen = dto.Voen
                };

                await _unitOfWork.Repository<Musteri>().AddAsync(musteri);
                await _unitOfWork.YaddaSaxlaAsync(); // 👈 ID burada yaranır
            }

            var hesab = new MusteriHesabi
            {
                Iban = dto.Hesab,
                ValyutaId = dto.ValyutaId,
                MusteriId = musteri.Id
            };

            await _unitOfWork.Repository<MusteriHesabi>().AddAsync(hesab);

            await _unitOfWork.YaddaSaxlaAsync();
        }


    }
}
