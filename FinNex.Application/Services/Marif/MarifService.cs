using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Marif;
using FinNex.Application.Interfaces.Communication;
using FinNex.Application.Interfaces.Marif;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Entities.Marif;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.Marif
{
    public class MarifService : IMarifService
    {
        private readonly IUnitOfWork _uow;
        private readonly IBildirisService _bildiris;

        public MarifService(IUnitOfWork uow, IBildirisService bildiris)
        {
            _uow = uow;
            _bildiris = bildiris;
        }

        // ══════════════════════════════ Admin tərəfi ══════════════════════════════

        private static Result? SualYoxla(MarifSualDto s, int sira)
        {
            if (string.IsNullOrWhiteSpace(s.SualMetni))
                return Result.Fail($"{sira + 1}-ci sualın mətni boş ola bilməz.");
            if (s.SualMetni.Trim().Length > 1000)
                return Result.Fail($"{sira + 1}-ci sual 1000 simvoldan uzun ola bilməz.");
            if (string.IsNullOrWhiteSpace(s.IzahMetni))
                return Result.Fail($"{sira + 1}-ci sualın izahı boş ola bilməz.");
            if (s.IzahMetni.Trim().Length > 2000)
                return Result.Fail($"{sira + 1}-ci sualın izahı 2000 simvoldan uzun ola bilməz.");

            if (s.Tip == MarifSualTipi.CoxSecimli)
            {
                if (string.IsNullOrWhiteSpace(s.VariantA) || string.IsNullOrWhiteSpace(s.VariantB)
                    || string.IsNullOrWhiteSpace(s.VariantC) || string.IsNullOrWhiteSpace(s.VariantD))
                    return Result.Fail($"{sira + 1}-ci sualın dörd variantı da doldurulmalıdır.");

                var dv = (s.DuzgunVariant ?? "").Trim().ToUpperInvariant();
                if (dv != "A" && dv != "B" && dv != "C" && dv != "D")
                    return Result.Fail($"{sira + 1}-ci sual üçün düzgün variant A/B/C/D-dən biri olmalıdır.");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(s.DuzgunCavabMetni))
                    return Result.Fail($"{sira + 1}-ci açıq sualın nümunəvi cavabı boş ola bilməz.");
            }

            return null;
        }

        private static MarifSual Yarat(MarifSualDto s, int sira)
        {
            var e = new MarifSual
            {
                Tip = s.Tip,
                SualMetni = s.SualMetni.Trim(),
                IzahMetni = s.IzahMetni.Trim(),
                Sira = sira
            };

            if (s.Tip == MarifSualTipi.CoxSecimli)
            {
                e.VariantA = s.VariantA!.Trim();
                e.VariantB = s.VariantB!.Trim();
                e.VariantC = s.VariantC!.Trim();
                e.VariantD = s.VariantD!.Trim();
                e.DuzgunVariant = s.DuzgunVariant!.Trim().ToUpperInvariant();
            }
            else
            {
                e.DuzgunCavabMetni = s.DuzgunCavabMetni!.Trim();
            }

            return e;
        }

        public async Task<Result<int>> SetYaratAsync(MarifSualSetiYaratDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Basliq))
                return Result<int>.Fail("Başlıq mütləqdir.");
            if (dto.Basliq.Trim().Length > 200)
                return Result<int>.Fail("Başlıq 200 simvoldan uzun ola bilməz.");
            if (dto.Suallar == null || dto.Suallar.Count == 0)
                return Result<int>.Fail("Ən azı bir sual olmalıdır.");

            for (int i = 0; i < dto.Suallar.Count; i++)
            {
                var xeta = SualYoxla(dto.Suallar[i], i);
                if (xeta != null) return Result<int>.Fail(xeta.Message ?? "Məlumat yanlışdır.");
            }

            var set = new MarifSualSeti
            {
                Basliq = dto.Basliq.Trim(),
                HRDaxiliQaydaId = dto.HRDaxiliQaydaId,
                HRQanunFaylId = dto.HRQanunFaylId,
                MenbeMetn = dto.MenbeMetn,
                Status = MarifSetStatusu.Qaralama
            };

            for (int i = 0; i < dto.Suallar.Count; i++)
                set.Suallar.Add(Yarat(dto.Suallar[i], i));

            await _uow.Repository<MarifSualSeti>().YaratAsync(set);
            await _uow.YaddaSaxlaAsync();

            return Result<int>.Ok(set.Id, "Sual dəsti yadda saxlanıldı.");
        }

        public async Task<Result> SetYenileAsync(int setId, MarifSualSetiYaratDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Basliq))
                return Result.Fail("Başlıq mütləqdir.");
            if (dto.Suallar == null || dto.Suallar.Count == 0)
                return Result.Fail("Ən azı bir sual olmalıdır.");

            for (int i = 0; i < dto.Suallar.Count; i++)
            {
                var xeta = SualYoxla(dto.Suallar[i], i);
                if (xeta != null) return xeta;
            }

            var set = await _uow.Repository<MarifSualSeti>()
                .GetirAsync(x => x.Id == setId, include: q => q.Include(x => x.Suallar));
            if (set == null) return Result.Fail("Sual dəsti tapılmadı.");
            if (set.Status != MarifSetStatusu.Qaralama)
                return Result.Fail("Göndərilmiş sual dəsti redaktə edilə bilməz — nəticələr artıq işçiyə bağlıdır.");

            foreach (var kohne in set.Suallar.ToList())
            {
                kohne.Silinib = true;
                await _uow.Repository<MarifSual>().YenileAsync(kohne);
            }

            set.Basliq = dto.Basliq.Trim();
            set.HRDaxiliQaydaId = dto.HRDaxiliQaydaId;
            set.HRQanunFaylId = dto.HRQanunFaylId;
            set.MenbeMetn = dto.MenbeMetn;

            for (int i = 0; i < dto.Suallar.Count; i++)
            {
                var yeni = Yarat(dto.Suallar[i], i);
                yeni.SualSetiId = setId;
                await _uow.Repository<MarifSual>().YaratAsync(yeni);
            }

            await _uow.Repository<MarifSualSeti>().YenileAsync(set);
            await _uow.YaddaSaxlaAsync();

            return Result.Ok("Sual dəsti yeniləndi.");
        }

        public async Task<IList<MarifSualSetiListDto>> HamisiniGetirAsync()
        {
            var setler = await _uow.Repository<MarifSualSeti>().Query().AsNoTracking()
                .OrderByDescending(x => x.YaradilmaTarixi)
                .ToListAsync();

            var netice = new List<MarifSualSetiListDto>();
            foreach (var s in setler)
            {
                var sualSayi = await _uow.Repository<MarifSual>().Query().AsNoTracking()
                    .CountAsync(x => x.SualSetiId == s.Id);
                var gonderilen = await _uow.Repository<MarifTesyinat>().Query().AsNoTracking()
                    .CountAsync(x => x.SualSetiId == s.Id);
                var tamamlayan = await _uow.Repository<MarifTesyinat>().Query().AsNoTracking()
                    .CountAsync(x => x.SualSetiId == s.Id && x.TamamlandiMi);

                netice.Add(new MarifSualSetiListDto
                {
                    Id = s.Id,
                    Basliq = s.Basliq,
                    Status = s.Status,
                    SualSayi = sualSayi,
                    GonderilenIsciSayi = gonderilen,
                    TamamlayanIsciSayi = tamamlayan,
                    YaradilmaTarixi = s.YaradilmaTarixi
                });
            }

            return netice;
        }

        public async Task<MarifSualSetiDetailDto?> DetailGetirAsync(int setId)
        {
            var set = await _uow.Repository<MarifSualSeti>().Query().AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == setId);
            if (set == null) return null;

            var suallar = await _uow.Repository<MarifSual>().Query().AsNoTracking()
                .Where(x => x.SualSetiId == setId)
                .OrderBy(x => x.Sira)
                .ToListAsync();

            var tesyinatlar = await _uow.Repository<MarifTesyinat>().Query().AsNoTracking()
                .Where(x => x.SualSetiId == setId)
                .Include(x => x.Isci)
                .OrderByDescending(x => x.GonderilmeTarixi)
                .ToListAsync();

            var coxSecimliSayi = suallar.Count(x => x.Tip == MarifSualTipi.CoxSecimli);

            var tesyinatNetice = new List<MarifTesyinatNeticeDto>();
            foreach (var t in tesyinatlar)
            {
                var duzgunSayi = await _uow.Repository<MarifCavab>().Query().AsNoTracking()
                    .CountAsync(x => x.TesyinatId == t.Id && x.DuzgunduMu == true);

                tesyinatNetice.Add(new MarifTesyinatNeticeDto
                {
                    TesyinatId = t.Id,
                    IsciId = t.IsciId,
                    IsciAdi = t.Isci != null ? $"{t.Isci.Ad} {t.Isci.Soyad}" : "—",
                    TamamlandiMi = t.TamamlandiMi,
                    GonderilmeTarixi = t.GonderilmeTarixi,
                    TamamlanmaTarixi = t.TamamlanmaTarixi,
                    CoxSecimliDuzgunSayi = duzgunSayi,
                    CoxSecimliCemi = coxSecimliSayi
                });
            }

            return new MarifSualSetiDetailDto
            {
                Id = set.Id,
                Basliq = set.Basliq,
                Status = set.Status,
                Suallar = suallar.Select(MapSual).ToList(),
                Tesyinatlar = tesyinatNetice
            };
        }

        private static MarifSualDto MapSual(MarifSual x) => new()
        {
            Id = x.Id,
            Tip = x.Tip,
            SualMetni = x.SualMetni,
            VariantA = x.VariantA,
            VariantB = x.VariantB,
            VariantC = x.VariantC,
            VariantD = x.VariantD,
            DuzgunVariant = x.DuzgunVariant,
            DuzgunCavabMetni = x.DuzgunCavabMetni,
            IzahMetni = x.IzahMetni,
            Sira = x.Sira
        };

        public async Task<Result> GonderAsync(MarifGonderDto dto)
        {
            if (dto.IsciIdler == null || dto.IsciIdler.Count == 0)
                return Result.Fail("Ən azı bir işçi seçilməlidir.");

            var set = await _uow.Repository<MarifSualSeti>().GetirAsync(x => x.Id == dto.SualSetiId);
            if (set == null) return Result.Fail("Sual dəsti tapılmadı.");

            var sualVar = await _uow.Repository<MarifSual>().Query().AsNoTracking()
                .AnyAsync(x => x.SualSetiId == dto.SualSetiId);
            if (!sualVar) return Result.Fail("Bu dəstdə heç bir sual yoxdur — göndərmədən əvvəl ən azı bir sual olmalıdır.");

            var movcudIsciler = await _uow.Repository<MarifTesyinat>().Query().AsNoTracking()
                .Where(x => x.SualSetiId == dto.SualSetiId)
                .Select(x => x.IsciId)
                .ToListAsync();

            var yeniIsciler = dto.IsciIdler.Distinct().Except(movcudIsciler).ToList();
            if (yeniIsciler.Count == 0)
                return Result.Fail("Seçilən işçilərin hamısına bu test artıq göndərilib.");

            var yeniTesyinatlar = new List<MarifTesyinat>();
            foreach (var isciId in yeniIsciler)
            {
                var t = new MarifTesyinat
                {
                    SualSetiId = dto.SualSetiId,
                    IsciId = isciId,
                    GonderilmeTarixi = DateTime.Now
                };
                await _uow.Repository<MarifTesyinat>().YaratAsync(t);
                yeniTesyinatlar.Add(t);
            }

            set.Status = MarifSetStatusu.Gonderilib;
            await _uow.Repository<MarifSualSeti>().YenileAsync(set);

            await _uow.YaddaSaxlaAsync(); // Tesyinat Id-ləri indi doludur

            // Bildirişlər ARDICIL göndərilir — paylaşılan DbContext üzərində paralel
            // yazı (Task.WhenAll) qadağandır (bax CLAUDE.md "Bildirişlər — Paralel Yazı").
            foreach (var t in yeniTesyinatlar)
            {
                await _bildiris.YaratAsync(
                    t.IsciId,
                    BildirisNovu.MarifTesyinatGonderildi,
                    "Yeni maarifləndirmə testi",
                    $"\"{set.Basliq}\" mövzusunda yeni suallar sizə göndərildi.",
                    redirectUrl: $"/User/Marif/Sual/{t.Id}");
            }

            return Result.Ok($"{yeniTesyinatlar.Count} işçiyə göndərildi.");
        }

        public async Task<Result> SetSilAsync(int setId)
        {
            var set = await _uow.Repository<MarifSualSeti>().GetirAsync(x => x.Id == setId);
            if (set == null) return Result.Fail("Sual dəsti tapılmadı.");
            if (set.Status != MarifSetStatusu.Qaralama)
                return Result.Fail("Göndərilmiş sual dəsti silinə bilməz — işçi nəticələri saxlanmalıdır.");

            set.Silinib = true;
            await _uow.Repository<MarifSualSeti>().YenileAsync(set);
            await _uow.YaddaSaxlaAsync();

            return Result.Ok("Sual dəsti silindi.");
        }

        // ══════════════════════════════ İşçi tərəfi ══════════════════════════════

        public async Task<IList<MarifIsciTesyinatListDto>> IsciTesyinatlariAsync(int isciId)
        {
            var tesyinatlar = await _uow.Repository<MarifTesyinat>().Query().AsNoTracking()
                .Where(x => x.IsciId == isciId)
                .Include(x => x.SualSeti)
                .OrderByDescending(x => x.GonderilmeTarixi)
                .ToListAsync();

            var netice = new List<MarifIsciTesyinatListDto>();
            foreach (var t in tesyinatlar)
            {
                var sualSayi = await _uow.Repository<MarifSual>().Query().AsNoTracking()
                    .CountAsync(x => x.SualSetiId == t.SualSetiId);

                netice.Add(new MarifIsciTesyinatListDto
                {
                    TesyinatId = t.Id,
                    Basliq = t.SualSeti?.Basliq ?? "—",
                    GonderilmeTarixi = t.GonderilmeTarixi,
                    TamamlandiMi = t.TamamlandiMi,
                    SualSayi = sualSayi
                });
            }

            return netice;
        }

        public async Task<MarifIsciSualSetiDto?> IsciSualSetiGetirAsync(int tesyinatId, int isciId)
        {
            var t = await _uow.Repository<MarifTesyinat>().Query().AsNoTracking()
                .Include(x => x.SualSeti)
                .FirstOrDefaultAsync(x => x.Id == tesyinatId && x.IsciId == isciId);
            if (t == null) return null;

            var suallar = await _uow.Repository<MarifSual>().Query().AsNoTracking()
                .Where(x => x.SualSetiId == t.SualSetiId)
                .OrderBy(x => x.Sira)
                .ToListAsync();

            var cavablananSualIdler = await _uow.Repository<MarifCavab>().Query().AsNoTracking()
                .Where(x => x.TesyinatId == tesyinatId)
                .Select(x => x.SualId)
                .ToListAsync();
            var cavablananSet = cavablananSualIdler.ToHashSet();

            return new MarifIsciSualSetiDto
            {
                TesyinatId = t.Id,
                Basliq = t.SualSeti?.Basliq ?? "—",
                TamamlandiMi = t.TamamlandiMi,
                Suallar = suallar.Select(s => new MarifIsciSualDto
                {
                    SualId = s.Id,
                    Tip = s.Tip,
                    SualMetni = s.SualMetni,
                    VariantA = s.VariantA,
                    VariantB = s.VariantB,
                    VariantC = s.VariantC,
                    VariantD = s.VariantD,
                    Sira = s.Sira,
                    CavablanibMi = cavablananSet.Contains(s.Id)
                }).ToList()
            };
        }

        private static string VariantMetni(MarifSual s, string? herf) => herf?.Trim().ToUpperInvariant() switch
        {
            "A" => $"A) {s.VariantA}",
            "B" => $"B) {s.VariantB}",
            "C" => $"C) {s.VariantC}",
            "D" => $"D) {s.VariantD}",
            _ => ""
        };

        public async Task<Result<MarifCavabNeticeDto>> CavabVerAsync(MarifCavabVerDto dto, int isciId)
        {
            var tesyinat = await _uow.Repository<MarifTesyinat>()
                .GetirAsync(x => x.Id == dto.TesyinatId && x.IsciId == isciId);
            if (tesyinat == null)
                return Result<MarifCavabNeticeDto>.Fail("Təyinat tapılmadı və ya sizə aid deyil.");

            var sual = await _uow.Repository<MarifSual>()
                .GetirAsync(x => x.Id == dto.SualId && x.SualSetiId == tesyinat.SualSetiId);
            if (sual == null)
                return Result<MarifCavabNeticeDto>.Fail("Sual tapılmadı.");

            var movcudCavab = await _uow.Repository<MarifCavab>()
                .GetirAsync(x => x.TesyinatId == dto.TesyinatId && x.SualId == dto.SualId);

            if (movcudCavab == null)
            {
                bool? duzgundumu = sual.Tip == MarifSualTipi.CoxSecimli
                    ? string.Equals((dto.Cavab ?? "").Trim(), sual.DuzgunVariant, StringComparison.OrdinalIgnoreCase)
                    : null;

                movcudCavab = new MarifCavab
                {
                    TesyinatId = dto.TesyinatId,
                    SualId = dto.SualId,
                    IsciCavabi = (dto.Cavab ?? "").Trim(),
                    DuzgunduMu = duzgundumu,
                    CavabTarixi = DateTime.Now
                };
                await _uow.Repository<MarifCavab>().YaratAsync(movcudCavab);

                var cemSualSayi = await _uow.Repository<MarifSual>().Query().AsNoTracking()
                    .CountAsync(x => x.SualSetiId == tesyinat.SualSetiId);
                var evvelkiCavabSayi = await _uow.Repository<MarifCavab>().Query().AsNoTracking()
                    .CountAsync(x => x.TesyinatId == dto.TesyinatId);

                if (evvelkiCavabSayi + 1 >= cemSualSayi)
                {
                    tesyinat.TamamlandiMi = true;
                    tesyinat.TamamlanmaTarixi = DateTime.Now;
                    await _uow.Repository<MarifTesyinat>().YenileAsync(tesyinat);
                }

                await _uow.YaddaSaxlaAsync();
            }

            var duzgunMetn = sual.Tip == MarifSualTipi.CoxSecimli
                ? VariantMetni(sual, sual.DuzgunVariant)
                : (sual.DuzgunCavabMetni ?? "");

            return Result<MarifCavabNeticeDto>.Ok(new MarifCavabNeticeDto
            {
                DuzgunduMu = movcudCavab.DuzgunduMu,
                DuzgunCavabMetni = duzgunMetn,
                IzahMetni = sual.IzahMetni,
                TestTamamlandi = tesyinat.TamamlandiMi
            });
        }
    }
}
