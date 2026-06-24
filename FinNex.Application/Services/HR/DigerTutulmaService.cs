using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.DigerTutulma;
using FinNex.Application.Interfaces.HR;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.HR
{
    /// <summary>
    /// İşçidən digər tutulma servisi. Maaş hesablamasına TƏSİR ETMİR —
    /// yalnız provodkada aylıq pay net ödənişdən çıxılır.
    /// </summary>
    public class DigerTutulmaService : IDigerTutulmaService
    {
        private readonly IUnitOfWork _uow;

        public DigerTutulmaService(IUnitOfWork uow) => _uow = uow;

        public async Task<Result<int>> YaratAsync(DigerTutulmaYaratDto dto, int yaradanIsciId)
        {
            try
            {
                if (dto.IsciId <= 0) return Result<int>.Fail("İşçi seçilməyib.");
                if (dto.Mebleg <= 0) return Result<int>.Fail("Məbləğ 0-dan böyük olmalıdır.");
                if (dto.MuddetAy <= 0) return Result<int>.Fail("Müddət (ay) 0-dan böyük olmalıdır.");
                if (dto.BaslamaAy < 1 || dto.BaslamaAy > 12) return Result<int>.Fail("Başlama ayı düzgün deyil.");
                if (dto.BaslamaIl < 2000) return Result<int>.Fail("Başlama ili düzgün deyil.");

                var isciVar = await _uow.Repository<Isci>().Query()
                    .AnyAsync(x => x.Id == dto.IsciId && !x.Silinib);
                if (!isciVar) return Result<int>.Fail("İşçi tapılmadı.");

                var entity = new IsciDigerTutulma
                {
                    IsciId      = dto.IsciId,
                    Mebleg      = dto.Mebleg,
                    MuddetAy    = dto.MuddetAy,
                    BaslamaIl   = dto.BaslamaIl,
                    BaslamaAy   = dto.BaslamaAy,
                    AyliqMebleg = Math.Round(dto.Mebleg / dto.MuddetAy, 2),
                    Aciqlama    = dto.Aciqlama,
                    Aktiv       = true,
                    YaradanIcraciId = yaradanIsciId > 0 ? yaradanIsciId : (int?)null
                };

                await _uow.Repository<IsciDigerTutulma>().YaratAsync(entity);
                await _uow.YaddaSaxlaAsync();
                return Result<int>.Ok(entity.Id, "Tutulma yadda saxlanıldı.");
            }
            catch (Exception ex)
            {
                return Result<int>.Fail($"Yadda saxlama zamanı xəta: {ex.Message}");
            }
        }

        public async Task<Result<IList<DigerTutulmaListDto>>> HamisiniGetirAsync()
        {
            try
            {
                var qeydler = await _uow.Repository<IsciDigerTutulma>().Query()
                    .Where(x => !x.Silinib)
                    .OrderByDescending(x => x.YaradilmaTarixi)
                    .ToListAsync();

                var isciAdlar = await _uow.Repository<Isci>().Query()
                    .Where(x => !x.Silinib)
                    .ToDictionaryAsync(x => x.Id, x => $"{x.Ad} {x.Soyad}");

                var indi = DateTime.Now;
                var list = qeydler.Select(x =>
                {
                    // Son tutulma ayı = başlama + (müddət − 1) ay
                    int sonIndex = (x.BaslamaIl * 12 + (x.BaslamaAy - 1)) + (x.MuddetAy - 1);
                    // Cədvəl üzrə cari aya qədər tutulan ay (başlama ayı daxil, müddətlə məhdud)
                    int kecen = (indi.Year - x.BaslamaIl) * 12 + (indi.Month - x.BaslamaAy) + 1;
                    int tutulubSayi = Math.Clamp(kecen, 0, x.MuddetAy);
                    return new DigerTutulmaListDto
                    {
                        Id = x.Id,
                        IsciId = x.IsciId,
                        IsciAdSoyad = isciAdlar.TryGetValue(x.IsciId, out var ad) ? ad : "—",
                        Mebleg = x.Mebleg,
                        MuddetAy = x.MuddetAy,
                        AyliqMebleg = x.AyliqMebleg,
                        SonAyMebleg = x.Mebleg - x.AyliqMebleg * (x.MuddetAy - 1),
                        BaslamaIl = x.BaslamaIl,
                        BaslamaAy = x.BaslamaAy,
                        BitmeIl = sonIndex / 12,
                        BitmeAy = sonIndex % 12 + 1,
                        TutulubSayi = tutulubSayi,
                        // Tam bitibsə qalıq dəqiq 0 (son ay qalığı udduğu üçün)
                        QalanMebleg = tutulubSayi >= x.MuddetAy ? 0m : Math.Max(0m, x.Mebleg - tutulubSayi * x.AyliqMebleg),
                        Aciqlama = x.Aciqlama,
                        Aktiv = x.Aktiv,
                        YaradilmaTarixi = x.YaradilmaTarixi
                    };
                }).ToList();

                return Result<IList<DigerTutulmaListDto>>.Ok(list);
            }
            catch (Exception ex)
            {
                return Result<IList<DigerTutulmaListDto>>.Fail($"Siyahı yüklənmədi: {ex.Message}");
            }
        }

        public async Task<Result> LegvEtAsync(int id)
        {
            try
            {
                var e = await _uow.Repository<IsciDigerTutulma>().Query()
                    .FirstOrDefaultAsync(x => x.Id == id && !x.Silinib);
                if (e == null) return Result.Fail("Qeyd tapılmadı.");

                e.Silinib = true;
                e.SilinmeTarixi = DateTime.Now;
                await _uow.Repository<IsciDigerTutulma>().YenileAsync(e);
                await _uow.YaddaSaxlaAsync();
                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail($"Silinmə zamanı xəta: {ex.Message}");
            }
        }

        public async Task<Dictionary<int, decimal>> GetAyTutulmaMapAsync(int il, int ay)
        {
            var map = new Dictionary<int, decimal>();
            var qeydler = await _uow.Repository<IsciDigerTutulma>().Query()
                .Where(x => !x.Silinib && x.Aktiv)
                .ToListAsync();

            foreach (var x in qeydler)
            {
                int index = (il - x.BaslamaIl) * 12 + (ay - x.BaslamaAy);
                if (index >= 0 && index < x.MuddetAy)
                {
                    // Son ay yuvarlaqlaşma qalığını udur ki, müddət üzrə cəm DƏQİQ = Mebleg olsun.
                    decimal ayliq = index == x.MuddetAy - 1
                        ? x.Mebleg - x.AyliqMebleg * (x.MuddetAy - 1)
                        : x.AyliqMebleg;
                    map[x.IsciId] = (map.TryGetValue(x.IsciId, out var c) ? c : 0m) + ayliq;
                }
            }
            return map;
        }

        public async Task<Dictionary<string, decimal>> GetAyTutulmaAdMapAsync(int il, int ay)
        {
            // Rounding məntiqi GetAyTutulmaMapAsync ilə EYNİDİR (cəm dəqiq = Mebleg);
            // yalnız qruplaşma AÇIQLAMA (sığorta adı) üzrədir → ümumi cəm dəyişmir.
            var map = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            var qeydler = await _uow.Repository<IsciDigerTutulma>().Query()
                .Where(x => !x.Silinib && x.Aktiv)
                .ToListAsync();

            foreach (var x in qeydler)
            {
                int index = (il - x.BaslamaIl) * 12 + (ay - x.BaslamaAy);
                if (index >= 0 && index < x.MuddetAy)
                {
                    decimal ayliq = index == x.MuddetAy - 1
                        ? x.Mebleg - x.AyliqMebleg * (x.MuddetAy - 1)
                        : x.AyliqMebleg;
                    var ad = string.IsNullOrWhiteSpace(x.Aciqlama) ? "" : x.Aciqlama.Trim();
                    map[ad] = (map.TryGetValue(ad, out var c) ? c : 0m) + ayliq;
                }
            }
            return map;
        }
    }
}
