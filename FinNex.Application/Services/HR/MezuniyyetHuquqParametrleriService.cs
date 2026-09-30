using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Mezuniyyet;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.HR
{
    public class MezuniyyetHuquqParametrleriService : IMezuniyyetHuquqParametrleriService
    {
        private readonly IUnitOfWork _unitOfWork;

        public MezuniyyetHuquqParametrleriService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<MezuniyyetHuquqParametrleriDto> AlAsync()
        {
            var qeyd = await _unitOfWork.Repository<MezuniyyetHuquqParametrleri>()
                .Query()
                .AsNoTracking()
                .Where(p => !p.Silinib)
                .OrderBy(p => p.Id)
                .FirstOrDefaultAsync();

            // Sətir tapılmasa (migration hələ işlədilməyib və ya silinib) — köhnə
            // hardcode dəyərlərlə EYNİ default. Hesablama heç vaxt boş qalmır.
            qeyd ??= MezuniyyetHuquqParametrleri.Defolt;

            return new MezuniyyetHuquqParametrleriDto
            {
                EsasGunAdi = qeyd.EsasGunAdi,
                EsasGunElil = qeyd.EsasGunElil,
                StajHedd1Il = qeyd.StajHedd1Il,
                StajHedd1Gun = qeyd.StajHedd1Gun,
                StajHedd2Il = qeyd.StajHedd2Il,
                StajHedd2Gun = qeyd.StajHedd2Gun,
                StajHedd3Il = qeyd.StajHedd3Il,
                StajHedd3Gun = qeyd.StajHedd3Gun,
                UsaqYasHeddi = qeyd.UsaqYasHeddi,
                EngelliUsaqYasHeddi = qeyd.EngelliUsaqYasHeddi,
                UsaqSayi2GunHeddi = qeyd.UsaqSayi2GunHeddi,
                UsaqGun2 = qeyd.UsaqGun2,
                UsaqSayi5GunHeddi = qeyd.UsaqSayi5GunHeddi,
                UsaqGun5 = qeyd.UsaqGun5
            };
        }

        public async Task<Result> SaxlaAsync(MezuniyyetHuquqParametrleriDto dto, int? icraciId)
        {
            // ── Validasiya — yarımçıq/məntiqsiz konfiqurasiya bazaya düşməsin ──
            if (dto.EsasGunAdi <= 0 || dto.EsasGunElil <= 0)
                return Result.Fail("Əsas gün sayı müsbət olmalıdır.");

            if (!(dto.StajHedd1Il < dto.StajHedd2Il && dto.StajHedd2Il < dto.StajHedd3Il))
                return Result.Fail("Staj həddləri ARTAN sırada olmalıdır (1-ci < 2-ci < 3-cü).");

            if (!(dto.StajHedd1Gun <= dto.StajHedd2Gun && dto.StajHedd2Gun <= dto.StajHedd3Gun))
                return Result.Fail("Staj əlavə günləri ARTAN (və ya bərabər) sırada olmalıdır.");

            if (dto.StajHedd1Il < 0 || dto.StajHedd1Gun < 0)
                return Result.Fail("Staj həddi/günü mənfi ola bilməz.");

            if (dto.UsaqYasHeddi <= 0 || dto.EngelliUsaqYasHeddi <= 0)
                return Result.Fail("Uşaq yaş həddi müsbət olmalıdır.");

            if (dto.UsaqSayi2GunHeddi <= 0 || dto.UsaqSayi5GunHeddi <= dto.UsaqSayi2GunHeddi)
                return Result.Fail("Uşaq sayı həddləri ARTAN sırada olmalıdır (2-günlük hədd < 5-günlük hədd).");

            if (dto.UsaqGun2 < 0 || dto.UsaqGun5 < dto.UsaqGun2)
                return Result.Fail("Uşaq əlavə günləri mənfi ola bilməz və ARTAN sırada olmalıdır.");

            var repo = _unitOfWork.Repository<MezuniyyetHuquqParametrleri>();
            var movcud = await repo.Query()
                .Where(p => !p.Silinib)
                .OrderBy(p => p.Id)
                .FirstOrDefaultAsync();

            if (movcud == null)
            {
                await repo.YaratAsync(new MezuniyyetHuquqParametrleri
                {
                    EsasGunAdi = dto.EsasGunAdi,
                    EsasGunElil = dto.EsasGunElil,
                    StajHedd1Il = dto.StajHedd1Il,
                    StajHedd1Gun = dto.StajHedd1Gun,
                    StajHedd2Il = dto.StajHedd2Il,
                    StajHedd2Gun = dto.StajHedd2Gun,
                    StajHedd3Il = dto.StajHedd3Il,
                    StajHedd3Gun = dto.StajHedd3Gun,
                    UsaqYasHeddi = dto.UsaqYasHeddi,
                    EngelliUsaqYasHeddi = dto.EngelliUsaqYasHeddi,
                    UsaqSayi2GunHeddi = dto.UsaqSayi2GunHeddi,
                    UsaqGun2 = dto.UsaqGun2,
                    UsaqSayi5GunHeddi = dto.UsaqSayi5GunHeddi,
                    UsaqGun5 = dto.UsaqGun5,
                    YaradanIcraciId = icraciId
                });
            }
            else
            {
                movcud.EsasGunAdi = dto.EsasGunAdi;
                movcud.EsasGunElil = dto.EsasGunElil;
                movcud.StajHedd1Il = dto.StajHedd1Il;
                movcud.StajHedd1Gun = dto.StajHedd1Gun;
                movcud.StajHedd2Il = dto.StajHedd2Il;
                movcud.StajHedd2Gun = dto.StajHedd2Gun;
                movcud.StajHedd3Il = dto.StajHedd3Il;
                movcud.StajHedd3Gun = dto.StajHedd3Gun;
                movcud.UsaqYasHeddi = dto.UsaqYasHeddi;
                movcud.EngelliUsaqYasHeddi = dto.EngelliUsaqYasHeddi;
                movcud.UsaqSayi2GunHeddi = dto.UsaqSayi2GunHeddi;
                movcud.UsaqGun2 = dto.UsaqGun2;
                movcud.UsaqSayi5GunHeddi = dto.UsaqSayi5GunHeddi;
                movcud.UsaqGun5 = dto.UsaqGun5;
                movcud.YenileyenIcraciId = icraciId;
                movcud.YenilenmeTarixi = DateTime.Now;
            }

            await _unitOfWork.YaddaSaxlaAsync();
            return Result.Ok("Parametrlər yadda saxlanıldı.");
        }
    }
}
