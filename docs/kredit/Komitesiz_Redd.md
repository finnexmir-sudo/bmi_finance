# Kredit Müraciəti — Komitəsiz Rədd (02.09.2026)

## Niyə

BMI-dəki `frm müraciətlər` iş prinsipi:

> müştəri müraciət edirdi (onlayn və ya oflayn), işçi onu sistemə yazırdı ki
> razılıq verildi və ya etiraz edildi, komitənin qərarını — və ya **komitəyə
> getmədən etiraz olubsa onu** — yazırdı.

Son hissə FinNex-də **yox idi**. `KreditMuracietStatus.ReddEdilib` statusunu
yalnız `KreditQerarService.QebulEtAsync` yaza bilirdi, o da tələb edirdi:

- rədd edən **komitə üzvü** olsun (`CanSeeKomitePages`);
- **protokol nömrəsi** boş olmasın;
- qərar tarixi;
- ən azı **bir aktiv** `KomiteUzvu` imzası.

Baxan işçinin əlində cəmi iki keçid var idi — «baxmağa götür» və «komitəyə
göndər»; üçüncüsünə cəhd `«Bu status keçidi İşçi üçün icazəli deyil.»` ilə
qarşılanırdı (`KreditMuracietController.IsciQiymetlendir`).

Nəticədə MKR-i pis çıxan, sənədi natamam olan müştərinin müraciəti ya
**uydurma protokolla** komitəyə göndərilməli, ya da **«Yoxlanılır» statusunda
əbədi qalmalı** idi.

## İstifadəçi qərarları (02.09.2026)

| Sual | Qərar |
|---|---|
| Kim rədd edə bilər? | **Baxan işçi** (`CanSeeIsciPages`) |
| Kim geri qaytara bilər? | **Rəddi yazan işçi** (+ Admin — aşağı bax) |
| «Komitə Qərarları» siyahısında görünsün? | **YOX** — o, komitə jurnalıdır, təmiz qalır |
| Səbəb necə yazılsın? | **Siyahıdan seçim**, lazım olsa artırılsın |

## AYRICA STATUS YOXDUR — `Qerar == null` ayırd edir

Yeni status **əlavə edilmədi** və ehtiyac da yoxdur:

```
Komitəsiz rədd  →  Status == ReddEdilib  VƏ  Qerar == null
Komitə rəddi    →  Status == ReddEdilib  VƏ  Qerar != null
```

`KreditQerar` sətri komitəsiz rəddə **yaradılmır** — «Komitə Qərarları»
səhifəsi (`Qerarlar`) yalnız `KreditQerar` sətirlərini göstərdiyi üçün işçi
rəddi ora **avtomatik düşmür**. İstifadəçinin (a) variantı budur.

⚠️ Yeni status əlavə etmək istəsən: `KreditMuracietStatus` enum-una sətir
əlavə etmək **kifayət etmir** — `Index.cshtml` status sekmeleri,
`ViewBag.StatusSaylari` (`KreditMuracietController` sətir ~129) və
`Detail.cshtml`-dəki `StatusText`/`StatusCls` funksiyaları da yenilənməlidir.

## `ReddEdilib` statusunun İKİ YAZICISI VAR

Bu, ən vacib qaydadır. Statusa toxunanda **hər ikisini** tutuşdur:

| Yazıcı | Yer | Şərt |
|---|---|---|
| Komitə qərarı | `KreditQerarService.QebulEtAsync` | protokol + aktiv üzv imzası, tranzaksiya içində |
| **Komitəsiz rədd** | `KreditMuracietService.KomitesizReddEtAsync` | səbəb məcburi, `Qerar` olmamalıdır |

## Qoruyucular

`KomitesizReddEtAsync`:
- status yalnız `Yeni` və ya `Yoxlanılır` ola bilər — **komitəyə göndərilmiş
  müraciəti işçi rədd edə bilməz**, o mərhələdə qərar komitənindir;
- `Qerar != null` olarsa istisna — data əl ilə dəyişdirilibsə də komitə qərarı qorunur;
- səbəb mövcud **və aktiv** olmalıdır.

`ReddiGeriQaytarAsync`:
- status `ReddEdilib` olmalıdır;
- **`Qerar != null` olarsa istisna** — komitənin protokolla verdiyi qərarı bir
  işçinin düyməsi ilə ləğv etmək olmaz;
- `adminMi == false` olduqda `ReddEdenIsciId == isciId` şərti;
- status geri qayıdır: `BaxanIsciId` varsa `Yoxlanılır`, yoxsa `Yeni`;
  rədd sahələrinin dördü də təmizlənir.

**Admin niyə əlavə edildi:** Admin onsuz da bütün müraciəti **silə** bilir
(`Delete`, yalnız Admin). Rəddi geri qaytara bilməməsi, amma sətri tamamilə
silə bilməsi uyğunsuz olardı. Rəddi yazan işçi işdən çıxsa qeyd də kilidli
qalardı. İstəsəniz `ReddiGeriQaytarAsync`-dəki `adminMi` şərtini götürmək
kifayətdir — bir sətirdir.

## Səbəb siyahısı — `KreditReddSebebleri`

