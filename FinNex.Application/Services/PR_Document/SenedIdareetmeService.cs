using System.Security.Cryptography;
using AutoMapper;
using FinNex.Application.DTOs.PR_Document;
using FinNex.Application.DTOs.SenedDovriyyesi;
using FinNex.Application.Interfaces.PR_Document;
using FinNex.Domain.Entities.SenedDovriyyesi;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.PR_Document
{
    public class SenedIdareetmeService : ISenedIdareetmeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly string _uploadRoot;

        public SenedIdareetmeService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _uploadRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "senedler");
        }

        public async Task<SenedDashboardDto> DashboardGetirAsync()
        {
            var repo = _unitOfWork.Repository<Sened>();

            var umumi = await repo.SayAsync();
            var yeni = await repo.SayAsync(s => s.Status == SenedStatusu.Yeni);
            var tesdiq = await repo.SayAsync(s => s.Status == SenedStatusu.Tesdiq);
            var arxiv = await repo.SayAsync(s => s.Status == SenedStatusu.Arxiv);

            var sonSenedler = await repo.SorguHazirla(
                include: q => q.Include(s => s.Sobe).Include(s => s.SenedNovu),
                izlemeden: true)
                .OrderByDescending(s => s.YaradilmaTarixi)
                .Take(5)
                .ToListAsync();

            return new SenedDashboardDto
            {
                UmumiSened = umumi,
                YeniSened = yeni,
                TesdiqSened = tesdiq,
                ArxivSened = arxiv,
                SonSenedler = sonSenedler.Select(s => new SenedListDto
                {
                    Id = s.Id,
                    Basliq = s.Basliq,
                    SobeAd = s.Sobe.Ad,
                    SenedNovuAd = s.SenedNovu.Ad,
                    AcarSoz = s.AcarSoz,
                    StatusAd = StatusAdGetir(s.Status),
                    StatusDeger = (int)s.Status,
                    YaradilmaTarixi = s.YaradilmaTarixi
                }).ToList()
            };
        }

        public async Task<IList<SenedListDto>> SiyahiGetirAsync(
            int? sobeId = null,
            int? senedNovuId = null,
            int? status = null,
            string? acarSoz = null,
            int sehife = 1,
            int sehifeOlcusu = 10)
        {
            var sorgu = _unitOfWork.Repository<Sened>().SorguHazirla(
                include: q => q.Include(s => s.Sobe).Include(s => s.SenedNovu),
                izlemeden: true);

            if (sobeId.HasValue)
                sorgu = sorgu.Where(s => s.SobeId == sobeId.Value);

            if (senedNovuId.HasValue)
                sorgu = sorgu.Where(s => s.SenedNovuId == senedNovuId.Value);

            if (status.HasValue)
                sorgu = sorgu.Where(s => (int)s.Status == status.Value);

            if (!string.IsNullOrWhiteSpace(acarSoz))
                sorgu = sorgu.Where(s =>
                    s.AcarSoz.Contains(acarSoz) ||
                    s.Basliq.Contains(acarSoz));

            var list = await sorgu
                .OrderByDescending(s => s.YaradilmaTarixi)
                .Skip((sehife - 1) * sehifeOlcusu)
                .Take(sehifeOlcusu)
                .ToListAsync();

            return list.Select(s => new SenedListDto
            {
                Id = s.Id,
                Basliq = s.Basliq,
                SobeAd = s.Sobe.Ad,
                SenedNovuAd = s.SenedNovu.Ad,
                AcarSoz = s.AcarSoz,
                StatusAd = StatusAdGetir(s.Status),
                StatusDeger = (int)s.Status,
                YaradilmaTarixi = s.YaradilmaTarixi
            }).ToList();
        }

        public async Task<int> SayGetirAsync(
            int? sobeId = null,
            int? senedNovuId = null,
            int? status = null,
            string? acarSoz = null)
        {
            var sorgu = _unitOfWork.Repository<Sened>().SorguHazirla(izlemeden: true);

            if (sobeId.HasValue)
                sorgu = sorgu.Where(s => s.SobeId == sobeId.Value);

            if (senedNovuId.HasValue)
                sorgu = sorgu.Where(s => s.SenedNovuId == senedNovuId.Value);

            if (status.HasValue)
                sorgu = sorgu.Where(s => (int)s.Status == status.Value);

            if (!string.IsNullOrWhiteSpace(acarSoz))
                sorgu = sorgu.Where(s =>
                    s.AcarSoz.Contains(acarSoz) ||
                    s.Basliq.Contains(acarSoz));

            return await sorgu.CountAsync();
        }

        public async Task<SenedDetailDto?> DetayGetirAsync(int id)
        {
            var sened = await _unitOfWork.Repository<Sened>().SorguHazirla(
                predicate: s => s.Id == id,
                include: q => q
                    .Include(s => s.Sobe)
                    .Include(s => s.SenedNovu)
                    .Include(s => s.Fayllar),
                izlemeden: true)
                .FirstOrDefaultAsync();

            if (sened == null) return null;

            return new SenedDetailDto
            {
                Id = sened.Id,
                SobeId = sened.SobeId,
                SobeAd = sened.Sobe.Ad,
                SenedNovuId = sened.SenedNovuId,
                SenedNovuAd = sened.SenedNovu.Ad,
                Basliq = sened.Basliq,
                AcarSoz = sened.AcarSoz,
                StatusAd = StatusAdGetir(sened.Status),
                StatusDeger = (int)sened.Status,
                YaradilmaTarixi = sened.YaradilmaTarixi,
                Fayllar = sened.Fayllar
                    .Where(f => f.AktivVersiya)
                    .Select(f => new SenedFaylDto
                    {
                        Id = f.Id,
                        OriginalAd = f.OriginalAd,
                        ContentType = f.ContentType,
                        OlcuBytes = f.OlcuBytes,
                        VersiyaNo = f.VersiyaNo,
                        YaradilmaTarixi = f.YaradilmaTarixi
                    }).ToList()
            };
        }

        public async Task<int> YukleAsync(SenedUploadDto dto)
        {
            var sened = new Sened
            {
                SobeId = dto.SobeId,
                SenedNovuId = dto.SenedNovuId,
                Basliq = dto.Basliq,
                AcarSoz = dto.AcarSoz,
                Status = SenedStatusu.Yeni
            };

            await _unitOfWork.Repository<Sened>().YaratAsync(sened);
            await _unitOfWork.YaddaSaxlaAsync();

            if (dto.Fayl != null && dto.Fayl.Length > 0)
            {
                var storedName = $"{Guid.NewGuid()}{Path.GetExtension(dto.Fayl.FileName)}";
                var filePath = Path.Combine(_uploadRoot, storedName);

                Directory.CreateDirectory(_uploadRoot);

                using var stream = new FileStream(filePath, FileMode.Create);
                await dto.Fayl.CopyToAsync(stream);
                stream.Position = 0;

                using var sha256 = SHA256.Create();
                var hash = Convert.ToHexString(await sha256.ComputeHashAsync(stream));

                var senedFayl = new SenedFayl
                {
                    SenedId = sened.Id,
                    VersiyaNo = 1,
                    OriginalAd = dto.Fayl.FileName,
                    StoredAd = storedName,
                    ContentType = dto.Fayl.ContentType,
                    OlcuBytes = dto.Fayl.Length,
                    Sha256 = hash,
                    Yol = filePath,
                    AktivVersiya = true
                };

                await _unitOfWork.Repository<SenedFayl>().YaratAsync(senedFayl);
                await _unitOfWork.YaddaSaxlaAsync();
            }

            return sened.Id;
        }

        public async Task<(byte[] bytes, string contentType, string fileName)?> FaylYukleAsync(int faylId)
        {
            var fayl = await _unitOfWork.Repository<SenedFayl>()
                .GetirAsync(f => f.Id == faylId && f.AktivVersiya, izlemeden: true);

            if (fayl == null || !File.Exists(fayl.Yol))
                return null;

            var bytes = await File.ReadAllBytesAsync(fayl.Yol);
            return (bytes, fayl.ContentType, fayl.OriginalAd);
        }

        private static string StatusAdGetir(SenedStatusu status) => status switch
        {
            SenedStatusu.Yeni => "Yeni",
            SenedStatusu.Yoxlanilir => "Yoxlanılır",
            SenedStatusu.Tesdiq => "Təsdiqlənib",
            SenedStatusu.Arxiv => "Arxiv",
            _ => "Naməlum"
        };
    }
}
