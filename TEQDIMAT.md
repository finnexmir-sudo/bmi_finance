# FinNex — Müəssisə üçün Kadr və Maliyyə İdarəetmə Sistemi

## Layihənin Təqdimatı

---

## Slayd 1 — Başlıq

**FinNex**
Müəssisə üçün inteqrasiya olunmuş HR və Maliyyə İdarəetmə Sistemi

- Platforma: ASP.NET Core 8.0 (Web)
- Memarlıq: Clean Architecture (Onion)
- Dil: C# / Azərbaycan dili interfeysi

---

## Slayd 2 — Problem və Məqsəd

**Həll etdiyi problemlər:**
- Əməkdaşların vahid bazada idarə olunmaması
- Maaş, məzuniyyət, davamiyyət proseslərinin əl ilə aparılması
- Sənəd dövriyyəsində nəzarət və izləmənin zəifliyi
- Kredit və ödəniş tapşırıqlarının dağınıq idarə edilməsi
- Hesabatların vaxtında və dəqiq hazırlanmaması

**Məqsəd:**
Müəssisənin HR, maliyyə, sənəd dövriyyəsi və daxili kommunikasiya proseslərini bir sistemdə birləşdirmək.

---

## Slayd 3 — Həll: FinNex nə edir?

FinNex — aşağıdakı əsas modulları özündə birləşdirən korporativ sistemdir:

1. **Kadr idarəetməsi (HR)** — işçilər, vəzifələr, departamentlər
2. **Əmək haqqı (Maaş)** — avtomatik hesablama, vergi pillələri, tarixçə
3. **Məzuniyyət və icazə** — balans, növlər, bayram günləri
4. **Davamiyyət** — növbə, ZkTeco biometrik cihaz inteqrasiyası
5. **Performans qiymətləndirmə** — meyarlı, dövri rəylər
6. **Təlim və sertifikatlar**
7. **Sənəd dövriyyəsi** — şablonlar, fayllar, audit log
8. **Kredit modulu** — müraciət, komitə, zamin, qərar
9. **Ödəniş tapşırıqları** — banklar, müştərilər, hesablar
10. **Daxili kommunikasiya** — çat, bildiriş, tapşırıq, görüş, xatırlatma

---

## Slayd 4 — Memarlıq (Clean Architecture)

Layihə 4 təbəqədən ibarətdir:

```
┌──────────────────────────────────────────────┐
│  FinNex.UI (ASP.NET Core MVC, SignalR)       │  Təqdimat
├──────────────────────────────────────────────┤
│  FinNex.Application (Services, DTO, Mapper)  │  Biznes məntiq
├──────────────────────────────────────────────┤
│  FinNex.DataAccess (EF Core, Repo, UoW)      │  Məlumat
├──────────────────────────────────────────────┤
│  FinNex.Domain (Entities, Interfaces)        │  Domen
└──────────────────────────────────────────────┘
```

**Əsas dizayn nümunələri (patterns):**
- Repository Pattern (`IRepositoryAsync<T>`)
- Unit of Work (vahid tranzaksiya idarəetməsi)
- Service Layer (`ServiceAsync<TEntity, TDto, TCreateDto, TUpdateDto>`)
- Result Pattern (`Result<T>` — exception-siz xəta idarəetməsi)
- Dependency Injection (126+ servis qeydiyyatı)

---

## Slayd 5 — Texnologiya Yığını

| Sahə | Texnologiya |
|---|---|
| Framework | .NET 8.0, ASP.NET Core MVC |
| ORM | Entity Framework Core 8.0.23 |
| Verilənlər bazası | Microsoft SQL Server |
| Autentifikasiya | ASP.NET Core Identity (cookie-based) |
| Mapping | AutoMapper 12.0.1 |
| Validasiya | FluentValidation 12.1.1 |
| Real-time | SignalR (ChatHub) |
| Excel ixrac | ClosedXML 0.105.0 |
| E-poçt | MailKit 4.9.0 |
| Loglama | Serilog 8.0.3 (rolling file, 30 gün) |
| Localization | az-Latn-AZ |

---

## Slayd 6 — Domen Modeli

**97 domen entitisi**, modullar üzrə:

- **HR** — 52 entity (Isci, Vezife, Departament, Maas, Mezuniyyet, Davamiyyet, Performans, Telim, Sertifikat, ...)
- **Sənəd dövriyyəsi** — 10 entity (Sened, SenedFayl, SenedSablon, Tag, AuditLog, ...)
- **Kredit** — 8 entity (KreditMuraciet, KreditQerar, KomiteUzvu, KreditZamin, ...)
- **Kommunikasiya** — 7 entity (Mesaj, Bildiris, Tapshiriq, Gorush, Xatirlatma, ChatMesaj)
- **Ödəniş tapşırıqları** — 6 entity (OdenisTapsirigi, Bank, BankHesabi, Musteri, ...)
- **Maliyyə/Büdcə** — Xerc, XercKateqoriyasi, Budce
- **Təşkilati** — Elan, Permission, UserPermission, UserDepartment

Bütün entity-lər `BaseEntity`-dən törəyir: **soft delete** (Silinib), **audit tracking** (YaradilmaTarixi, YaradanIcraciId, YenileyenIcraciId, SilinmeTarixi).

---

## Slayd 7 — İstifadəçi İnterfeysi (UI)

**ASP.NET Core MVC** — Area-lara bölünmüş struktur, **67 controller**:

| Area | Controller sayı | Funksionallıq |
|---|---|---|
| **Admin** | 10 | Dashboard, user/role, system log, permission |
| **HR** | 30 | İşçi, maaş, məzuniyyət, davamiyyət, analitika |
| **User** | 20 | Şəxsi dashboard, hesabat, çat, bildiriş |
| **SenedDovriyyesi** | 4 | Sənəd idarəetməsi |
| **PR_Odenis_Tapsirigi** | 3 | Ödəniş əməliyyatları |

**Real-time funksionallıq:**
- SignalR üzərindən daxili çat (ChatHub)
- AJAX ilə maaş və məzuniyyət üçün canlı preview

---

## Slayd 8 — Təhlükəsizlik

- **Autentifikasiya:** ASP.NET Core Identity, cookie-based
- **Sessiya:** 30 dəqiqəlik sliding expiration, `HttpOnly`, `Secure`, `SameSite=Lax`
- **Rate limiting:** Login endpoint — IP başına dəqiqədə 5 cəhd
- **Middleware:** Global Exception Handler, Security Headers

**Rollar (8 rol):**
- `Admin` — tam giriş
- `HR`, `HR_View` — kadr modulu
- `Rehber`, `SobeReisi` — rəhbər səviyyəsi
- `Muhasib` — maliyyə əməliyyatları
- `KreditAdmin` — kredit modulu
- `Operator`, `Viewer` — operativ / yalnız oxuma

**Authorization policies:** `HR_View`, `HR_Full`, `Admin_Full`

---

## Slayd 9 — Avtomatlaşdırılmış Arxa Plan Prosesləri

`IHostedService` əsaslı fon xidmətləri:

1. **ZkTecoSdkService** — biometrik davamiyyət cihazı ilə inteqrasiya
2. **XatirlatmaBackgroundService** — avtomatik xatırlatmalar
3. **MezuniyyetOdenisSchedulerService** — məzuniyyət ödənişlərinin avtomatik hesablanması
4. **QayibMarkerBackgroundService** — qayıbların avtomatik işarələnməsi
5. **ChatCleanupBackgroundService / KreditMailBackgroundService** — əlavə

---

## Slayd 10 — Hesabat və Analitika

- **Dashboard** — rəhbərlər üçün KPI və göstəricilər
- **Excel ixrac** (ClosedXML ilə):
  - İllik büdcə hesabatı
  - Aylıq maaş hesabatı (formatlanmış, rəngli başlıqlar)
  - İstifadəçi hesabatları
- **Xüsusi hesabatlar:**
  - `HesabatSablonu` — şablonlar
  - `HesabatKateqoriyasi` — kateqoriyalar
  - `HesabatTapshiriq` — tapşırıq təyinatı
- **Analytics controller** — performans və HR analitikası

---

## Slayd 11 — Verilənlər Bazası

- **SQL Server** + **EF Core Migrations**
- `AppDbContext` — `IdentityDbContext<AppUser, AppRole, int>`-dən törəyir
- **129+ DbSet** — bütün entitiləri əhatə edir
- **Soft delete** bütün entity-lərdə
- **Audit tracking** — kim, nə zaman yaratdı/yenilədi/sildi
- **Foreign key** əlaqələri cascade/no-action qaydaları ilə
- Başlanğıc üçün `vm_setup.sql` skripti

---

## Slayd 12 — Əsas Üstünlüklər

1. **Vahid sistem** — HR, maliyyə, sənəd, kredit bir platformada
2. **Miqyaslanma** — Clean Architecture modullu inkişafa imkan verir
3. **Təhlükəsizlik** — rol əsaslı giriş, rate limit, audit log
4. **Avtomatlaşdırma** — fon xidmətləri əl əməyini azaldır
5. **Real-time** — SignalR ilə dərhal bildiriş və çat
6. **Lokalizasiya** — tam Azərbaycan dilində
7. **Hesabatlılıq** — Excel ixrac və fərdi dashboard-lar
8. **Audit olunan** — bütün dəyişikliklər izlənir

---

## Slayd 13 — Rəqəmlərlə FinNex

| Göstərici | Dəyər |
|---|---|
| Domen entity sayı | **97** |
| Controller sayı | **67** |
| DI-qeydiyyatlı servis | **126+** |
| Rol sayı | **8** |
| Area sayı | **5** |
| Arxa plan xidməti | **4+** |
| Modul | **10+** |
| Dəstəklənən dil | Azərbaycan |

---

## Slayd 14 — Gələcək Planlar

- Mobil tətbiq (işçi üçün şəxsi kabinet)
- REST API-nin açılması və 3-cü tərəf inteqrasiyaları
- Daha çox analitik report (Power BI / inline charts)
- Çoxdilli dəstək (EN / RU)
- 2FA (iki mərhələli autentifikasiya)
- Elektron imza ilə sənəd təsdiqi

---

## Slayd 15 — Nəticə

**FinNex** — müasir .NET stack üzərində qurulmuş, Clean Architecture prinsiplərinə sadiq qalan,
**korporativ səviyyədə HR və maliyyə idarəetmə həllidir**.

Sistem həm inkişaf etdirməyə açıqdır (modul strukturu), həm də istifadəyə hazırdır
(tam funksional UI, avtomatik proseslər, təhlükəsizlik təbəqəsi).

**Təşəkkürlər!**
Suallar üçün hazıram.