Enum yox, **cədvəl**: siyahı biznes qərarıdır və artır; enum olsaydı hər yeni
səbəb build + deploy tələb edərdi.

Standart 7 sətir migration ilə yazılır (`migrationBuilder.Sql`, `NOT EXISTS`
ilə idempotent): MKR mənfi · Gəlir kifayət deyil · Sənədlər natamam ·
Girov/təminat uyğun deyil · Şərtlərə uyğun gəlmir · Müştəri özü imtina etdi · Digər.

İdarəetmə: **Admin → Kredit → Rədd səbəbləri** (`/Admin/KreditReddSebebi`),
rol `Admin` və ya `KreditAdmin`.

**SƏBƏB SİLİNMİR, DEAKTİV EDİLİR.** Keçmiş müraciətlər sətrə istinad edir
(`ReddSebebiId` FK); silinsə tarixçə «səbəbsiz» qalar. Deaktiv səbəb rədd
formasında görünmür (`AktivleriGetirAsync`), köhnə qeydlərdə görünməyə davam
edir (`Include(x => x.ReddSebebi)`).

Sərbəst izah üçün ayrıca `ReddQeyd` sahəsi var — **məcburi deyil**. Səbəbin
özü siyahıdandır ki, sonradan hesabat çıxsın («bu ay 40 müraciətdən 12-si
MKR-ə görə rədd olunub»); sərbəst mətndən belə hesabat çıxmır.

## Dəyişən fayllar

| Fayl | Nə |
|---|---|
| `FinNex.Domain/Entities/Kredit/KreditReddSebebi.cs` | **yeni** — açar cədvəli |
| `FinNex.Domain/Entities/Kredit/KreditMuraciet.cs` | 4 sahə: `ReddSebebiId`, `ReddQeyd`, `ReddTarixi`, `ReddEdenIsciId` |
| `DataAccess/Contexts/AppDbContext.cs` | DbSet + 2 FK (`NoAction`) + `Ad` max 200 |
| `DataAccess/Migrations/20260902100000_KreditKomitesizRedd.cs` | **yeni** — cədvəl + 4 sütun + FK + indeks + seed |
| `IKreditMuracietService` / `KreditMuracietService` | 2 metod + `Include(ReddSebebi/ReddEdenIsci)` |
| `IKreditReddSebebiService` / `KreditReddSebebiService` | **yeni** |
| `AddApplicationServices.cs` | DI qeydiyyatı |
| `KreditMuracietController` | `KomitesizRedd`, `ReddiGeriQaytar`, `ViewBag.ReddSebebleri` |
| `Areas/User/Views/KreditMuraciet/Detail.cshtml` | rədd forması + nəticə kartı + geri qaytarma |
| `Areas/Admin/Controllers/KreditReddSebebiController.cs` + `Views/KreditReddSebebi/Index.cshtml` | **yeni** |
| `Areas/Admin/Views/Shared/_AdminLayout.cshtml` | menyu sətri |

## Yoxlama siyahısı (build-dən sonra)

Bu kod **build edilməyib** — mühitdə `dotnet` yoxdur.

1. Proqram başlasın → migration `__EFMigrationsHistory`-yə düşsün:
   `SELECT TOP 5 MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC`
   → `20260902100000_KreditKomitesizRedd` görünməlidir.
   Görünmürsə: `FinNex.UI\Logs\log-<tarix>.txt` → `[Migration XƏTA]`.
2. `SELECT * FROM KreditReddSebebleri` → 7 sətir.
3. Admin → Rədd səbəbləri: yeni səbəb əlavə et, adını dəyiş, deaktiv et/aktivləşdir.
4. «Yeni» statuslu müraciət → Detal → **Komitəsiz rədd** → səbəb seç → rədd et.
   Status «Rədd edilib» olsun, kart səbəbi və adınızı göstərsin.
5. **«Komitə Qərarları» səhifəsinə bax — bu rədd ORADA GÖRÜNMƏMƏLİDİR.**
6. «Rəddi geri qaytar» → status «Yoxlanılır»-a qayıtsın, rədd kartı yox olsun.
7. Başqa işçi ilə gir → geri qaytarma düyməsi **görünməməlidir**
   («Rəddi yalnız onu yazan işçi geri qaytara bilər»).
8. Komitəyə göndərilmiş müraciətdə komitəsiz rədd forması **ümumiyyətlə
   görünməməlidir** (`Status <= Yoxlanilir` şərti).

## Toxunulmayanlar

- Komitə axını (`Komite`, `KomiteDetail`, `KomiteQerar`) — dəyişməyib;
- MKR / AsanFinance / zaminlər / randevu / SMS — dəyişməyib;
- Bildiriş **hələ də göndərilmir** (bütün müraciət modulunda yoxdur) —
  ayrı iş kimi durur;
- SMS **hələ də göndərilmir**, yalnız log yazılır (`KreditSmsService`) —
  gateway qoşulmayıb;
- Təsdiqlənmiş müraciət **hələ də müqaviləyə axmır** — `KreditMuraciet`
  (SQL Server) ilə `KreditMuqavile` (Oracle `odb.licschkre`) arasında bağ yoxdur.
