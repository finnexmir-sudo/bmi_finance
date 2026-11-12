using System.Globalization;
using System.Linq.Expressions;
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
using Microsoft.Extensions.Logging;

namespace FinNex.Application.Services.Goaml
{
    /// <summary>
    /// GOAML — "Əməliyyatlar" tabı. Namizəd Oracle-dan (DOC_DNA_ALL_RISK —
    /// 11.11.2026-dan, AYSEL-dən TAM MÜSTƏQİL, bax 90_Goaml_OracleSorgular.sql-in
    /// baş şərhi) OXUNUR, təsdiqlənən sətir isə bizim ÖZ SQL Server bazamıza
    /// (<see cref="GoamlEmeliyyat"/>) yazılır — Oracle-a YAZI qadağandır
    /// (layihənin kökü qaydası).
    /// </summary>
    public class GoamlService : IGoamlService
    {
        private readonly IOracleService _oracle;
        private readonly IOracleSorguService _sorgu;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GoamlService> _logger;

        private const string SorguAdi = "GOAML_EMELIYYAT_NAMIZED";

        /// <summary>"Risk" tabının namizəd sorğusu — ölkə/qara siyahı/hesab-sahibi
        /// vətəndaşlığı əsaslı, AYSEL-in `doc_dna_all_risk_t` staging cədvəlindən
        /// ASILI OLMAYAN ekvivalent (10-11.11.2026, canlı Oracle ilə doğrulanıb —
        /// bax docs/sql/goaml/91_Goaml_Risk_OracleSorgular.sql-in baş şərhi).</summary>
        private const string RiskSorguAdi = "GOAML_RISK_NAMIZED";

        public GoamlService(IOracleService oracle, IOracleSorguService sorgu, IUnitOfWork unitOfWork, ILogger<GoamlService> logger)
        {
            _oracle = oracle;
            _sorgu = sorgu;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public Task<Result<List<GoamlNamizedSetirDto>>> NamizedAxtarAsync(GoamlAxtarisDto dto, CancellationToken ct = default)
            => NamizedAxtarAsyncCore(SorguAdi, "docs/sql/goaml/90_Goaml_OracleSorgular.sql", dto, ct);

        public Task<Result<List<GoamlNamizedSetirDto>>> RiskNamizedAxtarAsync(GoamlAxtarisDto dto, CancellationToken ct = default)
            => NamizedAxtarAsyncCore(RiskSorguAdi, "docs/sql/goaml/91_Goaml_Risk_OracleSorgular.sql", dto, ct);

        /// <summary>Əməliyyatlar VƏ Risk tablarının ORTAQ namizəd-axtarış
        /// mexanizmi — hər ikisi eyni şəkildə saxlanmış Oracle sorğusunu
        /// (yalnız SorguAdi fərqli) oxuyub eyni formada (<see cref="GoamlNamizedSetirDto"/>)
        /// nəticə qaytarır və eyni dublikat-yoxlamasından keçir. Kod TƏKRAR
        /// YAZILMASIN deyə BİR yerdə saxlanılır.</summary>
        private async Task<Result<List<GoamlNamizedSetirDto>>> NamizedAxtarAsyncCore(
            string sorguAdi, string sqlFaylYolu, GoamlAxtarisDto dto, CancellationToken ct)
        {
            var sorgular = await _sorgu.HamisiniGetirAsync();
            var metin = sorgular?.Data?.FirstOrDefault(x =>
                string.Equals((x.SorguAdi ?? "").Trim(), sorguAdi, StringComparison.OrdinalIgnoreCase))?.SorguMetni;

            if (string.IsNullOrWhiteSpace(metin))
                return Result<List<GoamlNamizedSetirDto>>.Fail(
                    $"Oracle sorğusu ({sorguAdi}) tapılmadı — {sqlFaylYolu} SQL Server-də işlədilməlidir.");

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
                // Açar → artıq hansı Tip(lər)lə təsdiqlənib (12.11.2026, AML məsul
                // şəxsin tələbi: hər iki tərəfdə KYC olan əməliyyat ADƏTƏN iki
                // ayrı Tip ilə iki dəfə göndərilir — bax GoamlNamizedSetirDto.
                // TesdiqlenmisTipler şərhi). Tam "artıq göndərilib, bloklu" DEYİL.
                var tipleriLuget = movcud
                    .GroupBy(x => (x.CandidateFilialNumber, x.CandidateDateOper, x.CandidateRecNum))
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(x => x.Tip).Where(t => t != null).Select(t => t!.Value.Etiket()).Distinct().ToList());
                foreach (var s in setirler)
                {
                    if (tipleriLuget.TryGetValue((s.FilialNumber, s.DateOper, s.RecNum), out var tipler))
                    {
                        s.ArtiqTesdiqlenib = true;
                        s.TesdiqlenmisTipler = tipler;
                    }
                }
            }

            return Result<List<GoamlNamizedSetirDto>>.Ok(setirler);
        }

        public async Task<Result> TesdiqleAsync(GoamlTesdiqDto dto, int isciId)
        {
            if (dto.Tip == null)
                return Result.Fail("Tip seçilməlidir.");
            if (dto.ReportCode == null)
                return Result.Fail("Məlumatın növü seçilməlidir.");

            // Eyni namizəd (filial+tarix+recnum) EYNİ Tip ilə ikinci dəfə
            // təsdiqlənməsin — operatorun səhvən iki dəfə klikləməsi, ya da
            // fərqli axtarışda eyni sətrin yenidən seçilməsi. ⚠️ ARTIQ `Tip`-ə
            // BAXMADAN bloklamırıq (12.11.2026, AML məsul şəxsin tələbi): hər
            // iki tərəfdə (Debet VƏ Kredit) KYC tapılan əməliyyat ADƏTƏN İKİ
            // AYRI hesabat kimi — bir dəfə bir istiqamətli Tip ilə, bir dəfə
            // əks istiqamətli Tip ilə — göndərilməlidir. Köhnə şərt (Tip-i
            // yoxlamadan) bunu BLOKLAYIRDI — ikinci, fərqli Tip-li təsdiq
            // "artıq təsdiqlənib" xətası ilə rədd olunurdu.
            var movcuddur = await _unitOfWork.Repository<GoamlEmeliyyat>().MovcuddurmuAsync(x =>
                x.CandidateFilialNumber == dto.CandidateFilialNumber &&
                x.CandidateDateOper == dto.CandidateDateOper &&
                x.CandidateRecNum == dto.CandidateRecNum &&
                x.Tip == dto.Tip);
            if (movcuddur)
                return Result.Fail("Bu əməliyyat eyni Tip ilə artıq təsdiqlənib. Əks tərəfi də göndərmək lazımdırsa, fərqli (əks istiqamətli) Tip seçib yenidən təsdiqləyin.");

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
                DocumentSource = dto.DocumentSource,
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
                InstitutionNameBranchTo = dto.InstitutionNameBranchTo,
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

            // AYSEL-in "Update New" düyməsinin ekvivalenti — operator "Təsdiqlə"
            // basan kimi, HƏMİN KONKRET qeyd üçün avtomatik tətbiq olunur (ayrıca
            // düymə YOXDUR — istifadəçi qərarı, 06-07.11.2026). Yalnız Oracle-dan
            // OXUYUR, bu sətrin özünə (hələ DB-yə yazılmamış) yazır.
            await AvtoDolduruAsync(e, CancellationToken.None);

            await _unitOfWork.Repository<GoamlEmeliyyat>().YaratAsync(e);
            await _unitOfWork.YaddaSaxlaAsync();

            return Result.Ok("Əməliyyat təsdiqləndi.");
        }

        public async Task<Result<List<GoamlEmeliyyatListDto>>> HamisiniGetirAsync()
        {
            var setirler = await _unitOfWork.Repository<GoamlEmeliyyat>().HamisiniGetirAsync();
            var neice = (await SetirleriDtoyaCevirAsync(setirler))
                .OrderByDescending(x => x.YaradilmaTarixi)
                .ToList();

            return Result<List<GoamlEmeliyyatListDto>>.Ok(neice);
        }

        /// <summary>"Təsdiqlənmiş siyahı" (<see cref="HamisiniGetirAsync()"/>) VƏ "İxrac
        /// önizləməsi" (<see cref="IxracOnizlemeAsync"/>) EYNİ xəritələməni paylaşır —
        /// tək mənbə, iki oxuyucu (13.11.2026).</summary>
        private async Task<List<GoamlEmeliyyatListDto>> SetirleriDtoyaCevirAsync(IList<GoamlEmeliyyat> setirler)
        {
            var isciIdler = setirler.Select(x => x.TesdiqleyenIsciId).Distinct().ToList();
            var isciler = isciIdler.Count == 0
                ? new List<Isci>()
                : (await _unitOfWork.Repository<Isci>().HamisiniGetirAsync(x => isciIdler.Contains(x.Id))).ToList();
            var isciAdlari = isciler.ToDictionary(x => x.Id, x => $"{x.Ad} {x.Soyad}");

            return setirler
                .Select(x => new GoamlEmeliyyatListDto
                {
                    Id = x.Id,
                    CandidateRecNum = x.CandidateRecNum,
                    DateOper = x.DateOper ?? x.CandidateDateOper,
                    Debet = x.Debet,
                    Kredit = x.Kredit,
                    SummaVNacval = x.SummaVNacval,
                    Tip = x.Tip,
                    ReportCode = x.ReportCode,
                    TipEtiketi = x.Tip?.Etiket(),
                    ReportCodeEtiketi = x.ReportCode != null ? $"{x.ReportCode.Value.Kod()} — {x.ReportCode.Value.Aciqlama()}" : null,
                    Status = x.Status,
                    TesdiqleyenAdi = isciAdlari.TryGetValue(x.TesdiqleyenIsciId, out var ad) ? ad : null,
                    YaradilmaTarixi = x.YaradilmaTarixi,
                })
                .ToList();
        }

        public async Task<Result> SilAsync(int id)
        {
            var e = await _unitOfWork.Repository<GoamlEmeliyyat>().IdIleGetirAsync(id);
            if (e == null) return Result.Fail("Qeyd tapılmadı.");

            // İxrac edilmiş (gələcəkdə Status.İxracEdilib) qeydi silmirik — audit
            // izi qalmalıdır. Hazırda yalnız "Qaralama" statusu mövcuddur, ona
            // görə bu şərt indi HƏR ZAMAN keçir — gələcəkdə "Export to file"
            // (tapşırıq #15) yeni status əlavə edəndə bu qoruyucu AVTOMATİK işə
            // düşəcək, kodda əlavə dəyişiklik lazım olmayacaq.
            if (e.Status != GoamlEmeliyyatStatusu.Qaralama)
                return Result.Fail("Bu qeyd artıq ixrac edilib — silinə bilməz.");

            await _unitOfWork.Repository<GoamlEmeliyyat>().YumshakSilAsync(id);
            await _unitOfWork.YaddaSaxlaAsync();
            return Result.Ok("Qeyd silindi.");
        }

