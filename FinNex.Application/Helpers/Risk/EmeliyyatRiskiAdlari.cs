using FinNex.Domain.Entities.Risk;

namespace FinNex.Application.Helpers.Risk
{
    /// <summary>
    /// MB Qərar 04/1-in Əlavə 2/3/6 enum-larının Azərbaycan dilində göstərmə
    /// adları + "Qeydiyyat kodu" hesablaması (Əlavə 4, sahə 2 — nümunə
    /// "BS5R5.1"). Servis VƏ controller EYNİ metodları çağırır ki, kod bir
    /// yerdə qalsın (BiznesSahesi/RiskKateqoriyasi görüntü adı iki yerdə
    /// yazılsa, biri gec-tez köhnə qalar — layihədə dəfələrlə görülmüş tələ).
    /// </summary>
    public static class EmeliyyatRiskiAdlari
    {
        // ── Rəsmi kodlar (Əlavə 2/3-ün "Təsnifat kodu" sütunu) ──────────
        // İstifadəçi qərarı (01.10.2026): dropdown/siyahılarda ad TƏK BAŞINA
        // yox, kodla birlikdə göstərilsin ("BS1 — Korporativ maliyyə") ki,
        // Risk/AML işçisi seçimi rəsmi qərarın öz koduna görə yoxlaya bilsin.
        public static string BiznesSahesiKod(BiznesSahesi s) => $"BS{(int)s}";
        public static string RiskKateqoriyasi1Kod(RiskKateqoriyasi1 k) => $"R{(int)k}";
        public static string RiskKateqoriyasi2Kod(RiskKateqoriyasi2 k) => $"R{(int)k / 10}.{(int)k % 10}";

        public static string BiznesSahesiAdiKodlu(BiznesSahesi s) => $"{BiznesSahesiKod(s)} — {BiznesSahesiAdi(s)}";
        public static string RiskKateqoriyasi1AdiKodlu(RiskKateqoriyasi1 k) => $"{RiskKateqoriyasi1Kod(k)} — {RiskKateqoriyasi1Adi(k)}";
        public static string RiskKateqoriyasi2AdiKodlu(RiskKateqoriyasi2 k) => $"{RiskKateqoriyasi2Kod(k)} — {RiskKateqoriyasi2Adi(k)}";

        public static string BiznesSahesiAdi(BiznesSahesi s) => s switch
        {
            BiznesSahesi.KorporativMaliyye => "Korporativ maliyyə",
            BiznesSahesi.AktivlerinIdareEdilmesi => "Aktivlərin idarə edilməsi",
            BiznesSahesi.IstehlakBankciligi => "İstehlak bankçılığı",
            BiznesSahesi.KommersiyaBankciligi => "Kommersiya bankçılığı",
            BiznesSahesi.OdenisSistemleriVeHesablasmalar => "Ödəniş sistemləri və hesablaşmalar",
            BiznesSahesi.AgentlikXidmetleri => "Agentlik xidmətləri",
            _ => s.ToString()
        };

        public static string RiskKateqoriyasi1Adi(RiskKateqoriyasi1 k) => k switch
        {
            RiskKateqoriyasi1.IsMunasibetleriVeIsYeriTehlukesizliyi => "İş münasibətləri və iş yerinin təhlükəsizliyi",
            RiskKateqoriyasi1.MusterilerMehsullarVeBiznesMunasibetleri => "Müştərilər, məhsullar və biznes münasibətləri",
            RiskKateqoriyasi1.IcraCatdirilmaVeProseslerinIdareEdilmesi => "İcra, çatdırılma və proseslərin idarə edilməsi",
            RiskKateqoriyasi1.DaxiliDelelduzluq => "Daxili dələduzluq",
            RiskKateqoriyasi1.KenarDelelduzluq => "Kənar dələduzluq",
            RiskKateqoriyasi1.FealiyyetinPozulmasiVeSistemXetalari => "Fəaliyyətin pozulması və sistem xətaları",
            RiskKateqoriyasi1.FizikiAktivlereDeyenZerer => "Fiziki aktivlərə dəyən zərər",
            _ => k.ToString()
        };

