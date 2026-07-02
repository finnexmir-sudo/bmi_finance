using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Jeton;
using FinNex.Application.DTOs.HR.Motivasya;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.HR
{
    public class JetonTeklifleriService : IJetonTeklifleriService
    {
        private readonly IUnitOfWork _uow;
        private readonly IJetonService _jetonService;
        private const int NaharAraSiDeqiqe = 45;

        public JetonTeklifleriService(IUnitOfWork uow, IJetonService jetonService)
        {
            _uow = uow;
            _jetonService = jetonService;
        }

        public async Task<IList<JetonTeklifiDto>> GetGozleyenlerAsync(int? departamentId = null)
        {
            var query = _uow.Repository<JetonTeklifi>()
                .Query()
                .Include(x => x.Isci)
                    .ThenInclude(i => i.IsciTeyinatlari.Where(t => t.Aktivdir))
                        .ThenInclude(t => t.Departament)
                .Include(x => x.Isci)
                    .ThenInclude(i => i.IsciTeyinatlari.Where(t => t.Aktivdir))
                        .ThenInclude(t => t.Vezife)
                .Where(x => x.Status == JetonTeklifinStatusu.Gozlenir);

            if (departamentId.HasValue)
                query = query.Where(x => x.Isci.IsciTeyinatlari
                    .Any(t => t.Aktivdir && t.DepartamentId == departamentId.Value));

            var list = await query
                .OrderByDescending(x => x.YaradilmaTarixi)
                .ToListAsync();

            // Eyni mənbədən (işçi + növ + əlavə məlumat) yaranan dublikatları göstərmə.
            var dedup = list
                .GroupBy(x => $"{x.IsciId}|{(int)x.TeklifNovu}|{(string.IsNullOrEmpty(x.ElaveMelumat) ? x.Id.ToString() : x.ElaveMelumat)}")
                .Select(g => g.First())
                .ToList();

            return dedup.Select(MapDto).ToList();
        }

        public async Task<Result> JetonVerAsync(JetonTeklifiVerDto dto, int verenUserId)
        {
            var teklif = await _uow.Repository<JetonTeklifi>()
                .Query().FirstOrDefaultAsync(x => x.Id == dto.TeklifId);
            if (teklif == null) return Result.Fail("Teklif tapılmadı.");
            if (teklif.Status != JetonTeklifinStatusu.Gozlenir)
                return Result.Fail("Bu teklif artıq işlənilmişdir.");

            var jResult = await _jetonService.JetonVerAsync(new IsciJetonuCreateDto
            {
                IsciId = teklif.IsciId,
                JetonTeyinatiId = dto.JetonTeyinatiId,
                Sebeb = dto.Qeyd ?? teklif.Metn
            }, verenUserId);

            if (!jResult.Success) return jResult;

            teklif.Status = JetonTeklifinStatusu.JetonVerildi;
            teklif.IslemEdenUserId = verenUserId;
            teklif.IslemTarixi = DateTime.Now;
            await _uow.Repository<JetonTeklifi>().YenileAsync(teklif);
            await _uow.YaddaSaxlaAsync();
            return Result.Ok("Jeton verildi.");
        }

        public async Task<Result> ReddetAsync(int teklifId, int userId)
        {
            var teklif = await _uow.Repository<JetonTeklifi>()
                .Query().FirstOrDefaultAsync(x => x.Id == teklifId);
            if (teklif == null) return Result.Fail("Teklif tapılmadı.");

            teklif.Status = JetonTeklifinStatusu.Reddi;
            teklif.IslemEdenUserId = userId;
            teklif.IslemTarixi = DateTime.Now;
            await _uow.Repository<JetonTeklifi>().YenileAsync(teklif);
            await _uow.YaddaSaxlaAsync();
            return Result.Ok("Rədd edildi.");
        }

        public async Task TapshiriqTamamlandiAsync(int tapshiriqId)
        {
            var t = await _uow.Repository<Tapshiriq>()
                .Query()
                .Include(x => x.TeyinOlunanIsci)
                .FirstOrDefaultAsync(x => x.Id == tapshiriqId);
            if (t == null) return;
            if (t.TeyinOlunanIsci?.Status == IsciStatus.IshtenCixib) return;

            // Avoid duplicates
            var artiqVar = await _uow.Repository<JetonTeklifi>()
                .Query()
                .AnyAsync(x => x.IsciId == t.TeyinOlunanIsciId
                    && x.TeklifNovu == JetonTeklifinNovu.TapshiriqTamamlandi
                    && x.ElaveMelumat == tapshiriqId.ToString()
                    && x.Status == JetonTeklifinStatusu.Gozlenir);
            if (artiqVar) return;

            await YaratAsync(new JetonTeklifi
            {
                IsciId = t.TeyinOlunanIsciId,
                TeklifNovu = JetonTeklifinNovu.TapshiriqTamamlandi,
                Metn = $"\"{t.Bashliq}\" tapşırığını tamamladı",
                ElaveMelumat = tapshiriqId.ToString()
            });
        }

        public async Task EvezediciQebulEdildiAsync(int evezediciTesdiqId)
        {
            var e = await _uow.Repository<EvezediciTesdiq>()
                .Query()
                .Include(x => x.EvezediciIsci)
                .Include(x => x.Mezuniyyet)
                    .ThenInclude(m => m.Isci)
                .FirstOrDefaultAsync(x => x.Id == evezediciTesdiqId);
            if (e == null) return;
            if (e.EvezediciIsci?.Status == IsciStatus.IshtenCixib) return;

            var artiqVar = await _uow.Repository<JetonTeklifi>()
                .Query()
                .AnyAsync(x => x.IsciId == e.EvezediciIsciId
                    && x.TeklifNovu == JetonTeklifinNovu.EvezediciOldu
                    && x.ElaveMelumat == evezediciTesdiqId.ToString()
                    && x.Status == JetonTeklifinStatusu.Gozlenir);
            if (artiqVar) return;

            var mezunIsci = e.Mezuniyyet.Isci;
            await YaratAsync(new JetonTeklifi
            {
                IsciId = e.EvezediciIsciId,
                TeklifNovu = JetonTeklifinNovu.EvezediciOldu,
                Metn = $"{mezunIsci.Ad} {mezunIsci.Soyad} əvəzinə işə çıxdı",
                ElaveMelumat = evezediciTesdiqId.ToString()
            });
        }

        public async Task DavamiyyetYoxlaAsync(int davamiyyetId)
        {
            var d = await _uow.Repository<Davamiyyet>()
                .Query()
                .Include(x => x.Isci)
                .FirstOrDefaultAsync(x => x.Id == davamiyyetId);
            if (d == null) return;
            if (d.Isci?.Status == IsciStatus.IshtenCixib) return;

            // 1. Work hours exceeded check
            if (d.GirisVaxti.HasValue && d.CixisVaxti.HasValue)
            {
                var parametri = await _uow.Repository<IsParametri>()
                    .Query().FirstOrDefaultAsync() ?? new IsParametri();

                var standartDaqiqe = (parametri.StandartCixisVaxti - parametri.StandartGirisVaxti).TotalMinutes;
                var faktikiDaqiqe = (d.CixisVaxti.Value - d.GirisVaxti.Value).TotalMinutes - NaharAraSiDeqiqe;

                var artiqIshle = (int)(faktikiDaqiqe - standartDaqiqe);
                // 0 (kəsr dəqiqə → yuvarlaqda 0) üçün jeton təklifi yaradılmır.
                if (artiqIshle > 0)
                {
                    var artiqVar = await _uow.Repository<JetonTeklifi>()
                        .Query()
                        .AnyAsync(x => x.IsciId == d.IsciId
                            && x.TeklifNovu == JetonTeklifinNovu.IsGununuAshdi
                            && x.ElaveMelumat == davamiyyetId.ToString()
                            && x.Status == JetonTeklifinStatusu.Gozlenir);
                    if (!artiqVar)
                    {
                        await YaratAsync(new JetonTeklifi
                        {
                            IsciId = d.IsciId,
                            TeklifNovu = JetonTeklifinNovu.IsGununuAshdi,
                            Metn = $"{d.Tarix:dd.MM.yyyy} tarixdə iş normasından {artiqIshle} dəqiqə artıq işlədi",
                            ElaveMelumat = davamiyyetId.ToString()
                        });
                    }
                }
            }

            // 2. Gecikme 5-life check
            if (d.Status == DavamiyyetStatus.Gecikme)
            {
                var ilBaslangic = new DateTime(d.Tarix.Year, 1, 1);
                var ilSonu = ilBaslangic.AddYears(1);

                var gecimeSayi = await _uow.Repository<Davamiyyet>()
                    .Query()
                    .CountAsync(x => x.IsciId == d.IsciId
                        && x.Status == DavamiyyetStatus.Gecikme
                        && x.Tarix >= ilBaslangic && x.Tarix < ilSonu);

                var tetiklenenSayi = await _uow.Repository<JetonTeklifi>()
                    .Query()
                    .CountAsync(x => x.IsciId == d.IsciId
                        && x.TeklifNovu == JetonTeklifinNovu.GecikmeCanDoldu
                        && x.YaradilmaTarixi >= ilBaslangic && x.YaradilmaTarixi < ilSonu);

                if (gecimeSayi / 5 > tetiklenenSayi)
                {
                    await YaratAsync(new JetonTeklifi
                    {
                        IsciId = d.IsciId,
                        TeklifNovu = JetonTeklifinNovu.GecikmeCanDoldu,
                        Metn = $"{d.Tarix.Year} ilində {gecimeSayi}-ci gecikməsi — 5 can doldu, qara jeton tövsiyə olunur",
                        ElaveMelumat = gecimeSayi.ToString()
                    });
                }
            }
        }

        private async Task YaratAsync(JetonTeklifi teklif)
        {
            await _uow.Repository<JetonTeklifi>().YaratAsync(teklif);
            await _uow.YaddaSaxlaAsync();
        }

        private static JetonTeklifiDto MapDto(JetonTeklifi x)
        {
            var teyinat = x.Isci.IsciTeyinatlari.FirstOrDefault(t => t.Aktivdir);
            return new JetonTeklifiDto
            {
                Id = x.Id,
                IsciId = x.IsciId,
                IsciAd = x.Isci.Ad + " " + x.Isci.Soyad,
                Vezife = teyinat?.Vezife?.Ad,
                Departament = teyinat?.Departament?.Ad,
                IsciStatus = x.Isci.Status,
                IsciStatusAd = IsciStatusAdGetir(x.Isci.Status),
                TeklifNovu = x.TeklifNovu,
                TeklifNovuAd = TeklifNovuAdGetir(x.TeklifNovu),
                TeklifNovuReng = TeklifNovuRengGetir(x.TeklifNovu),
                Metn = x.Metn,
                ElaveMelumat = x.ElaveMelumat,
                Status = x.Status,
                YaradilmaTarixi = x.YaradilmaTarixi
            };
        }

        private static string TeklifNovuAdGetir(JetonTeklifinNovu nov) => nov switch
        {
            JetonTeklifinNovu.IsGununuAshdi => "İş normasını aşdı",
            JetonTeklifinNovu.EvezediciOldu => "Əvəzedici oldu",
            JetonTeklifinNovu.TapshiriqTamamlandi => "Tapşırıq tamamlandı",
            JetonTeklifinNovu.GecikmeCanDoldu => "5 gecikme — can doldu",
            _ => nov.ToString()
        };

        private static string TeklifNovuRengGetir(JetonTeklifinNovu nov) => nov switch
        {
            JetonTeklifinNovu.IsGununuAshdi => "#10b981",
            JetonTeklifinNovu.EvezediciOldu => "#3b82f6",
            JetonTeklifinNovu.TapshiriqTamamlandi => "#8b5cf6",
            JetonTeklifinNovu.GecikmeCanDoldu => "#ef4444",
            _ => "#6b7280"
        };

        private static string IsciStatusAdGetir(IsciStatus s) => s switch
        {
            IsciStatus.Aktiv => "İşdədir",
            IsciStatus.Mezuniyyetde => "Məzuniyyətdə",
            IsciStatus.IshtenCixib => "İşdən çıxıb",
            _ => s.ToString()
        };
    }
}
