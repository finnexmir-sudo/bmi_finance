using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.HesabatIzleme;
using FinNex.Application.Interfaces.HR;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.HR
{
    public class HesabatIzlemeService : IHesabatIzlemeService
    {
        private readonly IUnitOfWork _uow;

        public HesabatIzlemeService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // ═══════════════════════════════════════
        // KATEQORİYALAR
        // ═══════════════════════════════════════

        public async Task<Result<IList<HesabatKateqoriyaDto>>> GetKateqoriyalarAsync()
        {
            var list = await _uow.Repository<HesabatKateqoriyasi>()
                .Query()
                .Where(x => !x.Silinib && x.Aktivdir)
                .OrderBy(x => x.Ad)
                .Select(x => new HesabatKateqoriyaDto { Id = x.Id, Ad = x.Ad })
                .ToListAsync();
            return Result<IList<HesabatKateqoriyaDto>>.Ok(list);
        }

        public async Task<Result> KateqoriyaYaratAsync(string ad)
        {
            var movcud = await _uow.Repository<HesabatKateqoriyasi>()
                .AnyAsync(x => x.Ad == ad && !x.Silinib);
            if (movcud) return Result.Fail("Bu kateqoriya artıq mövcuddur.");

            await _uow.Repository<HesabatKateqoriyasi>().YaratAsync(new HesabatKateqoriyasi { Ad = ad });
            await _uow.YaddaSaxlaAsync();
            return Result.Ok("Kateqoriya yaradıldı.");
        }

        public async Task<Result> KateqoriyaSilAsync(int id)
        {
            var sablonVar = await _uow.Repository<HesabatSablonu>()
                .AnyAsync(x => x.KateqoriyaId == id && !x.Silinib);
            if (sablonVar) return Result.Fail("Bu kateqoriyaya bağlı şablonlar var, silinə bilməz.");

            await _uow.Repository<HesabatKateqoriyasi>().YumshakSilAsync(id);
            await _uow.YaddaSaxlaAsync();
            return Result.Ok("Kateqoriya silindi.");
        }

        // ═══════════════════════════════════════
        // ŞABLONLAR
        // ═══════════════════════════════════════

        public async Task<Result<IList<HesabatSablonListDto>>> GetSablonlarAsync(int? departamentId = null)
        {
            var query = _uow.Repository<HesabatSablonu>()
                .Query()
                .Where(x => !x.Silinib && x.Aktivdir)
                .Include(x => x.MesulIsci)
                .Include(x => x.Departament)
                .Include(x => x.Kateqoriya)
                .AsQueryable();

            if (departamentId.HasValue)
                query = query.Where(x => x.DepartamentId == departamentId.Value);

            var list = await query
                .OrderBy(x => x.Tezlik)
                .ThenBy(x => x.Ad)
                .Select(x => new HesabatSablonListDto
                {
                    Id = x.Id,
                    Ad = x.Ad,
                    Tesvir = x.Tesvir,
                    Tezlik = x.Tezlik,
                    KateqoriyaId = x.KateqoriyaId,
                    KateqoriyaAd = x.Kateqoriya.Ad,
                    Prioritet = x.Prioritet,
                    SonTarixGunu = x.SonTarixGunu,
                    SonTarixSaati = x.SonTarixSaati,
                    MesulIsciAd = x.MesulIsci.Ad + " " + x.MesulIsci.Soyad,
                    MesulIsciId = x.MesulIsciId,
                    DepartamentAd = x.Departament.Ad,
                    DepartamentId = x.DepartamentId,
                    Aktivdir = x.Aktivdir
                })
                .ToListAsync();

            return Result<IList<HesabatSablonListDto>>.Ok(list);
        }

        public async Task<Result> SablonYaratAsync(HesabatSablonCreateDto dto)
        {
            var sablon = new HesabatSablonu
            {
                Ad = dto.Ad,
                Tesvir = dto.Tesvir,
                Tezlik = dto.Tezlik,
                KateqoriyaId = dto.KateqoriyaId,
                Prioritet = dto.Prioritet,
                SonTarixGunu = dto.SonTarixGunu,
                SonTarixSaati = dto.SonTarixSaati,
                MesulIsciId = dto.MesulIsciId,
                DepartamentId = dto.DepartamentId,
                Aktivdir = true
            };

            await _uow.Repository<HesabatSablonu>().YaratAsync(sablon);
            await _uow.YaddaSaxlaAsync();
            return Result.Ok("Hesabat şablonu yaradıldı.");
        }

        public async Task<Result> SablonSilAsync(int id)
        {
            await _uow.Repository<HesabatSablonu>().YumshakSilAsync(id);
            await _uow.YaddaSaxlaAsync();
            return Result.Ok("Şablon silindi.");
        }

        // ═══════════════════════════════════════
        // TAPŞIRIQLAR
        // ═══════════════════════════════════════

        public async Task<Result<HesabatIzlemeDashboardDto>> GetDashboardAsync(int? departamentId = null)
        {
            var bugun = DateTime.Today;
            var ayBasi = new DateTime(bugun.Year, bugun.Month, 1);
            var aySonu = ayBasi.AddMonths(1).AddDays(-1);

            var query = _uow.Repository<HesabatTapshiriq>()
                .Query()
                .Where(x => !x.Silinib)
                .Include(x => x.Sablon).ThenInclude(s => s.MesulIsci)
                .Include(x => x.Sablon).ThenInclude(s => s.Departament)
                .Include(x => x.Sablon).ThenInclude(s => s.Kateqoriya)
                .Include(x => x.IcraEdenIsci)
                .AsQueryable();

            if (departamentId.HasValue)
                query = query.Where(x => x.Sablon.DepartamentId == departamentId.Value);

            var buAy = await query
                .Where(x => x.DovrBaslangic >= ayBasi && x.DovrBaslangic <= aySonu)
                .OrderBy(x => x.Deadline)
                .ToListAsync();

            var bugunku = buAy.Where(x => x.DovrBaslangic.Date == bugun).ToList();

            var gecikenler = await query
                .Where(x => x.Status != HesabatTapshiriqStatus.Tamamlandi && x.Deadline < DateTime.Now)
                .OrderBy(x => x.Deadline)
                .Take(20)
                .ToListAsync();

            var umumi = buAy.Count;
            var tamamlanan = buAy.Count(x => x.Status == HesabatTapshiriqStatus.Tamamlandi);
            var gozleyen = buAy.Count(x => x.Status == HesabatTapshiriqStatus.Gozleyir || x.Status == HesabatTapshiriqStatus.Icrada);
            var geciken = buAy.Count(x => x.Status != HesabatTapshiriqStatus.Tamamlandi && x.Deadline < DateTime.Now);

            var sablonResult = await GetSablonlarAsync(departamentId);
            var katResult = await GetKateqoriyalarAsync();

            var vm = new HesabatIzlemeDashboardDto
            {
                Umumi = umumi,
                Tamamlanan = tamamlanan,
                Gozleyen = gozleyen,
                Geciken = geciken,
                TamamlanmaFaizi = umumi > 0 ? (int)((double)tamamlanan / umumi * 100) : 0,
                BugunkuTapshiriqlar = bugunku.Select(MapToDto).ToList(),
                BuAyTapshiriqlar = buAy.Select(MapToDto).ToList(),
                GecikenTapshiriqlar = gecikenler.Select(MapToDto).ToList(),
                Sablonlar = sablonResult.Data?.ToList() ?? new(),
                Kateqoriyalar = katResult.Data?.ToList() ?? new()
            };

            return Result<HesabatIzlemeDashboardDto>.Ok(vm);
        }

        public async Task<Result<IList<HesabatTapshiriqListDto>>> GetTapshiriqlarAsync(int? departamentId = null, int? status = null)
        {
            var query = _uow.Repository<HesabatTapshiriq>()
                .Query()
                .Where(x => !x.Silinib)
                .Include(x => x.Sablon).ThenInclude(s => s.MesulIsci)
                .Include(x => x.Sablon).ThenInclude(s => s.Departament)
                .Include(x => x.Sablon).ThenInclude(s => s.Kateqoriya)
                .Include(x => x.IcraEdenIsci)
                .AsQueryable();

            if (departamentId.HasValue)
                query = query.Where(x => x.Sablon.DepartamentId == departamentId.Value);

            if (status.HasValue)
                query = query.Where(x => (int)x.Status == status.Value);

            var list = await query
                .OrderByDescending(x => x.Deadline)
                .Take(100)
                .ToListAsync();

            return Result<IList<HesabatTapshiriqListDto>>.Ok(
                list.Select(MapToDto).ToList() as IList<HesabatTapshiriqListDto>);
        }

        public async Task<Result> TamamlaAsync(int tapshiriqId, int isciId, string? qeyd)
        {
            var tap = await _uow.Repository<HesabatTapshiriq>().IdIleGetirAsync(tapshiriqId);
            if (tap == null) return Result.Fail("Tapşırıq tapılmadı.");

            tap.Status = HesabatTapshiriqStatus.Tamamlandi;
            tap.IcraEdenIsciId = isciId;
            tap.IcraTarixi = DateTime.Now;
            tap.Qeyd = qeyd;

            await _uow.Repository<HesabatTapshiriq>().YenileAsync(tap);
            await _uow.YaddaSaxlaAsync();
            return Result.Ok("Hesabat tamamlandı olaraq işarələndi.");
        }

        public async Task<Result> IcrayaBaslaAsync(int tapshiriqId, int isciId)
        {
            var tap = await _uow.Repository<HesabatTapshiriq>().IdIleGetirAsync(tapshiriqId);
            if (tap == null) return Result.Fail("Tapşırıq tapılmadı.");

            tap.Status = HesabatTapshiriqStatus.Icrada;
            tap.IcraEdenIsciId = isciId;

            await _uow.Repository<HesabatTapshiriq>().YenileAsync(tap);
            await _uow.YaddaSaxlaAsync();
            return Result.Ok("İcraya başlandı.");
        }

        // ═══════════════════════════════════════
        // AVTOMATİK TAPŞIRIQ YARATMA
        // ═══════════════════════════════════════

        public async Task<Result> DovrTapshiriqlariniYaratAsync()
        {
            var bugun = DateTime.Today;
            var sablonlar = await _uow.Repository<HesabatSablonu>()
                .Query()
                .Where(x => !x.Silinib && x.Aktivdir)
                .ToListAsync();

            foreach (var sablon in sablonlar)
            {
                var (dovrBas, dovrSon, deadline) = HesablaDovrVeDeadline(sablon, bugun);

                var movcud = await _uow.Repository<HesabatTapshiriq>()
                    .AnyAsync(x => x.SablonId == sablon.Id && x.DovrBaslangic == dovrBas && !x.Silinib);

                if (movcud) continue;

                await _uow.Repository<HesabatTapshiriq>().YaratAsync(new HesabatTapshiriq
                {
                    SablonId = sablon.Id,
                    DovrBaslangic = dovrBas,
                    DovrSon = dovrSon,
                    Deadline = deadline,
                    Status = HesabatTapshiriqStatus.Gozleyir
                });
            }

            await _uow.YaddaSaxlaAsync();
            return Result.Ok("Dövr tapşırıqları yaradıldı.");
        }

        public async Task<Result> GecikenleriYoxlaAsync()
        {
            var indi = DateTime.Now;
            var gecikenler = await _uow.Repository<HesabatTapshiriq>()
                .Query()
                .Where(x => !x.Silinib
                    && x.Status != HesabatTapshiriqStatus.Tamamlandi
                    && x.Status != HesabatTapshiriqStatus.Gecikib
                    && x.Deadline < indi)
                .ToListAsync();

            foreach (var tap in gecikenler)
            {
                tap.Status = HesabatTapshiriqStatus.Gecikib;
                await _uow.Repository<HesabatTapshiriq>().YenileAsync(tap);
            }

            await _uow.YaddaSaxlaAsync();
            return Result.Ok($"{gecikenler.Count} tapşırıq gecikib olaraq işarələndi.");
        }

        // ═══════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════

        private static (DateTime dovrBas, DateTime dovrSon, DateTime deadline) HesablaDovrVeDeadline(HesabatSablonu sablon, DateTime bugun)
        {
            DateTime dovrBas, dovrSon, deadline;

            switch (sablon.Tezlik)
            {
                case HesabatTezlik.Gunluk:
                    dovrBas = bugun;
                    dovrSon = bugun;
                    deadline = bugun.Add(sablon.SonTarixSaati);
                    break;
                case HesabatTezlik.Heftelik:
                    var hefteBasi = bugun.AddDays(-(int)bugun.DayOfWeek + (int)DayOfWeek.Monday);
                    dovrBas = hefteBasi;
                    dovrSon = hefteBasi.AddDays(6);
                    var hefdeSonGun = sablon.SonTarixGunu > 0 ? sablon.SonTarixGunu : 5;
                    deadline = hefteBasi.AddDays(hefdeSonGun - 1).Add(sablon.SonTarixSaati);
                    break;
                case HesabatTezlik.Aylik:
                    dovrBas = new DateTime(bugun.Year, bugun.Month, 1);
                    dovrSon = dovrBas.AddMonths(1).AddDays(-1);
                    var ayGunu = sablon.SonTarixGunu > 0 ? Math.Min(sablon.SonTarixGunu, DateTime.DaysInMonth(bugun.Year, bugun.Month)) : DateTime.DaysInMonth(bugun.Year, bugun.Month);
                    deadline = new DateTime(bugun.Year, bugun.Month, ayGunu).Add(sablon.SonTarixSaati);
                    break;
                case HesabatTezlik.Rubluk:
                    var rubBasi = ((bugun.Month - 1) / 3) * 3 + 1;
                    dovrBas = new DateTime(bugun.Year, rubBasi, 1);
                    dovrSon = dovrBas.AddMonths(3).AddDays(-1);
                    var rubSonAy = rubBasi + 2;
                    var rubGun = sablon.SonTarixGunu > 0 ? Math.Min(sablon.SonTarixGunu, DateTime.DaysInMonth(bugun.Year, rubSonAy)) : DateTime.DaysInMonth(bugun.Year, rubSonAy);
                    deadline = new DateTime(bugun.Year, rubSonAy, rubGun).Add(sablon.SonTarixSaati);
                    break;
                case HesabatTezlik.Illik:
                    dovrBas = new DateTime(bugun.Year, 1, 1);
                    dovrSon = new DateTime(bugun.Year, 12, 31);
                    var ilGun = sablon.SonTarixGunu > 0 ? Math.Min(sablon.SonTarixGunu, 31) : 31;
                    deadline = new DateTime(bugun.Year, 12, ilGun).Add(sablon.SonTarixSaati);
                    break;
                default:
                    dovrBas = bugun; dovrSon = bugun; deadline = bugun.Add(sablon.SonTarixSaati);
                    break;
            }

            return (dovrBas, dovrSon, deadline);
        }

        private static HesabatTapshiriqListDto MapToDto(HesabatTapshiriq x) => new()
        {
            Id = x.Id,
            SablonId = x.SablonId,
            SablonAd = x.Sablon?.Ad ?? "—",
            SablonTesvir = x.Sablon?.Tesvir,
            Tezlik = x.Sablon?.Tezlik ?? HesabatTezlik.Gunluk,
            KateqoriyaAd = x.Sablon?.Kateqoriya?.Ad ?? "—",
            KateqoriyaId = x.Sablon?.KateqoriyaId ?? 0,
            Prioritet = x.Sablon?.Prioritet ?? HesabatPrioritet.Orta,
            MesulIsciAd = x.Sablon?.MesulIsci != null ? $"{x.Sablon.MesulIsci.Ad} {x.Sablon.MesulIsci.Soyad}" : "—",
            DepartamentAd = x.Sablon?.Departament?.Ad ?? "—",
            DepartamentId = x.Sablon?.DepartamentId ?? 0,
            DovrBaslangic = x.DovrBaslangic,
            DovrSon = x.DovrSon,
            Deadline = x.Deadline,
            Status = x.Status,
            IcraEdenAd = x.IcraEdenIsci != null ? $"{x.IcraEdenIsci.Ad} {x.IcraEdenIsci.Soyad}" : null,
            IcraTarixi = x.IcraTarixi,
            Qeyd = x.Qeyd
        };
    }
}