        // ── İzah / Anlayışı mətnləri (Əlavə 2-nin "İzah" sütunu, Əlavə 3-ün
        // "Anlayışı" sütunu) — istifadəçi qərarı (01.10.2026): formada
        // DAİMİ görünmür, yer tutmasın deyə "ⓘ" düyməsi ilə klikləndə açılır.
        // Mətnlər qaydanın öz sözləridir, dəyişdirilməyib.
        public static string BiznesSahesiIzahi(BiznesSahesi s) => s switch
        {
            BiznesSahesi.KorporativMaliyye =>
                "Biznesə uzunmüddətli və strateji fəaliyyət, həmçinin likvidliyinin artırılması " +
                "istiqamətində maliyyə məsləhətlərinin verilməsi, habelə \"Qiymətli kağızlar bazarı " +
                "haqqında\" Azərbaycan Respublikasının Qanununda nəzərdə tutulan həcmdə və qaydada " +
                "qiymətli kağızlar və törəmə maliyyə alətləri ilə investisiya xidmətlərinin " +
                "(əməliyyatlarının) həyata keçirilməsi xidmətlərinin göstərilməsi",
            BiznesSahesi.AktivlerinIdareEdilmesi =>
                "Fiziki şəxslərə \"Qiymətli kağızlar bazarı haqqında\" Azərbaycan Respublikasının " +
                "Qanununda nəzərdə tutulan həcmdə və qaydada qiymətli kağızlar və törəmə maliyyə " +
                "alətləri ilə investisiya xidmətlərinin (əməliyyatlarının) həyata keçirilməsi " +
                "xidmətlərinin göstərilməsi",
            BiznesSahesi.IstehlakBankciligi =>
                "Sahibkarlıq və ya peşə fəaliyyəti ilə bağlı olmayan məqsədlər üçün fiziki şəxslərə " +
                "kreditlərin, o cümlədən daşınmaz əmlak kreditlərinin verilməsi, fiziki şəxslərdən " +
                "əmanətlərin cəlb edilməsi, ödəniş alətlərinin emissiyası və digər xidmətlərin göstərilməsi",
            BiznesSahesi.KommersiyaBankciligi =>
                "Biznesin maliyyələşdirilməsi, o cümlədən layihə maliyyələşməsi, ixracın maliyyələşməsi, " +
                "faktorinq, lizinq, qarantiya və digər biznes sahələrinə aid olmayan xidmətlərin göstərilməsi",
            BiznesSahesi.OdenisSistemleriVeHesablasmalar =>
                "Ödənişlər, pul köçürmələri, klirinq və hesablaşmalar",
            BiznesSahesi.AgentlikXidmetleri =>
                "Maliyyə agenti xidmətləri",
            _ => ""
        };

        public static string RiskKateqoriyasi1Izahi(RiskKateqoriyasi1 k) => k switch
        {
            RiskKateqoriyasi1.IsMunasibetleriVeIsYeriTehlukesizliyi =>
                "Əmək, sağlamlıq və ya təhlükəsizliklə bağlı qanunlara və (və ya) qaydalara, habelə " +
                "müqavilələrə zidd hərəkətlərdən, sağlamlığa dəyən zərər üzrə müavinətin ödənilməsindən " +
                "və ya işçilərə qanunvericiliyəzidd davranış və ayrı-seçkilik hərəkətlərindən yaranan zərər.",
            RiskKateqoriyasi1.MusterilerMehsullarVeBiznesMunasibetleri =>
                "Müəyyən müştərilər qarşısında götürülmüş peşəkar öhdəliklərin (etibarlılıq və uyğunluq " +
                "tələbləri daxil olmaqla) qeyri-ixtiyari və ya səhlənkarlıq nəticəsində yerinə " +
                "yetirilməməsindən, habelə bank məhsulunun təbiətindən və tərtibatından yaranan zərər.",
            RiskKateqoriyasi1.IcraCatdirilmaVeProseslerinIdareEdilmesi =>
                "Əməliyyatların səhv həyata keçirilməsindən və ya proseslərin idarə edilməsinin " +
                "qeyri-adekvatlığından və ya ticarət tərəfdaşları və satıcılarla münasibətlərdən yaranan zərər.",
            RiskKateqoriyasi1.DaxiliDelelduzluq =>
                "Bankın azı bir əməkdaşının iştirak etdiyi dələduzluq, bank əmlakının mənimsənilməsi " +
                "və ya qanunverciliyin, habelə bankın daxili siyasət və qaydalarının pozulması ilə " +
                "yaranan zərər.",
            RiskKateqoriyasi1.KenarDelelduzluq =>
                "Kənar şəxs tərəfindən törədilmiş dələduzluq, bank əmlakının mənimsənilməsi və ya " +
                "digər hüquqazidd əməl nəticəsində yaranan zərər.",
            RiskKateqoriyasi1.FealiyyetinPozulmasiVeSistemXetalari =>
                "Fəaliyyətin pozulması və ya sistemdəki xətalar nəticəsində yaranan zərər.",
            RiskKateqoriyasi1.FizikiAktivlereDeyenZerer =>
                "Təbii fəlakət və ya digər hadisələr nəticəsində fiziki aktivlərin itirilməsi və ya " +
                "zədələnməsi nəticəsində yaranan zərər.",
            _ => ""
        };

