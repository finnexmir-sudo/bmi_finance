using System.Text.Json;
using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Risk;
using FinNex.Application.Helpers.Risk;
using FinNex.Application.Interfaces.Risk;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Entities.Risk;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.Risk;

public class EmeliyyatRiskiHadisesiService : IEmeliyyatRiskiHadisesiService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmeliyyatRiskiParametrleriService _parametrleriService;
    // "Kim etdi" (YaradanIcraciId/YenileyenIcraciId) AppUser.Id-dir (login
    // edən istifadəçi) — Isci-dən FƏRQLİDİR. Ad üçün UserManager lazımdır,
    // canonical nümunə: ReytingService (_userManager.Users → Ad+Soyad).
    private readonly UserManager<AppUser> _userManager;

    public EmeliyyatRiskiHadisesiService(
        IUnitOfWork unitOfWork,
        IEmeliyyatRiskiParametrleriService parametrleriService,
        UserManager<AppUser> userManager)
    {
        _unitOfWork = unitOfWork;
        _parametrleriService = parametrleriService;
        _userManager = userManager;
    }

    public async Task<IList<EmeliyyatRiskiHadisesiDto>> SiyahiAsync(
        BiznesSahesi? biznesSahesi = null,
        RiskKateqoriyasi1? riskKateqoriyasi1 = null,
        DateTime? basTarix = null,
        DateTime? sonTarix = null)
    {
        var sorgu = _unitOfWork.Repository<EmeliyyatRiskiHadisesi>()
            .Query()
            .AsNoTracking()
            .AsQueryable();

        if (biznesSahesi.HasValue)
            sorgu = sorgu.Where(h => h.BiznesSahesi == biznesSahesi.Value);
        if (riskKateqoriyasi1.HasValue)
            sorgu = sorgu.Where(h => h.RiskKateqoriyasi1 == riskKateqoriyasi1.Value);
        if (basTarix.HasValue)
            sorgu = sorgu.Where(h => h.HadiseninBasVerdiyiTarix.Date >= basTarix.Value.Date);
        if (sonTarix.HasValue)
            sorgu = sorgu.Where(h => h.HadiseninBasVerdiyiTarix.Date <= sonTarix.Value.Date);

        var hadiseler = await sorgu
            .OrderByDescending(h => h.HadiseninBasVerdiyiTarix)
            .ToListAsync();

        var isciAdlari = await IsciAdlariAsync(hadiseler
            .Where(h => h.MueyyenlesdirenIsciId.HasValue)
            .Select(h => h.MueyyenlesdirenIsciId!.Value).Distinct());
        var icraciAdlari = await IcraciAdlariAsync(hadiseler
            .Where(h => h.YaradanIcraciId.HasValue)
            .Select(h => h.YaradanIcraciId!.Value).Distinct());

        return hadiseler.Select(h => Doldur(h, isciAdlari, icraciAdlari)).ToList();
    }

    public async Task<EmeliyyatRiskiHadisesiDto?> DetalAsync(int id)
    {
        var hadise = await _unitOfWork.Repository<EmeliyyatRiskiHadisesi>()
            .Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == id);

        if (hadise == null) return null;

        var isciAdlari = await IsciAdlariAsync(hadise.MueyyenlesdirenIsciId.HasValue
            ? new[] { hadise.MueyyenlesdirenIsciId.Value } : Array.Empty<int>());
        var icraciAdlari = await IcraciAdlariAsync(hadise.YaradanIcraciId.HasValue
            ? new[] { hadise.YaradanIcraciId.Value } : Array.Empty<int>());

        return Doldur(hadise, isciAdlari, icraciAdlari);
    }

    public async Task<IList<EmeliyyatRiskiHadisesiTarixceDto>> TarixceAsync(int hadiseId)
    {
        var tarixce = await _unitOfWork.Repository<EmeliyyatRiskiHadisesiTarixce>()
            .Query()
            .AsNoTracking()
            .Where(t => t.HadiseId == hadiseId)
            .OrderByDescending(t => t.YaradilmaTarixi)
            .ToListAsync();

        var icraciAdlari = await IcraciAdlariAsync(tarixce
            .Where(t => t.YaradanIcraciId.HasValue)
            .Select(t => t.YaradanIcraciId!.Value)
            .Distinct());

        return tarixce.Select(t => new EmeliyyatRiskiHadisesiTarixceDto
        {
            Id = t.Id,
            YaradilmaTarixi = t.YaradilmaTarixi,
            DeyisiklikTesviri = t.DeyisiklikTesviri,
            YaradanIcraciId = t.YaradanIcraciId,
            DeyisdirenIsciAdi = t.YaradanIcraciId.HasValue && icraciAdlari.TryGetValue(t.YaradanIcraciId.Value, out var ad)
                ? ad : null
        }).ToList();
    }

    public async Task<Result<int>> YaratAsync(EmeliyyatRiskiHadisesiCreateDto dto, int? icraciId)
    {
        var parametr = await _parametrleriService.AlAsync();

        var yoxlama = TedbirlerYoxla(dto, parametr.TedbirZererHeddi);
        if (!yoxlama.Success) return Result<int>.Fail(yoxlama.Message!);

        var validasiya = SahelərYoxla(dto);
        if (validasiya != null) return Result<int>.Fail(validasiya);

        var hadise = new EmeliyyatRiskiHadisesi
        {
            QeydiyyatKodu = EmeliyyatRiskiAdlari.QeydiyyatKoduHesabla(dto.BiznesSahesi, dto.RiskKateqoriyasi2),
            YaradanIcraciId = icraciId,
            // Bu andakı həddi DONDURUR — bax entity-dəki qeyd.
            TedbirZererHeddiYaradilmaAninda = parametr.TedbirZererHeddi
        };
        Yaz(hadise, dto);

        var repo = _unitOfWork.Repository<EmeliyyatRiskiHadisesi>();
        await repo.YaratAsync(hadise);
        await _unitOfWork.YaddaSaxlaAsync();

        return Result<int>.Ok(hadise.Id, "Əməliyyat riski hadisəsi qeydə alındı.");
    }

    public async Task<Result> YenileAsync(EmeliyyatRiskiHadisesiUpdateDto dto, int? icraciId)
    {
        if (string.IsNullOrWhiteSpace(dto.DeyisiklikTesviri))
            return Result.Fail("Dəyişikliyin qısa təsviri məcburidir (Əlavə 4, sahə 27) — tarixçədə iz qalmalıdır.");
        if (dto.DeyisiklikTesviri.Length > 500)
            return Result.Fail("Dəyişikliyin qısa təsviri 500 simvoldan çox ola bilməz.");

        var validasiya = SahelərYoxla(dto);
        if (validasiya != null) return Result.Fail(validasiya);

        var repo = _unitOfWork.Repository<EmeliyyatRiskiHadisesi>();
        var hadise = await repo.Query().FirstOrDefaultAsync(h => h.Id == dto.Id);
        if (hadise == null) return Result.Fail("Hadisə tapılmadı.");

        // Tədbir mütləqliyi qeydin YARADILDIĞI andakı dondurulmuş hədlə
        // yoxlanılır (h.TedbirZererHeddiYaradilmaAninda), CARİ parametr-lə
        // YOX — yoxsa hədd sonradan aşağı salınanda köhnə qeydlər adi bir
        // redaktəni (məs. səhv düzəlişini) belə bloklayardı (code-review, 30.09.2026).
        var yoxlama = TedbirlerYoxla(dto, hadise.TedbirZererHeddiYaradilmaAninda);
        if (!yoxlama.Success) return yoxlama;

        // Redaktədən ƏVVƏL tam snapshot — 30.09.2026 qərarı: "saxlanılsın".
        var snapshot = JsonSerializer.Serialize(hadise);
        await _unitOfWork.Repository<EmeliyyatRiskiHadisesiTarixce>().YaratAsync(new EmeliyyatRiskiHadisesiTarixce
        {
            HadiseId = hadise.Id,
            DeyisiklikTesviri = dto.DeyisiklikTesviri.Trim(),
            EvvelkiDeyerlerJson = snapshot,
            YaradanIcraciId = icraciId
        });

        Yaz(hadise, dto);
        hadise.QeydiyyatKodu = EmeliyyatRiskiAdlari.QeydiyyatKoduHesabla(dto.BiznesSahesi, dto.RiskKateqoriyasi2);
        hadise.SonDeyisiklikTesviri = dto.DeyisiklikTesviri.Trim();
        hadise.YenileyenIcraciId = icraciId;
        hadise.YenilenmeTarixi = DateTime.Now;

        await _unitOfWork.YaddaSaxlaAsync();
        return Result.Ok("Hadisə yeniləndi, köhnə vəziyyət tarixçəyə yazıldı.");
    }

    // ── Köməkçi metodlar ─────────────────────────────────────────────────

    private static void Yaz(EmeliyyatRiskiHadisesi h, EmeliyyatRiskiHadisesiCreateDto dto)
    {
        h.StrukturBolme = dto.StrukturBolme.Trim();
        h.MelumatiVerenSexs = string.IsNullOrWhiteSpace(dto.MelumatiVerenSexs) ? null : dto.MelumatiVerenSexs.Trim();
        h.HadiseninBasVerdiyiTarix = dto.HadiseninBasVerdiyiTarix;
        h.HadiseninMueyyenlesdirilmeTarixi = dto.HadiseninMueyyenlesdirilmeTarixi;
        h.MueyyenlesdirenIsciId = dto.MueyyenlesdirenIsciId;
        h.Tesvir = dto.Tesvir.Trim();
        h.Sebeb = dto.Sebeb.Trim();
        h.TezlikDerecesi = dto.TezlikDerecesi;
        h.TesirDerecesi = dto.TesirDerecesi;
        h.BiznesSahesi = dto.BiznesSahesi;
        h.BankMehsulu = string.IsNullOrWhiteSpace(dto.BankMehsulu) ? null : dto.BankMehsulu.Trim();
        h.RiskKateqoriyasi1 = dto.RiskKateqoriyasi1;
        h.RiskKateqoriyasi2 = dto.RiskKateqoriyasi2;
        h.RiskHadisesiNumune = string.IsNullOrWhiteSpace(dto.RiskHadisesiNumune) ? null : dto.RiskHadisesiNumune.Trim();
        h.ZererTesirKateqoriyasi = dto.ZererTesirKateqoriyasi;
        h.UmumiZererMebleg = dto.UmumiZererMebleg;
        h.PotensialZererMebleg = dto.PotensialZererMebleg;
        h.BerpaTarixi = dto.BerpaTarixi;
        h.BerpaOlunanMebleg = dto.BerpaOlunanMebleg;
        h.SigortaIleBerpaOlunanHisse = dto.SigortaIleBerpaOlunanHisse;
        h.TedbirlerinTarixi = dto.TedbirlerinTarixi;
        h.TedbirlerinTesviri = string.IsNullOrWhiteSpace(dto.TedbirlerinTesviri) ? null : dto.TedbirlerinTesviri.Trim();
        h.TedbirlereMesulBolme = string.IsNullOrWhiteSpace(dto.TedbirlereMesulBolme) ? null : dto.TedbirlereMesulBolme.Trim();
        h.TedbirlerinIcraStatusu = dto.TedbirlerinIcraStatusu;
    }

    // Migration-dakı sütun ölçüləri ilə EYNİ (bax 20260930130000_EmeliyyatRiskiHadiseleri.cs).
    // Uzun mətn SaveChanges-də "String or binary data would be truncated" ilə
    // BÜTÖV formani sındırırdı (code-review, 30.09.2026) — BMI-nin dar sütun
    // tələsi ilə eyni kateqoriya, ona görə burada ƏVVƏLCƏDƏN rədd edilir.
    private static string? SahelərYoxla(EmeliyyatRiskiHadisesiCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.StrukturBolme)) return "Struktur bölmə boş ola bilməz.";
        if (string.IsNullOrWhiteSpace(dto.Tesvir)) return "Hadisənin təsviri boş ola bilməz.";
        if (string.IsNullOrWhiteSpace(dto.Sebeb)) return "Hadisənin səbəbi boş ola bilməz.";
        if (dto.UmumiZererMebleg < 0) return "Ümumi zərər məbləği mənfi ola bilməz.";
        if (dto.HadiseninMueyyenlesdirilmeTarixi.Date < dto.HadiseninBasVerdiyiTarix.Date)
            return "Müəyyənləşdirilmə tarixi baş vermə tarixindən əvvəl ola bilməz.";

        // Əlavə 3-ün öz nömrələmə sxemi — 2-ci səviyyə 1-ci səviyyəyə aid
        // OLMALIDIR (bax RiskKateqoriyasi2 enum qeydi: onlar rəqəmi = valideyn).
        if ((int)dto.RiskKateqoriyasi2 / 10 != (int)dto.RiskKateqoriyasi1)
            return "Seçilən risk alt-kateqoriyası seçilən 1-ci səviyyə kateqoriyaya aid deyil.";

        if (dto.StrukturBolme.Length > 200) return "Struktur bölmə 200 simvoldan çox ola bilməz.";
        if (dto.MelumatiVerenSexs?.Length > 200) return "Məlumatı verən şəxs 200 simvoldan çox ola bilməz.";
        if (dto.Tesvir.Length > 2000) return "Hadisənin təsviri 2000 simvoldan çox ola bilməz.";
        if (dto.Sebeb.Length > 1000) return "Hadisənin səbəbi 1000 simvoldan çox ola bilməz.";
        if (dto.BankMehsulu?.Length > 200) return "Bank məhsulu 200 simvoldan çox ola bilməz.";
        if (dto.RiskHadisesiNumune?.Length > 500) return "Risk hadisəsi nümunəsi 500 simvoldan çox ola bilməz.";
        if (dto.TedbirlerinTesviri?.Length > 1000) return "Tədbirlərin təsviri 1000 simvoldan çox ola bilməz.";
        if (dto.TedbirlereMesulBolme?.Length > 200) return "Tədbirlərə məsul bölmə 200 simvoldan çox ola bilməz.";

        return null;
    }

    /// <summary><paramref name="hedd"/> — YaratAsync-də cari parametr, YenileAsync-də
    /// qeydin DONDURULMUŞ həddi (bax çağırış yerlərindəki qeydlər).</summary>
    private static Result TedbirlerYoxla(EmeliyyatRiskiHadisesiCreateDto dto, decimal hedd)
    {
        if (dto.UmumiZererMebleg < hedd) return Result.Ok();

        // Əlavə 4-ün qeydi: limitdən yuxarı zərərdə 23-26 MƏCBURİDİR.
        if (dto.TedbirlerinTarixi == null
            || string.IsNullOrWhiteSpace(dto.TedbirlerinTesviri)
            || string.IsNullOrWhiteSpace(dto.TedbirlereMesulBolme)
            || dto.TedbirlerinIcraStatusu == null)
        {
            return Result.Fail(
                $"Ümumi zərər ({dto.UmumiZererMebleg:N2} AZN) bankın daxili həddindən " +
                $"({hedd:N2} AZN) yuxarıdır — tədbirlər planı (tarix, təsvir, " +
                "məsul bölmə, icra statusu) məcburidir.");
        }

        return Result.Ok();
    }

    private static EmeliyyatRiskiHadisesiDto Doldur(
        EmeliyyatRiskiHadisesi h,
        IReadOnlyDictionary<int, string> isciAdlari,
        IReadOnlyDictionary<int, string> icraciAdlari)
    {
        return new EmeliyyatRiskiHadisesiDto
        {
            Id = h.Id,
            YaradilmaTarixi = h.YaradilmaTarixi,
            QeydiyyatKodu = h.QeydiyyatKodu,
            StrukturBolme = h.StrukturBolme,
            MelumatiVerenSexs = h.MelumatiVerenSexs,
            HadiseninBasVerdiyiTarix = h.HadiseninBasVerdiyiTarix,
            HadiseninMueyyenlesdirilmeTarixi = h.HadiseninMueyyenlesdirilmeTarixi,
            MueyyenlesdirenIsciId = h.MueyyenlesdirenIsciId,
            MueyyenlesdirenIsciAdi = h.MueyyenlesdirenIsciId.HasValue && isciAdlari.TryGetValue(h.MueyyenlesdirenIsciId.Value, out var mad)
                ? mad : null,
            Tesvir = h.Tesvir,
            Sebeb = h.Sebeb,
            TezlikDerecesi = h.TezlikDerecesi,
            TesirDerecesi = h.TesirDerecesi,
            BiznesSahesi = h.BiznesSahesi,
            BankMehsulu = h.BankMehsulu,
            RiskKateqoriyasi1 = h.RiskKateqoriyasi1,
            RiskKateqoriyasi2 = h.RiskKateqoriyasi2,
            RiskHadisesiNumune = h.RiskHadisesiNumune,
            ZererTesirKateqoriyasi = h.ZererTesirKateqoriyasi,
            UmumiZererMebleg = h.UmumiZererMebleg,
            PotensialZererMebleg = h.PotensialZererMebleg,
            BerpaTarixi = h.BerpaTarixi,
            BerpaOlunanMebleg = h.BerpaOlunanMebleg,
            SigortaIleBerpaOlunanHisse = h.SigortaIleBerpaOlunanHisse,
            TedbirlerinTarixi = h.TedbirlerinTarixi,
            TedbirlerinTesviri = h.TedbirlerinTesviri,
            TedbirlereMesulBolme = h.TedbirlereMesulBolme,
            TedbirlerinIcraStatusu = h.TedbirlerinIcraStatusu,
            SonDeyisiklikTesviri = h.SonDeyisiklikTesviri,
            YenilenmeTarixi = h.YenilenmeTarixi,
            YaradanIcraciId = h.YaradanIcraciId,
            YaradanIsciAdi = h.YaradanIcraciId.HasValue && icraciAdlari.TryGetValue(h.YaradanIcraciId.Value, out var yad)
                ? yad : null,
            TedbirlerMecburidir = h.UmumiZererMebleg >= h.TedbirZererHeddiYaradilmaAninda
        };
    }

    private async Task<IReadOnlyDictionary<int, string>> IsciAdlariAsync(IEnumerable<int> isciIdler)
    {
        var idler = isciIdler.Distinct().ToList();
        if (idler.Count == 0) return new Dictionary<int, string>();

        return await _unitOfWork.Repository<Isci>()
            .Query()
            .AsNoTracking()
            .Where(i => idler.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, i => $"{i.Ad} {i.Soyad}");
    }

    /// <summary>YaradanIcraciId/YenileyenIcraciId — sistemə DAXİL OLAN
    /// istifadəçinin (AppUser) ID-sidir, Isci DEYİL (bax konstruktor qeydi).
    /// Nümunə: ReytingService.cs.</summary>
    private async Task<IReadOnlyDictionary<int, string>> IcraciAdlariAsync(IEnumerable<int> icraciIdler)
    {
        var idler = icraciIdler.Distinct().ToList();
        if (idler.Count == 0) return new Dictionary<int, string>();

        var users = await _userManager.Users.Where(u => idler.Contains(u.Id)).ToListAsync();
        return users.ToDictionary(u => u.Id, u => $"{u.Ad} {u.Soyad}");
    }
}
