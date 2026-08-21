# Avtopark — müraciətdə vaxt sadələşdirildi (21.08.2026)

**Status:** kod yazılıb, **build edilməyib** (mühitdə `dotnet` yoxdur)

---

## İstifadəçi qərarı

> «Bitmə bilinmir axı — nə vaxt qayıtdı, kassada qayıtdı yazılacaq.»
> «Tarix bugünkü olar avtomatik, saatı ayrıca yazsın, arasına `:` avtomatik qoy,
> təqvim bizdə olandan seç — misal məzuniyyətdə olan.»
> Göstərici barədə: **«Göstəricini tamamilə çıxarmaq»**.

## Forma — əvvəl / indi

| Əvvəl | İndi |
|---|---|
| Başlama (`datetime-local`) | **Tarix** (`type="date"`, bugünlə hazır) |
| **Bitmə** (`datetime-local`) | **Saat** (`10:00` maskası — rəqəm yazılır, `:` avtomatik) |

Saat maskası İcazə modulundakı «Başlama saatı» ilə **eyni davranışdadır**
(`user_create_icaze.js` → `maskSaat`). Kod ora bağlanmadı, Create səhifəsində
təkrarlandı — o fayl İcazə formasının hesablamalarına bağlıdır və burada
olmayan elementlərə görə xəta verərdi.

Servis ikisini birləşdirir: `PlanBaslama = Tarix.Date + Saat`.
Format DTO-da `RegularExpression` ilə, **servisdə də təkrar** yoxlanılır —
forma birbaşa POST edilə bilər.

## Maşın ikiqat götürülə bilərmi? — XEYR

İnterval yoxlaması (`TarixKonfliktiTapAsync`) götürüldü, çünki planlaşdırılan
bitmə olmadan interval qurulmur. **Qoruyucu isə itmədi — sadəcə başqa
mərhələdədir:**

```csharp
// MasinMuracietService.CixdiAsync — Qoruyucu 4.1
if (acig != null)
    return Result.Fail($"Bu maşın hazırda çöldədir — {Ad(acig.Isci)} " +
                       $"({acig.CixisTarixi:dd.MM.yyyy HH:mm}-dan). Əvvəlcə «Gəldi» qeyd edilməlidir.");
```

Yəni açar verilən an maşın tutulur; «Gəldi» qeyd edilənə qədər ikinci açar
verilmir.

**NƏ DƏYİŞDİ:** iki nəfər eyni maşına eyni günə müraciət edib hər ikisi
təsdiqlənə bilər. İkinci adam bunu **müraciət anında yox, kassada** öyrənir.
Bu, açıq qərardır — data pozulmur, yalnız xəbər gec çatır.

## «Gecikib» göstəricisi — ÇIXARILDI

`PlanBitme < indi` şərtinə bağlı idi, bazası qalmadı. Silindiyi yerlər:

| Fayl | Nə getdi |
|---|---|
| `Tesdiq/AcigCixislar.cshtml` | «Planlaşdırılan bitmə» sütunu (`<th>` + `<td>`), `av-warn` sətir rəngi, `gecikib` nişanı |
| `Kassa/Index.cshtml` | `av-item--warn`, `gecikib` nişanı, plan aralığındakı bitmə |
| `Muraciet/Index`, `Tesdiq/Index`, `Kassa/Jurnal` | `→ bitmə` alt sətri |

`av-warn` / `av-item--warn` CSS sinifləri faylda qaldı — başqa yerdə lazım ola bilər.

## Baza

`MasinMuracietler.PlanBitme` → **nullable**
(`DataAccess/Migrations/20260821000000_AvtoparkPlanBitmeNullable.cs`).

- **Sütun silinmədi** — 21.08.2026-dan əvvəlki qeydlərdə real plan var,
  jurnal tarixçəsi pozulmamalıdır.
- Migration **mövcud dataya toxunmur**: nə `UPDATE`, nə `DELETE` — yalnız
  `NOT NULL` → `NULL`.
- `Down` geri qaytarmadan əvvəl boşlara `PlanBaslama` yazır, yoxsa
  `ALTER COLUMN` sınardı.

Jurnal süzgəci də dəyişdi: əvvəl interval kəsişməsinə baxırdı
(`PlanBaslama < son && PlanBitme >= bas`) — yeni qeydlərdə `PlanBitme` `null`
olduğu üçün onları **səssizcə atardı**. İndi tək nöqtəyə görədir:
`PlanBaslama >= bas && PlanBaslama < son`.

## Dəyişən fayllar

| Fayl | Nə |
|---|---|
| `FinNex.Domain/Entities/Avtopark/MasinMuraciet.cs` | `PlanBitme` → `DateTime?` + sənəd |
| `FinNex.Application/DTOs/Avtopark/MasinMuracietDtos.cs` | Create-də `Tarix` + `Saat`; List-də `PlanBitme` nullable |
| `FinNex.Application/Services/Avtopark/MasinMuracietService.cs` | `SaatiDeqiqeyeCevir`, konflikt bloku çıxarıldı, jurnal süzgəci, bildiriş mətnləri |
| `FinNex.UI/Areas/Avtopark/Controllers/MuracietController.cs` | default `Tarix`/`Saat` |
| `Views/Muraciet/Create.cshtml` | forma + saat maskası JS |
| `Views/Muraciet/Index`, `Tesdiq/Index`, `Tesdiq/AcigCixislar`, `Kassa/Index`, `Kassa/Jurnal` | bitmə göstərişi və «gecikib» |
| `DataAccess/Migrations/20260821000000_...` | sütun nullable |

## Bərpa

- **İnterval yoxlaması:** `MasinMuracietService`-də `TarixKonfliktiTapAsync`,
  `KonfliktMesaji` və `TutanStatuslar` bərpa edilir, `YaratAsync` ilə
  `TesdiqEtAsync`-də çağırışlar geri qoyulur (git: bu commitdən əvvəlki nüsxə).
- **Bitmə sahəsi:** DTO-ya `PlanBitme` qaytarılır, formaya input əlavə olunur.

## YOXLANMAYAN

- ⚠️ **Build edilməyib.**
- Migration real bazada işlədilməlidir (`Database.Migrate()` startup-da avtomatik).
  Əvvəlcə nəyin dəyişəcəyini görmək üçün:
  ```sql
  SELECT COUNT(*) AS Setir, COUNT(PlanBitme) AS PlanBitmesiDolu
    FROM MasinMuracietler WHERE ISNULL(Silinib,0) = 0;
  ```
  Migration bu rəqəmlərin **heç birini dəyişmir** — yalnız sütun `NULL` qəbul edir.
- Yeni müraciət yazılıb `PlanBaslama`-nın düzgün (tarix + saat) düşdüyü
  yoxlanmalıdır.
- Kassada eyni maşına ikinci açarın verilmədiyi yoxlanmalıdır (Qoruyucu 4.1).
