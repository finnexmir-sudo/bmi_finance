using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Paylasim;
using FinNex.Application.Interfaces.Paylasim;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Interfaces;
using Ganss.Xss;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.Paylasim
{
    public class PaylasimService : IPaylasimService
    {
        private readonly IUnitOfWork _uow;
        private readonly HtmlSanitizer _sanitizer;

        public PaylasimService(IUnitOfWork uow)
        {
            _uow = uow;

            // Quill-in çıxardığı HTML-i təmizləyir — <script>/onerror və s. atılır,
            // qalın/kursiv/siyahı/başlıq/link/şəkil (yalnız src, alt) saxlanılır.
            _sanitizer = new HtmlSanitizer();
            _sanitizer.AllowedTags.Add("img");
            _sanitizer.AllowedAttributes.Add("src");
            _sanitizer.AllowedAttributes.Add("alt");
        }

        public async Task<List<PaylasimListDto>> HamisiniGetirAsync()
        {
            var lista = await _uow.Repository<Melumat>().Query().AsNoTracking()
                .Include(x => x.GonderenAppUser)
                .OrderByDescending(x => x.YaradilmaTarixi)
                .ToListAsync();

            return lista.Select(Map).ToList();
        }

        public async Task<List<PaylasimListDto>> AktivLentAsync()
        {
            var lista = await _uow.Repository<Melumat>().Query().AsNoTracking()
                .Include(x => x.GonderenAppUser)
                .Where(x => x.Aktivdir)
                .OrderByDescending(x => x.YaradilmaTarixi)
                .Take(50)
                .ToListAsync();

            return lista.Select(Map).ToList();
        }

        public async Task<PaylasimListDto?> DetailGetirAsync(int id)
        {
            var m = await _uow.Repository<Melumat>().Query().AsNoTracking()
                .Include(x => x.GonderenAppUser)
                .FirstOrDefaultAsync(x => x.Id == id);

            return m == null ? null : Map(m);
        }

        public async Task<Result<int>> YaratAsync(PaylasimYaratDto dto, int appUserId)
        {
            var xeta = Yoxla(dto);
            if (xeta != null) return Result<int>.Fail(xeta);

            var melumat = new Melumat
            {
                Basliq = dto.Basliq.Trim(),
                MetnHtml = _sanitizer.Sanitize(dto.MetnHtml),
                GonderenAppUserId = appUserId,
                Aktivdir = true
            };

            await _uow.Repository<Melumat>().YaratAsync(melumat);
            await _uow.YaddaSaxlaAsync();

            return Result<int>.Ok(melumat.Id, "Paylaşım yaradıldı.");
        }

        public async Task<Result> YenileAsync(int id, PaylasimYaratDto dto)
        {
            var xeta = Yoxla(dto);
            if (xeta != null) return Result.Fail(xeta);

            var melumat = await _uow.Repository<Melumat>().IdIleGetirAsync(id);
            if (melumat == null) return Result.Fail("Paylaşım tapılmadı.");

            melumat.Basliq = dto.Basliq.Trim();
            melumat.MetnHtml = _sanitizer.Sanitize(dto.MetnHtml);
            await _uow.Repository<Melumat>().YenileAsync(melumat);
            await _uow.YaddaSaxlaAsync();

            return Result.Ok("Paylaşım yeniləndi.");
        }

        public async Task<Result> AktivlikDeyisAsync(int id)
        {
            var melumat = await _uow.Repository<Melumat>().IdIleGetirAsync(id);
            if (melumat == null) return Result.Fail("Paylaşım tapılmadı.");

            melumat.Aktivdir = !melumat.Aktivdir;
            await _uow.Repository<Melumat>().YenileAsync(melumat);
            await _uow.YaddaSaxlaAsync();

            return Result.Ok(melumat.Aktivdir ? "Lentdə göstərilir." : "Lentdən gizlədildi.");
        }

        public async Task<Result> SilAsync(int id)
        {
            var melumat = await _uow.Repository<Melumat>().IdIleGetirAsync(id);
            if (melumat == null) return Result.Fail("Paylaşım tapılmadı.");

            await _uow.Repository<Melumat>().YumshakSilAsync(id);
            await _uow.YaddaSaxlaAsync();

            return Result.Ok("Paylaşım silindi.");
        }

        private static string? Yoxla(PaylasimYaratDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Basliq))
                return "Başlıq boş ola bilməz.";
            if (dto.Basliq.Trim().Length > 300)
                return "Başlıq 300 simvoldan uzun ola bilməz.";
            if (string.IsNullOrWhiteSpace(dto.MetnHtml) || dto.MetnHtml.Trim() == "<p><br></p>")
                return "Məzmun boş ola bilməz.";
            return null;
        }

        private static PaylasimListDto Map(Melumat m) => new()
        {
            Id = m.Id,
            Basliq = m.Basliq,
            MetnHtml = m.MetnHtml,
            GonderenAdi = $"{m.GonderenAppUser.Ad} {m.GonderenAppUser.Soyad}",
            YaradilmaTarixi = m.YaradilmaTarixi,
            Aktivdir = m.Aktivdir
        };
    }
}