        public static string RiskKateqoriyasi2Adi(RiskKateqoriyasi2 k) => k switch
        {
            RiskKateqoriyasi2.IscilerleMunasibetler => "İşçilərlə münasibətlər",
            RiskKateqoriyasi2.TehlukesizMuhit => "Təhlükəsiz mühit",
            RiskKateqoriyasi2.QanunvericiliyeZiddDavranisVeAyriSeckilik => "Qanunvericiliyə zidd davranış və ayrı-seçkilik",
            RiskKateqoriyasi2.UygunlugMelumatlarinAciqlanmasiVeEtibarliliqPozuntusu => "Uyğunluq, məlumatların açıqlanması və etibarlılıq pozuntusu",
            RiskKateqoriyasi2.PeshekarligaUygunOlmayanBiznesVeBazarMunasibetleri => "Peşəkarlığa uyğun olmayan biznes/bazar münasibətləri",
            RiskKateqoriyasi2.MehsullardakiQusurlar => "Məhsullardakı qüsurlar",
            RiskKateqoriyasi2.MusteriSecimiSponsorlugVeKreditTelebininHecmi => "Müştəri seçimi, sponsorluq və kredit tələbinin həcmi",
            RiskKateqoriyasi2.MeslehetXidmeti => "Məsləhət xidməti",
            RiskKateqoriyasi2.IsIcraVeDavamiyyet => "İş, icra və davamiyyət",
            RiskKateqoriyasi2.MonitorinqVeHesabat => "Monitorinq və hesabat",
            RiskKateqoriyasi2.MusteriQebuluVeSenedlesdirme => "Müştəri qəbulu və sənədləşdirmə",
            RiskKateqoriyasi2.MusteriHesablarininIdareEdilmesi => "Müştəri hesablarının idarə edilməsi",
            RiskKateqoriyasi2.TicaretTerefdaslari => "Ticarət tərəfdaşları",
            RiskKateqoriyasi2.SaticilarVeTeminatcilar => "Satıcılar və təminatçılar",
            RiskKateqoriyasi2.SelahiyyetsizEmeliyyatlar => "Səlahiyyət olmadan aparılan əməliyyatlar",
            RiskKateqoriyasi2.TalamaVeDelelduzluqDaxili => "Talama və dələduzluq",
            RiskKateqoriyasi2.TalamaVeDelelduzluqKenar => "Talama və dələduzluq",
            RiskKateqoriyasi2.SistemTehlukesizliyi => "Sistem təhlükəsizliyi",
            RiskKateqoriyasi2.Sistemler => "Sistemlər",
            RiskKateqoriyasi2.TebiiFelaketlerVeDigerHadiseler => "Təbii fəlakətlər və digər hadisələr",
            _ => k.ToString()
        };

        public static string ZererTesirKateqoriyasiAdi(ZererTesirKateqoriyasi z) => z switch
        {
            ZererTesirKateqoriyasi.HuquqiMesuliyyet => "Hüquqi məsuliyyət",
            ZererTesirKateqoriyasi.NezaretTedbirleri => "Nəzarət tədbirləri",
            ZererTesirKateqoriyasi.AktivlereDeyenZerer => "Aktivlərə dəyən zərər",
            ZererTesirKateqoriyasi.DeymisZererinOdenilmesi => "Dəymiş zərərin ödənilməsi",
            ZererTesirKateqoriyasi.ResurslarinItirilmesi => "Resursların itirilməsi",
            ZererTesirKateqoriyasi.AktivlerinDeyerdenDusmesi => "Aktivlərin dəyərdən düşməsi",
            ZererTesirKateqoriyasi.Diger => "Əlavə mülahizələr",
            _ => z.ToString()
        };

        public static string RiskDerecesiAdi(RiskDerecesi d) => d switch
        {
            RiskDerecesi.Asagi => "Aşağı",
            RiskDerecesi.Orta => "Orta",
            RiskDerecesi.Yuksek => "Yüksək",
            _ => d.ToString()
        };

        public static string TedbirIcraStatusuAdi(TedbirIcraStatusu s) => s switch
        {
            TedbirIcraStatusu.Planlashdirilib => "Planlaşdırılıb",
            TedbirIcraStatusu.DavamEdir => "Davam edir",
            TedbirIcraStatusu.Tamamlanib => "Tamamlanıb",
            _ => s.ToString()
        };

        /// <summary>Əlavə 4, sahə 2 — "BS5R5.1" formatında qeydiyyat kodu.
        /// 2-ci səviyyə enum dəyərinin onlar rəqəmi 1-ci səviyyəni göstərir
        /// (bax <see cref="RiskKateqoriyasi2"/> qeydi), vahidlər rəqəmi
        /// alt-nömrədir.</summary>
        public static string QeydiyyatKoduHesabla(BiznesSahesi biznes, RiskKateqoriyasi2 kat2)
        {
            var alt = (int)kat2 % 10;
            var ust = (int)kat2 / 10;
            return $"BS{(int)biznes}R{ust}.{alt}";
        }
    }
}
