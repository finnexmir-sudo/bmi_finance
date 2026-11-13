namespace FinNex.Domain.Entities.Goaml
{
    /// <summary>Bizim öz iş axınımızın statusu — Oracle-da qarşılığı yoxdur.</summary>
    public enum GoamlEmeliyyatStatusu
    {
        /// <summary>Operator təsdiqləyib, hələ heç bir ixraca daxil edilməyib.</summary>
        Qaralama = 1,

        /// <summary>Export to JSON ilə faylda ixrac edilib (12.11.2026) — növbəti
        /// ixrac bu qeydi TƏKRAR GÖTÜRMƏYƏCƏK, "Qaralama" olanlar üzrə işləyir.</summary>
        IxracEdilib = 2,
    }

    /// <summary>
    /// "Tip" — göndərən/alan tərəfin növü (Fizik/Hüquqi) + istiqamət (IN/OUT),
    /// + Card-to-Card. Oracle `ODB.TIP_GOAML.TIP` (NUMBER) cədvəlindən,
    /// 06.11.2026-da `select * from tip_goaml` ilə TAM köçürülüb — enum
    /// dəyərləri Oracle-dakı RƏQƏMLƏRLƏ EYNİDİR (ixrac/müqayisə asan olsun deyə).
    /// </summary>
    public enum GoamlTipi
    {
        FizikOut = 1,
        FizikIn = 2,
        HuquqiOut = 3,
        HuquqiIn = 4,
        HToH = 5,
        HToHIn = 6,
        HToFIn = 7,
        FToF = 8,
        HToFOut = 11,
        FToHIn = 14,
        C2cOut = 17,
        FToHOut = 18,
        C2cIn = 19,
    }

    /// <summary>
    /// "Məlumatın növü" — Oracle `REPORT_CODE` sütunu (VARCHAR2). Azərbaycanın
    /// öz AML/CFT tənzimləyici hesabat kateqoriyalarıdır, 06.11.2026-da AYSEL-in
    /// canlı dropdown-undan (FIL_REPORT_CODE item-i) TAM köçürülüb.
    /// `GoamlHesabatNovuExtensions.Kod()` ilə əsl Oracle kodu (məs. "STR") alınır.
    /// </summary>
    public enum GoamlHesabatNovu
    {
        /// <summary>AAR — Anonim əməliyyatlara dair məlumat.</summary>
        Aar = 1,
        /// <summary>AIF — Sorğular əsasında əlavə məlumat.</summary>
        Aif = 2,
        /// <summary>BLR — Qara siyahıda olan ölkələr üzrə əməliyyatlara dair məlumat.</summary>
        Blr = 3,
        /// <summary>CTR — Cari əməliyyatlara dair məlumat (101).</summary>
        Ctr = 4,
        /// <summary>ETR — elektron köçürülməsi əməliyyatlarına dair məlumat.</summary>
        Etr = 5,
        /// <summary>FIR — Saxta eyniləşdirmə halları üzrə əməliyyatlara dair məlumat.</summary>
        Fir = 6,
        /// <summary>FMR — Üçüncü şəxslər vasitəsilə əməliyyatlara dair məlumat.</summary>
        Fmr = 7,
        /// <summary>PEP — "Siyasi xadimlərin" əməliyyatlarına dair məlumat.</summary>
        Pep = 8,
        /// <summary>QHT — Qeyri Hökumət Təşkilatı.</summary>
        Qht = 9,
        /// <summary>STR — Şübhəli əməliyyatlara dair məlumat (201). Defolt/əsas dəyər.</summary>
        Str = 10,
        /// <summary>TF — Mənşəyi Ermənistan.</summary>
        Tf = 11,
        /// <summary>TFR — Məbləğindən asılı olmayaraq pul vəsaitləri (TM).</summary>
        Tfr = 12,
        /// <summary>TPR1 — ƏDV (30 000 AZN-dən artıq nağd məxaric əməliyyatları).</summary>
        Tpr1 = 13,
        /// <summary>TPR2 — təsviri AYSEL ekranında kəsilib görünüb, dəqiqləşdirilməyib.</summary>
        Tpr2 = 14,
        /// <summary>TPR3 — Digər vergi ödəyiciləri üzrə 15 000 AZN-dən artıq nağd məxaric əməliyyatları.</summary>
        Tpr3 = 15,
        /// <summary>UNR — "BMT siyahıları" üzrə əməliyyatlara dair məlumat.</summary>
        Unr = 16,
    }

    public static class GoamlTipiExtensions
    {
        /// <summary>AYSEL-in canlı "Tip" kombobox-undan (06.11.2026, ekran-görüntüsü)
        /// köçürülmüş insan-oxunan etiket.</summary>
        public static string Etiket(this GoamlTipi t) => t switch
        {
            GoamlTipi.FizikOut => "Fiziki (OUT)",
            GoamlTipi.FizikIn => "Fiziki (IN)",
            GoamlTipi.HuquqiOut => "Hüquqi (OUT)",
            GoamlTipi.HuquqiIn => "Hüquqi (IN)",
            GoamlTipi.HToH => "H TO H",
            GoamlTipi.HToHIn => "H TO H in",
            GoamlTipi.HToFIn => "H to F in",
            GoamlTipi.FToF => "F to F",
            GoamlTipi.HToFOut => "H to F out",
            GoamlTipi.FToHIn => "F to H in",
            GoamlTipi.C2cOut => "Card to Card (OUT)",
            GoamlTipi.FToHOut => "F to H out",
            GoamlTipi.C2cIn => "Card to Card (IN)",
            _ => t.ToString(),
        };

        /// <summary>Tip-in İSTİQAMƏTİNDƏN (OUT/IN) bizim bankın hesabının Debet,
        /// yoxsa Kredit tərəfdə olduğunu söyləyir — CON/SIG (Nümayəndə/İmza
        /// hüququ) üçün "hansı hesabın sahibinə baxaq" sualının cavabı
        /// (12.11.2026, real hadisə: Debet=35025, Kredit=40140, köhnə kod
        /// HƏMİŞƏ Debet-ə baxırdı, Kredit-dəki əsl Rəhbəri heç yoxlamırdı).
        /// true = Debet bizimdir (OUT — biz göndəririk), false = Kredit
        /// bizimdir (IN — biz alırıq), null = Tip İSTİQAMƏT daşımır (HToH/
        /// FToF — hər iki tərəf bizim ola bilər, əməliyyatçı özü seçməlidir).</summary>
        public static bool? BizimTerefDebetdir(this GoamlTipi t) => t switch
        {
            GoamlTipi.FizikOut => true,
            GoamlTipi.HuquqiOut => true,
            GoamlTipi.HToFOut => true,
            GoamlTipi.C2cOut => true,
            GoamlTipi.FToHOut => true,
            GoamlTipi.FizikIn => false,
            GoamlTipi.HuquqiIn => false,
            GoamlTipi.HToHIn => false,
            GoamlTipi.HToFIn => false,
            GoamlTipi.FToHIn => false,
            GoamlTipi.C2cIn => false,
            GoamlTipi.HToH => null,
            GoamlTipi.FToF => null,
            _ => null,
        };
    }

    public static class GoamlHesabatNovuExtensions
    {
        /// <summary>Oracle `REPORT_CODE` sütununa yazılan əsl kod (məs. "STR").</summary>
        public static string Kod(this GoamlHesabatNovu n) => n switch
        {
            GoamlHesabatNovu.Aar => "AAR",
            GoamlHesabatNovu.Aif => "AIF",
            GoamlHesabatNovu.Blr => "BLR",
            GoamlHesabatNovu.Ctr => "CTR",
            GoamlHesabatNovu.Etr => "ETR",
            GoamlHesabatNovu.Fir => "FIR",
            GoamlHesabatNovu.Fmr => "FMR",
            GoamlHesabatNovu.Pep => "PEP",
            GoamlHesabatNovu.Qht => "QHT",
            GoamlHesabatNovu.Str => "STR",
            GoamlHesabatNovu.Tf => "TF",
            GoamlHesabatNovu.Tfr => "TFR",
            GoamlHesabatNovu.Tpr1 => "TPR1",
            GoamlHesabatNovu.Tpr2 => "TPR2",
            GoamlHesabatNovu.Tpr3 => "TPR3",
            GoamlHesabatNovu.Unr => "UNR",
            _ => n.ToString().ToUpperInvariant(),
        };

        /// <summary>
        /// 13.11.2026 — İstifadəçi: "bizde 6 fayl var ve bizde yalniz secimde
        /// onlari gosterek,o biriler islemir". Bu, hansı REPORT_CODE-un Oracle
        /// `malumatin_novu` vasitəsilə hansı real goAML `reportType`-ə
        /// tərcümə olunduğu TƏSDİQLƏNİB VƏ həmin `reportType`-in sxemi artıq
        /// bizdə VAR (CTR=1, ETR=2, NGR=3, PEP=4, HRZ=8, BCR=9 — 6 real
        /// nümunə ilə yoxlanılıb, bax CLAUDE.md "Fayl Fərqliliyi" bölməsi).
        /// SAR(6)/TFR(7)-nin real nümunəsi HƏLƏ YOXDUR (nə bizdə, nə
        /// istifadəçidə) — qalan kodlar da (ehtimal onlardan birinə düşür,
        /// amma TƏSDİQLƏNMƏYİB) "Məlumatın növü" seçimində GÖSTƏRİLMİR.
        ///
        /// Mənbə (hər biri üçün AYRI əsas):
        /// - Ctr/Etr/Pep — adın ÖZÜ real `reportType` adı ilə EYNİDİR;
        /// - Blr — CANLI export testi ilə TƏSDİQLƏNİB (nəticə reportType:8=HRZ
        ///   qayıtdı — "BLR" AYSEL-in daxili termini idi, real portalda yoxdur);
        /// - Qht — NGR-in (reportType=3) öz "organizationType" seçiminin
        ///   "QHT" dəyəri ilə adı üst-üstə düşür (bax "QHT müəmması" bölməsi).
        ///
        /// Yeni bir kodun real sxemi tapılanda (SAR/TFR nümunəsi gələndə, ya
        /// da qalan kodların hansı reportType-ə getdiyi Oracle-da canlı
        /// yoxlananda) BURAYA əlavə et — başqa heç nə dəyişmə, HƏR İKİ dropdown
        /// (Tesdiq.cshtml + Export.cshtml) bu metoda görə avtomatik yenilənir.
        /// </summary>
        public static bool SxemHazirdir(this GoamlHesabatNovu n) => n is
            GoamlHesabatNovu.Ctr or GoamlHesabatNovu.Etr or GoamlHesabatNovu.Blr
            or GoamlHesabatNovu.Pep or GoamlHesabatNovu.Qht;

        /// <summary>İnsan-oxunan qısa izah — "Tesdiq" formasındakı dropdown üçün
        /// (eyni mətn yuxarıdakı XML şərhlərindədir, təkrar yazılmasın deyə burda
        /// TƏK yerdə saxlanılır).</summary>
        public static string Aciqlama(this GoamlHesabatNovu n) => n switch
        {
            GoamlHesabatNovu.Aar => "Anonim əməliyyatlara dair məlumat",
            GoamlHesabatNovu.Aif => "Sorğular əsasında əlavə məlumat",
            GoamlHesabatNovu.Blr => "Qara siyahıda olan ölkələr üzrə əməliyyatlara dair məlumat",
            GoamlHesabatNovu.Ctr => "Cari əməliyyatlara dair məlumat (101)",
            GoamlHesabatNovu.Etr => "Elektron köçürülməsi əməliyyatlarına dair məlumat",
            GoamlHesabatNovu.Fir => "Saxta eyniləşdirmə halları üzrə əməliyyatlara dair məlumat",
            GoamlHesabatNovu.Fmr => "Üçüncü şəxslər vasitəsilə əməliyyatlara dair məlumat",
            GoamlHesabatNovu.Pep => "Siyasi xadimlərin əməliyyatlarına dair məlumat",
            GoamlHesabatNovu.Qht => "Qeyri Hökumət Təşkilatı",
            GoamlHesabatNovu.Str => "Şübhəli əməliyyatlara dair məlumat (201)",
            GoamlHesabatNovu.Tf => "Mənşəyi Ermənistan",
            GoamlHesabatNovu.Tfr => "Məbləğindən asılı olmayaraq pul vəsaitləri (TM)",
            GoamlHesabatNovu.Tpr1 => "ƏDV (30 000 AZN-dən artıq nağd məxaric əməliyyatları)",
            GoamlHesabatNovu.Tpr2 => "Təsviri dəqiqləşdirilməyib",
            GoamlHesabatNovu.Tpr3 => "Digər vergi ödəyiciləri üzrə 15 000 AZN-dən artıq nağd məxaric əməliyyatları",
            GoamlHesabatNovu.Unr => "BMT siyahıları üzrə əməliyyatlara dair məlumat",
            _ => "",
        };
    }
}
