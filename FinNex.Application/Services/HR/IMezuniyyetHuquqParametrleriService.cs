using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Mezuniyyet;

namespace FinNex.Application.Services.HR
{
    /// <summary>
    /// Əmək məzuniyyəti hüququ hesablamasının qanuni ədədlərini (yaş həddi,
    /// əlavə gün, staj pilləsi) idarə edir — HR bunu bir dəfə qurur,
    /// <see cref="IMezuniyyetHuquqService"/> bunu oxuyur. Qanun dəyişsə HR
    /// bu ədədi dəyişir, kod/deploy lazım olmur.
    /// </summary>
    public interface IMezuniyyetHuquqParametrleriService
    {
        /// <summary>
        /// Cari parametrləri qaytarır. Sətir bazada yoxdursa (migration hələ
        /// işlədilməyib və ya sətir silinib) koddakı köhnə hardcode dəyərlərlə
        /// EYNİ default DTO qaytarır — hesablama HEÇ VAXT boş qalmır.
        /// </summary>
        Task<MezuniyyetHuquqParametrleriDto> AlAsync();

        /// <summary>
        /// Parametrləri yaddaşa yazır (tək sətir — yoxdursa yaradır, varsa
        /// yeniləyir). Həddlər ARTAN sırada olmalıdır (StajHedd1 &lt; 2 &lt; 3,
        /// UsaqSayi2GunHeddi &lt; UsaqSayi5GunHeddi) — əks halda `Result.Fail`
        /// qaytarır, HEÇ NƏ yazılmır (yarımçıq/məntiqsiz konfiqurasiya bazaya düşməsin).
        /// </summary>
        Task<Result> SaxlaAsync(MezuniyyetHuquqParametrleriDto dto, int? icraciId);
    }
}
