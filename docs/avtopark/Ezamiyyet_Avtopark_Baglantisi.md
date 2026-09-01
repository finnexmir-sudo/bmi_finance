# Ezamiyyət ↔ Avtopark — «Bir forma, bir təsdiq» (01.09.2026)

**Status:** kod yazılıb, **build EDİLMƏYİB** (mühitdə `dotnet` yoxdur) + **migration işlədilməyib**

---

## İstifadəçi qərarı

> «Ezamda Avtoqarajla əlaqə lazımdır, çünki bəzən ezam gedəndə maşın ilə gedirlər,
> indiki halda gərək avtoqaraja təkrar girsin, iki iş olur.»

Seçilən variant (3 sualdan):

| Sual | Qərar |
|---|---|
| Yanaşma | **Bir forma, bir təsdiq** — maşın ezamiyyət formasında seçilir, rəhbər ezamiyyəti təsdiqləyəndə maşın müraciəti avtomatik yaranır və **artıq təsdiqlənmiş** olur |
| Maşını kim seçir | **İşçi seçir** (Avtoparkdakı indiki davranış saxlanılır) |
| HR-ın yaratdığı ezamiyyət | **Əhatə olunmur** — yalnız işçi müraciəti (`EzamiyyetMuraciet`) |

---

## İKİ AYRI «EZAMİYYƏT» VAR — QARIŞDIRMA

Layihədə ezamiyyət **iki müstəqil yerdə** saxlanılır və bir-birinə yazmır:

| | Cədvəl | Kim yaradır | Bu bağlantı |
|---|---|---|---|
| **A** | `EzamiyyetMuracietler` | İşçi (User portalı) | ✅ **DAXİLDİR** |
| **B** | `Mezuniyyetler` (`Nov = Ezamiyyet`) | HR (Xəstəlik/Ezamiyyət ekranı) | ❌ kənardadır |

B variantında **məkan, saat, sənəd sahələri ümumiyyətlə yoxdur** — maşın bölməsi
əlavə etmək üçün saatı ayrıca soruşmaq lazım gələrdi. İstifadəçi qərarı: hələlik
yalnız A. HR tərəfi istənsə ayrıca iş kimi əlavə olunacaq.

---

## Axın

```
İŞÇİ    → ezamiyyət müraciəti + «Xidməti maşınla gedirəm» + maşın seçimi
          (maşın YOXLANIR: mövcuddur + Aktiv — forma göndəriləndə, təsdiqdə YOX)
RƏHBƏR  → ezamiyyəti təsdiqləyir
          └─ eyni anda MasinMuraciet yaranır, statusu DƏRHAL «Təsdiqlənib»
             və bildiriş KASSA-ya gedir
KASSA   → «Çıxdı» / «Gəldi» (Avtoparkın öz ekranı, dəyişməyib)
```

**Rəhbər maşını AYRICA təsdiqləmir** — bir klik, iki qeyd. `MasinMuracietService.IlkinStatus`
burada işlətmir (o, müraciət sahibinin roluna baxır və adi işçidə `Gozlemede`
yazardı → rəhbər eyni səfəri iki dəfə təsdiqləməli olardı).

---

## İKİ SÜTUN, İKİ FƏRQLİ MƏNA

| Sütun | Mənası |
|---|---|
| `EzamiyyetMuracietler.MasinId` | İşçinin **İSTƏDİYİ** maşın (təsdiqə qədər saxlanılır) |
| `MasinMuracietler.EzamiyyetMuracietId` | Bu maşın müraciəti **hansı ezamiyyətdəndir** |

«Hansı maşın verildi» sualının cavabı **həmişə `MasinMuraciet`-dədir**.
`EzamiyyetMuraciet.MasinId` istəkdir — rəhbər imtina etsə orada dəyər qalır,
amma maşın müraciəti yaranmır.

Bağ **açıq sahə** ilədir, tarix/işçi uyğunluğu ilə YOX — eyni işçinin eyni günə
həm ezamiyyət maşını, həm əl ilə yazdığı Avtopark müraciəti ola bilər.

---

## Vaxt uyğunlaşdırması

`MasinMuraciet.PlanBaslama` saatsız ola bilməz, ezamiyyətdə isə saat **istəyə bağlıdır**:

