# AML → Hesab üzrə sorğu — FinNex modulu

**Tarix:** 19.08.2026 · **Status:** kod yazılıb, **build edilməyib** (bu mühitdə `dotnet` yoxdur)

BMI-dəki `AML → Hesab üzrə sorğu` forması (`BMI/BMI/AML/frmhesabsorgu.cs`)
FinNex-ə köçürüldü.

---

## 1. Harada açılır

**Risk → Hesab üzrə sorğu** (`/Risk/Aml`)

Risk sahəsi seçildi, çünki top-nav-dakı «Departamentlər → **Risk**» maddəsinin
alt yazısı onsuz da «Risk / **AML** hesabatları»dır. Ayrıca **«Hesabatlar»**
tabı isə artıq HR hesabatı (`User/Hesabat`) üçün işlənir — AML-i ora qoysaq
iki fərqli şey bir ad altında qalardı.

> Ayrıca «Hesabatlar» mərkəzi (departament → dövrilik → hesabat) qurulanda
> bu səhifə oradan da linklənə bilər — controller/servis dəyişmir.

---

## 2. Fayllar

| Qat | Fayl |
|---|---|
| DTO | `FinNex.Application/DTOs/Aml/AmlHesabDtos.cs` |
| İnterfeys | `FinNex.Application/Interfaces/Aml/IAmlHesabatService.cs` |
| Servis | `FinNex.Application/Services/Aml/AmlHesabatService.cs` |
| DI | `FinNex.Application/AddApplicationServices.cs` (Risk sətrinin yanında) |
| Controller | `FinNex.UI/Areas/Risk/Controllers/AmlController.cs` |
| View | `FinNex.UI/Areas/Risk/Views/Aml/Index.cshtml` |
| Giriş nöqtəsi | `FinNex.UI/Areas/Risk/Views/Dashboard/Index.cshtml` — «AML» kartı |
| SQL (test) | `docs/sql/aml/Hesab_Uzre_Sorgu_Yeni.sql`, `…_Huquqi.sql` |
| SQL (quraşdırma) | `docs/sql/aml/90_AML_OracleSorgular.sql` |

---

## 3. İŞƏ SALMAZDAN ƏVVƏL — İKİ ADDIM

### 3.1 Oracle sorğularını bazaya sal

SSMS-də işlət: **`docs/sql/aml/90_AML_OracleSorgular.sql`**

Üç sətir əlavə edir (idempotent):

| SorguAdi | Nə |
|---|---|
| `AML_HESAB_SORGU_FIZIKI` | «Fiziki şəxs» radio düyməsi |
| `AML_HESAB_SORGU_HUQUQI` | «Sahibkar / hüquqi şəxs VÖEN» radio düyməsi |
| `AML_HESAB_SORGU_QALIQ` | Şapka: hesabın adı + giriş/son qalıq |

Script sonda `SELECT` ilə nə əlavə olunduğunu göstərir.
Sorğu tapılmasa səhifə açıq xəta verir (səssiz boş nəticə YOX).

**Tokenlər:** `{HESAB}`, `{TARIX1}`, `{TARIX2}` — servis icradan əvvəl əvəz edir.
Admin sorğunu redaktə edəndə tokenləri silməməlidir.

### 3.2 Excel şablonunu yerinə qoy

```
C:\FinNex_DMS\hesabat-sablonlari\aml\AML_Hesab.xlsx
```

Bu, **yeni** 47 sütunlu şablondur (`vahidhesabchixarishi1.xlsx`).
Şablon yoxdursa hesabat dayanmır — sadə cədvəl generasiya olunur, amma
başlıqlar və formatlama olmur.

---

## 4. Nə hansı qatdadır

BMI-də SQL nəticəsi `dataGridView`-ə düşür, sonra `exceleat2()` içində
**xanaların üzərinə yazılırdı**. FinNex-də həmin çevirmələrin **hamısı
SQL-in üst qatındadır** — bir məntiq iki yerdə saxlanılmır:

| Sütun | BMI-də harada | FinNex-də harada |
|---|---|---|
| F Çatdırılma kanalı | C# `Cells[3]`, 6 `if` | SQL `CASE` (sıra TƏRSİNƏ — C#-da sonuncu üstələyirdi) |
| I Göndərənin adı | C# `Cells[4]` + `Hes_adlari` dövrü | SQL — `licsch` skalyar alt sorğusu |
| S Ölkə (25019… hesabı) | C# `Cells[11]` | SQL `CASE` |
| W/X/Y Alan tərəfin adı/VÖEN/FİN | C# `Cells[16..18]` | SQL — `licsch` + `regnom` |
| Z Hesab növü | C# `Cells[19]` | SQL `CASE` |
| AM Valyuta kodu | C# dövrdən SONRA `Cells[24] → 31` | SQL — `alan_valuta` |

Servis yalnız: parametrləri yoxlayır → sorğunu `OracleSorgular`-dan alır →
tokenləri əvəz edir → `IOracleService` ilə icra edir. **Heç bir hesablama yoxdur.**

---

## 5. Təhlükəsizlik və limitlər

- **Oracle-a YALNIZ SELECT** gedir (`OracleService.YalnizSelect`; sorğu `with`
  ilə başlayır — icazəlidir).
- Hesab nömrəsi sorğuya **mətn kimi** yerləşdirilir (bind dəyişəni yoxdur),
  ona görə servis **yalnız rəqəm** olduğunu yoxlayır — apostrof/boşluq keçmir.
- Sətir limiti **20 000**. Limitə çatanda ekranda açıq xəbərdarlıq çıxır
  («nəticə kəsildi, dövrü daraldın») — səssiz kəsilmə yoxdur.

---

## 6. YOXLANMAYAN / QALAN

- ⚠️ **Build edilməyib** — bu mühitdə `dotnet` yoxdur. İmzalar üç qatda əl ilə
  tutuşdurulub (interfeys / implementasiya / controller), amma kompilyator
  təsdiqi yoxdur.
- ⚠️ **Sorğunun özü Oracle-da tam icra edilməyib** — yeni sütun adları
  (`doc_vnesh_*.ID`, `.PLAT_SYSTEM`, `postupl.KREDIT_INN`, `muxbir_hesab.VOEN`)
  təsdiq gözləyir.
- Şablonun **sütun genişlikləri və 12-ci sətir formatı** yoxlanmalıdır —
  47 sütun köhnə 37-lik şablondan genişdir.
- «Çatdırılma kanalı» qaydaları BMI-dən **olduğu kimi** köçürülüb; real data
  ilə bir neçə sətir üzərində tutuşdurulmalıdır.
