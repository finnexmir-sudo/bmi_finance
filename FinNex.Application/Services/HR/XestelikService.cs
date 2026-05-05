using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Maas;
using FinNex.Application.Interfaces.HR;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.HR
{
    /// <summary>
    /// Xəstəlik bülletənləri xidməti.
    ///
    /// Hesablama məntiqi:
    ///   S            = Son 12 ayın cəmi qazancı (IsciAyliqQazanc)
    ///   Son12AyIsGun = Son 12 ayın iş günlərinin cəmi (BayramGunu nəzərə alınır)
    ///   BirGunluk    = S / Son12AyIsGun
    ///
    ///   Şirkət LİMİTİ: bütün cari il boyunca maksimum 14 iş günü
    ///   (qalan günlər DSMF-in payıdır — sistem yalnız informativ göstərir)
    /// </summary>
    public class XestelikService : IXestelikService
    {
        private readonly IUnitOfWork _unitOfWork;

        // Şirkət hər xəstəlik bülletəni üçün maksimum 14 iş günü ödəyir.
        // Qalan günlər DSMF tərəfindən ödənilir (informativ — sistem hesablamır).
        // NOTE: Əvvəlki versiyada illik limit idi; indi per-bülletəndir.
        private const int SIRKET_MAX_GUN = 14;

        public XestelikService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // ─────────────────────────────────────────────────────────
        // STAJ — ümumi iş stajı ilə (əvvəlki iş yerləri daxil), staj
        // başlanğıcı yoxdursa bu banka gəlmə tarixini istifadə edir.
        // Qanun üzrə default faizlər:
        //   < 8 il: 60%, 8–12 il: 80%, ≥ 12 il: 100%
        // Bu faizlər MaasParametri cədvəlində konfiqurasiya edilə bilər
        // (XestelikStaj8YasagidaFaizi / XestelikStaj8_12IlFaizi / XestelikStaj12YuxariFaizi).
        // Parametr yoxdursa default-a düşür.
        // ─────────────────────────────────────────────────────────
        private async Task<(int il, int ay, decimal faiz)> HesablaStajAsync(Isci isci, DateTime referansTarix)
        {
            var bas = isci.UmumiIsStajiBaslangic ?? isci.IsheQebulTarixi;

            decimal faizAsagi  = await GetStajFaiziAsync(MaasParametrNovu.XestelikStaj8YasagidaFaizi, referansTarix, 60m) / 100m;
            decimal faizOrta   = await GetStajFaiziAsync(MaasParametrNovu.XestelikStaj8_12IlFaizi,   referansTarix, 80m) / 100m;
            decimal faizYuxari = await GetStajFaiziAsync(MaasParametrNovu.XestelikStaj12YuxariFaizi,  referansTarix, 100m) / 100m;

            if (bas > referansTarix) return (0, 0, faizAsagi);

            int il = referansTarix.Year - bas.Year;
            int ay = referansTarix.Month - bas.Month;
            if (referansTarix.Day < bas.Day) ay--;
            if (ay < 0) { il--; ay += 12; }

            decimal faiz = il < 8 ? faizAsagi : il < 12 ? faizOrta : faizYuxari;
            return (il, ay, faiz);
        }

        private async Task<decimal> GetStajFaiziAsync(MaasParametrNovu nov, DateTime referansTarix, decimal defaultFaiz)
        {
            var p = (await _unitOfWork.Repository<MaasParametri>()
                .HamisiniGetirAsync(x =>
                    x.Aktivdir && !x.Silinib &&
                    x.Nov == nov &&
                    x.BaslamaTarixi <= referansTarix &&
                    (x.BitmeTarixi == null || x.BitmeTarixi >= referansTarix)))
                .OrderByDescending(x => x.BaslamaTarixi)
                .FirstOrDefault();
            return p?.Deyer ?? defaultFaiz;
        }

        public async Task<Result<IList<XestelikDto>>> GetListAsync()
        {
            try
            {
                var list = await _unitOfWork.Repository<Xestelik>()
                    .Query()
                    .Where(x => !x.Silinib)
                    .Include(x => x.Isci)
                    .Include(x => x.Odenisler)
                    .OrderByDescending(x => x.BaslamaTarixi)
                    .ToListAsync();

                // Hansı (IsciId, İl, Ay) cütlərinin maaşı hesablanıb — toplu yüklə
                var isciIdler = list.Select(x => x.IsciId).Distinct().ToList();
                var hesablanmisMaaslar = await _unitOfWork.Repository<Maas>()
                    .Query()
                    .Where(m => !m.Silinib &&
                                isciIdler.Contains(m.IsciId) &&
                                (m.Status == MaasStatus.Tesdiqlendi || m.Status == MaasStatus.Odenildi))
                    .Select(m => new { m.IsciId, m.Il, m.Ay })
                    .ToListAsync();

                var today = DateTime.Today;

                var dtos = list.Select(x =>
                {
                    string? sebeb = null;

                    if (x.BitmeTarixi.Date < today)
                        sebeb = $"Bitmə tarixi keçib ({x.BitmeTarixi:dd.MM.yyyy})";
                    else
                    {
                        // Bülletenin əhatə etdiyi ayları yoxla
                        var cur = new DateTime(x.BaslamaTarixi.Year, x.BaslamaTarixi.Month, 1);
                        var son = new DateTime(x.BitmeTarixi.Year, x.BitmeTarixi.Month, 1);
                        while (cur <= son)
                        {
                            if (hesablanmisMaaslar.Any(m =>
                                    m.IsciId == x.IsciId && m.Il == cur.Year && m.Ay == cur.Month))
                            {
                                sebeb = $"{cur:MM.yyyy} ayının əmək haqqı artıq hesablanıb";
                                break;
                            }
                            cur = cur.AddMonths(1);
                        }
                    }

                    return new XestelikDto
                    {
                        Id = x.Id,
                        IsciId = x.IsciId,
                        IsciAdSoyad = x.Isci != null ? $"{x.Isci.Ad} {x.Isci.Soyad}" : "",
                        BaslamaTarixi = x.BaslamaTarixi,
                        BitmeTarixi = x.BitmeTarixi,
                        IsGunSayi = x.IsGunSayi,
                        BulletenNomresi = x.BulletenNomresi,
                        MualiceMuessisesi = x.MualiceMuessisesi,
                        Qeyd = x.Qeyd,
                        Status = (int)x.Status,
                        HrTesdiqTarixi = x.HrTesdiqTarixi,
                        UmumiSirketOdenisi = x.Odenisler.Sum(o => o.SirketOdenis),
                        SilineBilir = sebeb == null,
                        SilinmemeSebebi = sebeb,
                    };
                }).ToList();

                return Result<IList<XestelikDto>>.Ok(dtos);
            }
            catch (Exception ex)
            {
                return Result<IList<XestelikDto>>.Fail($"Xəta: {ex.Message}");
            }
        }

        public async Task<Result<XestelikDto?>> GetByIdAsync(int id)
        {
            try
            {
                var x = await _unitOfWork.Repository<Xestelik>()
                    .Query()
                    .Where(x => x.Id == id && !x.Silinib)
                    .Include(x => x.Isci)
                    .Include(x => x.Odenisler)
                    .FirstOrDefaultAsync();

                if (x == null) return Result<XestelikDto?>.Fail("Tapılmadı.");

                var dto = new XestelikDto
                {
                    Id = x.Id,
                    IsciId = x.IsciId,
                    IsciAdSoyad = x.Isci != null ? $"{x.Isci.Ad} {x.Isci.Soyad}" : "",
                    BaslamaTarixi = x.BaslamaTarixi,
                    BitmeTarixi = x.BitmeTarixi,
                    IsGunSayi = x.IsGunSayi,
                    BulletenNomresi = x.BulletenNomresi,
                    MualiceMuessisesi = x.MualiceMuessisesi,
                    Qeyd = x.Qeyd,
                    Status = (int)x.Status,
                    HrTesdiqTarixi = x.HrTesdiqTarixi,
                    UmumiSirketOdenisi = x.Odenisler.Sum(o => o.SirketOdenis)
                };

                return Result<XestelikDto?>.Ok(dto);
            }
            catch (Exception ex)
            {
                return Result<XestelikDto?>.Fail($"Xəta: {ex.Message}");
            }
        }

        public async Task<Result<int>> CreateAsync(XestelikCreateDto input, int? hrIsciId)
        {
            try
            {
                if (input.BitmeTarixi < input.BaslamaTarixi)
                    return Result<int>.Fail("Bitmə tarixi başlama tarixindən sonra olmalıdır.");

                if (string.IsNullOrWhiteSpace(input.BulletenNomresi))
                    return Result<int>.Fail("Bülletən nömrəsi məcburidir.");

                int isGunSayi = await IsGunSayiniHesablaAsync(input.BaslamaTarixi, input.BitmeTarixi);
                if (isGunSayi <= 0)
                    return Result<int>.Fail("Verilmiş tarix aralığında iş günü yoxdur.");

                var entity = new Xestelik
                {
                    IsciId = input.IsciId,
                    BaslamaTarixi = input.BaslamaTarixi,
                    BitmeTarixi = input.BitmeTarixi,
                    IsGunSayi = isGunSayi,
                    BulletenNomresi = input.BulletenNomresi.Trim(),
                    MualiceMuessisesi = string.IsNullOrWhiteSpace(input.MualiceMuessisesi) ? null : input.MualiceMuessisesi.Trim(),
                    Qeyd = string.IsNullOrWhiteSpace(input.Qeyd) ? null : input.Qeyd.Trim(),
                    Status = XestelikStatus.Tesdiqlenib,
                    HrId = hrIsciId,
                    HrTesdiqTarixi = DateTime.Now
                };

                await _unitOfWork.Repository<Xestelik>().YaratAsync(entity);
                await _unitOfWork.YaddaSaxlaAsync();

                // Avtomatik ödəniş qeydləri yarat (hər ay üçün)
                try
                {
                    await OdenisleriYaratAsync(entity);
                }
                catch (Exception odEx)
                {
                    // Ödəniş yaradılması uğursuz olarsa — xəstəlik qeydi yenə qalır
                    // amma istifadəçiyə xəbər ver
                    return Result<int>.Ok(entity.Id);
                }

                return Result<int>.Ok(entity.Id);
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                return Result<int>.Fail($"Xəta: {msg}");
            }
        }

        public async Task<Result> DeleteAsync(int id)
        {
            try
            {
                var entity = await _unitOfWork.Repository<Xestelik>()
                    .Query()
                    .Where(x => x.Id == id && !x.Silinib)
                    .Include(x => x.Odenisler)
                    .FirstOrDefaultAsync();

                if (entity == null) return Result.Fail("Tapılmadı.");

                // ── Qoruma 1: Bitmə tarixi keçibsə silmək olmaz ──
                if (entity.BitmeTarixi.Date < DateTime.Today)
                    return Result.Fail(
                        $"Bu xəstəlik bülletəninin bitmə tarixi keçib " +
                        $"({entity.BitmeTarixi:dd.MM.yyyy}). Keçmiş dövrə aid qeydlər silinə bilməz.");

                // ── Qoruma 2: Bülletenin əhatə etdiyi ayların əmək haqqı hesablanıbsa silmək olmaz ──
                var aylar = new List<(int Il, int Ay)>();
                var cur = new DateTime(entity.BaslamaTarixi.Year, entity.BaslamaTarixi.Month, 1);
                var son = new DateTime(entity.BitmeTarixi.Year, entity.BitmeTarixi.Month, 1);
                while (cur <= son)
                {
                    aylar.Add((cur.Year, cur.Month));
                    cur = cur.AddMonths(1);
                }

                var hesablanmisAy = await _unitOfWork.Repository<Maas>()
                    .Query()
                    .AnyAsync(m =>
                        m.IsciId == entity.IsciId &&
                        !m.Silinib &&
                        (m.Status == MaasStatus.Tesdiqlendi || m.Status == MaasStatus.Odenildi) &&
                        aylar.Select(a => a.Il).Contains(m.Il) &&
                        aylar.Select(a => a.Ay).Contains(m.Ay));

                if (hesablanmisAy)
                    return Result.Fail(
                        "Bu bülletənin əhatə etdiyi ayın əmək haqqı artıq hesablanıb. " +
                        "Silinmə mümkün deyil.");

                // Ödəniş qeydlərini də sil
                foreach (var o in entity.Odenisler)
                {
                    o.Silinib = true;
                    o.SilinmeTarixi = DateTime.Now;
                }

                entity.Silinib = true;
                entity.SilinmeTarixi = DateTime.Now;
                entity.Status = XestelikStatus.LegvEdildi;

                await _unitOfWork.YaddaSaxlaAsync();
                return Result.Ok("Xəstəlik qeydi silindi.");
            }
            catch (Exception ex)
            {
                return Result.Fail($"Xəta: {ex.Message}");
            }
        }

        public async Task<Result<XestelikPreviewDto>> PreviewAsync(int isciId, DateTime baslama, DateTime bitme)
        {
            try
            {
                if (bitme < baslama)
                    return Result<XestelikPreviewDto>.Fail("Bitmə tarixi başlama tarixindən sonra olmalıdır.");

                // İşçini yüklə (staj hesablamaq üçün)
                var isci = await _unitOfWork.Repository<Isci>().IdIleGetirAsync(isciId);
                if (isci == null)
                    return Result<XestelikPreviewDto>.Fail("İşçi tapılmadı.");

                // Cari bülletən
                int isGunSayi = await IsGunSayiniHesablaAsync(baslama, bitme);
                int teqvimGunSayi = (bitme.Date - baslama.Date).Days + 1;

                // ── Son 12 ay dövrü (xəstəlik ayından əvvəlki 12 tam ay) ──
                var ayBitis = new DateTime(baslama.Year, baslama.Month, 1).AddDays(-1);
                var ayBaslangic = new DateTime(ayBitis.Year, ayBitis.Month, 1).AddMonths(-11);

                // ── Yeni düstur (DSMF-əsaslı) ───────────────────────────
                var dto = await YeniXestelikHesablaAsync(
                    isciId, baslama, bitme, ayBaslangic, ayBitis, teqvimGunSayi);

                dto.IsGunSayi = isGunSayi;
                return Result<XestelikPreviewDto>.Ok(dto);
            }
            catch (Exception ex)
            {
                return Result<XestelikPreviewDto>.Fail($"Xəta: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        // ─────────────────────────────────────────────────────────
        // YENİ XƏSTƏLİK ÖDƏNİŞİ DÜSTURU (May 2026+)
        //
        // Numerator   = Σ(son 12 ay DSMF işçi + DSMF şirkət) × Əmsal (4)
        // Denominator = (son 12 ay təqvim günü) − (son 12 ay xəstəlik təqvim günü)
        // GünlükPul   = Numerator ÷ Denominator
        // ŞirkətÖd.   = GünlükPul × min(cariTeqvimGun, 14)
        // DSMFÖd.     = GünlükPul × (cariTeqvimGun − 14)   [əgər >14]
        //
        // Staj faizi YOX, iş günü filtri YOX — yalnız təqvim günü.
        // ─────────────────────────────────────────────────────────
        private async Task<XestelikPreviewDto> YeniXestelikHesablaAsync(
            int isciId, DateTime baslama, DateTime bitme,
            DateTime ayBaslangic, DateTime ayBitis, int cariTeqvimGun)
        {
            var izah = new List<XestelikIzahDto>();

            // 1. Son 12 ay DSMF cəmləri (işçi + şirkət)
            var son12 = await _unitOfWork.Repository<IsciAyliqQazanc>()
                .Query()
                .Where(x => x.IsciId == isciId && !x.Silinib
                         && ((x.Il > ayBaslangic.Year)
                             || (x.Il == ayBaslangic.Year && x.Ay >= ayBaslangic.Month))
                         && ((x.Il < ayBitis.Year)
                             || (x.Il == ayBitis.Year && x.Ay <= ayBitis.Month)))
                .ToListAsync();

            decimal dsmfIsciCem = son12.Sum(x => x.DsmfIsci);
            decimal dsmfIsvCem  = son12.Sum(x => x.DsmfIsegoturen);
            decimal dsmfCem     = dsmfIsciCem + dsmfIsvCem;

            izah.Add(new XestelikIzahDto
            {
                Addim = "1. Son 12 ay DSMF cəmi (işçi)",
                Izah = $"{ayBaslangic:MM.yyyy} – {ayBitis:MM.yyyy} ərzində işçidən tutulan DSMF",
                Mebleg = Math.Round(dsmfIsciCem, 2)
            });
            izah.Add(new XestelikIzahDto
            {
                Addim = "2. Son 12 ay DSMF cəmi (şirkət)",
                Izah = "Şirkətin işçi üçün ödədiyi DSMF",
                Mebleg = Math.Round(dsmfIsvCem, 2)
            });

            // 2. Əmsal (parametrdən)
            decimal emsal = await GetXestelikEmsaliAsync(baslama);
            izah.Add(new XestelikIzahDto
            {
                Addim = "3. DSMF Əmsalı (parametr)",
                Izah = "MaasParametri.XestelikDsmfEmsali — default 4",
                Mebleg = emsal
            });

            decimal numerator = dsmfCem * emsal;
            izah.Add(new XestelikIzahDto
            {
                Addim = "4. Bölünən cəm (DSMF × əmsal)",
                Izah = $"({dsmfIsciCem:N2} + {dsmfIsvCem:N2}) × {emsal:G29} = {numerator:N2}",
                Mebleg = Math.Round(numerator, 2)
            });

            // 3. Son 12 ay təqvim günü
            int son12AyTeqvimGun = (ayBitis.Date - ayBaslangic.Date).Days + 1;

            // 4. Son 12 ayda xəstəlik təqvim günü (Xestelik cədvəlindən, intervala kəsilmiş)
            int son12XstTeqvimGun = await Son12AyXestelikTeqvimGunAsync(isciId, ayBaslangic, ayBitis);

            int denominator = son12AyTeqvimGun - son12XstTeqvimGun;

            izah.Add(new XestelikIzahDto
            {
                Addim = "5. Son 12 ay təqvim günü",
                Izah = $"{ayBaslangic:dd.MM.yyyy} – {ayBitis:dd.MM.yyyy}",
                Mebleg = son12AyTeqvimGun
            });
            izah.Add(new XestelikIzahDto
            {
                Addim = "6. Son 12 ay xəstəlik təqvim günü",
                Izah = "Bu intervala düşən bütün xəstəliklərin təqvim günü cəmi",
                Mebleg = son12XstTeqvimGun
            });
            izah.Add(new XestelikIzahDto
            {
                Addim = "7. Bölən gün sayı",
                Izah = $"{son12AyTeqvimGun} − {son12XstTeqvimGun} = {denominator}",
                Mebleg = denominator
            });

            // 5. Bir günlük
            decimal birGunluk = denominator > 0 ? numerator / denominator : 0;
            izah.Add(new XestelikIzahDto
            {
                Addim = "8. Bir günlük ödəniş (bölünən ÷ bölən)",
                Izah = $"{numerator:N2} ÷ {denominator} = {birGunluk:N4} ₼/gün",
                Mebleg = Math.Round(birGunluk, 4)
            });

            // 6. Cari xəstəlik bölgüsü (təqvim günü ilə)
            int sirketGun = Math.Min(cariTeqvimGun, SIRKET_MAX_GUN);
            int dsmfGun   = cariTeqvimGun - sirketGun;

            decimal sirketOd = Math.Round(birGunluk * sirketGun, 2);
            decimal dsmfOd   = Math.Round(birGunluk * dsmfGun, 2);

            izah.Add(new XestelikIzahDto
            {
                Addim = "9. Cari xəstəlik təqvim günü",
                Izah = $"{baslama:dd.MM.yyyy} – {bitme:dd.MM.yyyy}",
                Mebleg = cariTeqvimGun
            });
            izah.Add(new XestelikIzahDto
            {
                Addim = "10. Şirkət ödənişi",
                Izah = $"{birGunluk:N4} × {sirketGun} = {sirketOd:N2} ₼  (max 14 gün şirkət ödəyir)",
                Mebleg = sirketOd
            });
            if (dsmfGun > 0)
            {
                izah.Add(new XestelikIzahDto
                {
                    Addim = "11. DSMF ödənişi",
                    Izah = $"{birGunluk:N4} × {dsmfGun} = {dsmfOd:N2} ₼  (14 gündən sonrası DSMF tərəfindən)",
                    Mebleg = dsmfOd
                });
            }

            // Xəbərdarlıqlar
            string? xeberdarliq = null;
            if (son12.Count < 12)
                xeberdarliq = $"Yalnız {son12.Count}/12 ay DSMF tarixçəsi var — hesablama dəqiq olmaya bilər.";
            else if (denominator <= 0)
                xeberdarliq = "Məxrəc 0 və ya mənfi çıxdı. Son 12 ay xəstəlik günü çoxdur.";
            else if (dsmfGun > 0)
                xeberdarliq = $"14 gündən artıq ({dsmfGun} gün) DSMF tərəfindən ödənilir (şirkət limiti 14).";

            return new XestelikPreviewDto
            {
                TeqvimGunSayi = cariTeqvimGun,
                DsmfIsciCem = Math.Round(dsmfIsciCem, 2),
                DsmfIsegoturenCem = Math.Round(dsmfIsvCem, 2),
                DsmfEmsali = emsal,
                Son12AyTeqvimGunu = son12AyTeqvimGun,
                Son12AyXestelikTeqvimGunu = son12XstTeqvimGun,
                Son12AyBaslangic = ayBaslangic,
                Son12AyBitis = ayBitis,
                BirGunluk = Math.Round(birGunluk, 4),
                SirketGun = sirketGun,
                DsmfGun = dsmfGun,
                SirketOdenis = sirketOd,
                DsmfOdenis = dsmfOd,
                Addimlar = izah,
                Duslar = $"({dsmfIsciCem:N2}+{dsmfIsvCem:N2})×{emsal:G29} ÷ ({son12AyTeqvimGun}−{son12XstTeqvimGun}) × {sirketGun}",
                Xeberdarliq = xeberdarliq
            };
        }

        // Son 12 ayda işçinin xəstəlik təqvim günlərini hesablayır
        // (Xestelik cədvəlindən, hər bülletənin intervala kəsilmiş hissəsi cəmlənir)
        private async Task<int> Son12AyXestelikTeqvimGunAsync(int isciId, DateTime ayBaslangic, DateTime ayBitis)
        {
            var xestelikler = await _unitOfWork.Repository<Xestelik>()
                .Query()
                .Where(x => x.IsciId == isciId
                         && !x.Silinib
                         && x.Status == XestelikStatus.Tesdiqlenib
                         && x.BaslamaTarixi <= ayBitis
                         && x.BitmeTarixi >= ayBaslangic)
                .ToListAsync();

            int cem = 0;
            foreach (var x in xestelikler)
            {
                var bas = x.BaslamaTarixi.Date < ayBaslangic.Date ? ayBaslangic.Date : x.BaslamaTarixi.Date;
                var bit = x.BitmeTarixi.Date   > ayBitis.Date     ? ayBitis.Date     : x.BitmeTarixi.Date;
                if (bit >= bas) cem += (bit - bas).Days + 1;
            }
            return cem;
        }

        private async Task<decimal> GetXestelikEmsaliAsync(DateTime referansTarix)
        {
            var p = (await _unitOfWork.Repository<MaasParametri>()
                .HamisiniGetirAsync(x =>
                    x.Aktivdir && !x.Silinib &&
                    x.Nov == MaasParametrNovu.XestelikDsmfEmsali &&
                    x.BaslamaTarixi <= referansTarix &&
                    (x.BitmeTarixi == null || x.BitmeTarixi >= referansTarix)))
                .OrderByDescending(x => x.BaslamaTarixi)
                .FirstOrDefault();
            return p?.Deyer ?? 4m;
        }

        public async Task<List<Xestelik>> AyUzreXestelikleriGetirAsync(int isciId, int il, int ay)
        {
            var ayBaslangic = new DateTime(il, ay, 1);
            var ayBitis = ayBaslangic.AddMonths(1).AddDays(-1);

            return await _unitOfWork.Repository<Xestelik>()
                .Query()
                .Where(x =>
                    x.IsciId == isciId &&
                    !x.Silinib &&
                    x.Status == XestelikStatus.Tesdiqlenib &&
                    x.BaslamaTarixi <= ayBitis &&
                    x.BitmeTarixi >= ayBaslangic)
                .Include(x => x.Odenisler)
                .ToListAsync();
        }

        // ─────────────────────────────────────────────────────────
        // KÖMƏKÇI METODLAR
        // ─────────────────────────────────────────────────────────

        /// <summary>
        /// Tarix aralığında iş günlərini sayır.
        ///
        /// Qaydalar:
        ///   Default: Bazar ertəsi-Cümə = iş, Şənbə-Bazar = istirahət
        ///   Override (BayramGunu.Tip):
        ///     - Bayram → həmin gün istirahət (iş günü olsa belə)
        ///     - IsGunu → həmin gün iş (şənbə/bazar olsa belə)
        /// </summary>
        private async Task<int> IsGunSayiniHesablaAsync(DateTime baslama, DateTime bitme)
        {
            var ozelGunler = await _unitOfWork.Repository<BayramGunu>()
                .HamisiniGetirAsync(x =>
                    x.Tarix >= baslama &&
                    x.Tarix <= bitme &&
                    !x.Silinib);

            // Dictionary: Tarix → Tip
            var ozelDict = ozelGunler
                .GroupBy(x => x.Tarix.Date)
                .ToDictionary(g => g.Key, g => g.First().Tip);

            int sayi = 0;
            for (var t = baslama.Date; t <= bitme.Date; t = t.AddDays(1))
            {
                if (ozelDict.TryGetValue(t, out var tip))
                {
                    // Override mövcuddur
                    if (tip == GunTipi.IsGunu) sayi++;
                    // Bayram → sayılmır
                }
                else
                {
                    // Default: şənbə-bazar istirahət, qalan iş
                    if (t.DayOfWeek != DayOfWeek.Saturday &&
                        t.DayOfWeek != DayOfWeek.Sunday)
                        sayi++;
                }
            }
            return sayi;
        }

        /// <summary>
        /// Verilmiş tarix əsasında son 12 ayın iş günlərinin cəmini hesablayır.
        /// Ayın 1-dən başlayır, xəstəliyin başladığı aydan əvvəlki 12 ay götürülür.
        ///
        /// Məsələn, baslama = 14.04.2026 ⇒ 01.04.2025 → 31.03.2026 (12 tam ay)
        ///
        /// Daxili olaraq IsGunSayiniHesablaAsync istifadə edir
        /// (şənbə, bazar və BayramGunu cədvəlindəki bayramları çıxır).
        /// </summary>
        private async Task<int> Son12AyIsGununuHesablaAsync(DateTime baslama)
        {
            // Xəstəlik başlayan aydan əvvəlki 12 tam ay
            var ayBitis = new DateTime(baslama.Year, baslama.Month, 1).AddDays(-1);
            var ayBaslangic = new DateTime(ayBitis.Year, ayBitis.Month, 1).AddMonths(-11);

            return await IsGunSayiniHesablaAsync(ayBaslangic, ayBitis);
        }

        /// <summary>
        /// Cari ildə bu işçinin artıq aldığı şirkət xəstəlik günləri.
        /// (14 gün limitinin nə qədər istifadə olunduğunu yoxlamaq üçün)
        /// </summary>
        private async Task<int> OncekiSirketGununuTapAsync(int isciId, int il)
        {
            return await _unitOfWork.Repository<XestelikOdenis>()
                .Query()
                .Where(x => x.IsciId == isciId && x.Il == il && !x.Silinib)
                .SumAsync(x => x.SirketGunSayi);
        }

        /// <summary>
        /// Yeni xəstəlik üçün ödəniş qeydlərini yaradır (hər ay üzrə).
        /// YENİ DSMF-əsaslı düstur (May 2026+):
        ///   GünlükPul = ((son 12 ay DSMF işçi + DSMF şirkət) × Əmsal)
        ///               ÷ (son 12 ay təqvim günü − xəstəlik təqvim günü)
        /// 14 təqvim günü limiti per-bülletəndir.
        /// Staj faizi YOX.
        /// </summary>
        private async Task OdenisleriYaratAsync(Xestelik xestelik)
        {
            // 12 ay dövrü
            var ayBitis = new DateTime(xestelik.BaslamaTarixi.Year, xestelik.BaslamaTarixi.Month, 1).AddDays(-1);
            var ayBaslangic = new DateTime(ayBitis.Year, ayBitis.Month, 1).AddMonths(-11);

            // Son 12 ay DSMF cəmi (işçi + şirkət)
            var son12 = await _unitOfWork.Repository<IsciAyliqQazanc>().Query()
                .Where(x => x.IsciId == xestelik.IsciId && !x.Silinib
                         && ((x.Il > ayBaslangic.Year)
                             || (x.Il == ayBaslangic.Year && x.Ay >= ayBaslangic.Month))
                         && ((x.Il < ayBitis.Year)
                             || (x.Il == ayBitis.Year && x.Ay <= ayBitis.Month)))
                .ToListAsync();

            decimal dsmfCem = son12.Sum(x => x.DsmfIsci + x.DsmfIsegoturen);
            decimal emsal = await GetXestelikEmsaliAsync(xestelik.BaslamaTarixi);
            decimal numerator = dsmfCem * emsal;

            int son12AyTeqvimGun = (ayBitis.Date - ayBaslangic.Date).Days + 1;
            int son12XstTeqvimGun = await Son12AyXestelikTeqvimGunAsync(
                xestelik.IsciId, ayBaslangic, ayBitis);
            int denominator = son12AyTeqvimGun - son12XstTeqvimGun;

            decimal birGunluk = denominator > 0 ? numerator / denominator : 0m;

            // Hər aya bülletənin təqvim günlərini bölüşdür, 14 gün şirkət limiti
            int limitQalan = SIRKET_MAX_GUN;

            var aylar = AylaraBolAsync(xestelik.BaslamaTarixi, xestelik.BitmeTarixi);
            foreach (var (il, ay, ayBaslama, ayBitme) in aylar)
            {
                int ayTeqvimGun = (ayBitme.Date - ayBaslama.Date).Days + 1;
                if (ayTeqvimGun <= 0) continue;

                int aySirketGun = Math.Min(ayTeqvimGun, limitQalan);
                int ayDsmfGun = ayTeqvimGun - aySirketGun;
                limitQalan -= aySirketGun;

                var odenis = new XestelikOdenis
                {
                    XestelikId = xestelik.Id,
                    IsciId = xestelik.IsciId,
                    Il = il,
                    Ay = ay,
                    BirGunluk = Math.Round(birGunluk, 4),
                    SirketGunSayi = aySirketGun,
                    DsmfGunSayi = ayDsmfGun,
                    SirketOdenis = Math.Round(birGunluk * aySirketGun, 2),
                    DsmfOdenis = Math.Round(birGunluk * ayDsmfGun, 2)
                };

                await _unitOfWork.Repository<XestelikOdenis>().YaratAsync(odenis);
            }

            await _unitOfWork.YaddaSaxlaAsync();
        }

        /// <summary>
        /// Tarix aralığını aylara bölür (məs. 25.03 → 08.04 → 2 ay).
        /// </summary>
        private List<(int il, int ay, DateTime baslama, DateTime bitme)> AylaraBolAsync(DateTime baslama, DateTime bitme)
        {
            var nticeler = new List<(int, int, DateTime, DateTime)>();
            var current = new DateTime(baslama.Year, baslama.Month, 1);

            while (current <= bitme)
            {
                var ayBitis = current.AddMonths(1).AddDays(-1);
                var fact_baslama = current < baslama ? baslama : current;
                var fact_bitme = ayBitis > bitme ? bitme : ayBitis;

                if (fact_baslama <= fact_bitme)
                    nticeler.Add((current.Year, current.Month, fact_baslama, fact_bitme));

                current = current.AddMonths(1);
            }

            return nticeler;
        }
    }
}
