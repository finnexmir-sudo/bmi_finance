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

## Şərt ÜÇ YERDƏ var — birlikdə dəyişməlidir

| Yer | Fayl |
|---|---|
| **Server qoruması** | `FinNex.UI/Filters/MailIcazesiAttribute.cs` |
| Sidebar + üst panel ikonu | `Areas/User/Views/Shared/_UserLayout.cshtml` → `hasMail` |
| Profil kartı | `ProfileController` → `ViewBag.MailIcazesiVar` |

Biri köhnə qalsa: ya istifadəçi linki görüb **403** alar, ya da icazəsi olduğu
halda linki **tapa bilməz**. Heç bir xəta çıxmaz.

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
