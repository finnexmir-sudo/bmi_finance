# Mail — roldan icazəyə keçid (01.09.2026)

**Status:** kod yazılıb, **build EDİLMƏYİB** (mühitdə `dotnet` yoxdur) + **SQL işlədilməyib**

---

## İstifadəçi tələbi

> «Məndə mail tərəfi yalnız rəhbərdədir. Mən bunu admin tərəfində bəzi işçilərə
> icazələri vermək istəyirəm.»

## Yeni admin səhifəsi YAZILMADI — lazım deyildi

Layihədə hazır icazə sistemi var və Avtopark onu artıq işlədir:

| Nə | Harada |
|---|---|
| İcazə siyahısı | Admin → **Sistem İcazələri** (`SistemIcazeController`) |
| Kimə hansı icazə | Admin → **İstifadəçi İcazələri** (`UserPermissionsController`) |
| Kodda yoxlama | `[Icaze("kod")]` → `IUserPermissionService.HasPermissionAsync` |

Ona görə iş **bir icazə kodu + mövcud yoxlamaların rolden koda keçirilməsi**
ilə bitdi.

---

## Qərarlar (istifadəçi)

| Sual | Qərar |
|---|---|
| İcazə sayı | **Bir** — `mail_istifade` (həm qutu, həm Profildəki SMTP ayarı) |
| Rəhbər rolu | **Avtomatik saxlanılır** — şərt «Admin VƏ YA Rəhbər VƏ YA icazə» |
| SMTP-ni kim yazır | **İşçi özü**, Profil səhifəsində (indiki kimi) |

Bir icazə seçildi, çünki qutu onsuz da **şəxsidir** (`GelenMail.SahibUserId`) və
SMTP olmadan işləmir — ikiyə bölmək praktik fərq yaratmır, yalnız Admin-də iki
xana artırardı.

---

## Şərt BEŞ YERDƏ var — birlikdə dəyişməlidir

| Yer | Fayl |
|---|---|
| **Server qoruması** | `FinNex.UI/Filters/MailIcazesiAttribute.cs` |
| Sidebar + üst panel ikonu | `Areas/User/Views/Shared/_UserLayout.cshtml` → `hasMail` |
| Profil kartı | `ProfileController` → `ViewBag.MailIcazesiVar` |
| Profildəki **script bloku** | `Profile/Index.cshtml` → `@section Scripts` |
| **Fon sinxronizasiyası** | `GelenMailSyncService.GetAllImapCredentialsAsync` |

Biri köhnə qalsa: ya istifadəçi linki görüb **403** alar, ya da icazəsi olduğu
halda linki **tapa bilməz**. Heç bir xəta çıxmaz.

### ⚠️ FON SERVİSLƏRİ ROLA GÖRƏ SİYAHI QURUR — UNUDULUR

Bir funksiyanı roldan icazəyə keçirəndə **fon servislərini də yoxla**. Onların
HTTP istifadəçisi yoxdur, ona görə işçiləri **rol üzrə sadalayırlar**
(`GetUsersInRoleAsync`) və dəyişiklikdən kənarda qalırlar.

Real hadisə (01.09.2026): `mail_istifade` icazəsi verilmiş işçi
- səhifəni **AÇA bilirdi** ✔
- «Yenilə» düyməsi **işləyirdi** ✔ (o, cari istifadəçi ilə işləyir)
- amma fon servisi onun qutusunu **heç vaxt yoxlamırdı** ✘

Nəticə: mail **əl ilə** gəlirdi, bildiriş isə **heç vaxt** gəlmirdi. Xəta yox
idi; log yalnız «GelenMail: heç bir istifadəçidə IMAP məlumatları tapılmadı»
yazırdı — o da yanıldıcıdır, çünki məlumat VAR idi, sadəcə sorğu onu görmürdü.

### İki sinxronizasiya yolu var — biri bildiriş yaratmır