| Ezamiyyət | `PlanBaslama` |
|---|---|
| Saatlı (`BaslamaSaati` var) | həmin saat |
| Tam gün (`BaslamaSaati = null`) | `IsParametri.StandartGirisVaxti` (default 09:00) |

`00:00` yazılsaydı kassa ekranında gecə yarısı görünərdi.

Digər uyğunlaşdırmalar: `Meqsed` ← ezamiyyətin `Baslig`-i, `Marsrut` ← `Mekan.Ad`.
`PlanBitme` **yazılmır** (21.08.2026 qərarı — qayıdışın yeganə mənbəyi kassadır).

---

## LƏĞV — AÇAR VERİLİBSƏ TOXUNULMUR (KRİTİK)

Ezamiyyət ləğv olunanda bağlı maşın müraciəti də ləğv edilir — **amma yalnız
`Gozlemede`/`Tesdiqlenib` olduqda**. Maşın artıq **`Cixib`** statusundadırsa
sətrə toxunulmur: maşın fiziki olaraq çöldədir, onu ekran «ləğv edilmiş» sayarsa
kassa jurnalı yalan danışar və maşın heç vaxt «qayıtdı» olmaz.

Belə halda istifadəçiyə **xəbərdarlıq göstərilir**:

> «Ezamiyyət ləğv edildi, AMMA maşının açarı artıq verilib — maşını qaytarıb
> kassaya «Gəldi» qeyd etdirin.»

⚠️ Bu mətn `(ok: true, error: "...")` şəklində qayıdır — yəni **uğurda da mətn
gəlir**. Hər iki controller onu göstərir; udulsa istifadəçi «ləğv edildi» görər,
maşın isə çöldə qalar və heç kim xəbər tutmaz.

**Ləğvin İKİ giriş nöqtəsi var, hər ikisində qayda eynidir:**

| Giriş nöqtəsi | Metod |
|---|---|
| İşçi öz müraciətini ləğv edir | `EzamiyyetService.LegvEtAsync` |
| Rəhbər/HR ləğv edir | `EzamiyyetService.RehberHrLegvEtAsync` |

Birini unutsaq xəta **yalnız o yolda** təzahür edər (CLAUDE.md — məzuniyyət
routing tələsi ilə eyni nümunə).

---

## TRANZAKSİYA VƏ ROLLBACK (KRİTİK)

Ezamiyyətin təsdiqi ilə maşın müraciətinin yaranması **atomikdir**
(`_uow.BeginTransactionAsync()`). Ayrı-ayrı yazılsa: işçi təsdiq görər, kassada
isə heç nə olmaz — və bu, heç bir yerdə iz qoymaz.

**⚠️ ROLLBACK YADDAŞI GERİ QAYTARMIR.** `entity` izlənilir (tracked) və
dəyişdirilmiş vəziyyətdə qalır; eyni sorğuda sonrakı istənilən `YaddaSaxlaAsync`
(məs. bildiriş yazılışı) onu **bazaya yenidən yazardı**. Ona görə `Geriye()`
lokal funksiyası əvvəlki 5 sahəni əl ilə bərpa edir.

Maşın müraciəti yaradıla bilmirsə (məs. maşın bu arada təmirə düşüb)
**ezamiyyət də təsdiqlənmir** — rəhbər səbəbi görüb başqa qərar versin.

---

## Dublikat qoruması

`EzamiyyetdenYaratAsync` eyni ezamiyyət üçün ikinci sətir yaratmır — təsdiq
düyməsi iki dəfə basıla bilər. Ləğv/imtina edilmiş sətir saymır (o, qəsdən
bağlanıb, yenisi yazıla bilər).

---

## Toxunulan fayllar

**Domain**
- `FinNex.Domain/Entities/HR/EzamiyyetMuraciet.cs` — `MasinId` + nav
- `FinNex.Domain/Entities/Avtopark/MasinMuraciet.cs` — `EzamiyyetMuracietId` + nav

**DataAccess**
- `DataAccess/Contexts/AppDbContext.cs` — 2 FK (`NoAction`/`Restrict`) + indeks
- `DataAccess/Migrations/20260901000000_EzamiyyetAvtoparkBaglantisi.cs` — **yeni**

