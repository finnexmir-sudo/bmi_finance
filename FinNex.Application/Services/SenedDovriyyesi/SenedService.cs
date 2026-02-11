using FinNex.Application.DTOs.SenedDovriyyesi;
using FinNex.Application.Interfaces.SenedDovriyyesi;
using FinNex.Domain.Entities.SenedDovriyyesi;
using FinNex.Domain.Interfaces;

namespace FinNex.Application.Services.SenedDovriyyesi
{
    public class SenedService : ISenedService
    {
        private readonly IUnitOfWork _uow;
        private readonly IStorageService _storage;

        public SenedService(IUnitOfWork uow, IStorageService storage)
        {
            _uow = uow;
            _storage = storage;
        }

        public async Task<int> UploadAsync(SenedUploadDto dto, string userId, string? ip)
        {
            // 1) Fayl yoxlaması (minimal)
            var ext = Path.GetExtension(dto.Fayl.FileName).ToLowerInvariant();
            var allowed = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
            if (!allowed.Contains(ext))
                throw new Exception("Yalnız PDF/JPG/PNG icazəlidir.");

            // 2) Sened yarad
            var sened = new Sened
            {
                SobeId = dto.SobeId,
                SenedNovuId = dto.SenedNovuId,
                Basliq = dto.Basliq.Trim(),
                AcarSoz = dto.AcarSoz.Trim(),
                Status = SenedStatusu.Yeni
            };

            await _uow.Repository<Sened>().YaratAsync(sened);

            // 3) Hash hesabla (stream iki dəfə oxunacağı üçün MemoryStream istifadə edək)
            await using var ms = new MemoryStream();
            await dto.Fayl.CopyToAsync(ms);
            ms.Position = 0;

            var sha = await HashHelper.Sha256Async(ms);
            ms.Position = 0;

            // 4) Storage-a yaz
            // folderPath: 2026/02/SOBE/{senedId} -> amma sened.Id SaveChanges olmadan 0 olur
            // ona görə əvvəl SaveChanges, sonra yazırıq (bu, NORMALDIR)
            await _uow.YaddaSaxlaAsync(); // burada Id alınır

            var folder = Path.Combine(
                DateTime.Now.Year.ToString(),
                DateTime.Now.Month.ToString("00"),
                dto.SobeId.ToString(),
                sened.Id.ToString()
            );

            var (storedName, fullPath) = await _storage.SaveAsync(ms, dto.Fayl.FileName, ext, folder);

            // 5) DocumentFile
            var fayl = new SenedFayl
            {
                SenedId = sened.Id,
                VersiyaNo = 1,
                OriginalAd = dto.Fayl.FileName,
                StoredAd = storedName,
                ContentType = dto.Fayl.ContentType ?? "application/octet-stream",
                OlcuBytes = dto.Fayl.Length,
                Sha256 = sha,
                Yol = fullPath,
                AktivVersiya = true
            };

            await _uow.Repository<SenedFayl>().YaratAsync(fayl);

            // 6) Audit
            var log = new AuditLog
            {
                UserId = userId,
                Action = "Upload",
                SenedId = sened.Id,
                Ip = ip,
                DetailsJson = $"{{\"file\":\"{dto.Fayl.FileName}\",\"sha\":\"{sha}\"}}"
            };
            await _uow.Repository<AuditLog>().YaratAsync(log);

            // 7) Tək save
            await _uow.YaddaSaxlaAsync();
            return sened.Id;
        }
    }

}