| Yol | Mail gətirir | Bildiriş yaradır |
|---|---|---|
| «Yenilə» düyməsi (`GelenMailController.ManualSync`) | ✔ | ✘ **yox** |
| Fon servisi (hər 5 dəq) | ✔ | ✔ |

Bu, **qəsdəndir** — özün «Yenilə»yə basmısansa nəticəni onsuz da ekranda
görürsən. AMMA sınaq edərkən bunu bil: «Yenilə»yə bassan həmin məktub artıq
bazaya düşür və fon servisi onu `count = 0` sayır — o məktub üçün bildiriş
**heç vaxt gəlməyəcək**. Sınağı ikinci məktubla, düyməyə basmadan et.

### İcazə kodu TƏK YERDƏDİR

`FinNex.Domain/IcazeKodlari.cs` — `RoleNames` ilə eyni məntiq. Application
layihəsi UI-a istinad edə bilmir, kod isə hər ikisinə lazımdır. Literal kimi
yazılsa biri dəyişəndə o biri səssizcə köhnə qalar.

---

## Sidebar linki Rəhbər blokundan ÇIXARILDI

«Gələn Maillər» linki `@if (isRehberRol || isAdmin)` blokunun **içində** idi.
Yəni `mail_istifade` icazəsi verilmiş adi işçi üst paneldəki ikonu görər, sidebar
linkini isə **görməzdi**. Link öz `@if (hasMail)` blokuna köçürüldü.

---

## Bağlanan təhlükəsizlik boşluğu

`ProfileController.MailAyarlariYenile` və `MailSina` **heç bir rol/icazə
yoxlamasından keçmirdi** — yalnız Profil səhifəsindəki kart gizlədilirdi.
Yəni icazəsi olmayan istifadəçi birbaşa POST göndərib özünə SMTP qura bilər,
gecə sinxronizasiyası isə onun poçtunu çəkməyə başlayardı.

**Kartı gizlətmək qoruma deyil.** Hər iki endpoint indi `[MailIcazesi]` altındadır.

---

## `IcazeAttribute` — yeni `ElaveRol` xassəsi

```csharp
[Icaze("mail_istifade", ElaveRol = RoleNames.Rehber)]
```

Köhnə rolu birdən kəsmək mövcud istifadəçiləri **build-dən dərhal sonra**
funksiyadan məhrum edir. Bu xassə keçidi yumşaq edir. Default `null` —
mövcud `[Icaze]` istifadələrinə (Avtopark) heç bir təsiri yoxdur.

---

## Nə etməli

1. **VS-də build.**
2. SQL Server-də işlət: `docs/sql/mail/01_Mail_Istifade_Icazesi.sql`
   (idempotentdir, təkrar işlətmək təhlükəsizdir; heç kimə icazə VERMİR).
3. Admin panel → **İstifadəçi İcazələri** → işçi → **«Mail istifadəsi»** → Allowed.
4. İşçi öz Profil səhifəsində poçtunu və şifrəsini yazır → «Sınaq Maili» ilə yoxlayır.

## Yoxlama siyahısı

1. **Rəhbər** — hər şey əvvəlki kimi işləyir (heç nə itirməyib).
2. **İcazəsiz adi işçi** — sidebar linki YOX, üst panel ikonu YOX, Profildə kart YOX;
   `/User/GelenMail` ünvanını əl ilə yazsa **403**.
3. **İcazə verilmiş işçi** — link və kart görünür, öz qutusunu görür.
4. **İcazə verilmiş işçi rəhbərin məktublarını GÖRMÜR** (qutu şəxsidir).
5. **Admin** — icazəsiz də girir.
6. İcazə geri alınanda link dərhal itir (növbəti səhifə yüklənişində).

---

## Gələcək: Rəhbər rolunu tamamilə kəsmək

`MailIcazesiAttribute`-dəki `ElaveRol` sətri silinir — **amma əvvəlcə** cari
rəhbərlərə bu icazə verilməlidir, yoxsa maili dərhal itirərlər. Hazır SELECT
SQL faylının sonundadır.
