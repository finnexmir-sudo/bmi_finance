using FinNex.Application.DTOs.Dsmf;
using FinNex.Application.DTOs.HR.Maas;
using FinNex.Application.Interfaces.Dsmf;
using FinNex.Application.Interfaces.HR;
using FinNex.Application.Interfaces.Maas_If;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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

                // Kod 1 — ödənişli əsas məzuniyyət (Illik).
                Elave(1, i, await NovUzreHaqiqiIsGunuAsync(isci.Id, MezuniyyetNovu.Illik, il, ay));

                // Kod 3 — ödənişsiz məzuniyyət (Ə.M. 129). Mövcud, sınanmış servis metodu.
                Elave(3, i, await _maasHesablama.OzHesabinaIsGunuSayAsync(isci.Id, il, ay));

                // Kod 5 — xəstələnmə. Mövcud, sınanmış servis metodu (26.10.2026-da
                // düzəldilmiş "həqiqi iş günü" versiyası).
                Elave(5, i, await _maasHesablama.XestelikHaqiqiIsGunuSayAsync(isci.Id, il, ay));

                // Kod 11 — dövlət/ictimai vəzifə.
                Elave(11, i, await NovUzreHaqiqiIsGunuAsync(isci.Id, MezuniyyetNovu.DovletVezifelerininIcrasi, il, ay));
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
    private async Task<int> NovUzreHaqiqiIsGunuAsync(int isciId, MezuniyyetNovu nov, int il, int ay)
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
            qeydler.Select(x => (x.BaslamaTarixi, x.BitmeTarixi)), il, ay);
    }

    // Verilmiş tarix aralıqlarının bir aya düşən REAL iş günü sayı (BayramGunu +
    // həftəsonu qaydaları) — HashSet ilə üst-üstə düşən günlər ikiqat sayılmır.
    private async Task<int> HaqiqiIsGunuSayAsync(
        IEnumerable<(DateTime Baslama, DateTime Bitme)> araliqlar, int il, int ay)
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
                bool realIsGunu = ozelTipDict.TryGetValue(t.Date, out var tip)
                    ? tip == GunTipi.IsGunu
                    : t.DayOfWeek != DayOfWeek.Saturday && t.DayOfWeek != DayOfWeek.Sunday;
                if (realIsGunu) gunler.Add(t.Date);
            }
        }
        return gunler.Count;
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
    //   Bölmə 2: 4.2.6/102.1.8 (HYS, yalnız işçi) + 4.2.5/102.1.6 (200 AZN) +
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

        for (int i = 0; i < 3; i++)
        {
            var hys = await HysIsciPayiAsync(il, ayLar[i]);
            SetirAyaYaz(hysSetir, i, hys.Count, hys.Values.Sum());

            var standart = await StandartGuzestAsync(il, ayLar[i]);
            SetirAyaYaz(standartSetir, i, standart.Count, standart.Values.Sum());

            var qacqin = await GuzestNovuUzreAsync(il, ayLar[i], GuzestNovu.MecburiKockun);
            SetirAyaYaz(qacqinSetir, i, qacqin.Count, qacqin.Values.Sum());

            var elillik = await GuzestNovuUzreAsync(il, ayLar[i], GuzestNovu.Elillik);
            SetirAyaYaz(elillikSetir, i, elillik.Count, elillik.Values.Sum());

            var esger = await ManualGelirVergiIstisnasiAsync(il, ayLar[i]);
            SetirAyaYaz(esgerSetir, i, esger.Count, esger.Values.Sum());
        }

        dto.Setirler = new List<DsmfIstisnaSetirDto> { hysSetir, standartSetir, qacqinSetir, elillikSetir, esgerSetir };
        return dto;
    }

    // ── Bölmə 2/3-ün PAYLAŞDIĞI və Hissə 1-in DƏ istifadə etdiyi köməkçilər ──
    // 29.10.2026-da (Hissə 1 əlavə ediləndə) hamısı `(HashSet, decimal)`-dən
    // `Dictionary<IsciId, Mebleg>`-ə ÇEVRİLDİ — Bölmə 2/3/4/5 bu dict-in
    // `.Count`/`.Values.Sum()`-unu, Hissə 1 isə HƏR İŞÇİNİN ÖZ PAYINI oxuyur.
    // Nəticə DƏYİŞMİR (eyni cəm), sadəcə görüntü forması — "Bir Kəmiyyətə
    // Üç Yazıcı" riski olmasın deyə bu, DÖRD YERDƏ YOX, İKİ YERDƏ (bölmə
    // cədvəli + Hissə 1 sətri) EYNİ dict-dən oxunur.

    // HYS — YALNIZ işçi payı (IsciHYS.Mebleg), işəgötürən payı DAXİL DEYİL
    // (istifadəçi qərarı, 29.10.2026: "sirket yox" — bu qayda Bölmə 2-nin
    // VM 102.1.8 sətrinə VƏ Bölmə 3-ün 5.2.9-una EYNİ dərəcədə aiddir, ikisi
    // də bu metodu çağırır — əvvəlki "5.2.9 = işçi+işəgötürən" versiyası
    // səhv idi, düzəldildi). Dondurulmuş aylar (IIsciHYSDondurmaService) TƏK
    // mənbədən xaric edilir — bax CLAUDE.md "HYS — Bir Aylıq Dondurma".
    private async Task<Dictionary<int, decimal>> HysIsciPayiAsync(int il, int ay)
    {
        var ayBaslangic = new DateTime(il, ay, 1);
        var ayBitis = ayBaslangic.AddMonths(1).AddDays(-1);

        var qeydler = await _unitOfWork.Repository<IsciHYS>()
            .Query()
            .AsNoTracking()
            .Where(x => !x.Silinib
                     && x.BaslamaTarixi <= ayBitis
                     && (x.BitmeTarixi == null || x.BitmeTarixi >= ayBaslangic))
            .ToListAsync();

        var dondurulmus = await _hysDondurma.DondurulmusIdlerAsync(il, ay);
        if (dondurulmus.Count > 0)
            qeydler = qeydler.Where(x => !dondurulmus.Contains(x.Id)).ToList();

        return qeydler.GroupBy(x => x.IsciId).ToDictionary(g => g.Key, g => g.Sum(x => x.Mebleg));
    }

    // Standart 200 AZN gəlir vergisi güzəşti (VM 102.1.6, brüt ≤ 2500 — bax
    // MaasHesablamaService.cs "9.2 Standart 200 AZN güzəşti" bölməsi). Düstur
    // deep-dependency-lidir (brutMaasGuzestYoxlama elaveCemi/mezuniyyetAvansBrutu/
    // VM9821-dən asılıdır) — TƏKRAR YAZILMADI (dublikat risk), əvəzinə artıq
    // HESABLANMIŞ VƏ PERSİST EDİLMİŞ "Vergi Guzesti" izahat addımından (Maas.
    // HesablamaIzahi JSON) oxunur — tək mənbə, canlı payroll ilə HƏMİŞƏ
    // tutuşur.
    private async Task<Dictionary<int, decimal>> StandartGuzestAsync(int il, int ay)
    {
        var maaslar = await _unitOfWork.Repository<Maas>()
            .Query()
            .AsNoTracking()
            .Where(x => !x.Silinib && x.Il == il && x.Ay == ay
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

    // Əlillik/Qaçqın (MecburiKockun) güzəştinin HƏQİQƏTƏN tətbiq olunan payı.
    // İşçidə eyni anda BİRDƏN ÇOX növdə aktiv güzəşt varsa (nadir), vergi
    // hesablamasında yalnız ƏN BÖYÜYÜ tətbiq olunur (Madde 102 — toplanmır).
    // Bu metod HƏR növü ayrıca sorğulayır, amma yalnız HƏQİQƏTƏN qalib gələn
    // (Maas.HesablamaIzahi-dəki "İşçi güzəştləri" addımının Mebleg-i bu
    // Guzest-in Mebleg-inə DƏQİQ bərabərdirsə) sətirləri sayır — əks halda
    // güzəşt qeydiyyatda var, amma HEÇ VAXT tətbiq olunmayıb (başqa, daha
    // böyük güzəşt qalib gəlib) deməkdir, DSMF-ə yazılmamalıdır.
    private async Task<Dictionary<int, decimal>> GuzestNovuUzreAsync(int il, int ay, GuzestNovu novu)
    {
        var ayBaslangic = new DateTime(il, ay, 1);
        var ayBitis = ayBaslangic.AddMonths(1).AddDays(-1);

        var namizedler = await _unitOfWork.Repository<IsciGuzest>()
            .Query()
            .AsNoTracking()
            .Where(x => !x.Silinib
                     && x.BaslamaTarixi <= ayBitis
                     && (x.BitmeTarixi == null || x.BitmeTarixi >= ayBaslangic))
            .Include(x => x.Guzest)
            .Where(x => x.Guzest != null && !x.Guzest.Silinib && x.Guzest.Aktivdir && x.Guzest.Novu == novu)
            .Select(x => new { x.IsciId, x.Guzest!.Mebleg })
            .ToListAsync();

        if (namizedler.Count == 0) return new Dictionary<int, decimal>();

        var isciMax = namizedler.GroupBy(x => x.IsciId).ToDictionary(g => g.Key, g => g.Max(x => x.Mebleg));

        var maaslar = await _unitOfWork.Repository<Maas>()
            .Query()
            .AsNoTracking()
            .Where(x => !x.Silinib && x.Il == il && x.Ay == ay
                     && isciMax.Keys.Contains(x.IsciId)
                     && (x.Status == MaasStatus.Tesdiqlendi || x.Status == MaasStatus.Odenildi)
                     && x.HesablamaIzahi != null)
            .Select(x => new { x.IsciId, x.HesablamaIzahi })
            .ToListAsync();

        var netice = new Dictionary<int, decimal>();
        foreach (var m in maaslar)
        {
            var tetbiqOlunan = MaksIsciGuzestindenCixar(m.HesablamaIzahi!);
            if (tetbiqOlunan > 0 && tetbiqOlunan == isciMax[m.IsciId])
                netice[m.IsciId] = tetbiqOlunan;
        }
        return netice;
    }

    // Manual gəlir növlərindən (məs. "Hərbi Xidmət") QİSMƏN gəlir vergisindən
    // azad olanlar — ManualGelirIstisnasiAsync-dən FƏRQLİ: orada İşsizlik/İTSS
    // bazasından TAM azadlıq (bayraqla), burada gəlir vergisi bazasından YALNIZ
    // MaasNovu.GelirVergisiAzadMebleg qədər (Min(ödəniş, azad məbləğ)) azadlıq.
    private async Task<Dictionary<int, decimal>> ManualGelirVergiIstisnasiAsync(int il, int ay)
    {
        var novlər = await _unitOfWork.Repository<MaasNovu>()
            .Query()
            .AsNoTracking()
            .Where(x => x.Aktivdir && !x.Silinib && x.ManualGelir
                     && x.VergiyeCelb && x.GelirVergisiAzadMebleg > 0)
            .Select(x => new { x.Id, x.GelirVergisiAzadMebleg })
            .ToListAsync();

        if (novlər.Count == 0) return new Dictionary<int, decimal>();
        var azadMap = novlər.ToDictionary(x => x.Id, x => x.GelirVergisiAzadMebleg);

        var detallar = await _unitOfWork.Repository<MaasDetay>()
            .Query()
            .AsNoTracking()
            .Where(x => !x.Silinib && azadMap.Keys.Contains(x.MaasNovuId)
                     && x.Maas.Il == il && x.Maas.Ay == ay && !x.Maas.Silinib
                     && (x.Maas.Status == MaasStatus.Tesdiqlendi || x.Maas.Status == MaasStatus.Odenildi))
            .Select(x => new { x.Maas.IsciId, x.MaasNovuId, x.Mebleg })
            .ToListAsync();

        var netice = new Dictionary<int, decimal>();
        foreach (var d in detallar)
        {
            var azad = Math.Min(d.Mebleg, azadMap[d.MaasNovuId]);
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
            // 29.10.2026, istifadəçi düzəlişi: "sirket yox" — 5.2.9 YALNIZ
            // işçi payını (IsciHYS.Mebleg) göstərir, işəgötürən 15%-i artıq
            // DAXİL DEYİL (əvvəlki versiya birləşdirirdi — düzəldildi).
            // Bölmə 2-nin VM 102.1.8 sətri ilə EYNİ metodu (HysIsciPayiAsync)
            // çağırır — iki ayrı qanun maddəsi, amma eyni "yalnız işçi" qaydası.
            var hys = await HysIsciPayiAsync(il, ayLar[i]);
            var xst = await XestelikSirketOdenisiAsync(il, ayLar[i]);
            SetirAyaYaz(hysSetir, i, hys.Count, hys.Values.Sum());
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
    //   2.3 Hesablanmış əmək haqqı        → Maas.BrutMebleg
    //   2.3.1, 2.5, 2.6, 4.5              → HƏMİŞƏ boş (mühasibin öz "bos"
    //                                        qeydi — sistemdə izi yoxdur:
    //                                        xaric gəlir, HYS-erkən-xitam,
    //                                        texnologiya parkı seçimi [bank
    //                                        üçün N/A])
    //   3.1 Azadolunan/güzəşt cəmi        → Bölmə 2-nin bu işçi üçün CƏMİ
    //   4.1 DSMF azad cəmi                → Bölmə 3-ün bu işçi üçün CƏMİ
    //   5.1 İşsizlik azad cəmi            → Bölmə 4-ün bu işçi üçün CƏMİ
    //   6.1 İTSS azad cəmi                → Bölmə 5-in bu işçi üçün CƏMİ
    //
    // Son dördü (3.1/4.1/5.1/6.1) Bölmə 2-5-in ARTIQ QURULMUŞ per-işçi
    // dict-lərinin cəmidir — YENİ DÜSTUR YAZILMAYIB, elə yuxarıdakı
    // HysIsciPayiAsync/StandartGuzestAsync/GuzestNovuUzreAsync/
    // ManualGelirVergiIstisnasiAsync/XestelikSirketOdenisiAsync/
    // ManualGelirIstisnasiAsync-dən oxunur.
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
        var hys = new Dictionary<int, decimal>[3];
        var standart = new Dictionary<int, decimal>[3];
        var qacqin = new Dictionary<int, decimal>[3];
        var elillik = new Dictionary<int, decimal>[3];
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
            hys[i] = await HysIsciPayiAsync(il, ay);
            standart[i] = await StandartGuzestAsync(il, ay);
            qacqin[i] = await GuzestNovuUzreAsync(il, ay, GuzestNovu.MecburiKockun);
            elillik[i] = await GuzestNovuUzreAsync(il, ay, GuzestNovu.Elillik);
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
                    HesablananMebleg = brutMaplar[i].GetValueOrDefault(isci.Id),
                    AzadGuzestCemi = hys[i].GetValueOrDefault(isci.Id) + standart[i].GetValueOrDefault(isci.Id)
                                     + qacqin[i].GetValueOrDefault(isci.Id) + elillik[i].GetValueOrDefault(isci.Id)
                                     + esger[i].GetValueOrDefault(isci.Id),
                    DsmfAzadCemi = hys[i].GetValueOrDefault(isci.Id) + xst[i].GetValueOrDefault(isci.Id),
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
}
