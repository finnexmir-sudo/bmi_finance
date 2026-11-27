using FinNex.Application.DTOs.Dsmf;
using FinNex.Application.DTOs.HR.Maas;
using FinNex.Application.Interfaces.Dsmf;
using FinNex.Application.Interfaces.HR;
using FinNex.Application.Interfaces.Maas_If;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FinNex.Application.Services.Dsmf;

public class DsmfHesabatService : IDsmfHesabatService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMaasHesablamaService _maasHesablama;
    private readonly IIsciHYSDondurmaService _hysDondurma;

    private static readonly string[] AyAdlari =
    {
        "", "Yanvar", "Fevral", "Mart", "Aprel", "May", "İyun",
        "İyul", "Avqust", "Sentyabr", "Oktyabr", "Noyabr", "Dekabr"
    };

    public DsmfHesabatService(
        IUnitOfWork unitOfWork,
        IMaasHesablamaService maasHesablama,
        IIsciHYSDondurmaService hysDondurma)
    {
        _unitOfWork = unitOfWork;
        _maasHesablama = maasHesablama;
        _hysDondurma = hysDondurma;
    }

    public async Task<DsmfHissesi3Dto> Hissesi3HazirlaAsync(int il, int rub)
    {
        rub = Math.Clamp(rub, 1, 4);
        var ay1 = (rub - 1) * 3 + 1;
        var ay2 = ay1 + 1;
        var ay3 = ay1 + 2;

        var dto = new DsmfHissesi3Dto
        {
            Il = il,
            Rub = rub,
            Ay1 = ay1,
            Ay2 = ay2,
            Ay3 = ay3,
            Ay1Adi = AyAdlari[ay1],
            Ay2Adi = AyAdlari[ay2],
            Ay3Adi = AyAdlari[ay3],
            AvtomatikAskarlanmayanSebebler = DsmfIslemeyenGunSebebi.AvtomatikAskarlanmayanKodlar
                .Select(k => DsmfIslemeyenGunSebebi.Metinler[k]).ToList(),
        };

        // Gələcək rüb — bax RubGelecekdedir şərhi. Burada atlanmasaq, "aktiv"
        // (işdən çıxmamış) işçilər üçün hələ baş verməmiş Mezuniyyet/Xəstəlik
        // axtarışı aparılar (adətən boş qalır), amma garantisi yoxdur —
        // mühasib gələcəyə planlaşdırılmış məzuniyyət yaza bilər və bu, rübün
        // hələ "yaşanmadığı" faktını gizlədər.
        if (RubGelecekdedir(il, ay1))
        {
            dto.GelecekRubdur = true;
            return dto;
        }

        var iscilər = await IscilərRubdeAktivAsync(il, ay1, ay3);

        var ayLar = new[] { ay1, ay2, ay3 };
        var setirler = new List<DsmfHissesi3SetirDto>();

        foreach (var isci in iscilər)
        {
            // sebebKodu -> [ay1Gun, ay2Gun, ay3Gun]
            var sebebGunleri = new Dictionary<int, int[]>();
            void Elave(int sebebKodu, int ayIndex, int gun)
            {
                if (gun <= 0) return;
                if (!sebebGunleri.TryGetValue(sebebKodu, out var arr))
                {
                    arr = new int[3];
                    sebebGunleri[sebebKodu] = arr;
                }
                arr[ayIndex] += gun;
            }

            for (int i = 0; i < 3; i++)
            {
                var ay = ayLar[i];
                var ayBaslangic = new DateTime(il, ay, 1);
                var ayBitis = ayBaslangic.AddMonths(1).AddDays(-1);

                var aktivBaslangic = isci.IsheQebulTarixi < ayBaslangic ? ayBaslangic : isci.IsheQebulTarixi;
                var aktivBitis = (isci.IsdenAyrilmaTarixi.HasValue && isci.IsdenAyrilmaTarixi.Value < ayBitis)
                    ? isci.IsdenAyrilmaTarixi.Value
                    : ayBitis;

                if (aktivBaslangic > aktivBitis)
                {
                    // Bu ay üzrə işəgötürənlə heç bir müqavilə kəsişməsi yoxdur —
                    // kod 9: tam ayın həqiqi iş günü.
                    var tamAyGun = await HaqiqiIsGunuSayAsync(
                        new[] { (ayBaslangic, ayBitis) }, il, ay);
                    Elave(9, i, tamAyGun);
                    continue;
                }

                // Kod 8 — ayın ortasında işə qəbul (1-ci gün deyil).
                if (isci.IsheQebulTarixi > ayBaslangic && isci.IsheQebulTarixi <= ayBitis)
                {
                    var bitenGun = isci.IsheQebulTarixi.AddDays(-1);
                    if (bitenGun >= ayBaslangic)
                    {
                        var gun = await HaqiqiIsGunuSayAsync(new[] { (ayBaslangic, bitenGun) }, il, ay);
                        Elave(8, i, gun);
                    }
                }

                // Kod 6 — ay ərzində əmək müqaviləsinə xitam (son gün deyil).
                if (isci.IsdenAyrilmaTarixi.HasValue
                    && isci.IsdenAyrilmaTarixi.Value >= ayBaslangic
                    && isci.IsdenAyrilmaTarixi.Value < ayBitis)
                {
                    var baslayanGun = isci.IsdenAyrilmaTarixi.Value.AddDays(1);
                    var gun = await HaqiqiIsGunuSayAsync(new[] { (baslayanGun, ayBitis) }, il, ay);
                    Elave(6, i, gun);
                }

                // ⚠️ Dövlət vəzifəsi MÖVCUD ƏMƏK MƏZUNİYYƏTİNİ ƏVƏZ EDİR (bax
                // TabelService-dəki eyni şərh) — korreksiya yalnız Davamiyyət
                // statusunu üstələyir, altdakı İllik qeydi silinmir/qısalmır,
                // ona görə eyni gün HƏM Kod 1 (İllik), HƏM Kod 11 (dövlət vəzifəsi)
                // kimi İKİ DƏFƏ sayıla bilərdi. İstifadəçi (17.11.2026):
                // "dövlət vəzifəsi olana da məzuniyyət yazmışıq... bu səhvdir,
                // iş günü kimi düşməlidir." Kod 11 TAMAMİLƏ ÇIXARILDI — dövlət
                // vəzifəsi günləri heç bir "işləməyib" səbəbi kimi sayılmır,
                // ADİ İŞ GÜNÜ kimi qalır (Tabel-dəki fix ilə EYNİ qayda). Kod 1
                // isə bu günləri İSTİSNA edir ki, overlap halında ikiqat
                // çıxılmasın.
                var dovletVezifesiGunleri = await DovletVezifesiGunSetiAsync(isci.Id, il, ay);

                // Kod 1 — ödənişli əsas məzuniyyət (Illik), dövlət vəzifəsi
                // günləri istisna edilərək.
                Elave(1, i, await NovUzreHaqiqiIsGunuAsync(isci.Id, MezuniyyetNovu.Illik, il, ay, dovletVezifesiGunleri));

                // Kod 3 — ödənişsiz məzuniyyət (Ə.M. 129). Mövcud, sınanmış servis metodu.
                Elave(3, i, await _maasHesablama.OzHesabinaIsGunuSayAsync(isci.Id, il, ay));

                // Kod 5 — xəstələnmə. Mövcud, sınanmış servis metodu (26.10.2026-da
                // düzəldilmiş "həqiqi iş günü" versiyası).
                Elave(5, i, await _maasHesablama.XestelikHaqiqiIsGunuSayAsync(isci.Id, il, ay));
            }

            if (sebebGunleri.Count == 0) continue;

            var adSoyadAta = $"{isci.Soyad} {isci.Ad} {isci.AtaAdi}".Trim();
            foreach (var (sebebKodu, gunler) in sebebGunleri.OrderBy(x => x.Key))
            {
                setirler.Add(new DsmfHissesi3SetirDto
                {
                    IsciId = isci.Id,
                    AdSoyadAta = adSoyadAta,
                    Fin = isci.FIN,
                    SebebKodu = sebebKodu,
                    SebebMetni = DsmfIslemeyenGunSebebi.Metinler[sebebKodu],
                    Ay1Gun = gunler[0],
                    Ay2Gun = gunler[1],
                    Ay3Gun = gunler[2],
                });
            }
        }

        for (int i = 0; i < setirler.Count; i++)
            setirler[i].SiraNo = i + 1;

        dto.Setirler = setirler;
        return dto;
    }

    // ── Mezuniyyet.Nov üzrə REAL (həftəsonu/bayram çıxılmış) iş günü — ayrıca,
    // MaasHesablamaService-dən MÜSTƏQİL kopyadır. Payroll-un öz "əsl hesablama"
    // metodlarına toxunmamaq üçün (bax CLAUDE.md "İşlək funksiyanı silmək/dəyişmək
    // yalnız açıq icazə ilə") bu hesabat üçün ayrıca, oxu-yalnız bir nüsxə yazılıb —
    // MezuniyyetGunleriniSayGenisAsync işlədilmədi, çünki o, Illik+Ezamiyyət-i
    // BİRLİKDƏ qaytarır (Ezamiyyət DSMF-nin 12 səbəbindən biri DEYİL — işçi
    // ezamiyyətdə işləyir, "işləmədiyi gün" sayılmır).
    // xaricEt — bu günlər (varsa) nəticədən ÇIXARILIR. Dövlət vəzifəsinin
    // İllik-i əvəz etdiyi overlap günləri üçün (bax Hissesi3HazirlaAsync).
    private async Task<int> NovUzreHaqiqiIsGunuAsync(
        int isciId, MezuniyyetNovu nov, int il, int ay, HashSet<DateTime>? xaricEt = null)
    {
        var ayBaslangic = new DateTime(il, ay, 1);
        var ayBitis = ayBaslangic.AddMonths(1).AddDays(-1);

        var qeydler = await _unitOfWork.Repository<Mezuniyyet>()
            .Query()
            .AsNoTracking()
            .Where(x => x.IsciId == isciId
                     && x.Status == MezuniyyetStatus.Tesdiqlenib
                     && x.Nov == nov
                     && !x.JetonIleOdendi
                     && (x.IsGunlerininSayiManual ?? x.IsGunlerininSayi) > 0
                     && x.BaslamaTarixi <= ayBitis
                     && x.BitmeTarixi >= ayBaslangic)
            .Select(x => new { x.BaslamaTarixi, x.BitmeTarixi })
            .ToListAsync();

        if (qeydler.Count == 0) return 0;

        return await HaqiqiIsGunuSayAsync(
            qeydler.Select(x => (x.BaslamaTarixi, x.BitmeTarixi)), il, ay, xaricEt);
    }

    // Verilmiş tarix aralıqlarının bir aya düşən REAL iş günü sayı (BayramGunu +
    // həftəsonu qaydaları) — HashSet ilə üst-üstə düşən günlər ikiqat sayılmır.
    // xaricEt veriləndə (dövlət vəzifəsi overlap-i kimi) həmin günlər HEÇ VAXT
    // nəticəyə daxil edilmir.
    private async Task<int> HaqiqiIsGunuSayAsync(
        IEnumerable<(DateTime Baslama, DateTime Bitme)> araliqlar, int il, int ay,
        HashSet<DateTime>? xaricEt = null)
    {
        var ayBaslangic = new DateTime(il, ay, 1);
        var ayBitis = ayBaslangic.AddMonths(1).AddDays(-1);

        var ozelGunler = await _unitOfWork.Repository<BayramGunu>()
            .HamisiniGetirAsync(x => x.Tarix >= ayBaslangic && x.Tarix <= ayBitis);
        var ozelTipDict = ozelGunler
            .GroupBy(x => x.Tarix.Date)
            .ToDictionary(g => g.Key, g => g.First().Tip);

        var gunler = new HashSet<DateTime>();
        foreach (var (baslamaX, bitmeX) in araliqlar)
        {
            var baslama = baslamaX < ayBaslangic ? ayBaslangic : baslamaX;
            var bitme = bitmeX > ayBitis ? ayBitis : bitmeX;
            for (var t = baslama.Date; t <= bitme.Date; t = t.AddDays(1))
            {
                if (xaricEt != null && xaricEt.Contains(t.Date)) continue;
                bool realIsGunu = ozelTipDict.TryGetValue(t.Date, out var tip)
                    ? tip == GunTipi.IsGunu
                    : t.DayOfWeek != DayOfWeek.Saturday && t.DayOfWeek != DayOfWeek.Sunday;
                if (realIsGunu) gunler.Add(t.Date);
            }
        }
        return gunler.Count;
    }

    // Bu ay üçün işçinin dövlət vəzifəsi günləri (ay hüdudlarına kəsilmiş) —
    // Kod 1-in (İllik) overlap istisnası üçün. Kod 11 ARTIQ ayrıca sətir kimi
    // yazılmır (17.11.2026) — dövlət vəzifəsi günləri heç bir "işləməyib"
    // kateqoriyasına düşmür, sadəcə İllik-in çıxılmasının qarşısını alır.
    private async Task<HashSet<DateTime>> DovletVezifesiGunSetiAsync(int isciId, int il, int ay)
    {
        var ayBaslangic = new DateTime(il, ay, 1);
        var ayBitis = ayBaslangic.AddMonths(1).AddDays(-1);

        var qeydler = await _unitOfWork.Repository<Mezuniyyet>()
            .Query()
            .AsNoTracking()
            .Where(x => x.IsciId == isciId
                     && x.Status == MezuniyyetStatus.Tesdiqlenib
                     && x.Nov == MezuniyyetNovu.DovletVezifelerininIcrasi
                     && x.BaslamaTarixi <= ayBitis
                     && x.BitmeTarixi >= ayBaslangic)
            .Select(x => new { x.BaslamaTarixi, x.BitmeTarixi })
            .ToListAsync();

        var gunler = new HashSet<DateTime>();
        foreach (var q in qeydler)
        {
            var baslama = q.BaslamaTarixi < ayBaslangic ? ayBaslangic : q.BaslamaTarixi;
            var bitme = q.BitmeTarixi > ayBitis ? ayBitis : q.BitmeTarixi;
            for (var t = baslama.Date; t <= bitme.Date; t = t.AddDays(1))
                gunler.Add(t.Date);
        }
        return gunler;
    }

    // Rübün ilk ayı BUGÜNDƏN sonradırsa (hələ başlamayıb). DSMF yalnız
    // keçmiş/davam edən dövrü hesabat edir — gələcək rübdə heç bir Maas
    // (payroll) qeydi OLA BİLMƏZ, amma təqvim-əsaslı "iş günü" sayğacları
    // (HaqiqiIsGunuSayAsync) bunu bilmir, HƏR halda tam-ay dəyəri qaytarır.
    // Real hadisə (29.10.2026): 2027 seçiləndə hər işçi üçün "İş günü" sütunu
    // 21/22/23 göstərirdi, "Hesablanmış" isə "—" idi — yarım-dolu, çaşdırıcı
    // sətirlər. Qayda layihənin "İlin Son Günü Aşkarlanması" prinsipinin
    // əksidir: tək tarix-müqayisəsinə güvənmə demək YOX, BU DƏFƏ əksinə —
    // saf tarix-müqayisəsi KİFAYƏTDİR, çünki sual "bu dövr artıq başlayıbmı?"
    // sualıdır, "data varmı?" sualı deyil.
    private static bool RubGelecekdedir(int il, int ay1) => new DateTime(il, ay1, 1) > DateTime.Today;

    // Rübdə (ay1..ay3) İSTƏNİLƏN gün aktiv olmuş işçilər — Hissə 3-ün və Hissə
    // 1-in ORTAQ "kimi daxil et" sorğusu (əvvəl yalnız Hissə 3-ün öz daxilində
    // idi, Hissə 1 əlavə edilərkən TƏKRARLANMAMAQ üçün bura çıxarıldı). "Aktiv
    // işçi" kanonik filtri (IsciStatus.Aktiv) BURAYA YARAMIR — rüb ərzində
    // işdən çıxmış/işə qəbul olmuş işçi də lazımdır, tarix-kəsişməsi işlədilir.
    private async Task<List<Isci>> IscilərRubdeAktivAsync(int il, int ay1, int ay3)
    {
        var rubBaslangic = new DateTime(il, ay1, 1);
        var rubBitis = new DateTime(il, ay3, 1).AddMonths(1).AddDays(-1);

        return await _unitOfWork.Repository<Isci>()
            .Query()
            .AsNoTracking()
            .Where(x => x.IsheQebulTarixi <= rubBitis
                     && (x.IsdenAyrilmaTarixi == null || x.IsdenAyrilmaTarixi >= rubBaslangic))
            .OrderBy(x => x.Sira).ThenBy(x => x.Ad).ThenBy(x => x.Soyad)
            .ToListAsync();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Əlavə №4 Bölmə 2/3/4/5 — "güzəşt/azadolma" cədvəlləri (29.10.2026). Hissə
    // 3-dən FƏRQLİ struktur: işçi-sətri YOX, qanun-maddəsi-sətridir. İstifadəçi
    // ilə təsdiqlənmiş əhatə (yalnız bu bəndlər, qalanı bu banka aid deyil —
    // bax DsmfIstisnaGostericileri.cs):
    //   Bölmə 2: 4.2.6/102.1.8 (HYS, işçi VƏ işəgötürən payı — 29.10.2026-da
    //            "sirket yox" qərarı İKİNCİ dəfə, bu dəfə ƏKS istiqamətdə
    //            düzəldildi: Bölmə 2 üçün işəgötürən payı DA daxildir, bax
    //            HysIsegoturenMapAsync-in şərhi) + 4.2.5/102.1.6 (200 AZN) +
    //            4.5.3/102.4.5 (Qaçqın) + 4.4/102.3 (Əlillər) +
    //            4.2.7.7/102.1.14.16 (əsgər pulu)
    //   Bölmə 3: 5.2.9 (HYS, yalnız işçi — 29.10.2026-da düzəldildi) + 5.2.12 (xəstəlik)
    //   Bölmə 4: 6.3 (xəstəlik + manual gəlir IssizliyeCelb=false)
    //   Bölmə 5: 7.2 (xəstəlik + manual gəlir ItsseCelb=false)
    // ═══════════════════════════════════════════════════════════════════════
    // Mənbə şablon: UNIFIED_APPENDIX_4_DISCOUNTS.xlsx. 32 mümkün VM maddəsindən
    // yalnız 5-i sistemdə qarşılığı olan (BMI Finance üçün) maddələrdir, qalanı
    // (neft-qaz, texnologiya parkı, Ələt AİZ, diplomatik xidmət və s.) bu banka
    // aid deyil.
    public async Task<DsmfIstisnaBolmesiDto> Bolme2HazirlaAsync(int il, int rub)
    {
        var dto = BolmeBos(2, DsmfIstisnaGostericileri.Bolme2Basliq, il, rub);
        var ayLar = new[] { dto.Ay1, dto.Ay2, dto.Ay3 };

        var hysSetir = new DsmfIstisnaSetirDto { SiraNo = 1, Gostərici = DsmfIstisnaGostericileri.Madde1018 };
        var standartSetir = new DsmfIstisnaSetirDto { SiraNo = 2, Gostərici = DsmfIstisnaGostericileri.Madde1016 };
        var qacqinSetir = new DsmfIstisnaSetirDto { SiraNo = 3, Gostərici = DsmfIstisnaGostericileri.Madde10245 };
        var elillikSetir = new DsmfIstisnaSetirDto { SiraNo = 4, Gostərici = DsmfIstisnaGostericileri.Madde1023 };
        var esgerSetir = new DsmfIstisnaSetirDto { SiraNo = 5, Gostərici = DsmfIstisnaGostericileri.Madde10211416 };

        // 20.11.2026, istifadəçi düzəlişi: "işçinin işçi güzəştlərində şəxsi
        // azadolunmaları var, onları nəzərə almamısan" — `GuzestNovu.Diger`
        // tipli (HR-in kataloqa əlavə etdiyi, standart 2 tipdən (Elillik/
        // MecburiKockun) KƏNAR HƏR hansı) fərdi güzəşt əvvəl HEÇ sorğulanmırdı.
        // Bu tip qabaqcadan bilinməyən sayda qanun maddəsini daşıya bilər —
        // ona görə DİNAMİK sətirlərə (Madde, yoxdursa Ad üzrə) qruplaşdırılır,
        // SiraNo əsas 5 sətirdən SONRA (6-dan başlayaraq) verilir.
        var digerSetirler = new Dictionary<string, DsmfIstisnaSetirDto>();

        for (int i = 0; i < 3; i++)
        {
            // 29.10.2026, istifadəçi düzəlişi: "Azad güzəşt — mən burda sənə
            // səhv dedim + şirkətin ödədiyi [...]" — 4.2.6/102.1.8 İNDİ işçi
            // VƏ işəgötürən HYS paylarının CƏMİDİR (bax HysIsegoturenMapAsync-
            // in şərhi — ikisi fərqli qanuni əsasdadır, amma eyni "HYS gəlir
            // vergisindən azaddır" sətrinə düşür). İşəgötürən payı HƏMİŞƏ
            // işçi payından (hysMebleg-dən) hesablandığı üçün (bax
            // MaasHesablamaService "8.6") işçi dəsti artıq işəgötürən dəstini
            // örtür — amma `UnionWith` yenə də qoyulub, defensiv.
            var hys = await HysIsciPayiAsync(il, ayLar[i]);
            var hysIseg = await HysIsegoturenMapAsync(il, ayLar[i]);
            var hysIscilər = new HashSet<int>(hys.Keys);
            hysIscilər.UnionWith(hysIseg.Keys);
            SetirAyaYaz(hysSetir, i, hysIscilər.Count, hys.Values.Sum() + hysIseg.Values.Sum());

            var standart = await StandartGuzestAsync(il, ayLar[i]);
            SetirAyaYaz(standartSetir, i, standart.Count, standart.Values.Sum());

            // Fərdi güzəştlərin (işçi üzrə yalnız ƏN BÖYÜYÜ qalib gəlir) TƏK,
            // NÖVDƏN ASILI OLMAYAN mənbəyi — bax FerdiGuzestQalibleriAsync-in
            // şərhi (kök səbəb: Diger tipi heç sorğulanmırdı + köhnə uyğunlaşdırma
            // CANLI Guzest.Mebleg-ə bağlıydı).
            var ferdiGuzest = await FerdiGuzestQalibleriAsync(il, ayLar[i]);

            // 27.11.2026, istifadəçi düzəlişi: "problem odurki 4.5.3 ve 4.4
            // bosdur ama sen en asagida yeni setrde onlari elave etmisen, ele
            // onlar 4.5.3 ve 4.4-də olmalıdır." Kök səbəb: `GuzestNovu` enum-u
            // ƏSLİNDƏ məzuniyyət modulunun daxili klassifikasiyasıdır (bax
            // Guzest.cs-in öz şərhi: "məzuniyyət modulu əlilliyi bu tiplə
            // tanıyır... VERGİ HESABLAMASINA TƏSİR ETMİR, yalnız məzuniyyət
            // balansı üçün"). HR vergi-YALNIZ (DSMF hesabatı üçün) bir güzəşt
            // yaradanda, 42-günlük məzuniyyət balansı yan-təsirini TƏTİK
            // ETMƏMƏK üçün QƏSDƏN Novu=Diger saxlayıb, VM maddə referansını
            // sadəcə AZAD MƏTN kimi (Madde, yoxdursa Ad — "V.M. 102.4.5"/
            // "V.M. 102.3") yazıb. Ona görə Qaçqın/Əlillik indi İKİ yoldan
            // tanınır — Novu BİRBAŞA seçilibsə (köhnə yol, hələ dəstəklənir)
            // VƏ YA Madde/Ad mətni maddə NÖMRƏSİNİ daşıyırsa (yeni yol,
            // MaddeNomresineUygundur) — hər iki yol EYNİ nəticəyə aparır.
            var qacqin = new Dictionary<int, decimal>();
            var elillik = new Dictionary<int, decimal>();
            var digerQalanlar = new List<KeyValuePair<int, (string Ad, string? Madde, GuzestNovu Novu, decimal Mebleg)>>();
            foreach (var kv in ferdiGuzest)
            {
                var metin = kv.Value.Madde ?? kv.Value.Ad;
                if (kv.Value.Novu == GuzestNovu.MecburiKockun || MaddeNomresineUygundur(metin, "102.4.5"))
                    qacqin[kv.Key] = kv.Value.Mebleg;
                else if (kv.Value.Novu == GuzestNovu.Elillik || MaddeNomresineUygundur(metin, "102.3"))
                    elillik[kv.Key] = kv.Value.Mebleg;
                else
                    digerQalanlar.Add(kv);
            }
            SetirAyaYaz(qacqinSetir, i, qacqin.Count, qacqin.Values.Sum());
            SetirAyaYaz(elillikSetir, i, elillik.Count, elillik.Values.Sum());

            foreach (var grp in digerQalanlar.GroupBy(kv => kv.Value.Madde ?? kv.Value.Ad))
            {
                if (!digerSetirler.TryGetValue(grp.Key, out var s))
                {
                    s = new DsmfIstisnaSetirDto { SiraNo = 0, Gostərici = grp.Key };
                    digerSetirler[grp.Key] = s;
                }
                SetirAyaYaz(s, i, grp.Count(), grp.Sum(kv => kv.Value.Mebleg));
            }

            var esger = await ManualGelirVergiIstisnasiAsync(il, ayLar[i]);
            SetirAyaYaz(esgerSetir, i, esger.Count, esger.Values.Sum());
        }

        dto.Setirler = new List<DsmfIstisnaSetirDto> { hysSetir, standartSetir, qacqinSetir, elillikSetir, esgerSetir };
        var siraDiger = 6;
        foreach (var s in digerSetirler.Values)
        {
            s.SiraNo = siraDiger++;
            dto.Setirler.Add(s);
        }
        return dto;
    }

    /// <summary>
    /// 27.11.2026 — `metin` (bir güzəştin Madde ?? Ad sahəsi) VM maddə nömrəsini
    /// (məs. "102.4.5", "102.3") SƏRHƏD-TƏHLÜKƏSİZ şəkildə daşıyırmı. Sadə
    /// `Contains` kifayət etmir — "102.3" "102.30" və ya "102.3.1" kimi tam
    /// FƏRQLİ bir maddənin DAXİLİNDƏ də görünərdi. `(?&lt;!\d)...(?!\.?\d)`
    /// hər iki tərəfdə əlavə rəqəm olmamasını tələb edir.
    /// </summary>
    private static bool MaddeNomresineUygundur(string? metin, string maddeNomresi)
    {
        if (string.IsNullOrWhiteSpace(metin)) return false;
        var pattern = Regex.Escape(maddeNomresi);
        return Regex.IsMatch(metin, $@"(?<!\d){pattern}(?!\.?\d)");
    }

    // ── Bölmə 2/3-ün PAYLAŞDIĞI və Hissə 1-in DƏ istifadə etdiyi köməkçilər ──
    // 29.10.2026-da (Hissə 1 əlavə ediləndə) hamısı `(HashSet, decimal)`-dən
    // `Dictionary<IsciId, Mebleg>`-ə ÇEVRİLDİ — Bölmə 2/3/4/5 bu dict-in
    // `.Count`/`.Values.Sum()`-unu, Hissə 1 isə HƏR İŞÇİNİN ÖZ PAYINI oxuyur.
    // Nəticə DƏYİŞMİR (eyni cəm), sadəcə görüntü forması — "Bir Kəmiyyətə
    // Üç Yazıcı" riski olmasın deyə bu, DÖRD YERDƏ YOX, İKİ YERDƏ (bölmə
    // cədvəli + Hissə 1 sətri) EYNİ dict-dən oxunur.

    // HYS — işçi payı. 17.11.2026-da CANLI `IsciHYS.Mebleg`-dən DONDURULMUŞ
    // `MaasDetay`-a keçirildi (istifadəçi: "işçi HYS-ə gedən məbləğ redaktə
    // ilə azaldılıb — noyabrda iyul ayına baxanda görürük ki, hesabat
    // güzəştlərdəki CANLI HYS-dan götürür, bu da uyğunsuzluq yaradır").
    //
    // Kök səbəb: bu metod CANLI `IsciHYS` cədvəlindən oxuyurdu — `IsciHYS.
    // Mebleg` sonradan redaktə ilə dəyişsə, artıq keçmiş olan ayların bu
    // hesabatı da SƏSSİZCƏ dəyişirdi (eyni "canlı dəyərə güvənmə YALNIZ hələ
    // baş verməmiş bir şey üçün düzgündür" prinsipi — bax CLAUDE.md "Qara
    // Jeton — Kəsilən Saat CANLI Kataloq Dəyərindən Asılı İdi"). Qonşusu
    // `HysIsegoturenMapAsync` isə artıq DOĞRU idi (aşağı bax) — bu ikisi
    // arasındakı ASİMMETRİYA elə bugun özü idi.
    //
    // Arxiv YENİ YARADILMADI — onsuz da VAR: `MaasHesablamaService.cs:1428`
    // hər ay payroll hesablananda "HYS (İşçi)" adlı `MaasDetay` sətrini (o
    // ANKİ `hysMebleg` ilə, dondurma ARTIQ tətbiq olunmuş halda) yazır —
    // `HysIsegoturenMapAsync`-in "HYS (İşəgötürən)" oxuduğu ƏN EYNİ mənbə,
    // ƏN EYNİ naxış. `IIsciHYSDondurmaService`-ə artıq EHTİYAC YOXDUR —
    // dondurma `MaasHesablamaService`-də HESABLAMA ANINDA tətbiq olunub,
    // frozen sətrin özü bunu onsuz da daşıyır.
    //
    // HƏR DÖRD çağıran (Bölmə 2/3 + Hissə 1-in 3.1/4.1) nəticəni
    // `HysIsegoturenMapAsync`-in (işəgötürən payı) nəticəsi İLƏ BİRLİKDƏ
    // toplayır (29.10.2026, ÜÇÜNCÜ dalğa — bax o metodun şərhi).
    private async Task<Dictionary<int, decimal>> HysIsciPayiAsync(int il, int ay)
    {
        var detallar = await _unitOfWork.Repository<MaasDetay>()
            .Query()
            .AsNoTracking()
            .Where(x => !x.Silinib && x.MaasNovu.Ad == "HYS (İşçi)"
                     && x.Maas.Il == il && x.Maas.Ay == ay && !x.Maas.Silinib
                     && (x.Maas.Status == MaasStatus.Tesdiqlendi || x.Maas.Status == MaasStatus.Odenildi))
            .Select(x => new { x.Maas.IsciId, x.Mebleg })
            .ToListAsync();

        return detallar.GroupBy(x => x.IsciId).ToDictionary(g => g.Key, g => g.Sum(x => x.Mebleg));
    }

    // İşəgötürən tərəfindən ödənilən HYS haqqı (29.10.2026, istifadəçi düzəlişi:
    // "Azad güzəşt — mən burda sənə səhv dedim + şirkətin ödədiyi [...]").
    // İstifadə yerləri (29.10.2026, ÜÇÜNCÜ dalğa — İKİSİ DƏ işlədir):
    //   - Bölmə 2 (gəlir vergisi güzəşti, 4.2.6/102.1.8) VƏ Hissə 1-in 3.1
    //     "Azad güzəşt" sütunu;
    //   - Bölmə 3 (DSMF azad, 5.2.9) VƏ Hissə 1-in 4.1 "DSMF azad" sütunu —
    //     rəsmi şablonun (UNIFIED_APPENDIX_1_EMPLOYEE_LIST___.xlsx) K10
    //     xanasındakı mühasib qeydi "e/h M+T+U" (= Xəstəlik + işçi HYS +
    //     işəgötürən HYS) bunu TƏLƏB EDİR, istifadəçi təsdiqlədi.
    // Riyazi fon (niyə ikisi də düzgündür, baxmayaraq ki `dsmfBazasi` düsturu
    // `hysIsegoturen`-ə heç vaxt toxunmur):
    //   - `MaasHesablamaService.FerdiHesabla`-da `brutMaas = esasBrut +
    //     hysIsegoturen` — işəgötürən HYS-i BRÜT-Ə (deməli Hissə1-in 2.3
    //     "Hesablanmış" sütununa) DAXİLDİR;
    //   - `vergiBazasi`/`dsmfBazasi` düsturları `esasBrut`-dan başlayır,
    //     `hysIsegoturen`-i HEÇ VAXT əlavə etmir (nə +, nə -) — yəni işəgötürən
    //     HYS nə gəlir vergisi, nə DSMF bazasına DAXİL OLUB. Amma 2.3 onu
    //     DAŞIYIR (BrütMebleg-in tərkibində) — rəsmi hesabatın öz məntiqinə
    //     görə bu, 2.3-ün "izahı" kimi HƏM 3.1-də, HƏM 4.1-də "azad" sətri
    //     kimi göstərilməlidir, yoxsa 2.3−(3.1+4.1) real bazalarla uzlaşmaz.
    // Mənbə: `MaasHesablamaService.cs:1436`-dakı `DetayEkle("HYS (İşəgötürən)",
    // MaasDetayTipi.IsegoturenXerci, hysIsegoturen, ...)` — yeni hesablama
    // YAZILMADI, artıq persist edilmiş sətir oxunur.
    private async Task<Dictionary<int, decimal>> HysIsegoturenMapAsync(int il, int ay)
    {
        var detallar = await _unitOfWork.Repository<MaasDetay>()
            .Query()
            .AsNoTracking()
            .Where(x => !x.Silinib && x.MaasNovu.Ad == "HYS (İşəgötürən)"
                     && x.Maas.Il == il && x.Maas.Ay == ay && !x.Maas.Silinib
                     && (x.Maas.Status == MaasStatus.Tesdiqlendi || x.Maas.Status == MaasStatus.Odenildi))
            .Select(x => new { x.Maas.IsciId, x.Mebleg })
            .ToListAsync();

        return detallar.GroupBy(x => x.IsciId).ToDictionary(g => g.Key, g => g.Sum(x => x.Mebleg));
    }

    // Standart 200 AZN gəlir vergisi güzəşti (VM 102.1.6, brüt ≤ 2500 — bax
    // MaasHesablamaService.cs "9.2 Standart 200 AZN güzəşti" bölməsi). Düstur
    // deep-dependency-lidir (brutMaasGuzestYoxlama elaveCemi/mezuniyyetAvansBrutu/
    // VM9821-dən asılıdır) — TƏKRAR YAZILMADI (dublikat risk), əvəzinə artıq
    // HESABLANMIŞ VƏ PERSİST EDİLMİŞ "Vergi Guzesti" izahat addımından (Maas.
    // HesablamaIzahi JSON) oxunur — tək mənbə, canlı payroll ilə HƏMİŞƏ
    // tutuşur.
    //
    // ⚠️ `x.BrutMebleg > 0` qoruyucusu 20.11.2026-da əlavə edildi — istifadəçi:
    // "bizdə iki işçi var ki, onları uzun müddətli öz hesabına məzuniyyətə
    // göndəriblər və onlara heç bir maaş verilmir, amma sən onlarda 200 AZN
    // az da yazmışsan." Kök səbəb: `MaasHesablamaService.cs:1238`-dəki
    // `standartGuzest = brutMaasGuzestYoxlama <= firstBracketMax ? ... : 0`
    // şərti BRÜT=0 olanda da (0 ≤ 2500) TRUE qayıdır — bu, ƏSL VERGİ
    // HESABLAMASI üçün ZƏRƏRSİZDİR (vergiBazasi da 0 olduğu üçün net məbləğə
    // təsir etmir), AMMA DSMF-in "güzəşt/azadolma" sütunu HƏQİQİ EXEMPTED
    // GƏLİRİ bildirir — gəliri SIFIR olan işçi üçün "200 AZN azad edildi"
    // demək MƏNTİQSİZDİR (heç bir gəlir yoxdur ki, azad edilsin). Bu qoruyucu
    // bütün "fərdi güzəşt" oxuyucularına (bax aşağı FerdiGuzestQalibleriAsync)
    // tətbiq olunur.
    private async Task<Dictionary<int, decimal>> StandartGuzestAsync(int il, int ay)
    {
        var maaslar = await _unitOfWork.Repository<Maas>()
            .Query()
            .AsNoTracking()
            .Where(x => !x.Silinib && x.Il == il && x.Ay == ay
                     && x.BrutMebleg > 0
                     && (x.Status == MaasStatus.Tesdiqlendi || x.Status == MaasStatus.Odenildi)
                     && x.HesablamaIzahi != null)
            .Select(x => new { x.IsciId, x.HesablamaIzahi })
            .ToListAsync();

        var netice = new Dictionary<int, decimal>();
        foreach (var m in maaslar)
        {
            var standart = StandartGuzestdenCixar(m.HesablamaIzahi!);
            if (standart > 0) netice[m.IsciId] = standart;
        }
        return netice;
    }

    // Fərdi (işçi) güzəştlərin HƏQİQƏTƏN tətbiq olunan (qalib gələn) payı —
    // İL/AY üzrə, NÖVÜNDƏN ASILI OLMAYARAQ (Diger/Elillik/MecburiKockun).
    // Əvvəlki `GuzestNovuUzreAsync` (20.11.2026-da SİLİNİB, bu metodla əvəz
    // olunub) İKİ bugdan əziyyət çəkirdi — istifadəçi: "DSMF hesabatında azad
    // güzəştdə sən yalnız 200 vergiyə görəni nəzərə alırsan, amma işçinin
    // işçi güzəştlərində şəxsi azadolunmaları var, onları nəzərə almamısan":
    //
    //  1. `GuzestNovu.Diger` (= 0, "adi vergi güzəşti", HR-in kataloqa əlavə
    //     etdiyi ƏN GENİŞ/DEFOLT tip) HEÇ VAXT sorğulanmırdı — yalnız
    //     Elillik/MecburiKockun axtarılırdı. Kataloqda yaradılan İSTƏNİLƏN
    //     fərdi güzəşt (bu iki standart tipdən BAŞQA) DSMF-ə HEÇ DÜŞMÜRDÜ.
    //  2. Qalan iki tip üçün belə, uyğunlaşdırma CANLI `Guzest.Mebleg`-ə
    //     (kataloqun HAZIRKI dəyərinə) bağlı idi (`tetbiqOlunan ==
    //     isciMax[...]`) — güzəştin məbləği sonradan HR tərəfindən redaktə
    //     edilsə, keçmiş ayların FROZEN tətbiq olunan dəyəri artıq canlı
    //     kataloqla uyğun gəlmir və sətir SƏSSİZCƏ İTİRDİ (eyni "canlı dəyərə
    //     güvənmə YALNIZ hələ baş verməmiş bir şey üçün düzgündür" tələsi —
    //     bax HysIsciPayiAsync-in 17.11.2026 düzəlişi, HYS üçün artıq bir dəfə
    //     düzəldilmiş EYNİ sinif xəta).
    //
    // Həll — uyğunlaşdırma MİQDARA görə YOX, ADA görə gedir: "İşçi güzəştləri"
    // addımının Izah mətnindəki "Seçilən (ən böyük): {Ad}" hissəsi FROZEN
    // qalib güzəştin ADINI, həmin addımın Mebleg sahəsi isə onun FROZEN
    // tətbiq olunan MƏBLƏĞİNİ birbaşa daşıyır — kataloqa YALNIZ Novu/Madde
    // ETİKETİ üçün (hələ də canlı, AMMA bu YALNIZ görüntü/qruplaşdırma
    // üçündür, MƏBLƏĞƏ/SAYMAĞA HEÇ TƏSİR ETMİR) müraciət olunur. Güzəşt
    // kataloqdan sonradan silinsə/adı dəyişsə belə sətir "Diger" növü ilə,
    // YENƏ DƏ DOĞRU MƏBLƏĞLƏ (Madde=null) qalır — pul HEÇ VAXT SƏSSİZCƏ İTMİR.
    //
    // `x.BrutMebleg > 0` qoruyucusu StandartGuzestAsync-dəki EYNİ qaydadır
    // (bax onun şərhi, 20.11.2026) — maaşı olmayan (tam ay öz hesabına
    // məzuniyyət) işçiyə heç bir güzəşt "azad edildi" kimi yazılmır.
    private async Task<Dictionary<int, (string Ad, string? Madde, GuzestNovu Novu, decimal Mebleg)>>
        FerdiGuzestQalibleriAsync(int il, int ay)
    {
        var maaslar = await _unitOfWork.Repository<Maas>()
            .Query()
            .AsNoTracking()
            .Where(x => !x.Silinib && x.Il == il && x.Ay == ay
                     && x.BrutMebleg > 0
                     && (x.Status == MaasStatus.Tesdiqlendi || x.Status == MaasStatus.Odenildi)
                     && x.HesablamaIzahi != null)
            .Select(x => new { x.IsciId, x.HesablamaIzahi })
            .ToListAsync();

        var qalibler = new Dictionary<int, (string Ad, decimal Mebleg)>();
        foreach (var m in maaslar)
        {
            var (ad, mebleg) = IsciGuzestiQalibiCixar(m.HesablamaIzahi!);
            if (ad != null && mebleg > 0)
                qalibler[m.IsciId] = (ad, mebleg);
        }
        if (qalibler.Count == 0) return new();

        // Kataloqdan YALNIZ ETİKET (Madde/Novu) üçün — canlı, AMMA cəm/sayı
        // DƏYİŞMİR, yuxarıdaki şərhə bax.
        var adlar = qalibler.Values.Select(x => x.Ad).Distinct().ToList();
        var kataloq = await _unitOfWork.Repository<Guzest>()
            .Query()
            .AsNoTracking()
            .Where(g => adlar.Contains(g.Ad))
            .Select(g => new { g.Ad, g.Madde, g.Novu })
            .ToListAsync();
        var kataloqMap = kataloq.GroupBy(g => g.Ad).ToDictionary(g => g.Key, g => g.First());

        var netice = new Dictionary<int, (string Ad, string? Madde, GuzestNovu Novu, decimal Mebleg)>();
        foreach (var (isciId, qalib) in qalibler)
        {
            netice[isciId] = kataloqMap.TryGetValue(qalib.Ad, out var k)
                ? (qalib.Ad, k.Madde, k.Novu, qalib.Mebleg)
                : (qalib.Ad, null, GuzestNovu.Diger, qalib.Mebleg);
        }
        return netice;
    }

    // "İşçi güzəştləri" addımının Izah mətnindən qalib güzəştin ADINI çıxarır —
    // format (bax MaasHesablamaService.cs-in "İşçi güzəştləri" addım yazılışı):
    // "Aktiv güzəştlər: {Ad} — {Mebleg} ₼, .... Seçilən (ən böyük): {Ad}".
    // Format tanınmasa belə MƏBLƏĞ İTMİR — "Naməlum güzəşt" etiketi ilə qalır.
    private static (string? Ad, decimal Mebleg) IsciGuzestiQalibiCixar(string? hesablamaIzahiJson)
    {
        var addim = IzahatlarıOxu(hesablamaIzahiJson)?.FirstOrDefault(x => x.Addim == "İşçi güzəştləri");
        if (addim == null || addim.Mebleg <= 0) return (null, 0m);

        const string acar = "Seçilən (ən böyük): ";
        var idx = addim.Izah?.IndexOf(acar, StringComparison.Ordinal) ?? -1;
        if (idx < 0) return ("Naməlum güzəşt", addim.Mebleg);

        var ad = addim.Izah![(idx + acar.Length)..].Trim();
        if (string.IsNullOrWhiteSpace(ad) || ad == "—") return ("Naməlum güzəşt", addim.Mebleg);

        return (ad, addim.Mebleg);
    }

    // Manual gəlir növlərindən gəlir vergisindən azad olanlar — QİSMƏN
    // (VergiyeCelb=true + GelirVergisiAzadMebleg>0, Min(ödəniş, azad məbləğ))
    // VƏ TAM (VergiyeCelb=false, bütün ödəniş) hər ikisi. ManualGelirIstisnasiAsync-
    // dən FƏRQLİ: orada İşsizlik/İTSS bazasından azadlıq (bayraqla), burada
    // GƏLİR VERGİSİ bazasından azadlıq.
    //
    // ⚠️ VergiyeCelb=false (TAM azad) qolu 17.11.2026-da əlavə edildi —
    // istifadəçi: "hərbi xidmətə görə ödənilən pulu o biri sütunda yazmışıq
    // amma burda [3.1-də] göstərməmişik." Əvvəlki sorğu YALNIZ qismən-azad
    // (VergiyeCelb=true+cap) halını tuturdu — "Hərbi Xidmət" kimi TAM azad
    // (VergiyeCelb=false) manual gəlir növləri bu filtri HEÇ KEÇMİRDİ (nə
    // VergiyeCelb=true şərti, nə GelirVergisiAzadMebleg>0 — tam azad olanda
    // bu sahəyə bir cap yazmağa ehtiyac yoxdur), ona görə 3.1-ə HEÇ
    // ƏLAVƏ OLUNMURDU, baxmayaraq ki eyni növ İşsizlik/İTSS bazasından
    // (ManualGelirIstisnasiAsync, bayraqla, bu filtrə görə ASILI DEYİL)
    // doğru-düzgün azad edilirdi — bax "o biri sütun".
    private async Task<Dictionary<int, decimal>> ManualGelirVergiIstisnasiAsync(int il, int ay)
    {
        var novlər = await _unitOfWork.Repository<MaasNovu>()
            .Query()
            .AsNoTracking()
            .Where(x => x.Aktivdir && !x.Silinib && x.ManualGelir
                     && (!x.VergiyeCelb || x.GelirVergisiAzadMebleg > 0))
            .Select(x => new { x.Id, x.VergiyeCelb, x.GelirVergisiAzadMebleg })
            .ToListAsync();

        if (novlər.Count == 0) return new Dictionary<int, decimal>();
        var novMap = novlər.ToDictionary(x => x.Id, x => x);

        var detallar = await _unitOfWork.Repository<MaasDetay>()
            .Query()
            .AsNoTracking()
            .Where(x => !x.Silinib && novMap.Keys.Contains(x.MaasNovuId)
                     && x.Maas.Il == il && x.Maas.Ay == ay && !x.Maas.Silinib
                     && (x.Maas.Status == MaasStatus.Tesdiqlendi || x.Maas.Status == MaasStatus.Odenildi))
            .Select(x => new { x.Maas.IsciId, x.MaasNovuId, x.Mebleg })
            .ToListAsync();

        var netice = new Dictionary<int, decimal>();
        foreach (var d in detallar)
        {
            var nov = novMap[d.MaasNovuId];
            var azad = nov.VergiyeCelb ? Math.Min(d.Mebleg, nov.GelirVergisiAzadMebleg) : d.Mebleg;
            if (azad <= 0) continue;
            netice[d.IsciId] = netice.GetValueOrDefault(d.IsciId) + azad;
        }
        return netice;
    }

    // ── Maas.HesablamaIzahi (JSON) oxuyucuları — Bölmə 2-nin HYS-dən başqa HƏR
    // sətri bu ikisindən biri ilə işləyir. Parse xətası (köhnə/boş/korrupt JSON)
    // SƏSSİZ 0 qaytarır — DSMF sətri əskik qalar, amma səhv rəqəm YAZILMAZ.
    private static List<HesablamaIzahiDto>? IzahatlarıOxu(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<List<HesablamaIzahiDto>>(json); }
        catch (JsonException) { return null; }
    }

    private static decimal MaksIsciGuzestindenCixar(string? hesablamaIzahiJson)
        => IzahatlarıOxu(hesablamaIzahiJson)?.FirstOrDefault(x => x.Addim == "İşçi güzəştləri")?.Mebleg ?? 0m;

    private static decimal StandartGuzestdenCixar(string? hesablamaIzahiJson)
    {
        var izahatlar = IzahatlarıOxu(hesablamaIzahiJson);
        var vergiGuzesti = izahatlar?.FirstOrDefault(x => x.Addim == "Vergi Guzesti");
        if (vergiGuzesti == null) return 0m;
        return Math.Max(0, vergiGuzesti.Mebleg - MaksIsciGuzestindenCixar(hesablamaIzahiJson));
    }

    public async Task<DsmfIstisnaBolmesiDto> Bolme3HazirlaAsync(int il, int rub)
    {
        var dto = BolmeBos(3, DsmfIstisnaGostericileri.Bolme3Basliq, il, rub);
        var ayLar = new[] { dto.Ay1, dto.Ay2, dto.Ay3 };

        var hysSetir = new DsmfIstisnaSetirDto { SiraNo = 1, Gostərici = DsmfIstisnaGostericileri.Madde529 };
        var xstSetir = new DsmfIstisnaSetirDto { SiraNo = 2, Gostərici = DsmfIstisnaGostericileri.Madde5212 };

        for (int i = 0; i < 3; i++)
        {
            // 29.10.2026, ÜÇÜNCÜ dalğa — "sirket yox" qərarı BÖLMƏ 3 ÜÇÜN DƏ
            // TƏRSİNƏ ÇEVRİLDİ. Səbəb: rəsmi şablonun (UNIFIED_APPENDIX_1_
            // EMPLOYEE_LIST___.xlsx, "Əlavə1_Hissə1_Mudzlu işə" vərəqi) K10
            // xanasında mühasibin öz qeydi "e/h M+T+U" idi — 4.1 (DSMF azad)
            // İKİ yox, ÜÇ komponentdən ibarətdir: Xəstəlik vərəqəsi + Ödənilmiş
            // (işçi) HYS + İşgötürən ödənilən HYS. İstifadəçi bunu AÇIQ
            // TƏSDİQLƏDİ (AskUserQuestion, 29.10.2026) — eyni günün əvvəlki
            // "yalnız işçi" qərarını (bax aşağı HysIsciPayiAsync-in şərhi,
            // hələ köhnə qalıb) bununla ÜSTƏLƏYİR.
            // Riyazi əsas da tutarlıdır: Hissə 1-in 2.3 (Hesablanmış) sütunu
            // `Maas.BrutMebleg`-dən oxunur, o isə `brutMaas = esasBrut +
            // hysIsegoturen` düsturuna görə İŞGÖTÜRƏN HYS-i ONSUZ DA DAŞIYIR —
            // deməli 4.1-in onu "DSMF-dən azaddır" deyə göstərməsi, 2.3-ün
            // tərkibini izah etmək üçün MƏCBURİDİR (əks halda 2.3−4.1 real
            // DSMF bazası ilə uzlaşmazdı).
            var hys = await HysIsciPayiAsync(il, ayLar[i]);
            var hysIseg = await HysIsegoturenMapAsync(il, ayLar[i]);
            var hysIscilər = new HashSet<int>(hys.Keys);
            hysIscilər.UnionWith(hysIseg.Keys);
            var xst = await XestelikSirketOdenisiAsync(il, ayLar[i]);
            SetirAyaYaz(hysSetir, i, hysIscilər.Count, hys.Values.Sum() + hysIseg.Values.Sum());
            SetirAyaYaz(xstSetir, i, xst.Count, xst.Values.Sum());
        }

        dto.Setirler = new List<DsmfIstisnaSetirDto> { hysSetir, xstSetir };
        return dto;
    }

    public async Task<DsmfIstisnaBolmesiDto> Bolme4HazirlaAsync(int il, int rub)
        => await BirSetirlikBolmeHazirlaAsync(4, DsmfIstisnaGostericileri.Bolme4Basliq,
            DsmfIstisnaGostericileri.Madde63, il, rub, issizlikFlag: true);

    public async Task<DsmfIstisnaBolmesiDto> Bolme5HazirlaAsync(int il, int rub)
        => await BirSetirlikBolmeHazirlaAsync(5, DsmfIstisnaGostericileri.Bolme5Basliq,
            DsmfIstisnaGostericileri.Madde72, il, rub, issizlikFlag: false);

    // Bölmə 4 və 5 EYNİ naxışdadır (xəstəlik + manual-gəlir-istisnası, TƏK sətir) —
    // yalnız hansı MaasNovu bayrağına (İşsizlik/İTSS) baxıldığı fərqlənir.
    private async Task<DsmfIstisnaBolmesiDto> BirSetirlikBolmeHazirlaAsync(
        int bolmeNo, string basliq, string maddeMetni, int il, int rub, bool issizlikFlag)
    {
        var dto = BolmeBos(bolmeNo, basliq, il, rub);
        var ayLar = new[] { dto.Ay1, dto.Ay2, dto.Ay3 };
        var setir = new DsmfIstisnaSetirDto { SiraNo = 1, Gostərici = maddeMetni };

        for (int i = 0; i < 3; i++)
        {
            var xst = await XestelikSirketOdenisiAsync(il, ayLar[i]);
            var manual = await ManualGelirIstisnasiAsync(il, ayLar[i], issizlikFlag);

            var birlesmisIscilər = new HashSet<int>(xst.Keys);
            birlesmisIscilər.UnionWith(manual.Keys);
            SetirAyaYaz(setir, i, birlesmisIscilər.Count, xst.Values.Sum() + manual.Values.Sum());
        }

        dto.Setirler = new List<DsmfIstisnaSetirDto> { setir };
        return dto;
    }

    private static DsmfIstisnaBolmesiDto BolmeBos(int bolmeNo, string basliq, int il, int rub)
    {
        rub = Math.Clamp(rub, 1, 4);
        var ay1 = (rub - 1) * 3 + 1;
        return new DsmfIstisnaBolmesiDto
        {
            BolmeNo = bolmeNo,
            Basliq = basliq,
            Il = il,
            Rub = rub,
            Ay1 = ay1,
            Ay2 = ay1 + 1,
            Ay3 = ay1 + 2,
            Ay1Adi = AyAdlari[ay1],
            Ay2Adi = AyAdlari[ay1 + 1],
            Ay3Adi = AyAdlari[ay1 + 2],
        };
    }

    private static void SetirAyaYaz(DsmfIstisnaSetirDto s, int ayIndex, int isciSayi, decimal mebleg)
    {
        switch (ayIndex)
        {
            case 0: s.Ay1IsciSayi = isciSayi; s.Ay1Mebleg = mebleg; break;
            case 1: s.Ay2IsciSayi = isciSayi; s.Ay2Mebleg = mebleg; break;
            case 2: s.Ay3IsciSayi = isciSayi; s.Ay3Mebleg = mebleg; break;
        }
    }

    // Xəstəlik vərəqəsi üzrə ŞİRKƏT ödənişi (MaasHesablamaService.FerdiHesabla-nın
    // DSMF/İşsizlik/İTSS bazasından çıxdığı ƏN TƏK dəyər — bax o faylın 1147-1149-cu
    // sətirləri) — yalnız real Maas-a bağlanmış (MaasId != null) və TƏSDİQLƏNMİŞ/
    // ÖDƏNİLMİŞ statusda olan qeydlər sayılır, Layihə (hələ hesablanmamış) YOX.
    private async Task<Dictionary<int, decimal>> XestelikSirketOdenisiAsync(int il, int ay)
    {
        var qeydler = await _unitOfWork.Repository<XestelikOdenis>()
            .Query()
            .AsNoTracking()
            .Where(x => !x.Silinib && x.Il == il && x.Ay == ay && x.SirketOdenis > 0
                     && x.MaasId != null
                     && (x.Maas!.Status == MaasStatus.Tesdiqlendi || x.Maas!.Status == MaasStatus.Odenildi))
            .Select(x => new { x.IsciId, x.SirketOdenis })
            .ToListAsync();

        return qeydler.GroupBy(x => x.IsciId).ToDictionary(g => g.Key, g => g.Sum(x => x.SirketOdenis));
    }

    // Konfiqurasiyalı manual gəlir növlərindən (HR → Əmək haqqı parametrləri →
    // "Konfiqurasiyalı Gəlir Növləri", məs. "Hərbi Xidmət") İşsizlik/İTSS
    // bazasından AZAD olanlar — ADLA YOX, BAYRAQLA tutulur (`issizlikFlag` true
    // olanda IssizliyeCelb=false, false olanda ItsseCelb=false). Gələcəkdə
    // yaradılan HƏR yeni manual gəlir növü, eyni bayraqla işarələnərsə, kodda
    // dəyişiklik lazım olmadan avtomatik düşür. Yalnız real Maas-a bağlanmış,
    // TƏSDİQLƏNMİŞ/ÖDƏNİLMİŞ sətirlər sayılır (Layihə YOX) —
    // XestelikSirketOdenisiAsync ilə EYNİ status qaydası.
    private async Task<Dictionary<int, decimal>> ManualGelirIstisnasiAsync(
        int il, int ay, bool issizlikFlag)
    {
        var novIdler = await _unitOfWork.Repository<MaasNovu>()
            .Query()
            .AsNoTracking()
            .Where(x => x.Aktivdir && !x.Silinib && x.ManualGelir
                     && (issizlikFlag ? !x.IssizliyeCelb : !x.ItsseCelb))
            .Select(x => x.Id)
            .ToListAsync();

        if (novIdler.Count == 0) return new Dictionary<int, decimal>();

        var detallar = await _unitOfWork.Repository<MaasDetay>()
            .Query()
            .AsNoTracking()
            .Where(x => !x.Silinib && novIdler.Contains(x.MaasNovuId)
                     && x.Maas.Il == il && x.Maas.Ay == ay && !x.Maas.Silinib
                     && (x.Maas.Status == MaasStatus.Tesdiqlendi || x.Maas.Status == MaasStatus.Odenildi))
            .Select(x => new { x.Maas.IsciId, x.Mebleg })
            .ToListAsync();

        return detallar.GroupBy(x => x.IsciId).ToDictionary(g => g.Key, g => g.Sum(x => x.Mebleg));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Əlavə №1 Hissə 1 — "işçilər üzrə ümumi məlumat / maaş tərkibi" (29.10.2026).
    // Hissə 3 kimi İŞÇİ-sətridir (qanun-maddəsi-sətri YOX), 11 sütun × 3 ay.
    // Şablon: UNIFIED_APPENDIX_1_EMPLOYEE_LIST.xlsx. Sütun xəritəsi real
    // şablonun BAŞLIQ sətirlərindən (3-5) və mühasibin öz nümunə sətrindəki
    // (10-cu sətir) "e/h dan S+O", "e/h M+T+U" kimi qeydlərdən çıxarılıb:
    //
    //   2.1 Əsas iş yeri olması          → HƏMİŞƏ "Bəli" (istifadəçi qərarı,
    //                                        29.10.2026 — sistemdə bele bayraq
    //                                        yoxdur, 27 nümunə sətirdən 25-i
    //                                        onsuz da "Bəli" idi)
    //   2.2 Faktiki iş günü               → ayın real iş günü − Hissə 3-dəki
    //                                        bu işçinin işləmədiyi gün cəmi
    //                                        (Hissesi3HazirlaAsync-in ÖZ
    //                                        nəticəsindən, TƏKRAR hesablanmır)
    //   2.3 Hesablanmış əmək haqqı        → Maas.BrutMebleg + VM 98.2.1
    //                                        gəlirləri (29.10.2026 düzəlişi,
    //                                        bax Vm9821MapAsync-in şərhi —
    //                                        VM9821 brütə daxil edilmir, amma
    //                                        sütunun öz başlığı "...VƏ DİGƏR
    //                                        GƏLİRLƏR"dir)
    //   2.3.1, 2.5, 2.6, 4.5              → HƏMİŞƏ boş (mühasibin öz "bos"
    //                                        qeydi — sistemdə izi yoxdur:
    //                                        xaric gəlir, HYS-erkən-xitam,
    //                                        texnologiya parkı seçimi [bank
    //                                        üçün N/A])
    //   3.1 Azadolunan/güzəşt cəmi        → Bölmə 2-nin bu işçi üçün CƏMİ
    //                                        (HYS-in İKİ PAYI da daxildir —
    //                                        işçi VƏ işəgötürən, 29.10.2026
    //                                        düzəlişi, bax HysIsegoturenMapAsync)
    //   4.1 DSMF azad cəmi                → Bölmə 3-ün bu işçi üçün CƏMİ
    //                                        (HYS-in İKİ PAYI da daxildir —
    //                                        işçi VƏ işəgötürən, 29.10.2026
    //                                        ÜÇÜNCÜ dalğa düzəlişi, bax
    //                                        HysIsegoturenMapAsync)
    //   5.1 İşsizlik azad cəmi            → Bölmə 4-ün bu işçi üçün CƏMİ
    //   6.1 İTSS azad cəmi                → Bölmə 5-in bu işçi üçün CƏMİ
    //
    // Son dördü (3.1/4.1/5.1/6.1) Bölmə 2-5-in ARTIQ QURULMUŞ per-işçi
    // dict-lərinin cəmidir — YENİ DÜSTUR YAZILMAYIB, elə yuxarıdakı
    // HysIsciPayiAsync/HysIsegoturenMapAsync/StandartGuzestAsync/
    // FerdiGuzestQalibleriAsync/ManualGelirVergiIstisnasiAsync/
    // XestelikSirketOdenisiAsync/ManualGelirIstisnasiAsync-dən oxunur.
    public async Task<DsmfHissesi1Dto> Hissesi1HazirlaAsync(int il, int rub)
    {
        rub = Math.Clamp(rub, 1, 4);
        var ay1 = (rub - 1) * 3 + 1;
        var ay2 = ay1 + 1;
        var ay3 = ay1 + 2;
        var ayLar = new[] { ay1, ay2, ay3 };

        var dto = new DsmfHissesi1Dto
        {
            Il = il,
            Rub = rub,
            Ay1 = ay1,
            Ay2 = ay2,
            Ay3 = ay3,
            Ay1Adi = AyAdlari[ay1],
            Ay2Adi = AyAdlari[ay2],
            Ay3Adi = AyAdlari[ay3],
        };

        // Gələcək rüb — bax RubGelecekdedir şərhi. Hissə 3 özü də eyni
        // şərtlə erkən qayıdır, amma ONA GÜVƏNMƏK KİFAYƏT ETMƏZ: bura hələ
        // DƏ öz "ayinRealIsGunu"nu (saf təqvim) HESABLAYACAQDI — Hissə 3-ün
        // boş "işləməyib" cəmi ilə birləşəndə "tam ay işləyib" kimi YANLIŞ
        // bir nəticə verərdi. Ona görə BURADA DA AÇIQ YOXLANIR.
        if (RubGelecekdedir(il, ay1))
        {
            dto.GelecekRubdur = true;
            return dto;
        }

        // Hissə 3-ün ÖZ nəticəsi — "faktiki iş günü" elə onun "işləməyib"
        // cəminin ƏKSİDİR, TƏKRAR gün-sayma məntiqi YAZILMADI.
        var hisse3 = await Hissesi3HazirlaAsync(il, rub);
        var islemeyenCemi = new Dictionary<(int IsciId, int AyIndex), int>();
        void IslemeyenEleve(int isciId, int ayIndex, int gun)
        {
            if (gun <= 0) return;
            var key = (isciId, ayIndex);
            islemeyenCemi[key] = islemeyenCemi.GetValueOrDefault(key) + gun;
        }
        foreach (var s in hisse3.Setirler)
        {
            IslemeyenEleve(s.IsciId, 0, s.Ay1Gun);
            IslemeyenEleve(s.IsciId, 1, s.Ay2Gun);
            IslemeyenEleve(s.IsciId, 2, s.Ay3Gun);
        }

        var ayinRealIsGunu = new int[3];
        var brutMaplar = new Dictionary<int, decimal>[3];
        var mezQabaqBrut = new Dictionary<int, decimal>[3];
        var vm9821 = new Dictionary<int, decimal>[3];
        var hys = new Dictionary<int, decimal>[3];
        var hysIseg = new Dictionary<int, decimal>[3];
        var standart = new Dictionary<int, decimal>[3];
        // Fərdi güzəşt (Diger/Elillik/MecburiKockun — HAMISI, bax FerdiGuzestQalibleriAsync),
        // 20.11.2026-da `qacqin`/`elillik` iki ayrı sorğusunun ƏVƏZİNƏ TƏK dict —
        // bu sütun (3.1) növdən asılı olmayaraq CƏMİ istəyir, Bölmə 2-nin
        // NÖV-ÜZRƏ ayrı sətirlərinə ehtiyac yoxdur.
        var ferdiGuzest = new Dictionary<int, decimal>[3];
        var esger = new Dictionary<int, decimal>[3];
        var xst = new Dictionary<int, decimal>[3];
        var manualIssizlik = new Dictionary<int, decimal>[3];
        var manualItss = new Dictionary<int, decimal>[3];

        for (int i = 0; i < 3; i++)
        {
            var ay = ayLar[i];
            ayinRealIsGunu[i] = await HaqiqiIsGunuSayAsync(
                new[] { (new DateTime(il, ay, 1), new DateTime(il, ay, 1).AddMonths(1).AddDays(-1)) }, il, ay);
            brutMaplar[i] = await MaasBrutMapAsync(il, ay);
            mezQabaqBrut[i] = await MezQabaqcadanBrutMapAsync(il, ay);
            vm9821[i] = await Vm9821MapAsync(il, ay);
            hys[i] = await HysIsciPayiAsync(il, ay);
            hysIseg[i] = await HysIsegoturenMapAsync(il, ay);
            standart[i] = await StandartGuzestAsync(il, ay);
            ferdiGuzest[i] = (await FerdiGuzestQalibleriAsync(il, ay))
                .ToDictionary(kv => kv.Key, kv => kv.Value.Mebleg);
            esger[i] = await ManualGelirVergiIstisnasiAsync(il, ay);
            xst[i] = await XestelikSirketOdenisiAsync(il, ay);
            manualIssizlik[i] = await ManualGelirIstisnasiAsync(il, ay, issizlikFlag: true);
            manualItss[i] = await ManualGelirIstisnasiAsync(il, ay, issizlikFlag: false);
        }

        var iscilər = await IscilərRubdeAktivAsync(il, ay1, ay3);
        var setirler = new List<DsmfHissesi1SetirDto>();
        int sira = 1;

        foreach (var isci in iscilər)
        {
            var setir = new DsmfHissesi1SetirDto
            {
                SiraNo = sira++,
                IsciId = isci.Id,
                AdSoyadAta = $"{isci.Soyad} {isci.Ad} {isci.AtaAdi}".Trim(),
                Fin = isci.FIN,
            };

            var aylar = new DsmfHissesi1AyDto[3];
            for (int i = 0; i < 3; i++)
            {
                var islememisGun = islemeyenCemi.GetValueOrDefault((isci.Id, i));
                aylar[i] = new DsmfHissesi1AyDto
                {
                    FaktikiIsGunu = Math.Max(0, ayinRealIsGunu[i] - islememisGun),
                    // 2.3 = BrutMebleg + Qabaqcadan mez. brütünün bu ayın payı + VM 98.2.1.
                    // Qabaqcadan ödənilən məzuniyyətin brütü HEÇ VAXT Maas.BrutMebleg-ə
                    // daxil edilmir (MaasHesablamaService.cs:1110, brutMaas = esasBrut +
                    // hysIsegoturen — mezuniyyetAvansBrutu YOXDUR), ona görə leave
                    // günlərinin düşdüyü ayların 2.3-ü bu payı bax
                    // MezQabaqcadanBrutMapAsync-in şərhi) daşımadan boş qalırdı
                    // (17.11.2026, istifadəçi: "ödənilmiş mezuniyyet pulu bəzilərində
                    // yoxdur, bu yəqin ki qabaqcadan verilmişlərdədi"). VM9821 ilə EYNİ
                    // qayda (bax Vm9821MapAsync-in şərhi — brütə daxil edilməsə də,
                    // sütunun öz başlığı "...VƏ DİGƏR GƏLİRLƏR"dir).
                    HesablananMebleg = brutMaplar[i].GetValueOrDefault(isci.Id)
                                     + mezQabaqBrut[i].GetValueOrDefault(isci.Id)
                                     + vm9821[i].GetValueOrDefault(isci.Id),
                    // ⚠️ AzadGuzestCemi (3.1) VƏ DsmfAzadCemi (4.1) HƏR İKİSİ
                    // `hysIseg`-i daxil edir (29.10.2026, ÜÇÜNCÜ dalğa — bax
                    // HysIsegoturenMapAsync-in şərhi, rəsmi şablonun K10 qeydi
                    // "e/h M+T+U" əsasında). `ferdiGuzest[i]` (20.11.2026) —
                    // köhnə `qacqin[i]+elillik[i]`-nin ƏVƏZİNƏ, Diger/Elillik/
                    // MecburiKockun NÖVLƏRİNİN HAMISININ CƏMİ (bax
                    // FerdiGuzestQalibleriAsync-in şərhi) — bu sütun növdən
                    // asılı olmayaraq TOPLAM istəyir.
                    AzadGuzestCemi = hys[i].GetValueOrDefault(isci.Id) + hysIseg[i].GetValueOrDefault(isci.Id)
                                     + standart[i].GetValueOrDefault(isci.Id)
                                     + ferdiGuzest[i].GetValueOrDefault(isci.Id)
                                     + esger[i].GetValueOrDefault(isci.Id),
                    DsmfAzadCemi = hys[i].GetValueOrDefault(isci.Id) + hysIseg[i].GetValueOrDefault(isci.Id)
                                     + xst[i].GetValueOrDefault(isci.Id),
                    IssizlikAzadCemi = xst[i].GetValueOrDefault(isci.Id) + manualIssizlik[i].GetValueOrDefault(isci.Id),
                    ItssAzadCemi = xst[i].GetValueOrDefault(isci.Id) + manualItss[i].GetValueOrDefault(isci.Id),
                };
            }
            setir.Ay1 = aylar[0];
            setir.Ay2 = aylar[1];
            setir.Ay3 = aylar[2];

            setirler.Add(setir);
        }

        dto.Setirler = setirler;
        return dto;
    }

    // Maas.BrutMebleg-i (2.3 — "Hesablanmış əmək haqqı və digər gəlirlər") BİR
    // DƏFƏYƏ, bütün işçilər üçün toplu oxuyur — N+1 qarşısı alınır (Hissə 1-in
    // hər sətrinin öz Maas sorğusu yazmasının qarşısında).
    private async Task<Dictionary<int, decimal>> MaasBrutMapAsync(int il, int ay)
    {
        var maaslar = await _unitOfWork.Repository<Maas>()
            .Query()
            .AsNoTracking()
            .Where(x => !x.Silinib && x.Il == il && x.Ay == ay
                     && (x.Status == MaasStatus.Tesdiqlendi || x.Status == MaasStatus.Odenildi))
            .Select(x => new { x.IsciId, x.BrutMebleg })
            .ToListAsync();

        return maaslar.GroupBy(x => x.IsciId).ToDictionary(g => g.Key, g => g.Sum(x => x.BrutMebleg));
    }

    // Qabaqcadan ödənilmiş məzuniyyətin BU AYA düşən brüt payı (17.11.2026,
    // istifadəçi: "hesablanmış əmək haqqında ödənilmiş mezuniyyet pulu bəzilərində
    // yoxdur, bu yəqin ki qabaqcadan verilmişlərdədi"). TƏKRAR DÜSTUR YAZILMADI —
    // `MaasController.MezQabaqcadanBrutMapAsync`-in BİRƏ-BİR eyni naxışı (tək
    // mənbə — `IMaasHesablamaService.AvansAylaraBolunurmu`/`MezuniyyetAvansAyPaylariAsync`,
    // bax CLAUDE.md "Ay-ay payların yeganə mənbəyi"): kəsimdən (01.08.2026) sonra
    // aylara-bölünmüş modeldə hər qabaqcadan mezuniyyətin bu AYA düşən payı
    // `MezuniyyetAvansAyPaylariAsync`-dan oxunur; kəsimdən ƏVVƏLKİ aylarda isə
    // köhnə "ödənmə ayı" qaydası (`OdenilmeTarixi`) tətbiq olunur.
    private async Task<Dictionary<int, decimal>> MezQabaqcadanBrutMapAsync(int il, int ay)
    {
        bool avansAylaraBolunur = _maasHesablama.AvansAylaraBolunurmu(il, ay);
        var ayIlkGunu    = new DateTime(il, ay, 1);
        var novbetiAyIlk = ayIlkGunu.AddMonths(1);

        var avansBazaSorgu = _unitOfWork.Repository<Mezuniyyet>()
            .Query()
            .AsNoTracking()
            .Where(x =>
                !x.Silinib &&
                x.OdenisTipi == MezuniyyetOdenisTipi.QabaqcadanOdenis &&
                x.OdenisStatus == MezuniyyetOdenisStatus.Odenilib);

        var avanslar = avansAylaraBolunur
            ? await avansBazaSorgu
                .Where(x => x.BaslamaTarixi < novbetiAyIlk && x.BitmeTarixi >= ayIlkGunu)
                .ToListAsync()
            : await avansBazaSorgu
                .Where(x => x.OdenilmeTarixi.HasValue &&
                            x.OdenilmeTarixi.Value.Year == il &&
                            x.OdenilmeTarixi.Value.Month == ay)
                .ToListAsync();

        var map = new Dictionary<int, decimal>();
        foreach (var av in avanslar)
        {
            decimal brut;
            if (avansAylaraBolunur)
            {
                var paylar = await _maasHesablama.MezuniyyetAvansAyPaylariAsync(
                    av.IsciId, av.BaslamaTarixi, av.BitmeTarixi, av.OdenenMebleg);
                var buAy = paylar.FirstOrDefault(p => p.Il == il && p.Ay == ay);
                brut = buAy?.Brut ?? 0m;
            }
            else
            {
                brut = av.OdenenMeblegBrut ?? 0m;
            }
            if (brut <= 0) continue;
            map[av.IsciId] = map.GetValueOrDefault(av.IsciId) + brut;
        }
        return map;
    }

    // VM-nin 98.2.1-ci maddəsinə əsasən vergiyə cəlb olunan gəlirlər (29.10.2026,
    // istifadəçi: "Əlavə №1 — Hissə 1-bu hesabatda + VM-nin 98.2.1-ci maddəsinə
    // əsasən vergiyə cəlb olunan gəlirlər"). `MaasHesablamaService.FerdiHesabla`-da
    // bu, `input.VM9821Meblegi` kimi BÜTÜN 4 bazaya (vergi, DSMF, İşsizlik, İTSS)
    // ƏLAVƏ OLUNUR, AMMA `brutMaas`-a (deməli `Maas.BrutMebleg`-ə) HEÇ VAXT DAXİL
    // EDİLMİR (kodun öz şərhi: "VM 98.2.1 gəlirləri BRÜT-ə DAXİL EDİLMİR [...]
    // gəlir kimi maaşı ARTIRMIR"). Yəni DSMF-in real hesabladığı baza
    // `BrutMebleg`-dən BÖYÜKDÜR — bu fərq 2.3 "Hesablanmış əmək haqqı VƏ DİGƏR
    // GƏLİRLƏR" sütununun ÖZ başlığında onsuz da nəzərdə tutulub (bax yuxarı
    // MaasBrutMapAsync-in şərhi). Mənbə: `MaasHesablamaService.cs:1417`-dəki
    // `DetayEkle("VM 98.2.1 Gəlirləri", MaasDetayTipi.Gelir, input.VM9821Meblegi,
    // ...)` — yeni hesablama YAZILMADI, artıq persist edilmiş sətir oxunur.
    //
    // ⚠️ Yalnız 2.3-ə (Hesablanmış) əlavə olunur — 3.1/4.1/5.1/6.1 "azad"
    // sütunlarına HEÇ TOXUNMUR, çünki VM 98.2.1 "azadolma/güzəşt" DEYİL, əksinə
    // HƏR DÖRD bazaya BİLƏRƏKDƏN vergiyə-cəlb-olunan kimi ƏLAVƏ olunan gəlirdir.
    private async Task<Dictionary<int, decimal>> Vm9821MapAsync(int il, int ay)
    {
        var detallar = await _unitOfWork.Repository<MaasDetay>()
            .Query()
            .AsNoTracking()
            .Where(x => !x.Silinib && x.MaasNovu.Ad == "VM 98.2.1 Gəlirləri"
                     && x.Maas.Il == il && x.Maas.Ay == ay && !x.Maas.Silinib
                     && (x.Maas.Status == MaasStatus.Tesdiqlendi || x.Maas.Status == MaasStatus.Odenildi))
            .Select(x => new { x.Maas.IsciId, x.Mebleg })
            .ToListAsync();

        return detallar.GroupBy(x => x.IsciId).ToDictionary(g => g.Key, g => g.Sum(x => x.Mebleg));
    }
}
