# Avtopark — Yol vərəqəsi + `avtopark_idare` icazəsi

**Tarix:** 20.08.2026 · **Status:** kod yazılıb, **build edilməyib** (mühitdə `dotnet` yoxdur)

---

## 1. `avtopark_idare` icazəsi

### Əvvəl necə idi

`Avtopark → Maşınlar` və `Müddətlər` səhifələri `[Authorize(Roles = Admin)]` idi.
Yəni təsərrüfat işçisinə maşın kartını açmaq üçün **tam Admin** vermək lazım
gəlirdi — o isə maaşdan sistem ayarlarına qədər hər şeyi açır.

### İndi

| Kim | Girir? |
|---|---|
| **Admin** | həmişə (icazə lazım deyil) |
| `avtopark_idare` icazəsi verilmiş işçi | **bəli** — yalnız Avtopark idarəetməsi |
| Digərləri | xeyr |

Eyni yanaşma layihədə artıq işlənir: Mühasibat paneli `muhasibat_dashboard_bax`
icazəsi ilə paylaşılır.

### Fayllar

| Fayl | Nə |
|---|---|
| `FinNex.UI/Areas/Avtopark/AvtoparkIdareIcazesiAttribute.cs` | filtr — Admin **və ya** icazə |
| `MasinController.cs`, `MuddetController.cs` | `[Authorize] [AvtoparkIdareIcazesi]` |
| `_UserLayout.cshtml` → `hasAvtoparkIdare` | sidebar şərti — filtrlə **eyni** |
| `docs/sql/avtopark/03_Avtopark_Idare_Icazesi.sql` | `Permissions` sətri |

> ⚠️ Sidebar şərti ilə filtr şərti **eyni qalmalıdır**. Biri dəyişsə istifadəçi
> ya linki görüb 403 alacaq, ya da icazəsi olduğu halda linki görməyəcək.

### Quraşdırma

1. SSMS-də `docs/sql/avtopark/03_Avtopark_Idare_Icazesi.sql` işlət (idempotent,
   əvvəl `SELECT` ilə nə dəyişəcəyini göstərir).
2. Admin panel → **Users** → işçi → **İcazələr** → «Avtopark idarəetməsi» → Allowed.

---

## 2. Yol vərəqəsi

**Yer:** `Avtopark → Yol vərəqəsi` (`/Avtopark/YolVereqesi`)
**Giriş:** `[Authorize]` — **hər işçi**. Sənəd sürücünün özündə olmalıdır,
ona görə rol şərti qoyulmadı.

### Necə işləyir

Maşın (yalnız **Aktiv**) + sürücü + dövr seçilir → Word sənədi yüklənir.
Sürücü boş qalarsa maşının **təhkim olunmuş sürücüsü** yazılır (həm formada
JS ilə, həm serverdə — JS söndürülsə də nəticə düzgündür).

### Şablon

```
C:\FinNex_DMS\hesabat-sablonlari\avtopark\Yol_vereqesi.docx
```

Repodakı nüsxə: **`docs/sablon/avtopark/Yol_vereqesi.docx`** — bu faylı həmin
qovluğa köçürmək lazımdır. Şablon tapılmasa səhifə açıq xəta verir və **tam
yolu göstərir** (səssiz boş fayl yoxdur).

Doldurulan tokenlər (`KreditWordService.Doldur` ilə — run-lara bölünmüş
tokenləri də tutur):

| Token | Mənbə |
|---|---|
| `{dovr}` | formadakı iki tarixdən cümlə qurulur |
| `{marka}` | `Masin.Marka` + `Model` |
| `{nomre}` | `Masin.DovletNomresi` |
| `{surucu}` | formadakı ad, boşdursa `TehkimSurucuAdi` |
| `{rehber}` | `appsettings.json → Emr:MudirAd` (əmrlərdəki ilə eyni mənbə) |

### ÇIXIŞ / QAYIDIŞ — TOXUNULMADI

«Qaracdan çıxış vaxtı: **00:01**» və «Qaraca qayıdan vaxt: **24:00**» sətirləri
şablonda **sabit mətndir**, token deyil. İstifadəçi qərarı: «çıxış qayıdış
olduğu kimi qalır». Dəyişmək lazım olsa Word şablonunun özündə düzəldilir —
koda toxunmağa ehtiyac yoxdur.

### Orijinal .docx-dən iki fərq

1. **Dövr cümləsinin formatı.** Orijinalda başlanğıc sözlə («05 yanvar 2026»),
   bitmə isə rəqəmlə («31.12.2026») yazılmışdı — bir cümlədə iki format.
   İndi hər ikisi sözlə: «05 yanvar 2026-cı ildən 31 dekabr 2026-cı il tarixədək».
2. **Sıra şəkilçisi.** Orijinalda bitmə tarixi «2026-**ci** il» idi; düzgünü
   «2026-**cı**»dır (altı → ı). Şəkilçi `IlSekilcisi()` ilə hesablanır
   (2027 → «ci», 2030 → «cu», 2033 → «cü»).

Orijinal yazılış lazımdırsa dəyişiklik **yalnız `DovrMetni()` metodundadır**.

---

## 3. YOXLANMAYAN

- ⚠️ **Build edilməyib** — mühitdə `dotnet` yoxdur.
- Şablonun doldurulmuş nəticəsi (şrift, mərkəzləmə) real Word-də yoxlanmalıdır.
- `avtopark_idare` icazəsi real istifadəçi üzərində sınanmalıdır: icazə verilən
  işçi Maşınlar səhifəsini görməli, verilməyən görməməlidir.
