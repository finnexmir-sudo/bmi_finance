using System.Text.Json;
using System.Text.Json.Serialization;
using FinNex.Application.DTOs.HR.Mezuniyyet;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.HR
{
    public class MezuniyyetHuquqService : IMezuniyyetHuquqService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMezuniyyetHuquqParametrleriService _parametrleriService;

        public MezuniyyetHuquqService(IUnitOfWork uow, IMezuniyyetHuquqParametrleriService parametrleriService)
        {
            _uow = uow;
            _parametrleriService = parametrleriService;
        }

        public async Task<IList<MezuniyyetHuquqDto>> HesablaAsync(DateTime? tarix = null)
        {
            var refTarix = (tarix ?? DateTime.Today).Date;

            // Qanuni ədədlər (yaş həddi, əlavə gün, staj pilləsi) — əvvəllər
            // burada HARDCODE idi, indi HR-in bir dəfə qurduğu parametrdən
            // gəlir (30.09.2026, istifadəçi tələbi). Sətir tapılmasa servis
            // koddakı köhnə dəyərlərlə EYNİ default qaytarır.
            var p = await _parametrleriService.AlAsync();

            // Aktiv işçilər + aktiv təyinat (vəzifə + şöbə). Yalnız oxuma.
            var isciler = await _uow.Repository<Isci>()
                .Query()
                .AsNoTracking()
                .Where(x => !x.Silinib && x.Status == IsciStatus.Aktiv)
                .Include(x => x.IsciTeyinatlari.Where(t => t.Aktivdir && !t.Silinib))
                    .ThenInclude(t => t.Vezife)
                .Include(x => x.IsciTeyinatlari.Where(t => t.Aktivdir && !t.Silinib))
                    .ThenInclude(t => t.Departament)
                .Include(x => x.Usaqlar.Where(u => !u.Silinib))
                .ToListAsync();

            // Əlil işçilər: güzəşt təyinatı (Novu = Elillik), refTarix-də aktiv.
            // Əlillik "adla" yox, güzəşt TİPİ ilə tanınır (1b markeri).
            var elilIds = (await _uow.Repository<IsciGuzest>()
                .Query()
                .AsNoTracking()
                .Where(ig => !ig.Silinib
                          && !ig.Guzest.Silinib
                          && ig.Guzest.Novu == GuzestNovu.Elillik
                          && ig.BaslamaTarixi.Date <= refTarix
                          && (ig.BitmeTarixi == null || ig.BitmeTarixi.Value.Date >= refTarix))
                .Select(ig => ig.IsciId)
                .Distinct()
                .ToListAsync())
                .ToHashSet();

            var netice = new List<MezuniyyetHuquqDto>();

            foreach (var isci in isciler)
            {
                var teyinat = isci.IsciTeyinatlari.FirstOrDefault(t => t.Aktivdir && !t.Silinib);
                int vezifeGun = teyinat?.Vezife?.EsasMezuniyyetGunu ?? 0;
                bool elil = elilIds.Contains(isci.Id);

                // Əsas: əlil → p.EsasGunElil (M.119); yoxsa vəzifə 30 → 30; başqa hər şey → p.EsasGunAdi.
                int esas = elil ? p.EsasGunElil : (vezifeGun == 30 ? 30 : p.EsasGunAdi);

                // Staj (ümumi — bütün iş yerləri). Əlildə staj əlavəsi verilmir (M.116.3).
                // 3 pilləli, ƏN YÜKSƏK həddən aşağı yoxlanır (p.SaxlaAsync həddlərin
                // artan sırada olduğunu təmin edir — bax MezuniyyetHuquqParametrleriService).
                double stajIl = UmumiStajIl(isci, refTarix);
                int stajGun = elil ? 0
                            : stajIl >= (double)p.StajHedd3Il ? p.StajHedd3Gun
                            : stajIl >= (double)p.StajHedd2Il ? p.StajHedd2Gun
                            : stajIl >= (double)p.StajHedd1Il ? p.StajHedd1Gun
                            : 0;

                // Uşaq (M.117): uşaqlar doğum tarixi ilə (IsciUsaq). Yaş avtomatik.
                // İl-sonu qoruma (M.117.3): uşaq il ərzində yaş həddinə (əlildə ayrıca
                // həddə) çatanda, doğum ili + hədd cari ilin 1 yanvarından ≥ olarsa
                // hələ də sayılır.
                var ilBasi = new DateTime(refTarix.Year, 1, 1);
                int under14 = isci.Usaqlar.Count(u => !u.Silinib
                    && u.DogumTarixi.Date <= refTarix
                    && u.DogumTarixi.AddYears(p.UsaqYasHeddi) >= ilBasi);
                bool engelliUsaq = isci.Usaqlar.Any(u => !u.Silinib
                    && u.Elillidir
                    && u.DogumTarixi.Date <= refTarix
                    && u.DogumTarixi.AddYears(p.EngelliUsaqYasHeddi) >= ilBasi);

                // Yalnız QADIN, ya da TƏK VALİDEYN ata alır (M.117.1–2).
                // Əlil işçi (119) uşaq əlavəsini də ALMIR (M.117.4).
                // ƏN YÜKSƏK hədd əvvəl yoxlanır (defolt dəyərlərdə əvvəlki
                // hardcode "under14==2 → 2" ilə tam eynidir, çünki >=3 qolu
                // artıq üstündə tutulur; hədd dəyişsə də məntiqli qalır).
                bool uygunSexs = isci.Cins != Cins.Kisi || isci.TekValideyn;
                int usaqGun = (elil || !uygunSexs) ? 0
                            : (under14 >= p.UsaqSayi5GunHeddi || engelliUsaq) ? p.UsaqGun5
                            : (under14 >= p.UsaqSayi2GunHeddi ? p.UsaqGun2 : 0);

                netice.Add(new MezuniyyetHuquqDto
                {
                    IsciId          = isci.Id,
                    IsciAdSoyad     = $"{isci.Ad} {isci.Soyad}",
                    SobeAdi         = teyinat?.Departament?.Ad ?? "-",
                    VezifeAdi       = teyinat?.Vezife?.Ad ?? "-",
                    Elildir         = elil,
                    VezifeGun       = vezifeGun,
                    EsasGun         = esas,
                    StajIl          = Math.Round(stajIl, 1),
                    StajGun         = stajGun,
                    UsaqSayi        = under14,
                    EngelliUsaqVar  = engelliUsaq,
                    UsaqGun         = usaqGun,
                    IllikHuquq      = esas + stajGun + usaqGun,
                    IsheQebulTarixi = isci.IsheQebulTarixi
                });
            }

            return netice
                .OrderBy(x => x.SobeAdi)
                .ThenBy(x => x.IsciAdSoyad)
                .ToList();
        }

        // Ümumi staj (il) = əvvəlki iş dövrləri (EvvelkiStajPeriodlari JSON) + bu bankda staj.
        private static double UmumiStajIl(Isci isci, DateTime refTarix)
        {
            double gun = 0;

            // Bu bankda staj: IsheQebulTarixi → refTarix
            if (isci.IsheQebulTarixi != default && isci.IsheQebulTarixi.Date <= refTarix)
                gun += (refTarix - isci.IsheQebulTarixi.Date).TotalDays;

            // Əvvəlki iş dövrləri: [{"s":"2010-01-15","e":"2015-06-30"}, ...]
            if (!string.IsNullOrWhiteSpace(isci.EvvelkiStajPeriodlari))
            {
                try
                {
                    var dovrler = JsonSerializer.Deserialize<List<StajDovru>>(isci.EvvelkiStajPeriodlari);
                    if (dovrler != null)
                    {
                        foreach (var d in dovrler)
                        {
                            if (DateTime.TryParse(d.S, out var bas)
                                && DateTime.TryParse(d.E, out var son)
                                && son.Date > bas.Date)
                            {
                                gun += (son.Date - bas.Date).TotalDays;
                            }
                        }
                    }
                }
                catch
                {
                    // Pozğun JSON — yalnız bank stajı sayılır (hesablama pozulmasın).
                }
            }

            return gun / 365.25;
        }

        private sealed class StajDovru
        {
            [JsonPropertyName("s")] public string? S { get; set; }
            [JsonPropertyName("e")] public string? E { get; set; }
        }
    }
}
