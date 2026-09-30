namespace FinNex.Domain.Entities.Risk
{
    /// <summary>
    /// MB Qərar 04/1 ("Banklarda əməliyyat risklərinin idarə edilməsi Qaydası"),
    /// Əlavə 2 — biznes sahələri bölgüsü. Rəsmi normativ siyahıdır, tez-tez
    /// dəyişmir — ona görə DB-dən idarə olunan parametr yox, sabit enum.
    /// </summary>
    public enum BiznesSahesi
    {
        KorporativMaliyye = 1,              // BS1
        AktivlerinIdareEdilmesi = 2,         // BS2
        IstehlakBankciligi = 3,              // BS3
        KommersiyaBankciligi = 4,            // BS4
        OdenisSistemleriVeHesablasmalar = 5, // BS5
        AgentlikXidmetleri = 6               // BS6
    }

    /// <summary>
    /// Əlavə 3 — risk hadisəsi kateqoriyası, 1-ci səviyyə (R1-R7).
    /// </summary>
    public enum RiskKateqoriyasi1
    {
        IsMunasibetleriVeIsYeriTehlukesizliyi = 1,       // R1
        MusterilerMehsullarVeBiznesMunasibetleri = 2,     // R2
        IcraCatdirilmaVeProseslerinIdareEdilmesi = 3,     // R3
        DaxiliDelelduzluq = 4,                            // R4
        KenarDelelduzluq = 5,                             // R5
        FealiyyetinPozulmasiVeSistemXetalari = 6,         // R6
        FizikiAktivlereDeyenZerer = 7                     // R7
    }

    /// <summary>
    /// Əlavə 3 — risk hadisəsi kateqoriyası, 2-ci səviyyə (R{1-7}.{alt}).
    /// Qiymətlər QƏSDƏN "onlar rəqəmi = 1-ci səviyyə, vahidlər = alt-nömrə"
    /// sxemi ilə seçilib (məs. 51 = R5.1) — <see cref="RiskKategoriyasiUst"/>
    /// bu sxemə görə valideyn kateqoriyanı hesablayır, ikinci enum saxlamağa
    /// ehtiyac qalmır.
    /// </summary>
    public enum RiskKateqoriyasi2
    {
        IscilerleMunasibetler = 11,                                    // R1.1
        TehlukesizMuhit = 12,                                          // R1.2
        QanunvericiliyeZiddDavranisVeAyriSeckilik = 13,                // R1.3

        UygunlugMelumatlarinAciqlanmasiVeEtibarliliqPozuntusu = 21,    // R2.1
        PeshekarligaUygunOlmayanBiznesVeBazarMunasibetleri = 22,       // R2.2
        MehsullardakiQusurlar = 23,                                    // R2.3
        MusteriSecimiSponsorlugVeKreditTelebininHecmi = 24,            // R2.4
        MeslehetXidmeti = 25,                                          // R2.5

        IsIcraVeDavamiyyet = 31,                                       // R3.1
        MonitorinqVeHesabat = 32,                                      // R3.2
        MusteriQebuluVeSenedlesdirme = 33,                             // R3.3
        MusteriHesablarininIdareEdilmesi = 34,                         // R3.4
        TicaretTerefdaslari = 35,                                      // R3.5
        SaticilarVeTeminatcilar = 36,                                  // R3.6

        SelahiyyetsizEmeliyyatlar = 41,                                // R4.1
        TalamaVeDelelduzluqDaxili = 42,                                // R4.2

        TalamaVeDelelduzluqKenar = 51,                                 // R5.1
        SistemTehlukesizliyi = 52,                                     // R5.2

        Sistemler = 61,                                                // R6.1

        TebiiFelaketlerVeDigerHadiseler = 71                           // R7.1
    }

    /// <summary>Qaydada rəqəmsal şkala göstərilmir — İstilik xəritəsi (Əlavə 1) üçün
    /// standart 3 pilləli qiymətləndirmə. Sonradan dəyişsə, yalnız bu enum-a
    /// dəyər əlavə etmək kifayətdir (BiznesSahesi/RiskKateqoriyası kimi qanuni
    /// mətn deyil, daxili qiymətləndirmə şkalasıdır).</summary>
    public enum RiskDerecesi
    {
        Asagi = 1,
        Orta = 2,
        Yuksek = 3
    }

    /// <summary>Əlavə 6 — zərərin təsir kateqoriyaları.</summary>
    public enum ZererTesirKateqoriyasi
    {
        HuquqiMesuliyyet = 1,
        NezaretTedbirleri = 2,
        AktivlereDeyenZerer = 3,
        DeymisZererinOdenilmesi = 4,
        ResurslarinItirilmesi = 5,
        AktivlerinDeyerdenDusmesi = 6,
        Diger = 7
    }

    /// <summary>Əlavə 4, sahə 26 — tədbirlər planının icra statusu (yalnız
    /// limitdən yuxarı zərərlərdə doldurulur).</summary>
    public enum TedbirIcraStatusu
    {
        Planlashdirilib = 1,
        DavamEdir = 2,
        Tamamlanib = 3
    }
}
