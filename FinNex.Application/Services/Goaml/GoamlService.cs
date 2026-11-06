using System.Globalization;
using System.Text.RegularExpressions;
using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Goaml;
using FinNex.Application.DTOs.Oracle;
using FinNex.Application.Interfaces.Goaml;
using FinNex.Application.Interfaces.Oracle;
using FinNex.Application.Interfaces.Sorgular;
using FinNex.Domain.Entities.Goaml;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;

namespace FinNex.Application.Services.Goaml
{
    /// <summary>
    /// GOAML — "Əməliyyatlar" tabı. Namizəd Oracle-dan (DOC_DNA_ALL_TAB+RISK)
    /// OXUNUR, təsdiqlənən sətir isə bizim ÖZ SQL Server bazamıza (<see cref="GoamlEmeliyyat"/>)
    /// yazılır — Oracle-a YAZI qadağandır (layihənin kökü qaydası).
    /// </summary>
    public class GoamlService : IGoamlService
    {
        private readonly IOracleService _oracle;
        private readonly IOracleSorguService _sorgu;
        private readonly IUnitOfWork _unitOfWork;

        private const string SorguAdi = "GOAML_EMELIYYAT_NAMIZED";

        public GoamlService(IOracleService oracle, IOracleSorguService sorgu, IUnitOfWork unitOfWork)
        {
            _oracle = oracle;
            _sorgu = sorgu;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<List<GoamlNamizedSetirDto>>> NamizedAxtarAsync(GoamlAxtarisDto dto, CancellationToken ct = default)
        {
            var sorgular = await _sorgu.HamisiniGetirAsync();
            var metin = sorgular?.Data?.FirstOrDefault(x =>
                string.Equals((x.SorguAdi ?? "").Trim(), SorguAdi, StringComparison.OrdinalIgnoreCase))?.SorguMetni;

            if (string.IsNullOrWhiteSpace(metin))
                return Result<List<GoamlNamizedSetirDto>>.Fail(
                    $"Oracle sorğusu ({SorguAdi}) tapılmadı — docs/sql/goaml/90_Goaml_OracleSorgular.sql SQL Server-də işlədilməlidir.");

            // Minimum məbləğ — operator yoxdursa şərt söndürülür ("1=1").
            var mebleqSert = dto.MinMebleg is decimal m
                ? $"and t.summa_v_nacval >= {m.ToString(CultureInfo.InvariantCulture)}"
                : "and 1=1";

            // VÖEN/hesab — xam istifadəçi mətni birbaşa SQL-ə yapışdırıldığı üçün
            // əvvəlcə TƏHLÜKƏSİZLƏŞDİRİLİR (yalnız hərf/rəqəm qalır — eyni risk sinfi
            // BmiLatin.Tehlukesiz-dəki kimi).
            var voenSert = "and 1=1";
            if (!string.IsNullOrWhiteSpace(dto.VoenVeyaHesab))
            {
                var tehlukesiz = Tehlukesiz(dto.VoenVeyaHesab);
                if (tehlukesiz.Length > 0)
                    voenSert = $"and ( t.debet like '{tehlukesiz}%' or t.kredit like '{tehlukesiz}%' " +
                               $"or t.inn_licsch_de like '{tehlukesiz}%' or t.inn_licsch_kre like '{tehlukesiz}%' )";
            }

            var sql = metin
                .Replace("{DOVREVVEL}", dto.BasTarix.ToString("dd-MM-yyyy"))
                .Replace("{DOVRSON}", dto.SonTarix.ToString("dd-MM-yyyy"))
                .Replace("{MEBLEG_SERT}", mebleqSert)
                .Replace("{VOEN_HESAB_SERT}", voenSert);

            OracleNetice netice;
            try
            {
                netice = await _oracle.SelectXamAsync(sql, 1000, ct);
            }
            catch (Exception ex)
            {
                return Result<List<GoamlNamizedSetirDto>>.Fail($"Oracle sorğusu uğursuz oldu: {ex.Message}");
            }

            // Sütun SIRASI sorğuda sabitdir (bax 90_Goaml_OracleSorgular.sql SELECT siyahısı) —
            // dictionary yox, POZİSİYA ilə oxuyuruq (eyni adlı sütun təkrarlanmır burada, risk yoxdur).
            var setirler = netice.Setirler.Select(r => new GoamlNamizedSetirDto
            {
                RecNum = ToLong(r[0]),
                DateOper = ToDate(r[1]) ?? default,
                FilialNumber = (int)ToLong(r[2]),
                Debet = r[3] as string,
                Kredit = r[4] as string,
                SummaVInval = ToDecimal(r[5]),
                SummaVNacval = ToDecimal(r[6]),
                KodValuti = r[7] == null ? null : (int)ToLong(r[7]),
                KursValuti = ToDecimal(r[8]),
                Primechanie = r[9] as string,
                InnLicschDe = r[10] as string,
                InnLicschKre = r[11] as string,
                CountryCodeDe = r[12] as string,
                CountryCodeKre = r[13] as string,
                RegistracNomerDe = r[14] as string,
                RegistracNomerKre = r[15] as string,
                DocumentSource = r[16] as string,
                NameLicschDe = r[17] as string,
                NameLicschKre = r[18] as string,
            }).ToList();

            if (setirler.Count > 0)
            {
                var minTarix = setirler.Min(x => x.DateOper);
                var maxTarix = setirler.Max(x => x.DateOper);
                var movcud = await _unitOfWork.Repository<GoamlEmeliyyat>()
                    .HamisiniGetirAsync(x => x.CandidateDateOper >= minTarix && x.CandidateDateOper <= maxTarix);
                var acarlar = movcud
                    .Select(x => (x.CandidateFilialNumber, x.CandidateDateOper, x.CandidateRecNum))
                    .ToHashSet();
                foreach (var s in setirler)
                    s.ArtiqTesdiqlenib = acarlar.Contains((s.FilialNumber, s.DateOper, s.RecNum));
            }

            return Result<List<GoamlNamizedSetirDto>>.Ok(setirler);
        }

        public async Task<Result> TesdiqleAsync(GoamlTesdiqDto dto, int isciId)
        {
            if (dto.Tip == null)
                return Result.Fail("Tip seçilməlidir.");
            if (dto.ReportCode == null)
                return Result.Fail("Məlumatın növü seçilməlidir.");

            // Eyni namizəd (filial+tarix+recnum) ikinci dəfə təsdiqlənməsin —
            // operatorun səhvən iki dəfə klikləməsi, ya da fərqli axtarışda
            // eyni sətrin yenidən seçilməsi.
            var movcuddur = await _unitOfWork.Repository<GoamlEmeliyyat>().MovcuddurmuAsync(x =>
                x.CandidateFilialNumber == dto.CandidateFilialNumber &&
                x.CandidateDateOper == dto.CandidateDateOper &&
                x.CandidateRecNum == dto.CandidateRecNum);
            if (movcuddur)
                return Result.Fail("Bu əməliyyat artıq təsdiqlənib.");

            var e = new GoamlEmeliyyat
            {
                TesdiqleyenIsciId = isciId,
                CandidateFilialNumber = dto.CandidateFilialNumber,
                CandidateDateOper = dto.CandidateDateOper,
                CandidateRecNum = dto.CandidateRecNum,
                Debet = dto.Debet,
                Kredit = dto.Kredit,
                VipNetDa = dto.VipNetDa,
                SummaVInval = dto.SummaVInval,
                SummaVNacval = dto.SummaVNacval,
                KodValuti = dto.KodValuti,
                KursValuti = dto.KursValuti,
                Tip = dto.Tip,
                ReportCode = dto.ReportCode,
                TransactionNumber = dto.TransactionNumber,
                Primechanie = dto.Primechanie,
                DateOper = dto.DateOper,
                TransmodeCode = dto.TransmodeCode,
                TransmodeComment = dto.TransmodeComment,
                AmountLocal = dto.AmountLocal,
                FromFundsCode = dto.FromFundsCode,
                ForeignCurrencyCode = dto.ForeignCurrencyCode,
                ForeignCurrencyCodeFrom = dto.ForeignCurrencyCodeFrom,
                ForeignAmount = dto.ForeignAmount,
                ForeignExchangeRate = dto.ForeignExchangeRate,
                ToFundsCode = dto.ToFundsCode,
                Action = dto.Action,
                Reason = dto.Reason,
                Url = dto.Url,
                Opened = dto.Opened,
                Business = dto.Business,
                RegistracNomer = dto.RegistracNomer,
                Branch = dto.Branch,
                Iban = dto.Iban,
                PersonalAccountType = dto.PersonalAccountType,
                GelirMenbeyi = dto.GelirMenbeyi,
                ToFizik = dto.ToFizik,
                ToYurik = dto.ToYurik,
                ToPred = dto.ToPred,
                PanTo = dto.PanTo,
                ConAdi = dto.ConAdi,
                ConAtasininAdi = dto.ConAtasininAdi,
                ConSoyadi = dto.ConSoyadi,
                ConDogulduguTarix = dto.ConDogulduguTarix,
                ConDogulduguYer = dto.ConDogulduguYer,
                ConVetendasligi = dto.ConVetendasligi,
                ConTelefonNovu = dto.ConTelefonNovu,
                ConRabiteNovu = dto.ConRabiteNovu,
                ConTelefonNomresi = dto.ConTelefonNomresi,
                ConUnvaninNovu = dto.ConUnvaninNovu,
                ConUnvan = dto.ConUnvan,
                ConSeher = dto.ConSeher,
                ConOlke = dto.ConOlke,
                ConSenedinNovu = dto.ConSenedinNovu,
                ConSenedinSeriyaNomresi = dto.ConSenedinSeriyaNomresi,
                ConSenedinVerilmeTarixi = dto.ConSenedinVerilmeTarixi,
                ConSenedinEtibarliliqMuddeti = dto.ConSenedinEtibarliliqMuddeti,
                ConVerenOrqan = dto.ConVerenOrqan,
                ConVerildiyiOlke = dto.ConVerildiyiOlke,
                ConCinsi = dto.ConCinsi,
                ConPin = dto.ConPin,
                ReprIdType = dto.ReprIdType,
                ReprIdSerial = dto.ReprIdSerial,
                ReprIdNumber = dto.ReprIdNumber,
                SigAdi = dto.SigAdi,
                SigAtasininAdi = dto.SigAtasininAdi,
                SigSoyadi = dto.SigSoyadi,
                SigDogulduguTarix = dto.SigDogulduguTarix,
                SigDogulduguYer = dto.SigDogulduguYer,
                SigVetendasligi = dto.SigVetendasligi,
                SigTelefonNovu = dto.SigTelefonNovu,
                SigRabiteNovu = dto.SigRabiteNovu,
                SigTelefonNomresi = dto.SigTelefonNomresi,
                SigUnvaninNovu = dto.SigUnvaninNovu,
                SigUnvan = dto.SigUnvan,
                SigSeher = dto.SigSeher,
                SigOlke = dto.SigOlke,
                SigSenedinNovu = dto.SigSenedinNovu,
                SigSenedinSeriyaNomresi = dto.SigSenedinSeriyaNomresi,
                SigSenedinVerilmeTarixi = dto.SigSenedinVerilmeTarixi,
                SigSenedinEtibarliliqMuddeti = dto.SigSenedinEtibarliliqMuddeti,
                SigVerenOrqan = dto.SigVerenOrqan,
                SigVerildiyiOlke = dto.SigVerildiyiOlke,
                SigVezife = dto.SigVezife,
                SigIsYeriAdi = dto.SigIsYeriAdi,
                InstitutionName = dto.InstitutionName,
                Swift = dto.Swift,
                Account = dto.Account,
                IncorporationNumber = dto.IncorporationNumber,
                IncorporationCountryCode = dto.IncorporationCountryCode,
                NameUr = dto.NameUr,
                FromCountry = dto.FromCountry,
                HesabinAcilmaTarixi = dto.HesabinAcilmaTarixi,
                BankinAdi = dto.BankinAdi,
                SeherinKodu = dto.SeherinKodu,
                FealiyyatIstiqameti = dto.FealiyyatIstiqameti,
                ToAdi = dto.ToAdi,
                ToAtasininAdi = dto.ToAtasininAdi,
                ToSoyadi = dto.ToSoyadi,
                ToDogulduguTarix = dto.ToDogulduguTarix,
                ToDogulduguYer = dto.ToDogulduguYer,
                ToVetendasligi = dto.ToVetendasligi,
                ToTelefonNovu = dto.ToTelefonNovu,
                ToRabiteNovu = dto.ToRabiteNovu,
                ToTelefonNomresi = dto.ToTelefonNomresi,
                ToUnvaninNovu = dto.ToUnvaninNovu,
                ToUnvan = dto.ToUnvan,
                ToSeher = dto.ToSeher,
                ToOlke = dto.ToOlke,
                ToOlkeAdi = dto.ToOlkeAdi,
                ToSenedinNovu = dto.ToSenedinNovu,
                ToSenedinSeriyaNomresi = dto.ToSenedinSeriyaNomresi,
                ToSenedinVerilmeTarixi = dto.ToSenedinVerilmeTarixi,
                ToSenedinEtibarliliqMuddeti = dto.ToSenedinEtibarliliqMuddeti,
                ToVerenOrqan = dto.ToVerenOrqan,
                ToVerildiyiOlke = dto.ToVerildiyiOlke,
                IncorporationCountryCodeTo = dto.IncorporationCountryCodeTo,
                IncorporationNumberTo = dto.IncorporationNumberTo,
                NameUrTo = dto.NameUrTo,
                InstitutionNameTo = dto.InstitutionNameTo,
                SwiftTo = dto.SwiftTo,
                AccountTo = dto.AccountTo,
                AlaqeNovuTo = dto.AlaqeNovuTo,
                Gender = dto.Gender,
                Snn = dto.Snn,
                GenderTo = dto.GenderTo,
                SnnTo = dto.SnnTo,
                Occupation = dto.Occupation,
                EmployerName = dto.EmployerName,
                SenderIdType = dto.SenderIdType,
                SenderIdSerial = dto.SenderIdSerial,
                SenderIdNumber = dto.SenderIdNumber,
                RecIdType = dto.RecIdType,
                RecIdSerial = dto.RecIdSerial,
                RecIdNumber = dto.RecIdNumber,
            };

            await _unitOfWork.Repository<GoamlEmeliyyat>().YaratAsync(e);
            await _unitOfWork.YaddaSaxlaAsync();

            return Result.Ok("Əməliyyat təsdiqləndi.");
        }

        public async Task<Result<List<GoamlEmeliyyatListDto>>> HamisiniGetirAsync()
        {
            var setirler = await _unitOfWork.Repository<GoamlEmeliyyat>().HamisiniGetirAsync();
            var isciIdler = setirler.Select(x => x.TesdiqleyenIsciId).Distinct().ToList();
            var isciler = await _unitOfWork.Repository<Isci>()
                .HamisiniGetirAsync(x => isciIdler.Contains(x.Id));
            var isciAdlari = isciler.ToDictionary(x => x.Id, x => $"{x.Ad} {x.Soyad}");

            var neice = setirler
                .OrderByDescending(x => x.YaradilmaTarixi)
                .Select(x => new GoamlEmeliyyatListDto
                {
                    Id = x.Id,
                    DateOper = x.DateOper ?? x.CandidateDateOper,
                    Debet = x.Debet,
                    Kredit = x.Kredit,
                    SummaVNacval = x.SummaVNacval,
                    Tip = x.Tip,
                    ReportCode = x.ReportCode,
                    Status = x.Status,
                    TesdiqleyenAdi = isciAdlari.TryGetValue(x.TesdiqleyenIsciId, out var ad) ? ad : null,
                    YaradilmaTarixi = x.YaradilmaTarixi,
                })
                .ToList();

            return Result<List<GoamlEmeliyyatListDto>>.Ok(neice);
        }

        public async Task<Result<GoamlTesdiqDto>> DetalGetirAsync(int id)
        {
            var e = await _unitOfWork.Repository<GoamlEmeliyyat>().IdIleGetirAsync(id);
            if (e == null) return Result<GoamlTesdiqDto>.Fail("Qeyd tapılmadı.");

            var dto = new GoamlTesdiqDto
            {
                CandidateFilialNumber = e.CandidateFilialNumber,
                CandidateDateOper = e.CandidateDateOper,
                CandidateRecNum = e.CandidateRecNum,
                Debet = e.Debet,
                Kredit = e.Kredit,
                VipNetDa = e.VipNetDa,
                SummaVInval = e.SummaVInval,
                SummaVNacval = e.SummaVNacval,
                KodValuti = e.KodValuti,
                KursValuti = e.KursValuti,
                Tip = e.Tip,
                ReportCode = e.ReportCode,
                TransactionNumber = e.TransactionNumber,
                Primechanie = e.Primechanie,
                DateOper = e.DateOper,
                TransmodeCode = e.TransmodeCode,
                TransmodeComment = e.TransmodeComment,
                AmountLocal = e.AmountLocal,
                FromFundsCode = e.FromFundsCode,
                ForeignCurrencyCode = e.ForeignCurrencyCode,
                ForeignCurrencyCodeFrom = e.ForeignCurrencyCodeFrom,
                ForeignAmount = e.ForeignAmount,
                ForeignExchangeRate = e.ForeignExchangeRate,
                ToFundsCode = e.ToFundsCode,
                Action = e.Action,
                Reason = e.Reason,
                Url = e.Url,
                Opened = e.Opened,
                Business = e.Business,
                RegistracNomer = e.RegistracNomer,
                Branch = e.Branch,
                Iban = e.Iban,
                PersonalAccountType = e.PersonalAccountType,
                GelirMenbeyi = e.GelirMenbeyi,
                ToFizik = e.ToFizik,
                ToYurik = e.ToYurik,
                ToPred = e.ToPred,
                PanTo = e.PanTo,
                ConAdi = e.ConAdi,
                ConAtasininAdi = e.ConAtasininAdi,
                ConSoyadi = e.ConSoyadi,
                ConDogulduguTarix = e.ConDogulduguTarix,
                ConDogulduguYer = e.ConDogulduguYer,
                ConVetendasligi = e.ConVetendasligi,
                ConTelefonNovu = e.ConTelefonNovu,
                ConRabiteNovu = e.ConRabiteNovu,
                ConTelefonNomresi = e.ConTelefonNomresi,
                ConUnvaninNovu = e.ConUnvaninNovu,
                ConUnvan = e.ConUnvan,
                ConSeher = e.ConSeher,
                ConOlke = e.ConOlke,
                ConSenedinNovu = e.ConSenedinNovu,
                ConSenedinSeriyaNomresi = e.ConSenedinSeriyaNomresi,
                ConSenedinVerilmeTarixi = e.ConSenedinVerilmeTarixi,
                ConSenedinEtibarliliqMuddeti = e.ConSenedinEtibarliliqMuddeti,
                ConVerenOrqan = e.ConVerenOrqan,
                ConVerildiyiOlke = e.ConVerildiyiOlke,
                ConCinsi = e.ConCinsi,
                ConPin = e.ConPin,
                ReprIdType = e.ReprIdType,
                ReprIdSerial = e.ReprIdSerial,
                ReprIdNumber = e.ReprIdNumber,
                SigAdi = e.SigAdi,
                SigAtasininAdi = e.SigAtasininAdi,
                SigSoyadi = e.SigSoyadi,
                SigDogulduguTarix = e.SigDogulduguTarix,
                SigDogulduguYer = e.SigDogulduguYer,
                SigVetendasligi = e.SigVetendasligi,
                SigTelefonNovu = e.SigTelefonNovu,
                SigRabiteNovu = e.SigRabiteNovu,
                SigTelefonNomresi = e.SigTelefonNomresi,
                SigUnvaninNovu = e.SigUnvaninNovu,
                SigUnvan = e.SigUnvan,
                SigSeher = e.SigSeher,
                SigOlke = e.SigOlke,
                SigSenedinNovu = e.SigSenedinNovu,
                SigSenedinSeriyaNomresi = e.SigSenedinSeriyaNomresi,
                SigSenedinVerilmeTarixi = e.SigSenedinVerilmeTarixi,
                SigSenedinEtibarliliqMuddeti = e.SigSenedinEtibarliliqMuddeti,
                SigVerenOrqan = e.SigVerenOrqan,
                SigVerildiyiOlke = e.SigVerildiyiOlke,
                SigVezife = e.SigVezife,
                SigIsYeriAdi = e.SigIsYeriAdi,
                InstitutionName = e.InstitutionName,
                Swift = e.Swift,
                Account = e.Account,
                IncorporationNumber = e.IncorporationNumber,
                IncorporationCountryCode = e.IncorporationCountryCode,
                NameUr = e.NameUr,
                FromCountry = e.FromCountry,
                HesabinAcilmaTarixi = e.HesabinAcilmaTarixi,
                BankinAdi = e.BankinAdi,
                SeherinKodu = e.SeherinKodu,
                FealiyyatIstiqameti = e.FealiyyatIstiqameti,
                ToAdi = e.ToAdi,
                ToAtasininAdi = e.ToAtasininAdi,
                ToSoyadi = e.ToSoyadi,
                ToDogulduguTarix = e.ToDogulduguTarix,
                ToDogulduguYer = e.ToDogulduguYer,
                ToVetendasligi = e.ToVetendasligi,
                ToTelefonNovu = e.ToTelefonNovu,
                ToRabiteNovu = e.ToRabiteNovu,
                ToTelefonNomresi = e.ToTelefonNomresi,
                ToUnvaninNovu = e.ToUnvaninNovu,
                ToUnvan = e.ToUnvan,
                ToSeher = e.ToSeher,
                ToOlke = e.ToOlke,
                ToOlkeAdi = e.ToOlkeAdi,
                ToSenedinNovu = e.ToSenedinNovu,
                ToSenedinSeriyaNomresi = e.ToSenedinSeriyaNomresi,
                ToSenedinVerilmeTarixi = e.ToSenedinVerilmeTarixi,
                ToSenedinEtibarliliqMuddeti = e.ToSenedinEtibarliliqMuddeti,
                ToVerenOrqan = e.ToVerenOrqan,
                ToVerildiyiOlke = e.ToVerildiyiOlke,
                IncorporationCountryCodeTo = e.IncorporationCountryCodeTo,
                IncorporationNumberTo = e.IncorporationNumberTo,
                NameUrTo = e.NameUrTo,
                InstitutionNameTo = e.InstitutionNameTo,
                SwiftTo = e.SwiftTo,
                AccountTo = e.AccountTo,
                AlaqeNovuTo = e.AlaqeNovuTo,
                Gender = e.Gender,
                Snn = e.Snn,
                GenderTo = e.GenderTo,
                SnnTo = e.SnnTo,
                Occupation = e.Occupation,
                EmployerName = e.EmployerName,
                SenderIdType = e.SenderIdType,
                SenderIdSerial = e.SenderIdSerial,
                SenderIdNumber = e.SenderIdNumber,
                RecIdType = e.RecIdType,
                RecIdSerial = e.RecIdSerial,
                RecIdNumber = e.RecIdNumber,
            };

            return Result<GoamlTesdiqDto>.Ok(dto);
        }

        // ── Köməkçilər ──────────────────────────────────────────────────────

        /// <summary>Yalnız hərf/rəqəm saxlanır — xam istifadəçi mətni raw SQL-ə
        /// yapışdırılmazdan əvvəl (dırnaq/nöqtəli vergül kimi işarələr atılır).</summary>
        private static string Tehlukesiz(string xam) =>
            Regex.Replace(xam.Trim(), "[^A-Za-z0-9]", "");

        /// <summary>Oracle NUMBER-i (adətən `decimal`) uzun ədədə çevirir —
        /// stringə çevirib geri parse ETMİRİK (CLAUDE.md "Oracle Rəqəmi" qaydası).</summary>
        private static long ToLong(object? v) => v switch
        {
            null => 0,
            long l => l,
            decimal d => (long)d,
            int i => i,
            double db => (long)db,
            _ => 0,
        };

        private static decimal? ToDecimal(object? v) => v switch
        {
            null => null,
            decimal d => d,
            double db => (decimal)db,
            long l => l,
            int i => i,
            _ => null,
        };

        private static DateTime? ToDate(object? v) => v switch
        {
            null => null,
            DateTime d => d,
            _ => null,
        };
    }
}