        public async Task<Result<GoamlEmeliyyatDetalDto>> DetalGetirAsync(int id)
        {
            var e = await _unitOfWork.Repository<GoamlEmeliyyat>().IdIleGetirAsync(id);
            if (e == null) return Result<GoamlEmeliyyatDetalDto>.Fail("Qeyd tapılmadı.");

            var tesdiqleyen = (await _unitOfWork.Repository<Isci>()
                .HamisiniGetirAsync(x => x.Id == e.TesdiqleyenIsciId)).FirstOrDefault();

            var dto = new GoamlEmeliyyatDetalDto
            {
                Id = e.Id,
                Status = e.Status,
                TesdiqleyenAdi = tesdiqleyen == null ? null : $"{tesdiqleyen.Ad} {tesdiqleyen.Soyad}",
                YaradilmaTarixi = e.YaradilmaTarixi,
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
                DocumentSource = e.DocumentSource,
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
                InstitutionNameBranchTo = e.InstitutionNameBranchTo,
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

            return Result<GoamlEmeliyyatDetalDto>.Ok(dto);
        }

        // ── "Update New" ekvivalenti — avtomatik doldurma (07.11.2026) ─────────
        //
        // AYSEL-in "Update New" düyməsinin (06-07.11.2026-da v$sql ilə tapılan,
        // 49 Oracle UPDATE-dən ibarət kaskadın) PORTU — TƏK fərqlə: AYSEL bütün
        // bir DATE_OPER aralığını toplu emal edir, bura isə YALNIZ bu KONKRET
        // qeydi (operator "Təsdiqlə" basan kimi, TesdiqleAsync-dən çağırılır).
        // Oracle-a YALNIZ OXUYUR (BALSCHT_FIZ/BALSCHT_UR/MMX_ID_SERIAL/
        // TYPES_OF_DOCUMENTS/BIC_CODES/ATTRIB/VIEW_LICSCH_ALL/LICSCH_ALL_2/MFO),
        // nəticəni `e`-nin (hələ DB-yə yazılmamış) sahələrinə yazır.
        //
        // 49 qaydadan BİLƏRƏKDƏN TƏTBİQ OLUNMAYAN 23-ü:
        //   - TIP təyin edən 15 qayda — bizdə TIP operatorun ƏL İLƏ seçdiyi,
        //     MƏCBURİ sahədir (yuxarı, TesdiqleAsync-in başındakı yoxlama).
        //     AYSEL-də TIP boş başlayır, bu kaskad onu HESABLAYIR; bizdə əksinə,
        //     TIP artıq operatordan gəlib — yenidən hesablamaq onun AÇIQ seçimini
        //     SƏSSİZCƏ üstələyərdi. Eyni səbəbdən "TIP=6 xüsusi qaydası"
        //     (dch73bdggbkrn) da atlanıb.
        //   - REPORT_CODE təyin edən 4 qayda (CTR ×3, ETR fallback) — eyni səbəb,
        //     "Məlumatın növü" də operatorun məcburi seçimidir.
        //   - 3 "no-op" qayda (`SET X=0 WHERE X=0`) — Oracle-un ÖZÜNDƏ də heç nə
        //     etmir, kodda qarşılığı yoxdur.
        //   - 7 qayda `ID_VD`-yə (DOC_VNESH_SWIFT/INVAL/POSTUPL/NACVAL/
        //     DOC_INTERNAL_INVAL) bağlıdır — bizim namizəd sorğumuzda bu açar
        //     YOXDUR, haradan gəldiyi araşdırılmayıb (gələcək iş, CLAUDE.md-də
        //     qeyd olunub).
        //
        // Bu metod "ən yaxşı cəhd"dir — Oracle əlçatmaz olsa belə TƏSDİQ əməliyyatı
        // UĞURSUZ OLMAMALIDIR (operator artıq özü doldurduğu KYC-ni itirməsin),
        // ona görə HƏR Oracle sorğusu öz köməkçisində try/catch-lə əhatə olunub.
        private async Task AvtoDolduruAsync(GoamlEmeliyyat e, CancellationToken ct)
        {
            // ── 1) VÖEN-in 10-cu rəqəminə görə Fiziki/Hüquqi/Sahibkar — YALNIZ
            //      operator HEÇ BİRİNİ işarələməyibsə (əks halda onun seçimi
            //      toxunulmaz qalır). Oracle: 58hwazj6jxj4q / 5x3d8n7bth8aq /
            //      9gh5b10zb284n. ──────────────────────────────────────────────
            if (e.ToYurik != true && e.ToPred != true && e.ToFizik != true &&
                string.Equals(e.IncorporationCountryCodeTo, "AZ", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(e.IncorporationNumberTo) && e.IncorporationNumberTo!.Trim().Length >= 10)
            {
                var reqem = e.IncorporationNumberTo.Trim()[9];
                if (reqem == '1') e.ToYurik = true;
                else if (reqem == '2') e.ToPred = true;
                else e.ToFizik = true;
            }

            // ── 2) Sənəd seriya/nömrəsinin parçalanması (CON/SIG/TO) —
            //      MMX_ID_SERIAL-dan ƏN UZUN uyğun gələn prefiksi seçirik (AYSEL-in
            //      özündə 1/2/3-char qaydaları eyni şərtlə, sırasız işləyirdi —
            //      "ən uzun uyğunluq qalib" bunun təhlükəsiz, deterministik
            //      qarşılığıdır). Oracle: 026fz39mr7as3/4ybzrfvr6f1hj/5jnvxwbx51r9y
            //      (CON), 1a0h8d7fqx74s/c36az8naz33vr/gfcpk6qsx9t3h (SIG),
            //      2wcj7fg4u6hcz/cf70ryjttg2vk/8vuah1ppks3s8 (TO). ────────────────
            var serialler = await MmxIdSerialSiyahisiAsync(ct);
            if (serialler.Count > 0)
            {
                if (ParcalaSenedNomresi(e.ConSenedinSeriyaNomresi, serialler, out var reprSer, out var reprNum))
                { e.ReprIdSerial = reprSer; e.ReprIdNumber = reprNum; }

                if (ParcalaSenedNomresi(e.SigSenedinSeriyaNomresi, serialler, out var sendSer, out var sendNum))
                { e.SenderIdSerial = sendSer; e.SenderIdNumber = sendNum; }

                if (ParcalaSenedNomresi(e.ToSenedinSeriyaNomresi, serialler, out var recSer, out var recNum))
                { e.RecIdSerial = recSer; e.RecIdNumber = recNum; }
            }

            // ── 3) Sənəd növünün goAML MMX kodu — TYPES_OF_DOCUMENTS.ISARE →
            //      MMX_ID (CON/SIG/TO). Oracle: 0r10bu036mrxr / f35ct0dr8tvtz /
            //      1qx5pc7pd6p7x. ──────────────────────────────────────────────
            var senedNovleri = await TypesOfDocumentsXeritesiAsync(ct);
            if (senedNovleri.Count > 0)
            {
                if (!string.IsNullOrWhiteSpace(e.ConSenedinNovu) && senedNovleri.TryGetValue(e.ConSenedinNovu!.Trim(), out var reprType))
                    e.ReprIdType = reprType;
                if (!string.IsNullOrWhiteSpace(e.SigSenedinNovu) && senedNovleri.TryGetValue(e.SigSenedinNovu!.Trim(), out var sendType))
                    e.SenderIdType = sendType;
                if (!string.IsNullOrWhiteSpace(e.ToSenedinNovu) && senedNovleri.TryGetValue(e.ToSenedinNovu!.Trim(), out var recType))
                    e.RecIdType = recType;
            }

            // ── 4) Alan tərəfin (TO) KYC-si boşdursa — ƏVVƏLCƏ imza hüququ olan
            //      şəxsdən (SIG) köçür, YENƏ DƏ boşdursa ADI (NAME_UR_TO) mətnindən
            //      parçala. İKİ AYRI `if` (AYSEL-də bunlar İKİ MÜSTƏQİL UPDATE-dir —
            //      "else if" yazsaq TIP=FToF-da, SIG özü boş olanda, fallback-in
            //      işə düşməsinin qarşısını səhvən kəsərdik). Oracle: 11m7u3wqnmcku,
            //      21cr0xunnyhpz. ──────────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(e.ToAdi) && e.Tip is GoamlTipi.FizikOut or GoamlTipi.FizikIn
                or GoamlTipi.HuquqiOut or GoamlTipi.HuquqiIn or GoamlTipi.FToF)
            {
                e.ToAdi = e.SigAdi;
                e.ToAtasininAdi = e.SigAtasininAdi;
                e.ToSoyadi = e.SigSoyadi;
                e.ToDogulduguTarix = e.SigDogulduguTarix;
                e.ToDogulduguYer = e.SigDogulduguYer;
                e.ToVetendasligi = e.SigVetendasligi;
                e.ToTelefonNovu = e.SigTelefonNovu;
                e.ToRabiteNovu = e.SigRabiteNovu;
                e.ToTelefonNomresi = e.SigTelefonNomresi;
                e.ToUnvaninNovu = e.SigUnvaninNovu;
                e.ToUnvan = e.SigUnvan;
                e.ToSeher = e.SigSeher;
                e.ToOlke = e.SigOlke;
                e.ToSenedinNovu = e.SigSenedinNovu;
                e.ToSenedinSeriyaNomresi = e.SigSenedinSeriyaNomresi;
                e.ToSenedinVerilmeTarixi = e.SigSenedinVerilmeTarixi;
                e.ToSenedinEtibarliliqMuddeti = e.SigSenedinEtibarliliqMuddeti;
                e.ToVerenOrqan = e.SigVerenOrqan;
                e.ToVerildiyiOlke = e.SigVerildiyiOlke;
                e.GenderTo = e.Gender;
                e.SnnTo = e.Snn;
                // AYSEL-in öz qaydası — SIG-dən köçürüləndə TO-nun öz
                // (əvvəllər 2-ci addımda doldurulmuş) seriyası təmizlənir.
                e.RecIdNumber = null;
                e.RecIdSerial = null;
            }

            if (string.IsNullOrWhiteSpace(e.ToAdi) && e.Tip is GoamlTipi.FToHIn or GoamlTipi.FToF
                && (e.ToFizik == true || e.ToPred == true) && !string.IsNullOrWhiteSpace(e.NameUrTo))
            {
                // AYSEL-in REGEXP_SUBSTR naxışı: "SOYAD AD ATA_ADI" sırası —
                // 1-ci söz Ad, 2-ci söz Soyad, 3-cü söz Ata adı.
                var hisseler = e.NameUrTo!.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                e.ToAdi = hisseler.Length > 0 ? Kes(hisseler[0], 100) : null;
                e.ToSoyadi = hisseler.Length > 1 ? Kes(hisseler[1], 100) : null;
                e.ToAtasininAdi = hisseler.Length > 2 ? Kes(hisseler[2], 100) : null;
            }

            // ── 5) Alan tərəf bizim ÖZ bankımızın hesabıdırsa (DEBET prefiksi
            //      hüquqi şəxs balans sinfindədirsə VƏ TIP Hüquqi-Hüquqi/Fiziki
            //      istiqamətindədirsə) — NAME_UR_TO/ACCOUNT_TO/VÖEN LICSCH_ALL_2-dən,
            //      bank adı/SWIFT/ölkə kodu bizim ÖZ ATTRIB sətrimizdən, filial adı
            //      MFO-dan doldurulur. Oracle: 25ga6yfpu7j4z. ─────────────────────
            if (string.IsNullOrWhiteSpace(e.NameUrTo) && e.Tip is GoamlTipi.HToHIn or GoamlTipi.HToFIn or GoamlTipi.HToH
                && !string.IsNullOrWhiteSpace(e.Debet) && await BalschtDaxilindedirAsync("balscht_ur", e.Debet!, ct))
            {
                var licsch = await LicschAdiVeVoeniAsync(e.CandidateFilialNumber, e.Debet!, ct);
                if (licsch != null)
                {
                    e.NameUrTo = licsch.Value.Adi;
                    e.AccountTo = e.Debet;
                    e.IncorporationNumberTo = licsch.Value.Voen;
                }

                var ozBankimiz = await OzBankimizinAttributlariAsync(ct);
                if (ozBankimiz != null)
                {
                    e.InstitutionNameTo = ozBankimiz.Value.AdSoyad;
                    // ⚠️ AYSEL-in ÖZ sorğusu bunu belə yazır (ATTRIB.INN_BANK →
                    // INCORPORATION_COUNTRY_CODE_TO) — semantik qəribə görünsə də,
                    // BİRƏ-BİR köçürülüb, dəyişdirilmədi.
                    e.IncorporationCountryCodeTo = ozBankimiz.Value.InnBank;
                    e.SwiftTo = ozBankimiz.Value.BicSwift;
                }

                var filialAdi = await MfoFilialAdiAsync(e.CandidateFilialNumber, e.Debet!, ct);
                if (filialAdi != null) e.InstitutionNameBranchTo = filialAdi;
            }

            // ── 6) Bank adı SWIFT koduna görə (BIC_CODES) — yalnız 5-ci addım
            //      doldurmayıbsa (fərqli TIP) və SWIFT_TO operatorun özü tərəfindən
            //      daxil edilibsə. Oracle: 597z46xbd85t4. ────────────────────────
            if (string.IsNullOrWhiteSpace(e.InstitutionNameTo) && !string.IsNullOrWhiteSpace(e.SwiftTo))
            {
                var bankAdi = await BicBankAdiAsync(e.SwiftTo!, ct);
                if (bankAdi != null) e.InstitutionNameTo = bankAdi;
            }

            // ── 7) Vəsaitin növü (TRANSMODE_CODE) — DEBET fiziki şəxs hesabıdırsa
            //      VƏ hər iki tərəf "41" balans sinfindədirsə (kassa/nəğd keçid
            //      hesabları). Oracle: fgy83sfv5x5a7 (filial şərti ÇIXARILIB —
            //      tək filial, bax CLAUDE.md). ─────────────────────────────────
            if (string.IsNullOrWhiteSpace(e.TransmodeCode) &&
                RequemSertiVar(e.Debet, 2, "41") && RequemSertiVar(e.Kredit, 2, "41") &&
                !string.IsNullOrWhiteSpace(e.Debet) && await BalschtDaxilindedirAsync("balscht_fiz", e.Debet!, ct))
            {
                e.TransmodeCode = "1002";
                e.TransmodeComment = "Pul vəsaitinin hesabdan köçürülməsi";
            }

            // ── 8) Defolt sahələr (NVL-tipli, Oracle sorğusuna ehtiyac yoxdur) —
            //      Oracle: 2d2br1x497137 / 76b2tupjc44ga / bb14x3a2db6v6 /
            //      908gtupuhh4wp / gpkvxqm3arh94 / ckjaz0k9r8tf9. ────────────────
            e.FromCountry ??= "AZ";
            e.ToOlkeAdi ??= "AZ";
            e.IncorporationCountryCode ??= "AZ";
            if (e.Snn == "-") e.Snn = null;
            if (e.SnnTo == "-") e.SnnTo = null;
            e.ToFundsCode ??= "A";
        }

        /// <summary>CON/SIG/TO sənəd seriya+nömrəsini (məs. "AZE1234567")
        /// `MMX_ID_SERIAL`-dakı ƏN UZUN uyğun gələn prefiksə görə seriya/nömrə
        /// hissələrinə ayırır. 1, 2, 3 simvollu prefikslər sınanır — uzun olan
        /// üstün tutulur (AYSEL-in özündə sıra qeyri-müəyyən idi, bax yuxarı).</summary>
        private static bool ParcalaSenedNomresi(string? xam, List<string> serialler, out string? seriya, out string? nomre)
        {
            seriya = null; nomre = null;
            if (string.IsNullOrWhiteSpace(xam)) return false;
            var temiz = xam.Trim();

            for (var uzunluq = Math.Min(3, temiz.Length); uzunluq >= 1; uzunluq--)
            {
                var prefiks = temiz.Substring(0, uzunluq);
                if (serialler.Any(s => string.Equals(s, prefiks, StringComparison.OrdinalIgnoreCase)))
                {
                    seriya = prefiks;
                    nomre = temiz.Length > uzunluq ? temiz.Substring(uzunluq, Math.Min(30, temiz.Length - uzunluq)).Trim() : null;
                    return true;
                }
            }
            return false;
        }

        /// <summary>DEBET/KREDIT kimi hesab kodlarının ilk N simvolunun verilmiş
        /// sabit mətnə bərabər olub-olmadığını yoxlayır (xana boşdursa/qısadırsa
        /// `false`).</summary>
        private static bool RequemSertiVar(string? deyer, int uzunluq, string gozlenilen) =>
            !string.IsNullOrWhiteSpace(deyer) && deyer!.Trim().Length >= uzunluq &&
            string.Equals(deyer.Trim().Substring(0, uzunluq), gozlenilen, StringComparison.OrdinalIgnoreCase);

        /// <summary>Oracle hesab kodunu (DEBET/KREDIT, daxili məlumatdır, amma
        /// ehtiyat üçün) SQL-ə yapışdırmazdan əvvəl təmizləyir.</summary>
        private static string HesabTehlukesiz(string xam) =>
            Regex.Replace(xam.Trim(), "[^A-Za-z0-9]", "");

        private static object? DVal(Dictionary<string, object?> row, string kolon) =>
            row.FirstOrDefault(kv => string.Equals(kv.Key, kolon, StringComparison.OrdinalIgnoreCase)).Value;

        private async Task<List<string>> MmxIdSerialSiyahisiAsync(CancellationToken ct)
        {
            try
            {
                var setirler = await _oracle.SelectAsync("select id_serial from odb.mmx_id_serial", 500, ct);
                return setirler
                    .Select(r => DVal(r, "ID_SERIAL") as string)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s!.Trim())
                    .ToList();
            }
            catch { return new List<string>(); }
        }

