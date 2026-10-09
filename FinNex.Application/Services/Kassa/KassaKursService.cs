using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Kassa;
using FinNex.Application.Interfaces.Kassa;
using FinNex.Domain.Entities.Kassa;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.Kassa
{
    public class KassaKursService : IKassaKursService
    {
        private readonly IUnitOfWork _uow;

        // Valyuta siyahısı TƏK YERDƏ — BMI-dəki 5 valyuta (USD/AVRO/IRR/AED/RUB).
        // Yeni valyuta əlavə etmək lazımdırsa, YALNIZ bura əlavə et.
        public static readonly string[] Valyutalar = { "USD", "AVRO", "IRR", "AED", "RUB" };

        public KassaKursService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<KassaKursGunlukDto> GunlukGetirAsync(DateTime tarix)
        {
            var gun = tarix.Date;
            var beyanname = await _uow.Repository<KassaKursBeyannamesi>().Query()
                .Where(x => !x.Silinib && x.Tarix == gun)
                .Include(x => x.Setirler)
                .Include(x => x.Icraci)
                .Include(x => x.TesdiqEden)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            if (beyanname == null)
            {
                return new KassaKursGunlukDto
                {
                    Tarix = gun,
                    Setirler = Valyutalar.Select(v => new KassaKursSetriDto { Valyuta = v }).ToList()
                };
            }

            var setirlerByValyuta = beyanname.Setirler.ToDictionary(s => s.Valyuta);

            return new KassaKursGunlukDto
            {
                BeyannameId = beyanname.Id,
                Tarix = beyanname.Tarix,
                Status = beyanname.Status,
                IcraciAdi = beyanname.Icraci?.TamAd,
                TesdiqEdenAdi = beyanname.TesdiqEden?.TamAd,
                TesdiqTarixi = beyanname.TesdiqTarixi,
                ImtinaSebebi = beyanname.ImtinaSebebi,
                Setirler = Valyutalar.Select(v => setirlerByValyuta.TryGetValue(v, out var s)
                    ? new KassaKursSetriDto
                    {
                        Valyuta = v,
                        NagdAlis = s.NagdAlis,
                        NagdSatis = s.NagdSatis,
                        QeyriNagdAlis = s.QeyriNagdAlis,
                        QeyriNagdSatis = s.QeyriNagdSatis
                    }
                    : new KassaKursSetriDto { Valyuta = v }).ToList()
            };
        }

        public async Task<Result> SaxlaAsync(KassaKursSaxlaDto dto, int icraciIsciId)
        {
            var gun = dto.Tarix.Date;
            var repo = _uow.Repository<KassaKursBeyannamesi>();

            var movcud = await repo.Query()
                .Where(x => !x.Silinib && x.Tarix == gun)
                .Include(x => x.Setirler)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            // BMI-dəki bug BURADA bağlanır: "Gözləyir"/"Təsdiqləndi" olan
            // beyannamə ÜZƏRİNƏ yazmaq əvəzinə AÇIQ RƏDD edilir — səssiz
            // no-op (0 sətir update + yalan "uğurlu" mesajı) ola bilməz.
            if (movcud != null && movcud.Status != KassaKursStatus.Imtina)
            {
                var statusAdi = movcud.Status == KassaKursStatus.Gozleyir ? "gözləmədədir" : "artıq təsdiqlənib";
                return Result.Fail($"Bu gün üçün kurs artıq göndərilib və {statusAdi} — yenidən yazıla bilməz.");
            }

            if (movcud != null)
            {
                // İmtina olunmuş beyannamə — yenidən yazılır, "Gözləyir"ə qayıdır
                movcud.Status = KassaKursStatus.Gozleyir;
                movcud.TesdiqEdenIsciId = null;
                movcud.TesdiqTarixi = null;
                movcud.ImtinaSebebi = null;
                movcud.YenileyenIcraciId = icraciIsciId;
                movcud.YenilenmeTarixi = DateTime.Now;

                var setirRepo = _uow.Repository<KassaKursSetri>();
                var movcudByValyuta = movcud.Setirler.ToDictionary(s => s.Valyuta);
                foreach (var setir in dto.Setirler)
                {
                    if (movcudByValyuta.TryGetValue(setir.Valyuta, out var mevcudSetir))
                    {
                        mevcudSetir.NagdAlis = setir.NagdAlis;
                        mevcudSetir.NagdSatis = setir.NagdSatis;
                        mevcudSetir.QeyriNagdAlis = setir.QeyriNagdAlis;
                        mevcudSetir.QeyriNagdSatis = setir.QeyriNagdSatis;
                        mevcudSetir.YenileyenIcraciId = icraciIsciId;
                        mevcudSetir.YenilenmeTarixi = DateTime.Now;
                        await setirRepo.YenileAsync(mevcudSetir);
                    }
                    else
                    {
                        await setirRepo.YaratAsync(new KassaKursSetri
                        {
                            BeyannameId = movcud.Id,
                            Valyuta = setir.Valyuta,
                            NagdAlis = setir.NagdAlis,
                            NagdSatis = setir.NagdSatis,
                            QeyriNagdAlis = setir.QeyriNagdAlis,
                            QeyriNagdSatis = setir.QeyriNagdSatis,
                            YaradanIcraciId = icraciIsciId
                        });
                    }
                }

                await repo.YenileAsync(movcud);
                await _uow.YaddaSaxlaAsync();
                return Result.Ok("Kurs yenidən göndərildi — təsdiq gözləyir.");
            }

            // Yeni beyannamə
            var yeni = new KassaKursBeyannamesi
            {
                Tarix = gun,
                IcraciIsciId = icraciIsciId,
                Status = KassaKursStatus.Gozleyir,
                YaradanIcraciId = icraciIsciId,
                Setirler = dto.Setirler.Select(s => new KassaKursSetri
                {
                    Valyuta = s.Valyuta,
                    NagdAlis = s.NagdAlis,
                    NagdSatis = s.NagdSatis,
                    QeyriNagdAlis = s.QeyriNagdAlis,
                    QeyriNagdSatis = s.QeyriNagdSatis,
                    YaradanIcraciId = icraciIsciId
                }).ToList()
            };

            await repo.YaratAsync(yeni);
            await _uow.YaddaSaxlaAsync();
            return Result.Ok("Kurs göndərildi — təsdiq gözləyir.");
        }

        public async Task<IList<KassaKursSiyahiDto>> SonBeyannameleriGetirAsync(int gunSayi = 60)
        {
            var baslangic = DateTime.Now.Date.AddDays(-gunSayi);

            var beyannameler = await _uow.Repository<KassaKursBeyannamesi>().Query()
                .Where(x => !x.Silinib && x.Tarix >= baslangic)
                .Include(x => x.Setirler)
                .Include(x => x.Icraci)
                .Include(x => x.TesdiqEden)
                .OrderByDescending(x => x.Tarix)
                .ThenByDescending(x => x.Id)
                .ToListAsync();

            var rows = new List<KassaKursSiyahiDto>();
            foreach (var b in beyannameler)
            {
                foreach (var v in Valyutalar)
                {
                    var s = b.Setirler.FirstOrDefault(x => x.Valyuta == v);
                    rows.Add(new KassaKursSiyahiDto
                    {
                        BeyannameId = b.Id,
                        Tarix = b.Tarix,
                        Valyuta = v,
                        NagdAlis = s?.NagdAlis,
                        NagdSatis = s?.NagdSatis,
                        QeyriNagdAlis = s?.QeyriNagdAlis,
                        QeyriNagdSatis = s?.QeyriNagdSatis,
                        IcraciAdi = b.Icraci?.TamAd ?? "",
                        Status = b.Status,
                        TesdiqTarixi = b.TesdiqTarixi,
                        TesdiqEdenAdi = b.TesdiqEden?.TamAd
                    });
                }
            }
            return rows;
        }

        public async Task<IList<KassaKursBeyannameOzetDto>> GozleyenleriGetirAsync()
        {
            var beyannameler = await _uow.Repository<KassaKursBeyannamesi>().Query()
                .Where(x => !x.Silinib && x.Status == KassaKursStatus.Gozleyir)
                .Include(x => x.Setirler)
                .Include(x => x.Icraci)
                .OrderByDescending(x => x.Tarix)
                .ThenByDescending(x => x.Id)
                .ToListAsync();

            return beyannameler.Select(MapOzet).ToList();
        }

        public async Task<IList<KassaKursBeyannameOzetDto>> SonNeticelenmisleriGetirAsync(int say = 20)
        {
            var beyannameler = await _uow.Repository<KassaKursBeyannamesi>().Query()
                .Where(x => !x.Silinib && x.Status != KassaKursStatus.Gozleyir)
                .Include(x => x.Setirler)
                .Include(x => x.Icraci)
                .Include(x => x.TesdiqEden)
                .OrderByDescending(x => x.TesdiqTarixi)
                .Take(say)
                .ToListAsync();

            return beyannameler.Select(MapOzet).ToList();
        }

        public async Task<Result> TesdiqEtAsync(int beyannameId, int tesdiqEdenIsciId)
        {
            var repo = _uow.Repository<KassaKursBeyannamesi>();
            var beyanname = await repo.IdIleGetirAsync(beyannameId);
            if (beyanname == null || beyanname.Silinib)
                return Result.Fail("Beyannamə tapılmadı.");
            if (beyanname.Status != KassaKursStatus.Gozleyir)
                return Result.Fail("Bu beyannamə artıq nəticələnib — təkrar təsdiqlənə bilməz.");

            beyanname.Status = KassaKursStatus.Tesdiqlendi;
            beyanname.TesdiqEdenIsciId = tesdiqEdenIsciId;
            beyanname.TesdiqTarixi = DateTime.Now;
            beyanname.YenileyenIcraciId = tesdiqEdenIsciId;
            beyanname.YenilenmeTarixi = DateTime.Now;
            await repo.YenileAsync(beyanname);
            await _uow.YaddaSaxlaAsync();
            return Result.Ok("Kurs təsdiqləndi.");
        }

        public async Task<Result> ImtinaEtAsync(int beyannameId, int tesdiqEdenIsciId, string sebeb)
        {
            if (string.IsNullOrWhiteSpace(sebeb))
                return Result.Fail("İmtina səbəbi yazılmalıdır.");

            var repo = _uow.Repository<KassaKursBeyannamesi>();
            var beyanname = await repo.IdIleGetirAsync(beyannameId);
            if (beyanname == null || beyanname.Silinib)
                return Result.Fail("Beyannamə tapılmadı.");
            if (beyanname.Status != KassaKursStatus.Gozleyir)
                return Result.Fail("Bu beyannamə artıq nəticələnib — təkrar imtina edilə bilməz.");

            beyanname.Status = KassaKursStatus.Imtina;
            beyanname.TesdiqEdenIsciId = tesdiqEdenIsciId;
            beyanname.TesdiqTarixi = DateTime.Now;
            beyanname.ImtinaSebebi = sebeb.Trim();
            beyanname.YenileyenIcraciId = tesdiqEdenIsciId;
            beyanname.YenilenmeTarixi = DateTime.Now;
            await repo.YenileAsync(beyanname);
            await _uow.YaddaSaxlaAsync();
            return Result.Ok("Kurs imtina edildi.");
        }

        private static KassaKursBeyannameOzetDto MapOzet(KassaKursBeyannamesi b) => new()
        {
            BeyannameId = b.Id,
            Tarix = b.Tarix,
            IcraciAdi = b.Icraci?.TamAd ?? "",
            Status = b.Status,
            TesdiqTarixi = b.TesdiqTarixi,
            TesdiqEdenAdi = b.TesdiqEden?.TamAd,
            ImtinaSebebi = b.ImtinaSebebi,
            Setirler = Valyutalar.Select(v =>
            {
                var s = b.Setirler.FirstOrDefault(x => x.Valyuta == v);
                return new KassaKursSetriDto
                {
                    Valyuta = v,
                    NagdAlis = s?.NagdAlis,
                    NagdSatis = s?.NagdSatis,
                    QeyriNagdAlis = s?.QeyriNagdAlis,
                    QeyriNagdSatis = s?.QeyriNagdSatis
                };
            }).ToList()
        };
    }
}
