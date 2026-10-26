using FinNex.Application.Common.Results;
using FinNex.Application.Interfaces.HR;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.HR
{
    public class IsciHYSDondurmaService : IIsciHYSDondurmaService
    {
        private readonly IUnitOfWork _unitOfWork;

        public IsciHYSDondurmaService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> DondurAsync(int isciHYSId, int il, int ay, string? sebeb)
        {
            if (ay < 1 || ay > 12)
                return Result.Fail("Ay 1–12 arasında olmalıdır.");

            var hys = await _unitOfWork.Repository<IsciHYS>()
                .Query()
                .FirstOrDefaultAsync(x => x.Id == isciHYSId && !x.Silinib);
            if (hys == null)
                return Result.Fail("HYS təyinatı tapılmadı.");

            // Dondurulacaq ay HYS-in öz aktiv dövrünə düşməlidir — əks halda
            // dondurmağın mənası yoxdur, çünki o ay onsuz da hesablamaya daxil
            // olmayacaq.
            var ayBaslangic = new DateTime(il, ay, 1);
            var ayBitis = ayBaslangic.AddMonths(1).AddDays(-1);
            if (hys.BaslamaTarixi > ayBitis || (hys.BitmeTarixi.HasValue && hys.BitmeTarixi.Value < ayBaslangic))
                return Result.Fail(
                    $"{ay:D2}.{il} ayı bu HYS təyinatının aktiv dövrünə ({hys.BaslamaTarixi:dd.MM.yyyy} – " +
                    $"{(hys.BitmeTarixi.HasValue ? hys.BitmeTarixi.Value.ToString("dd.MM.yyyy") : "müddətsiz")}) düşmür.");

            var movcud = await _unitOfWork.Repository<IsciHYSDondurma>()
                .Query()
                .AnyAsync(x => !x.Silinib && x.IsciHYSId == isciHYSId && x.Il == il && x.Ay == ay);
            if (movcud)
                return Result.Fail($"{ay:D2}.{il} ayı bu HYS üçün artıq dondurulub.");

            // YaradanIcraciId ƏL İLƏ YAZILMIR — AppDbContext.SaveChangesAsync-in
            // audit interceptor-u (EntityState.Added qolu) bunu HttpContext-dəki
            // cari istifadəçi ID-si ilə ŞƏRTSİZ üstələyir; manual dəyər versək də
            // itəcəkdi. Layihənin heç bir servisi (məs. IsciHYSController-in özü)
            // bu sahəni əl ilə yazmır.
            var entity = new IsciHYSDondurma
            {
                IsciHYSId = isciHYSId,
                Il = il,
                Ay = ay,
                Sebeb = string.IsNullOrWhiteSpace(sebeb) ? null : sebeb.Trim()
            };
            await _unitOfWork.Repository<IsciHYSDondurma>().YaratAsync(entity);
            await _unitOfWork.YaddaSaxlaAsync();

            return Result.Ok("Dondurma qeydi yaradıldı.");
        }

        public async Task<Result> LegvEtAsync(int id)
        {
            var entity = await _unitOfWork.Repository<IsciHYSDondurma>().IdIleGetirAsync(id);
            if (entity == null || entity.Silinib)
                return Result.Fail("Dondurma qeydi tapılmadı.");

            // EfRepositoryAsync.YumshakSilAsync-in ETDİYİ İLƏ EYNİ: Silinib +
            // SilinmeTarixi burada yazılır (SaveChangesAsync interceptor-u
            // SilinmeTarixi artıq dolu olanda SilenIcraciId-i doldurmur — bu,
            // layihənin bütün YumshakSilAsync yollarında eyni, mövcud davranışdır,
            // bura xüsusi istisna əlavə edilmədi).
            entity.Silinib = true;
            entity.SilinmeTarixi = DateTime.Now;
            await _unitOfWork.YaddaSaxlaAsync();

            return Result.Ok("Dondurma ləğv edildi — bu ay yenidən hesablamaya daxil olunacaq.");
        }

        public async Task<List<IsciHYSDondurma>> IsciHYSUzreGetirAsync(int isciHYSId)
        {
            return await _unitOfWork.Repository<IsciHYSDondurma>()
                .Query()
                .Where(x => !x.Silinib && x.IsciHYSId == isciHYSId)
                .OrderByDescending(x => x.Il).ThenByDescending(x => x.Ay)
                .ToListAsync();
        }

        public async Task<HashSet<int>> DondurulmusIdlerAsync(int il, int ay)
        {
            var idler = await _unitOfWork.Repository<IsciHYSDondurma>()
                .Query()
                .Where(x => !x.Silinib && x.Il == il && x.Ay == ay)
                .Select(x => x.IsciHYSId)
                .ToListAsync();
            return idler.ToHashSet();
        }
    }
}