        private async Task<Dictionary<string, string>> TypesOfDocumentsXeritesiAsync(CancellationToken ct)
        {
            var xerite = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var setirler = await _oracle.SelectAsync("select isare, mmx_id from odb.types_of_documents", 500, ct);
                foreach (var r in setirler)
                {
                    var isare = DVal(r, "ISARE") as string;
                    var mmxId = DVal(r, "MMX_ID");
                    if (!string.IsNullOrWhiteSpace(isare) && mmxId != null)
                        xerite[isare.Trim()] = mmxId.ToString()!;
                }
            }
            catch { /* best-effort — Oracle əlçatmazdırsa bu addım sakitcə ötürülür */ }
            return xerite;
        }

        private async Task<bool> BalschtDaxilindedirAsync(string cedvel, string hesab, CancellationToken ct)
        {
            var temiz = HesabTehlukesiz(hesab);
            if (temiz.Length < 5) return false;
            var prefiks = temiz.Substring(0, 5);
            try
            {
                var setirler = await _oracle.SelectAsync(
                    $"select 1 from odb.{cedvel} where balscht = '{prefiks}'", 1, ct);
                return setirler.Count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GOAML BalschtDaxilindedirAsync({Cedvel}) uğursuz oldu — prefiks={Prefiks}", cedvel, prefiks);
                return false;
            }
        }

        private async Task<(string? Adi, string? Voen)?> LicschAdiVeVoeniAsync(int filial, string hesab, CancellationToken ct)
        {
            var temiz = HesabTehlukesiz(hesab);
            if (temiz.Length == 0) return null;
            try
            {
                var setirler = await _oracle.SelectAsync(
                    $"select name_licsch, inn_licsch from odb.licsch_all_2 where filialnumber = {filial} and licsch = '{temiz}'", 1, ct);
                if (setirler.Count == 0) return null;
                return (DVal(setirler[0], "NAME_LICSCH") as string, DVal(setirler[0], "INN_LICSCH") as string);
            }
            catch { return null; }
        }

        // ── Hesab sahibinin KYC-si — CON/SIG avtomatik doldurma (09.11.2026,
        //    CON mənbəyi 10.11.2026-da NUMAYENDE-yə düzəldildi) ───────────────
        //
        // 12.11.2026 YENİLƏNDİ — bax `HesabSahibiKycTapAsync`-in ÖZ şərhi: artıq
        // "Debet-ə bax, faydalı olsa dayan" YOXDUR, hər iki tərəf HƏMİŞƏ oxunur,
        // Tip-in istiqaməti seçimi edir. Aşağıdakı (ilkin) izah YALNIZ bir TƏK
        // hesabın (`HesabSahibiKycAsync`) KYC-sinin NECƏ tapıldığına aiddir.
        //
        // İstifadəçi qərarı: Debet/Kredit-dən BİZİM BANKIN hesabı olanının sahibi
        // fiziki şəxsdirsə CON+SIG-in hər ikisinə, hüquqi şəxsdirsə SIG-ə şirkətin
        // "Rəhbər"i, CON-a isə "Nümayəndə" seçim siyahısı doldurulur (operator
        // dropdown-dan seçir — "sistem gətirə bilmirsə, işçi doldurur"). Mənbə
        // (canlı Oracle araşdırması, PL/SQL Developer-dən əl ilə təsdiqlənib):
        //   - Fiziki: `odb.regnom` (açar: substr(hesab,10,6) = regnom.regnom),
        //     ingiliscə sütunlar — NAME/SURNAME/PATRONYMIC/BIRTHDATE/BIRTHADDRESS/
        //     GENDER/REGISTRATIONADDRESS/CITIZENSHIP/EMPLOYER/PIN/DOCUMENTNUMBER/
        //     GIVENDATE/EXPIREDATE/GIVENORGANIZATOIN — HAMISI VARCHAR2 (tarix
        //     sahələri DAXİL, DATE tipi DEYİL).
        //   - Hüquqi SIG ("Rəhbər") — `odb.imza_huquqi_olan_shexsler` (REGNOM-a
        //     görə), rolu `odb.aml_related_persons`(ID_AML, ID_RELATED_PERSONS)
        //     → `odb.aml_setup_related_persons`(ID, DESCRIPT) ilə tapılır.
        //     ID_RELATED_PERSONS=2 = "Rəhbər" (sabit açar cədvəl, istifadəçinin
        //     öz ekran-görüntüsü ilə təsdiqlənib). `IMZA_BITME IS NULL` =
        //     hazırda aktiv imza hüququ.
        //   - Hüquqi CON ("Nümayəndə") seçim siyahısı — **`odb.NUMAYENDE`**
        //     (10.11.2026, canlı Oracle araşdırması + istifadəçi təsdiqi: bu,
        //     "hesabın ÖZ nümayəndələrini" saxlayan XÜSUSİ cədvəldir,
        //     `imza_huquqi_olan_shexsler`-dən — imza HÜQUQU, fərqli konsept —
        //     TAMAMİLƏ AYRIDIR). Əvvəl (09-10.11.2026 arası) CON dropdown-u
        //     SƏHVƏN `imza_huquqi_olan_shexsler`-dən qurulurdu. `NUMAYENDE.REGNOM`
        //     hesabın regnom koduna bağlıdır (`imza_huquqi_olan_shexsler`-in
        //     elə özü kimi), `IMZA_BITME IS NULL` eyni "aktiv dövr" filtridir.
        //     Sütunları `imza_huquqi_olan_shexsler`-dən DAHA ZƏNGİNDİR (tam
        //     ünvan parçalanması, PEP/sanksiya bayraqları) — hələ CON formasının
        //     (Tesdiq.cshtml) özü bu əlavə sahələri DAŞIMIR, gələcək iş.
        // ── 12.11.2026, KRİTİK DÜZƏLİŞ — "Debet-ə bax, faydalı olsa DAYAN"
        //    SƏHV İDİ. Real hadisə: Debet=35025, Kredit=40140, hər ikisi hüquqi.
        //    35025-in NUMAYENDE (CON) siyahısı var idi, AMMA aktiv "Rəhbər" (SIG)
        //    YOX idi — köhnə kod bunu "faydalı nəticə" sayıb DAYANIRDI, Kredit-ə
        //    (40140, orada əsl Rəhbər ola bilərdi) HEÇ BAXMIRDI. Nəticə: imza
        //    sahibi boş qaldı, baxmayaraq ki məlumat digər tərəfdə var idi.
        //
        //    İndi HƏR İKİ tərəf HƏMİŞƏ oxunur, seçim buraya YOX, çağırana
        //    (EmeliyyatlarController) həvalə olunur — o da Tip-in istiqamətinə
        //    (OUT→Debet, IN→Kredit, HToH/FToF→operator seçsin) görə qərar verir.
        public async Task<GoamlHesabSahibiKycHerIkiTerefDto> HesabSahibiKycTapAsync(int filial, string? debet, string? kredit, CancellationToken ct = default)
        {
            var debetNeticesi = await HesabSahibiKycAsync(debet, ct);
            var kreditNeticesi = await HesabSahibiKycAsync(kredit, ct);
            return new GoamlHesabSahibiKycHerIkiTerefDto
            {
                Debet = Faydali(debetNeticesi) ? debetNeticesi : null,
                Kredit = Faydali(kreditNeticesi) ? kreditNeticesi : null,
            };
        }

        private static bool Faydali(GoamlHesabSahibiKycDto? d) =>
            d != null && (d.TekSexs != null || d.ElaqaliSexsler.Count > 0);

        /// <summary>"Nümayəndə" (CON) — əməliyyatın ÖZÜNÜN DOC_ID-si (=RECNUM) ilə
        /// uyğunlaşan Emitent (E_) tərəf (13.11.2026, istifadəçi tapıntısı). Açar
        /// YENİ DEYİL — `docs/sql/aml/90_AML_OracleSorgular.sql`-dəki artıq işlək
        /// "AML Hesab üzrə sorğu"nun özündə `t.recnum = k.doc_id(+)` kimi istifadə
        /// olunur, bura TƏKRAR SINANMADAN köçürülüb. Hesab-əsaslı NUMAYENDE
        /// siyahısından (HesabSahibiKycAsync) TAMAMİLƏ MÜSTƏQİLDİR — konkret bu
        /// əməliyyatı kim APARIB, hesabın ÜMUMİ nümayəndə siyahısından DAHA
        /// DƏQİQDİR. Yalnız ad/soyad VARSA "faydalı" sayılır — boş sətir (və ya
        /// yalnız B_ tərəfi dolu sətir) `null` qaytarır, CON forması boş qalır.</summary>
        public async Task<GoamlElaqaliSexsDto?> EmitentKycAsync(long recNum, CancellationToken ct = default)
        {
            try
            {
                var setirler = await _oracle.SelectAsync(
                    "select e_adi, e_soyadi, e_ata_adi, e_tevellud, e_dogum_yeri, e_pincode, " +
                    "e_vetendashligi, e_unvanin_teyinati, e_unvan, e_unvan_sheher, e_unvan_olke, " +
                    "e_senedin_novu, e_senedin_seriya_ve_nomresi, e_senedin_verilme_tarixi, " +
                    "e_senedin_etibarlig_muddeti, e_senedin_veren_organin_adi, e_senedin_verildiyi_olke, " +
                    "e_cinsi, e_ish_yerinin_adi, e_ish_yerinin_vezifesi " +
                    "from odb.emitent_benefisiar where doc_id = " + recNum, 1, ct);

                if (setirler.Count == 0) return null;
                var r = setirler[0];

                var adi = DVal(r, "E_ADI") as string;
                var soyadi = DVal(r, "E_SOYADI") as string;
                if (string.IsNullOrWhiteSpace(adi) && string.IsNullOrWhiteSpace(soyadi)) return null;

                return new GoamlElaqaliSexsDto
                {
                    Adi = adi,
                    Soyadi = soyadi,
                    AtaAdi = DVal(r, "E_ATA_ADI") as string,
                    DogulduguTarix = TarixiParseEt(DVal(r, "E_TEVELLUD")),
                    DogulduguYer = DVal(r, "E_DOGUM_YERI") as string,
                    Pin = DVal(r, "E_PINCODE") as string,
                    Vetendasligi = DVal(r, "E_VETENDASHLIGI") as string,
                    UnvaninNovu = DVal(r, "E_UNVANIN_TEYINATI") as string,
                    Unvan = DVal(r, "E_UNVAN") as string,
                    Seher = DVal(r, "E_UNVAN_SHEHER") as string,
                    Olke = DVal(r, "E_UNVAN_OLKE") as string,
                    SenedinNovu = DVal(r, "E_SENEDIN_NOVU") as string,
                    SenedinSeriyaNomresi = DVal(r, "E_SENEDIN_SERIYA_VE_NOMRESI") as string,
                    SenedinVerilmeTarixi = TarixiParseEt(DVal(r, "E_SENEDIN_VERILME_TARIXI")),
                    SenedinEtibarliliqMuddeti = TarixiParseEt(DVal(r, "E_SENEDIN_ETIBARLIG_MUDDETI")),
                    VerenOrqan = DVal(r, "E_SENEDIN_VEREN_ORGANIN_ADI") as string,
                    VerildiyiOlke = DVal(r, "E_SENEDIN_VERILDIYI_OLKE") as string,
                    Cinsi = DVal(r, "E_CINSI") as string,
                    // İş yeri/vəzifə cədvəldə var, AMMA GoamlTesdiqDto-nun CON
                    // blokunda bu ikisi üçün sahə YOXDUR (yalnız SIG-də) —
                    // obyektin özündə saxlanır (gələcək CON genişlənməsi üçün),
                    // çağıran tərəf bunları formaya YAZMIR.
                    IsYeriAdi = DVal(r, "E_ISH_YERININ_ADI") as string,
                    Vezife = DVal(r, "E_ISH_YERININ_VEZIFESI") as string,
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GOAML EmitentKycAsync uğursuz oldu — recNum={RecNum}", recNum);
                return null;
            }
        }

        private async Task<GoamlHesabSahibiKycDto?> HesabSahibiKycAsync(string? hesab, CancellationToken ct)
        {
            var temiz = HesabTehlukesiz(hesab ?? "");
            if (temiz.Length < 15) return null; // substr(hesab,10,6) üçün ən azı 15 simvol lazımdır

            // Fiziki/hüquqi ayrımını BİRBAŞA BALSCHT üzvlüyündən götürürük —
            // `odb.regnom`-a GÜVƏNMİRİK, çünki o cədvəl hər hüquqi şəxsin öz
            // sətrini daşımır (09.11.2026-da real hadisə: BALSCHT_UR-da
            // tanınan, imza_huquqi_olan_shexsler-də aktiv sətri OLAN bir
            // şirkət üçün regnom-da sətir TAPILMADI, köhnə kod bunu "heç nə
            // tapılmadı" sayıb erkən `null` qaytarırdı, əslində data var idi).
            var fizPrefiksdir = await BalschtDaxilindedirAsync("balscht_fiz", temiz, ct);
            var urPrefiksdir = !fizPrefiksdir && await BalschtDaxilindedirAsync("balscht_ur", temiz, ct);
            if (!fizPrefiksdir && !urPrefiksdir) return null;

            var regnomKodu = temiz.Substring(9, 6);

            try
            {
                if (fizPrefiksdir)
                {
                    var regnomSetirler = await _oracle.SelectAsync(
                        "select name, surname, patronymic, birthdate, birthaddress, gender, " +
                        "registrationaddress, citizenship, employer, pin, documentnumber, givendate, " +
                        "expiredate, givenorganizatoin from odb.regnom where regnom = '" + regnomKodu + "'", 1, ct);
                    if (regnomSetirler.Count == 0) return null;

                    var r = regnomSetirler[0];
                    return new GoamlHesabSahibiKycDto
                    {
                        Fiziki = true,
                        TekSexs = new GoamlElaqaliSexsDto
                        {
                            Adi = DVal(r, "NAME") as string,
                            Soyadi = DVal(r, "SURNAME") as string,
                            AtaAdi = DVal(r, "PATRONYMIC") as string,
                            DogulduguTarix = TarixiParseEt(DVal(r, "BIRTHDATE") as string),
                            DogulduguYer = DVal(r, "BIRTHADDRESS") as string,
                            Cinsi = DVal(r, "GENDER") as string,
                            Unvan = DVal(r, "REGISTRATIONADDRESS") as string,
                            Vetendasligi = DVal(r, "CITIZENSHIP") as string,
                            IsYeriAdi = DVal(r, "EMPLOYER") as string,
                            Pin = DVal(r, "PIN") as string,
                            SenedinSeriyaNomresi = DVal(r, "DOCUMENTNUMBER") as string,
                            SenedinVerilmeTarixi = TarixiParseEt(DVal(r, "GIVENDATE") as string),
                            SenedinEtibarliliqMuddeti = TarixiParseEt(DVal(r, "EXPIREDATE") as string),
                            VerenOrqan = DVal(r, "GIVENORGANIZATOIN") as string,
                        },
                    };
                }

                // Hüquqi şəxs (BALSCHT_UR) — İKİ AYRI, MÜSTƏQİL mənbə (10.11.2026,
                // canlı Oracle araşdırması: `odb.NUMAYENDE`-nin özü "hesabın öz
                // nümayəndələrini" saxlayan XÜSUSİ cədvəldir, `imza_huquqi_olan_
                // shexsler`-dən (imza HÜQUQU) KONSEPTUAL FƏRQLİDİR):
                //   - SIG ("Hesab imza hüququ olan şəxsin") — imza_huquqi_olan_
                //     shexsler-dən, "Rəhbər" rolu (ID_RELATED_PERSONS=2). DƏYİŞMİR.
                //   - CON ("Nümayəndənin") seçim siyahısı — artıq `odb.NUMAYENDE`-dən
                //     (əvvəl səhvən imza_huquqi_olan_shexsler-dən idi — cədvəl adı
                //     ilə forma sahəsi indi BİRƏ-BİR üst-üstə düşür).
                // Biri boş qayıtsa o biri YENƏ DƏ doldurulsun deyə HEÇ BİRİ digərinin
                // "tapılmadı" halında erkən `return` etmir.
                //
                // "AKTİV" = IMZA_BITME BOŞ DEYİL (12.11.2026, KRİTİK): real data
                // "səlahiyyətin bitmə tarixi"ni HƏMİŞƏ `null` kimi yazmır — bəzi
                // sətirlərdə uzaq-gələcək bir sentinel tarix (məs. 05-05-2125)
                // yazılıb, "hələ bitməyib" mənasında. Yalnız `IS NULL` yoxlaması
                // belə sətirləri "qeyri-aktiv" sayıb KYC-ni TAMAMİLƏ gizlədirdi —
                // nə CON, nə SIG dolmurdu, nə də "avtomatik gətirildi"/"hər iki
                // tərəf" banner-lərindən heç biri çıxmırdı (hər iki tərəf boş
                // qayıtdığı üçün `Faydali()` də `false` idi). Düzgün şərt:
                // `IMZA_BITME IS NULL OR IMZA_BITME > SYSDATE` — bitmə tarixi
                // YOXDURSA, VƏ YA hələ KEÇMƏYİBSƏ, səlahiyyət aktivdir.
                var rehberSetirleri = await _oracle.SelectAsync(
                    "select s.id_aml, s.adi, s.soyadi, s.ata_adi, s.doguldugu_tarix, s.doguldugu_yer, " +
                    "s.cinsi, s.fin, s.telefon_nomresi, s.unvanin_teyinati, s.kuchenin_adi_ve_nomresi, " +
                    "s.sheher, s.olke, s.senedin_novu, s.seriyasi_ve_nomresi, s.verilme_tarixi, " +
                    "s.etibarliliq_muddeti, s.veren_orqanin_adi, s.verildiyi_olke, s.vezifesi, " +
                    "s.ish_yerinin_adi " +
                    "from odb.imza_huquqi_olan_shexsler s " +
                    "where s.regnom = '" + regnomKodu + "' and (s.imza_bitme is null or s.imza_bitme > sysdate) " +
                    "and exists (select 1 from odb.aml_related_persons rp where rp.id_aml = s.id_aml and rp.id_related_persons = 2)",
                    1, ct);

                var rehber = rehberSetirleri.Count > 0 ? ImzaHuququSetirdenQur(rehberSetirleri[0]) : null;

                var numayendeler = await NumayendeSiyahisiAsync(regnomKodu, ct);

                return new GoamlHesabSahibiKycDto
                {
                    Fiziki = false,
                    TekSexs = rehber,
                    ElaqaliSexsler = numayendeler,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GOAML HesabSahibiKycAsync uğursuz oldu — regnomKodu={RegnomKodu}, fizPrefiksdir={FizPrefiksdir}", regnomKodu, fizPrefiksdir);
                return null;
            }
        }

        /// <summary>CON ("Nümayəndənin") seçim siyahısı — `odb.NUMAYENDE`-dən,
        /// hesabın regnom koduna görə, hazırda aktiv (IMZA_BITME boş VƏ YA
        /// hələ keçməmiş gələcək tarix — bax 12.11.2026 qeydi, CLAUDE.md)
        /// sətirlər. Sütunlar 09.11.2026-da (CLAUDE.md) istifadəçinin canlı `SELECT *`
        /// nəticəsindən TƏSDİQLƏNİB — `imza_huquqi_olan_shexsler`-dən FƏRQLİ
        /// olaraq ADRES/SƏNƏD sahələri qat-qat zəngindir, amma CON formasının
        /// (Tesdiq.cshtml → gmlConSec) oxuduğu sahələr eynidir, əlavə sahə (PEP/
        /// sanksiya bayraqları) HƏLƏ FORMADA YOXDUR — gələcək iş.</summary>
        private async Task<List<GoamlElaqaliSexsDto>> NumayendeSiyahisiAsync(string regnomKodu, CancellationToken ct)
        {
            try
            {
                var setirler = await _oracle.SelectAsync(
                    "select n.id_aml, n.adi, n.soyadi, n.ata_adi, n.doguldugu_tarix, n.doguldugu_yer, " +
                    "n.cinsi, n.fin, n.telefon_nomresi, n.unvanin_teyinati, n.kuchenin_adi_ve_nomresi, " +
                    "n.sheher, n.olke, n.senedin_novu, n.seriyasi_ve_nomresi, n.verilme_tarixi, " +
                    "n.etibarliliq_muddeti, n.veren_orqanin_adi, n.verildiyi_olke, n.vezifesi " +
                    "from odb.numayende n " +
                    "where n.regnom = '" + regnomKodu + "' and (n.imza_bitme is null or n.imza_bitme > sysdate)", 50, ct);
                return setirler.Select(NumayendeSetirdenQur).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GOAML NumayendeSiyahisiAsync uğursuz oldu — regnomKodu={RegnomKodu}", regnomKodu);
                return new List<GoamlElaqaliSexsDto>();
            }
        }

        private GoamlElaqaliSexsDto NumayendeSetirdenQur(Dictionary<string, object?> r) => new()
        {
            IdAml = (int)ToLong(DVal(r, "ID_AML")),
            // "Rollar" sahəsi burada vəzifəni daşıyır (NUMAYENDE-nin öz rol
            // cədvəli yoxdur/araşdırılmayıb) — CON dropdown-unda "Adı Soyadı
            // (Vəzifə)" kimi görünür, imza_huquqi_olan_shexsler-dəki "(Rəhbər)"
            // etiketi ilə EYNİ yerdə, fərqli mənbədən.
            Rollar = DVal(r, "VEZIFESI") as string,
            Adi = DVal(r, "ADI") as string,
            Soyadi = DVal(r, "SOYADI") as string,
            AtaAdi = DVal(r, "ATA_ADI") as string,
            DogulduguTarix = TarixiParseEt(DVal(r, "DOGULDUGU_TARIX")),
            DogulduguYer = DVal(r, "DOGULDUGU_YER") as string,
            Cinsi = DVal(r, "CINSI") as string,
            Pin = DVal(r, "FIN") as string,
            TelefonNomresi = DVal(r, "TELEFON_NOMRESI") as string,
            UnvaninNovu = DVal(r, "UNVANIN_TEYINATI") as string,
            Unvan = DVal(r, "KUCHENIN_ADI_VE_NOMRESI") as string,
            Seher = DVal(r, "SHEHER") as string,
            Olke = DVal(r, "OLKE") as string,
            SenedinNovu = DVal(r, "SENEDIN_NOVU") as string,
            SenedinSeriyaNomresi = DVal(r, "SERIYASI_VE_NOMRESI") as string,
            SenedinVerilmeTarixi = TarixiParseEt(DVal(r, "VERILME_TARIXI")),
            SenedinEtibarliliqMuddeti = TarixiParseEt(DVal(r, "ETIBARLILIQ_MUDDETI")),
            VerenOrqan = DVal(r, "VEREN_ORQANIN_ADI") as string,
            VerildiyiOlke = DVal(r, "VERILDIYI_OLKE") as string,
        };

        private GoamlElaqaliSexsDto ImzaHuququSetirdenQur(Dictionary<string, object?> r) => new()
        {
            IdAml = (int)ToLong(DVal(r, "ID_AML")),
            Rollar = DVal(r, "ROLLAR") as string,
            Adi = DVal(r, "ADI") as string,
            Soyadi = DVal(r, "SOYADI") as string,
            AtaAdi = DVal(r, "ATA_ADI") as string,
            DogulduguTarix = TarixiParseEt(DVal(r, "DOGULDUGU_TARIX") as string),
            DogulduguYer = DVal(r, "DOGULDUGU_YER") as string,
            Cinsi = DVal(r, "CINSI") as string,
            Pin = DVal(r, "FIN") as string,
            TelefonNomresi = DVal(r, "TELEFON_NOMRESI") as string,
            UnvaninNovu = DVal(r, "UNVANIN_TEYINATI") as string,
            Unvan = DVal(r, "KUCHENIN_ADI_VE_NOMRESI") as string,
            Seher = DVal(r, "SHEHER") as string,
            Olke = DVal(r, "OLKE") as string,
            SenedinNovu = DVal(r, "SENEDIN_NOVU") as string,
            SenedinSeriyaNomresi = DVal(r, "SERIYASI_VE_NOMRESI") as string,
            SenedinVerilmeTarixi = TarixiParseEt(DVal(r, "VERILME_TARIXI") as string),
            SenedinEtibarliliqMuddeti = TarixiParseEt(DVal(r, "ETIBARLILIQ_MUDDETI") as string),
            VerenOrqan = DVal(r, "VEREN_ORQANIN_ADI") as string,
            VerildiyiOlke = DVal(r, "VERILDIYI_OLKE") as string,
            Vezife = DVal(r, "VEZIFESI") as string,
            IsYeriAdi = DVal(r, "ISH_YERININ_ADI") as string,
        };

        /// <summary>Oracle-dan gələn tarix dəyərini oxuyur — sütun VARCHAR2 (mətn,
        /// `imza_huquqi_olan_shexsler`-in bütün tarix sahələri kimi) VƏ ya əsl
        /// DATE (NUMAYENDE-nin IMZA_BASHLAMA/BITME-si kimi) ola bilər, hər ikisini
        /// dəstəkləyir — tipi TƏXMİN ETMƏK əvəzinə (10.11.2026, NUMAYENDE
        /// əlavəsi zamanı möhkəmləndirildi).</summary>
        private static DateTime? TarixiParseEt(object? deyer)
        {
            if (deyer is DateTime dt) return dt;
            if (deyer is not string metn || string.IsNullOrWhiteSpace(metn)) return null;
            var temiz = metn.Trim();
            string[] formatlar = { "dd.MM.yyyy", "dd.MM.yyyy HH:mm:ss", "yyyy-MM-dd", "dd-MM-yyyy", "dd/MM/yyyy", "yyyy.MM.dd" };
            foreach (var f in formatlar)
                if (DateTime.TryParseExact(temiz, f, CultureInfo.InvariantCulture, DateTimeStyles.None, out var tarix))
                    return tarix;
            return DateTime.TryParse(temiz, CultureInfo.InvariantCulture, DateTimeStyles.None, out var tarix2) ? tarix2 : null;
        }

        private async Task<(string? AdSoyad, string? InnBank, string? BicSwift)?> OzBankimizinAttributlariAsync(CancellationToken ct)
        {
            try
            {
                var setirler = await _oracle.SelectAsync(
                    "select naimen_uchrej, inn_bank, bic_swift from odb.attrib", 1, ct);
                if (setirler.Count == 0) return null;
                return (DVal(setirler[0], "NAIMEN_UCHREJ") as string, DVal(setirler[0], "INN_BANK") as string, DVal(setirler[0], "BIC_SWIFT") as string);
            }
            catch { return null; }
        }

        private async Task<string?> MfoFilialAdiAsync(int filial, string hesab, CancellationToken ct)
        {
            var temiz = HesabTehlukesiz(hesab);
            if (temiz.Length == 0) return null;
            try
            {
                var setirler = await _oracle.SelectAsync(
                    $"select m.bank_shot_name from odb.view_licsch_all b join odb.mfo m on b.mfo = m.mfo " +
                    $"where b.filialnumber = {filial} and b.licsch = '{temiz}'", 1, ct);
                return setirler.Count > 0 ? DVal(setirler[0], "BANK_SHOT_NAME") as string : null;
            }
            catch { return null; }
        }

        private async Task<string?> BicBankAdiAsync(string swift, CancellationToken ct)
        {
            var temiz = HesabTehlukesiz(swift).ToUpperInvariant();
            if (temiz.Length == 0) return null;
            var rpad = temiz.PadRight(11, 'X').Substring(0, 11);
            try
            {
                var setirler = await _oracle.SelectAsync(
                    $"select distinct bank_name from odb.bic_codes where bic_code = '{rpad}'", 1, ct);
                return setirler.Count > 0 ? DVal(setirler[0], "BANK_NAME") as string : null;
            }
            catch { return null; }
        }

        /// <summary>Mətni max uzunluğa kəsir (Oracle `SUBSTR(...,1,N)` ilə eyni) —
        /// `NAME_UR_TO`-nun ad-parçalanması nəticəsi 100 simvoldan uzun ola bilməz.</summary>
        private static string? Kes(string? xam, int maxUzunluq) =>
            string.IsNullOrEmpty(xam) ? xam : (xam.Length <= maxUzunluq ? xam : xam.Substring(0, maxUzunluq));

        // ════════════════════════════════════════════════════════════════════
        // GOAML — Export to JSON (#15, 12.11.2026)
        //
        // `docs/sql/goaml/97_Goaml_JSON_MMX_IN_Tam_Metn.sql`-ın (AYSEL-in
        // `JSON_MMX_IN` prosedurunun — YALNIZ "_IN" (GƏLƏN əməliyyat) variantı,
        // "_SEND" (gedən) HƏLƏ PORT EDİLMƏYİB) C#-dəki qarşılığıdır. Oracle
        // `GOAML_CLIENTS` cədvəlini oxumur — bizim ÖZ `GoamlEmeliyyat` cədvəlimizi
        // (Təsdiqlə ilə artıq doldurulmuş sətirlər) oxuyur.
        //
        // ⚠️ BİLƏRƏKDƏN PORT EDİLMƏYƏN HİSSƏLƏR (gələcək iş, bu artım XARİCİNDƏ):
        //  - `dubl_number` (huquqi_shexs.REGISTRATION_NUMBER, VÖEN üzrə) — boş qalır.
        //  - QHT/PEP/BLR-ə xas əlavə header sahələri (tin, hrzCountry,
        //    customerRiskDegree, typeOfConnectionToPep/Hrz və s.) — bunlar ƏLAVƏ
        //    canlı KYC/risk sorğuları (pv_regnom_all.Riskler, r_emitent_benefisiar,
        //    c_PV_HUQUQI) tələb edir, header YALNIZ sendDate/reportType/
        //    reportReason daşıyır.
        //  - `ID_VD`/`OPCODE_2`-əsaslı dəqiq trn_payment_system (UTF/XKS/SWF/AZP/
        //    AOS) — bizim namizəd sorğumuzda bu açarlar yoxdur (06.11.2026-dan
        //    məlum, "Update New" portunda da eyni səbəbdən atlanıb). Yalnız
        //    document_source-əsaslı CBB/IAP/PST təyini VAR, defolt 'CBB'.
        //  - V_YURIK__NAME (bizim öz bankımızın hesabıdırsa KYC-ni ordan çəkmə),
        //    C_INN_BANK (bank VÖEN-i SWIFT koduna görə) — sender/receiver_account
        //    sahələri YALNIZ bizim ÖZ entity-mizdəki (InstitutionName/Swift/...)
        //    dəyərlərdən qurulur, əlavə Oracle join aparılmır.
        //  - Representative-in "SIG" qolu (Oracle-un öz mənbəyində DEAD CODE-dur
        //    — `ConAdi null` şərti onsuz da NRP qolunun OR-unda tutulur, SIG
        //    elsif-i HEÇ VAXT işə düşmür) — ona görə bizdə YALNIZ PAT/NRP var.
        //
        // Hər QEYD üçün AYRI bir "report" obyekti yaradılır (header+transactions[1])
        // — elə AYSEL-in öz c_goal LOOP-u da BELƏ yazır, "reports" massivi N
        // elementdən ibarətdir, N=ixrac edilən qeyd sayı.
        // ════════════════════════════════════════════════════════════════════

        /// <summary>Export-un "hansı qeydlər uyğun gəlir" şərti — TƏK yerdə, həm
        /// <see cref="JsonIxracAsync"/> (real ixrac), həm <see cref="IxracOnizlemeAsync"/>
        /// (ixracdan ƏVVƏL göstərilən önizləmə) bunu çağırır ki, "nə göstərilir"
        /// ilə "nə ixrac olunur" HEÇ VAXT fərqlənməsin (13.11.2026, istifadəçi
        /// tələbi: "hansı sənədlər ixrac olunacaq onu göstər").</summary>
        private static Expression<Func<GoamlEmeliyyat, bool>> IxracSertiQur(GoamlJsonIxracFiltrDto filtr)
        {
            var basTarix = filtr.BasTarix.Date;
            var sonTarix = filtr.SonTarix.Date.AddDays(1).AddTicks(-1);
            return x =>
                x.Status == GoamlEmeliyyatStatusu.Qaralama &&
                x.DateOper != null && x.DateOper >= basTarix && x.DateOper <= sonTarix &&
                (filtr.Tip == null || x.Tip == filtr.Tip) &&
                (filtr.ReportCode == null || x.ReportCode == filtr.ReportCode);
        }

        /// <summary>"İxrac et (JSON)"-dan ƏVVƏL, uyğun gələcək (hələ ixrac olunmamış,
        /// "Qaralama") qeydlərin siyahısını göstərir — HEÇ NƏYİ dəyişdirmir, Status
        /// mutasiyası YOXDUR (13.11.2026). <see cref="JsonIxracAsync"/> ilə EYNİ
        /// <see cref="IxracSertiQur"/> şərtini paylaşır.</summary>
        public async Task<Result<List<GoamlEmeliyyatListDto>>> IxracOnizlemeAsync(GoamlJsonIxracFiltrDto filtr, CancellationToken ct = default)
        {
            var setirler = await _unitOfWork.Repository<GoamlEmeliyyat>().HamisiniGetirAsync(IxracSertiQur(filtr));
            var neice = (await SetirleriDtoyaCevirAsync(setirler))
                .OrderBy(x => x.DateOper)
                .ToList();

            return Result<List<GoamlEmeliyyatListDto>>.Ok(neice);
        }

        public async Task<Result<GoamlJsonIxracNeticeDto>> JsonIxracAsync(GoamlJsonIxracFiltrDto filtr, CancellationToken ct = default)
        {
            var setirler = await _unitOfWork.Repository<GoamlEmeliyyat>().HamisiniGetirAsync(IxracSertiQur(filtr));

            if (setirler.Count == 0)
                return Result<GoamlJsonIxracNeticeDto>.Fail(
                    "Seçilmiş aralıqda ixrac ediləcək (hələ ixrac olunmamış) təsdiqlənmiş qeyd yoxdur.");

            var xeberdarliqlar = new List<string>();
            var malumatinNovuKesi = new Dictionary<string, (int reportType, int reportReason)?>();
            var valyutaKoduKesi = new Dictionary<string, string?>();

            var root = new GoamlJsonRootDto();

            foreach (var e in setirler.OrderBy(x => x.DateOper))
            {
                var reportCodeKod = e.ReportCode?.Kod() ?? "STR";

                if (!malumatinNovuKesi.TryGetValue(reportCodeKod, out var mn))
                {
                    mn = await MalumatinNovuTapAsync(reportCodeKod, ct);
                    malumatinNovuKesi[reportCodeKod] = mn;
                    if (mn == null)
                        xeberdarliqlar.Add($"\"{reportCodeKod}\" üçün reportType/reportReason Oracle-dan (malumatin_novu) tapılmadı — 0 yazıldı, əl ilə yoxlayın.");
                }

                root.Reports.Add(await ReportQurAsync(e, mn, reportCodeKod, valyutaKoduKesi, ct));
            }

            var json = System.Text.Json.JsonSerializer.Serialize(root,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });

            foreach (var e in setirler)
                e.Status = GoamlEmeliyyatStatusu.IxracEdilib;
            await _unitOfWork.YaddaSaxlaAsync();

            var faylAdi = $"GOAML_Ixrac_{(filtr.ReportCode?.Kod() ?? "HAMISI")}_{DateTime.Now:yyyyMMdd_HHmmss}_TEST.json";

            return Result<GoamlJsonIxracNeticeDto>.Ok(new GoamlJsonIxracNeticeDto
            {
                JsonMetni = json,
                TeklifOlunanFaylAdi = faylAdi,
                SetirSayi = setirler.Count,
                Xeberdarliqlar = xeberdarliqlar,
            });
        }

        private async Task<GoamlJsonReportDto> ReportQurAsync(GoamlEmeliyyat e, (int reportType, int reportReason)? mn,
            string reportCodeKod, Dictionary<string, string?> valyutaKoduKesi, CancellationToken ct)
        {
            var report = new GoamlJsonReportDto
            {
                Header = new GoamlJsonHeaderDto
                {
                    SendDate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.001Z", CultureInfo.InvariantCulture),
                    ReportType = mn?.reportType ?? 0,
                    ReportReason = mn?.reportReason ?? 0,
                },
            };

            var bloc = new GoamlJsonTransactionBlocDto
            {
                Transaction = TransactionQur(e),
                Representative = RepresentativeQur(e),
                Sender = await SenderQurAsync(e, valyutaKoduKesi, ct),
                Receiver = await ReceiverQurAsync(e, valyutaKoduKesi, ct),
                Correspondence = new GoamlJsonCorrespondenceDto
                {
                    Debet = Kes(e.Debet, 5) ?? "",
                    Credit = Kes(e.Kredit, 5) ?? "",
                },
            };
            report.Transactions.Add(bloc);
            return report;
        }

        private static GoamlJsonTransactionDto TransactionQur(GoamlEmeliyyat e)
        {
            var tarix = (e.DateOper ?? e.CandidateDateOper).ToString("yyyy-MM-ddTHH:mm:ss.002Z", CultureInfo.InvariantCulture);
            return new GoamlJsonTransactionDto
            {
                TrnStatus = "EXC",
                TrnActualDate = tarix,
                TrnBankDate = tarix,
                TrnExternalNumber = e.TransactionNumber ?? "",
                TrnInternalNumber = e.TransactionNumber ?? "",
                TrnType = TrnTipTeyinEt(e),
                TrnPaymentSystem = TrnOdenisSistemiTeyinEt(e),
                TrnPaymentSubSystem = "", // S1CARDSC-əsaslı VIS/MCR/BCC təyini port edilməyib
                Purpose = new GoamlJsonPurposeDto
                {
                    TrnDescription = (e.Primechanie ?? "").Replace("\r", "").Replace("\n", " "),
                    TrnDetailedId = e.RegistracNomer ?? "",
                    TrnFundSource = "",
                },
                TrnChanel = TrnKanalTeyinEt(e),
            };
        }

        private static bool Has(string? menbe, string teq) =>
            !string.IsNullOrEmpty(menbe) && menbe.Contains(teq, StringComparison.OrdinalIgnoreCase);

        private static bool Pref3(string? hesab, string teq) =>
            !string.IsNullOrEmpty(hesab) && hesab.Length >= 3 && hesab.Substring(0, 3) == teq;

        /// <summary>Oracle sətir 618-644 — "nağd" (CSH) vs "qeyri-nağd" (NCH) təyini,
        /// document_source mətn-uyğunluğu + hesab prefiksi + ETR report-kodu ilə.</summary>
        private static string TrnTipTeyinEt(GoamlEmeliyyat e)
        {
            var ds = e.DocumentSource;
            if (Has(ds, "EXPRESS") || Has(ds, "EMANAT") || Has(ds, "MILLION") || Has(ds, "SWPAY") ||
                Has(ds, "ATM") || Has(ds, "(CIN)") || Has(ds, "(OC)") || Has(ds, "(POS)") ||
                Pref3(e.Debet, "100") || Pref3(e.Kredit, "100"))
                return "CSH";

            if (Has(ds, "IPS") || Has(ds, "XOHKS") || Has(ds, "VSHAPE") || Has(ds, "SWIFT") ||
                Has(ds, "B2B") || e.ReportCode == GoamlHesabatNovu.Etr)
                return "NCH";

            return "NCH"; // Oracle-un öz NVL(V_trn_type,'NCH') defolt dəyəri
        }

        /// <summary>Oracle sətir 647-717 — yalnız document_source-əsaslı hissə (CBB/IAP/PST).
        /// ID_VD/pla_sys/OPCODE_2-əsaslı (UTF/XKS/SWF/AZP/AOS) dəqiqləşdirmə YOXDUR.</summary>
        private static string TrnOdenisSistemiTeyinEt(GoamlEmeliyyat e)
        {
            var ds = e.DocumentSource;
            if (Has(ds, "OC") || Pref3(e.Debet, "100") || Pref3(e.Kredit, "100"))
                return "CBB";
            if (Has(ds, "EXPRESS") || Has(ds, "EMANAT") || Has(ds, "MILLION") || Has(ds, "SWPAY"))
                return "IAP";
            if (Has(ds, "POS") || Has(ds, "ATM") || Has(ds, "(CIN)") || Has(ds, "(CTC)") ||
                Has(ds, "(RET)") || Has(ds, "(PST)") || Has(ds, "(ELC)"))
                return "PST";
            return "CBB";
        }

        /// <summary>Oracle sətir 728-772 — ardıcıl `if` (elsif DEYİL), SON uyğun gələn
        /// QALİB gəlir, eynilə AYSEL-in öz davranışı.</summary>
        private static string TrnKanalTeyinEt(GoamlEmeliyyat e)
        {
            var ds = e.DocumentSource;
            var kanal = "FOT";
            if (Has(ds, "EXPRESS") || Has(ds, "EMANAT") || Has(ds, "MILLION") || Has(ds, "SWPAY")) kanal = "PTM";
            if (Has(ds, "EC")) kanal = "ECM";
            if (Has(ds, "ATM") || Has(ds, "(CIN)")) kanal = "ATM";
            if (Has(ds, "POS")) kanal = "POS";
            // Oracle: `if V_MN.REPORTTYPE='CTR'` — REPORT_CODE-un ÖZÜNDƏN oxunur
            // (digər bütün 'CTR' müqayisələri ilə EYNİ mənbə, ehtimal olunan
            // Oracle-dakı yazı tutarsızlığının DÜZƏLDİLMİŞ forması).
            if (e.ReportCode == GoamlHesabatNovu.Ctr) kanal = "FOT";
            return kanal;
        }

        /// <summary>Oracle sətir 1009-1067 — Representative (Nümayəndə) blokunun
        /// PAT/NRP qolları. "SIG" elsif-i Oracle-un öz mənbəyində ÇATILMAZDIR
        /// (bax yuxarıdakı baş şərh), bura KÖÇÜRÜLMƏYİB.</summary>
        private static GoamlJsonRepresentativeDto RepresentativeQur(GoamlEmeliyyat e)
        {
            var konInstitusional = e.Tip is GoamlTipi.HToH or GoamlTipi.HToHIn or GoamlTipi.HToFIn or GoamlTipi.HToFOut;
            if (!string.IsNullOrWhiteSpace(e.ConAdi) && !konInstitusional)
            {
                return new GoamlJsonRepresentativeDto
                {
                    ReprStatus = "PAT",
                    ReprCitizenship = e.ConVetendasligi ?? "",
                    ReprIdSerial = e.ConSenedinSeriyaNomresi ?? "",
                    ReprIdNumber = e.ReprIdNumber ?? "",
                    ReprIdIssueDate = Tarix(e.ConSenedinVerilmeTarixi),
                    ReprIdExpiryDate = Tarix(e.ConSenedinEtibarliliqMuddeti),
                    ReprBirthDate = Tarix(e.ConDogulduguTarix),
                    ReprPhone = e.ConTelefonNomresi ?? "",
                    ReprPin = e.ConPin ?? "",
                    ReprFirstName = (e.ConAdi ?? "").Trim(),
                    ReprLastName = (e.ConSoyadi ?? "").Trim(),
                    ReprMiddleName = (e.ConAtasininAdi ?? "").Trim(),
                    ReprIdType = e.ReprIdType ?? "",
                };
            }

            return new GoamlJsonRepresentativeDto { ReprStatus = "NRP" };
        }

        private static string Tarix(DateTime? d) =>
            d == null ? "" : d.Value.ToString("yyyy-MM-ddTHH:mm:ss.001Z", CultureInfo.InvariantCulture);

        /// <summary>Oracle sətir 1070-1246 — "_IN" (GƏLƏN əməliyyat) prosedurunda
        /// Sender.Person/Account ƏSLİNDƏ "To*" (bizim "Alan fiziki şəxsin" bloku)
        /// sahələrindən doldurulur — xarici tərəf PULU GÖNDƏRƏN kimi qeydə alınır.
        /// Bu, Oracle mənbəyindən birbaşa götürülüb, SƏHV DEYİL.</summary>
        private async Task<GoamlJsonSenderDto> SenderQurAsync(GoamlEmeliyyat e, Dictionary<string, string?> valyutaKoduKesi, CancellationToken ct)
        {
            var sender = new GoamlJsonSenderDto { SenderCountryName = e.FromCountry ?? "" };

            if (!string.IsNullOrWhiteSpace(e.NameUr))
            {
                sender.Entity = new GoamlJsonSenderEntityDto
                {
                    SenderEntityName = e.NameUr ?? "",
                    SenderEntityRegCountry = e.IncorporationCountryCode ?? "",
                    SenderEntityTaxNumber = e.IncorporationNumber ?? "",
                };
            }
            else if (!string.IsNullOrWhiteSpace(e.ToAdi) || !string.IsNullOrWhiteSpace(e.ToSoyadi))
            {
                sender.Person = new GoamlJsonSenderPersonDto
                {
                    SenderFirstName = (e.ToAdi ?? "").Trim(),
                    SenderLastName = (e.ToSoyadi ?? "").Trim(),
                    SenderMiddleName = (e.ToAtasininAdi ?? "").Trim(),
                    SenderCitizenship = e.ToVetendasligi ?? "",
                    SenderIdSerial = e.RecIdSerial ?? "",
                    SenderIdNumber = e.RecIdNumber ?? "",
                    SenderPinCode = e.SnnTo ?? "",
                    SenderIdIssueDate = Tarix(e.ToSenedinVerilmeTarixi),
                    SenderIdExpiryDate = Tarix(e.ToSenedinEtibarliliqMuddeti),
                    SenderBirthDate = Tarix(e.ToDogulduguTarix),
                    SenderPhoneNumber = e.ToTelefonNomresi ?? "",
                    SenderTaxId = e.IncorporationNumberTo ?? "",
                    SenderIdType = e.RecIdType ?? "",
                };
            }

            var (currType, currAmount) = await ValyutaVeMeblegAsync(e, debetTerefMi: true, valyutaKoduKesi, ct);
            sender.Funds = new GoamlJsonSenderFundsDto
            {
                SenderCurrType = currType,
                SenderCurrAmount = currAmount,
                SenderAmountLocal = e.SummaVNacval ?? 0,
            };

            sender.Account = new GoamlJsonSenderAccountDto
            {
                SenderAccountNumber = (e.AccountTo ?? e.Account) ?? "",
                SenderAccountDublNumber = "", // huquqi_shexs.REGISTRATION_NUMBER — bax baş şərh
                SenderAccountCurrency = currType,
                SenderAccountFinInstCountry = e.FromCountry ?? "",
                SenderAccountFinInstBicId = (e.SwiftTo ?? e.Swift) ?? "",
                SenderAccountFinInstName = (e.InstitutionNameTo ?? e.InstitutionName) ?? "",
                SenderAccountFinInstBranch = (e.InstitutionNameTo ?? e.InstitutionName) ?? "",
                SenderAccountFinInstBic = (e.SwiftTo ?? e.Swift) ?? "",
            };

            return sender;
        }

        /// <summary>Receiver — Oracle sətir 1251-1403, "Sig" (Hesab imza hüququ olan
        /// şəxs) sahələrindən doldurulur (alan tərəf BİZİM bankın müştərisidir).</summary>
        private async Task<GoamlJsonReceiverDto> ReceiverQurAsync(GoamlEmeliyyat e, Dictionary<string, string?> valyutaKoduKesi, CancellationToken ct)
        {
            var receiver = new GoamlJsonReceiverDto { ReceiverCountryName = e.ToOlkeAdi ?? "" };

            if (!string.IsNullOrWhiteSpace(e.NameUr) && e.Tip != GoamlTipi.HuquqiOut)
            {
                receiver.Entity = new GoamlJsonReceiverEntityDto
                {
                    ReceiverEntityName = e.NameUr ?? "",
                    ReceiverEntityRegCountry = e.IncorporationCountryCode ?? "",
                    ReceiverEntityTaxNumber = e.IncorporationNumber ?? "",
                };
            }
            else if (!string.IsNullOrWhiteSpace(e.SigAdi) || !string.IsNullOrWhiteSpace(e.SigSoyadi))
            {
                receiver.Person = new GoamlJsonReceiverPersonDto
                {
                    ReceiverFirstName = (e.SigAdi ?? "").Trim(),
                    ReceiverLastName = (e.SigSoyadi ?? "").Trim(),
                    ReceiverMiddleName = (e.SigAtasininAdi ?? "").Trim(),
                    ReceiverCitizenship = e.SigVetendasligi ?? "",
                    ReceiverIdSerial = e.SenderIdSerial ?? "",
                    ReceiverIdNumber = e.SenderIdNumber ?? "",
                    ReceiverPinCode = e.Snn ?? "",
                    ReceiverIdIssueDate = Tarix(e.SigSenedinVerilmeTarixi),
                    ReceiverIdExpiryDate = Tarix(e.SigSenedinEtibarliliqMuddeti),
                    ReceiverPhoneNumber = e.SigTelefonNomresi ?? "",
                    ReceiverTaxId = e.IncorporationNumber ?? "",
                    ReceiverBirthDate = Tarix(e.SigDogulduguTarix),
                    ReceiverIdType = e.SenderIdType ?? "",
                };
            }

            var (currType, currAmount) = await ValyutaVeMeblegAsync(e, debetTerefMi: false, valyutaKoduKesi, ct);
            receiver.Funds = new GoamlJsonReceiverFundsDto
            {
                ReceiverCurrType = currType,
                ReceiverCurrAmount = currAmount,
                ReceiverAmountLocal = e.SummaVNacval ?? 0,
            };

            receiver.Account = new GoamlJsonReceiverAccountDto
            {
                ReceiverAccountNumber = (e.Account ?? e.AccountTo) ?? "",
                ReceiverAccountDublNumber = "", // huquqi_shexs.REGISTRATION_NUMBER — bax baş şərh
                ReceiverAccountCurrency = currType,
                // Oracle: V_ATTRIB.NAIMEN_UCHREJ (bizim ÖZ bankımızın adı, ATTRIB-dən) —
                // bu lookup port edilməyib, Oracle-un ÖZ ŞƏRHƏ ALINMIŞ ehtiyat düsturu
                // (`NVL(institution_name, institution_name_to)`) işlədilir.
                ReceiverAccountFinInstName = (e.InstitutionName ?? e.InstitutionNameTo) ?? "",
                ReceiverAccountFinInstCountry = e.IncorporationCountryCode ?? "",
                ReceiverAccountFinInstBicId = (e.Swift ?? e.SwiftTo) ?? "",
                ReceiverAccountFinInstBranch = (e.InstitutionName ?? e.InstitutionNameTo) ?? "",
                ReceiverAccountFinInstBic = (e.Swift ?? e.SwiftTo) ?? "",
            };

            return receiver;
        }

        /// <summary>Oracle sətir 1141-1185/1309-1351 — mübadilə-stolu prefiksləri
        /// (1005010060/1006010050) xüsusi, qalanı `func_get_Ekv(SUBSTR(hesab,6,2))`
        /// ilə təyin olunur. `func_get_Ekv` — elə bu SESSİYANIN ƏVVƏLİNDƏ (Proqres
        /// "Hesablar" ekranını araşdırarkən) tapdığımız STRUCTURE_LICSCH→KURVAL
        /// zəncirinin IKINCI yarısıdır: `SUBSTR(hesab,6,2)` artıq valyuta koduDUR
        /// (struktur-axtarışına ehtiyac yoxdur, mövqe SABİTDİR), ona görə YALNIZ
        /// `KURVAL`-a baxılır.</summary>
        private async Task<(string currType, decimal amount)> ValyutaVeMeblegAsync(
            GoamlEmeliyyat e, bool debetTerefMi, Dictionary<string, string?> valyutaKoduKesi, CancellationToken ct)
        {
            var prefiks = (e.Debet ?? "").Length >= 5 && (e.Kredit ?? "").Length >= 5
                ? e.Debet!.Substring(0, 5) + e.Kredit!.Substring(0, 5) : "";

            if (prefiks == "1005010060")
                return debetTerefMi ? ("AZN", e.SummaVNacval ?? 0) : (e.ForeignCurrencyCode ?? "AZN", e.SummaVInval ?? 0);
            if (prefiks == "1006010050")
                return debetTerefMi ? (e.ForeignCurrencyCodeFrom ?? "AZN", e.SummaVInval ?? 0) : ("AZN", e.SummaVNacval ?? 0);

            var hesab = debetTerefMi ? e.Debet : e.Account ?? e.Debet;
            var valyuta = await ValyutaKoduAsync(hesab, valyutaKoduKesi, ct) ?? "AZN";
            var meblegde = valyuta == "AZN" ? (e.SummaVNacval ?? 0) : (e.SummaVInval != 0 ? (e.SummaVInval ?? 0) : (e.SummaVNacval ?? 0));
            return (valyuta, meblegde);
        }

        /// <summary>`KURVAL.SOKNAMEVALUT` — hesab nömrəsinin 6-7-ci simvolundan
        /// (sabit mövqe, `func_get_Ekv`-in öz oxuduğu mövqe) oxunur, nəticə
        /// ixrac boyu keşlənir (eyni kod təkrar sorğulanmır).</summary>
        private async Task<string?> ValyutaKoduAsync(string? hesab, Dictionary<string, string?> kes, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(hesab) || hesab.Length < 7) return null;
            var kod = Tehlukesiz(hesab.Substring(5, 2));
            if (kes.TryGetValue(kod, out var keshlenen)) return keshlenen;
            try
            {
                var setirler = await _oracle.SelectAsync($"select soknamevalut from kurval where kodvaluti = '{kod}'", 1, ct);
                var netice = setirler.Count > 0 ? DVal(setirler[0], "SOKNAMEVALUT") as string : null;
                kes[kod] = netice;
                return netice;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GOAML Export — KURVAL sorğusu uğursuz oldu (kod={Kod})", kod);
                kes[kod] = null;
                return null;
            }
        }

        /// <summary>`odb.Malumatin_novu` — REPORT_CODE üzrə reportType/reportReason.</summary>
        private async Task<(int reportType, int reportReason)?> MalumatinNovuTapAsync(string reportCodeKod, CancellationToken ct)
        {
            try
            {
                var kod = Tehlukesiz(reportCodeKod);
                var setirler = await _oracle.SelectAsync(
                    $"select reporttype, reportreason from odb.malumatin_novu where isare = '{kod}'", 1, ct);
                if (setirler.Count == 0) return null;
                return ((int)(ToLong(DVal(setirler[0], "REPORTTYPE"))), (int)(ToLong(DVal(setirler[0], "REPORTREASON"))));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GOAML Export — malumatin_novu sorğusu uğursuz oldu (kod={Kod})", reportCodeKod);
                return null;
            }
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