**Application**
- `DTOs/HR/Ezamiyyet/EzamiyyetMuracietCreateDto.cs` — `MasinId`
- `DTOs/HR/Ezamiyyet/EzamiyyetMuracietListDto.cs` — `MasinId`, `MasinAdi`, `MasinVar`
- `Interfaces/Avtopark/IMasinMuracietService.cs` — 3 yeni metod
- `Services/Avtopark/MasinMuracietService.cs` — `MasinSecimiYoxlaAsync`,
  `EzamiyyetdenYaratAsync`, `EzamiyyetLegvindeMasiniLegvEtAsync`
- `Services/HR/EzamiyyetService.cs` — `IMasinMuracietService` inject, yaratma
  yoxlaması, təsdiq tranzaksiyası, hər iki ləğv yolu, 4 sorğuya `Include(x => x.Masin)`

**UI**
- `Areas/User/Controllers/EzamiyyetMuracietController.cs` — `IMasinService`,
  `DoldurMasinlarAsync`, ləğv mesajı
- `Areas/HR/Controllers/EzamiyyetController.cs` — ləğv mesajı
- `Areas/User/Views/EzamiyyetMuraciet/Create.cshtml` — maşın bölməsi + JS
- `Areas/User/Views/EzamiyyetMuraciet/Index.cshtml` — maşın nişanı
- `Areas/User/Views/Tesdiq/EzamiyyetDetal.cshtml` — **təsdiq düyməsindən əvvəl** maşın sətri
- `Areas/HR/Controllers/EzamiyyetController.cs` — `GetMuracietler` JSON proyeksiyasına
  `MasinVar`/`MasinAdi` (proyeksiya AÇIQDIR — sahə yazılmasa JS-də `undefined` olur, xətasız)
- `wwwroot/js/hr/hr-ezamiyyet.js` — HR/Rəhbər modalında maşın sətri + siyahı sətrində nişan

### ⚠️ TƏSDİQİN İKİ EKRANI VAR

Ezamiyyəti təsdiqləmək üçün **iki ayrı ekran** eyni servis metodunu
(`EzamiyyetService.RehberTesdiqAsync`) çağırır:

| Ekran | Fayl |
|---|---|
| İşçi portalı → Təsdiq | `Areas/User/Views/Tesdiq/EzamiyyetDetal.cshtml` |
| HR → Ezamiyyət İzləmə (modal) | `wwwroot/js/hr/hr-ezamiyyet.js` + `Areas/HR/Views/Ezamiyyet/Index.cshtml` |

Təsdiq ekranına məlumat əlavə edəndə **hər ikisini birlikdə yenilə**. 01.09.2026-da
yalnız birincisi yenilənmişdi — rəhbər HR ekranından təsdiqləyəndə maşından xəbərsiz
qalırdı (istifadəçi tapdı: «burda maşınla gedəcəyi bildirilmir?»).

---

## Checkbox POST-a GETMİR

Formadakı «Xidməti maşınla gedirəm» checkbox-u yalnız bölməni açıb-bağlayır və
serverə **göndərilmir**. Həqiqətin yeganə mənbəyi `MasinId`-dir.

İşarə götürüləndə `<select>` də **təmizlənir** — yoxsa gizli qalan sahə köhnə
maşını göndərər və ezamiyyət təsdiqlənəndə gözlənilməz maşın müraciəti yaranardı
(heç bir xəta vermədən — CLAUDE.md «Şərtli render + default parametr» tələsi).

---

## Yoxlama siyahısı (build-dən sonra)

1. **Maşınsız ezamiyyət** — köhnə davranış, heç nə dəyişməməlidir.
2. **Maşınlı ezamiyyət → təsdiq** → Avtopark → Kassa siyahısında sətir görünür,
   statusu «Təsdiqlənib», məqsəd = ezamiyyətin başlığı, marşrut = məkan.
3. **Rəhbərin təsdiq ekranı** — maşın sətri təsdiqdən ƏVVƏL görünür.
4. **İmtina** — maşın müraciəti YARANMAMALIDIR.
5. **Təsdiqdən sonra ləğv (açar verilməyib)** — maşın müraciəti «Ləğv edildi».
6. **Təsdiqdən sonra ləğv (açar verilib, «Çıxıb»)** — maşın sətri TOXUNULMUR,
   ekranda xəbərdarlıq çıxır.
7. **Tam gün ezamiyyət** — `PlanBaslama` saatı 09:00 (yaxud `IsParametri`-dəki dəyər).
8. **Təsdiq düyməsinə iki dəfə basmaq** — ikinci maşın müraciəti yaranmamalıdır.
