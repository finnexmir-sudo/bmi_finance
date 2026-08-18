# İşçi Kreditləri üzrə VM 98.2.1 Hesabi Gəliri — Razılaşdırılmış Layihə

**Status:** düstur TƏSDİQLƏNDİ (18.08.2026) · qurulma gözləyir · **Tarix:** 18.08.2026
**Mənbə:** mühasibin `202607_isciler_kredit_uzre_gelir_vergisi.xlsx` faylı + BMI Oracle

---

## 1. Məsələ

İşçiyə verilən kredit bazar dərəcəsindən **aşağı faizlə** olduğu üçün aradakı fərq
VM-nin 98.2.1-ci maddəsinə əsasən **hesabi (imputed) gəlir** sayılır: işçiyə pul
ödənmir, amma vergi/ayırma bazalarına düşür → **netdən tutulma yaranır**.

Hazırda mühasib bunu Excel-də hesablayır və maaş cədvəlinin **13-cü sütununa**
(«VM-nin 98.2.1-ci maddəsinə əsasən vergiyə cəlb olunan gəlirlər») əl ilə yazır.
FinNex-də isə `TopluHesabla` səhifəsində əl ilə daxil edilir.

**Məqsəd:** rəqəmi sistem özü hesablasın, mühasib yalnız yoxlasın.

---

## 2. Sistemdə ARTIQ olan (toxunulmur)

`MaasHesablamaService`-də `VM9821Meblegi` mexanizmi **düzgün qurulub**:

| Yer | Nə edir |
|---|---|
| sətir 863 | brütə **ƏLAVƏ EDİLMİR** — işçiyə pul ödənmir |
| sətir 910 | `vergiBazasi += VM9821Meblegi` |
| sətir 911 | `dsmfBazasi += VM9821Meblegi` |
| sətir 912 | `issizlikBazasi += VM9821Meblegi` |
| sətir 913 | `itssBazasi += VM9821Meblegi` |
| sətir 1001 | 200 AZN güzəşt həddinə (2500) sayılır |
| CLAUDE.md | `IsciAyliqQazanc`-a (məzuniyyət ortalaması) **düşmür** |

**Maaş hesablamasına toxunmuruq** — yalnız əl ilə yazılan rəqəmi avtomatik dolduracağıq.

---

## 3. Data mənbəyi (Oracle, YALNIZ OXUMA)

### 3.1 İşçi kreditinin əlaməti

`licschkre.tipzaloga = 10` → işçi krediti. Sütunlar:

| Sütun | Mənası | Nümunə |
|---|---|---|
| `licschpkre` | adi faizin hesabı (Dt) | `21212000000008700000` |
| `trlicschkre` | onun `arh_dd`-də Kt tərəfi | `64012000000000600000` |
| `licschppkre` | **vaxtı keçmiş** faizin hesabı (Dt) | `21214000000008700000` |
| `trlicsch_19` | onun `arh_dd`-də Kt tərəfi | `64710000000000600000` |
| `subschkre` | KS (`arh_dd.ssd` ilə uyğunlaşır) | 14 |
| `procstavkre` | işçinin faiz dərəcəsi | 8 |
| `procstav_19` | vaxtı keçmiş faiz dərəcəsi | 13 |

- **Müştəri kodu** = `SUBSTR(licschpkre, 10, 6)` → `000087`
- **Valyuta** = `SUBSTR(licschpkre, 6, 2)` → `'00'` = AZN, qalanları valyuta

`(licschpkre, subschkre)` cütünün unikal olduğu yoxlanıldı — dublikat **yoxdur**.

### 3.2 Saxlanacaq sorğu (`OracleSorgular` → «İşçi Kredit Faizi»)

```sql
SELECT SUBSTR(l.licschpkre,10,6) AS musteri_kodu,
       r.pincode                 AS fin,            -- ← İŞÇİ BAĞI
       r.name_regnom             AS musteri_adi,
       SUBSTR(l.licschpkre, 6,2) AS valyuta,          -- '00' = AZN
       l.subschkre               AS subkod,
       l.procstavkre             AS isci_faizi,       -- 8
       l.procstav_19             AS vk_faizi,         -- 13
       SUM(CASE WHEN a.debet = l.licschpkre  THEN a.summa_v_nacval ELSE 0 END) AS faiz_adi,
       SUM(CASE WHEN a.debet = l.licschppkre THEN a.summa_v_nacval ELSE 0 END) AS faiz_vk
FROM   licschkre l
JOIN   arh_dd a ON  a.ssd = l.subschkre
                AND (   (a.debet = l.licschpkre  AND a.kredit = l.trlicschkre)
                     OR (a.debet = l.licschppkre AND a.kredit = l.trlicsch_19) )
LEFT JOIN regnom r ON r.regnom = SUBSTR(l.licschpkre,10,6)   -- ← LEFT, İNNER YOX
WHERE  l.tipzaloga = 10
  AND  a.date_oper BETWEEN '{BAS}' AND '{SON}'
GROUP BY SUBSTR(l.licschpkre,10,6), r.pincode, r.name_regnom,
         SUBSTR(l.licschpkre,6,2), l.subschkre, l.procstavkre, l.procstav_19
```

Hazır INSERT: `docs/sql/kredit/Isci_Kredit_Faizi_OracleSorgu.sql`
(`to_date(''{BAS}'',''dd/mm/yyyy'')` — layihənin Oracle konvensiyası).

**`date_close` şərti QƏSDƏN yoxdur.** Dövrü provodkalar özü təyin edir:
- ay ortasında **bağlanan** kredit → o günə qədər faiz hesablanıb → düşür ✅
- ay ortasında **götürülən** kredit → provodkası var → düşür ✅
- həmin ay faiz hesablanmayan kredit → `arh_dd`-də sətir yoxdur → düşmür ✅
  (iyulda Nərminə Qulamova və Rafael Quliyev məhz belədir — qalıqları var,
  vergiləri isə 0-dır)

> **KRİTİK:** qalıqdan (`summa`) getmək OLMAZ. Mühasib qalıqdan yox, **faktiki
> hesablanmış faizdən** gedir. Nümunə: Qulamova Nərminə — qalıq 14 569,93, faiz 0,
> vergi 0. Heydərova Mirvari — qalıq cəmi 1 928,26, faiz 77,88, vergi 12,17.
> Qalıqla hesablasaq 6 nəfərdə tam səhv nəticə çıxır.

---

## 4. Dövr — təqvim ayı DEYİL

Maaş ayın sonuna yaxın verildiyi üçün təqvim ayı ilə **gün itir**.

**Razılaşdırılmış qayda:**

```
BAS = sonuncu maaş günü          (daxil)
SON = cari gün − 1               (daxil)
```

Boşluq və təkrar yaranmır: növbəti dövr yeni maaş günündən başlayır, əvvəlki
ondan bir gün əvvəl bitir.

**Yoxlanıldı:** iyul hesabatının real dövrü Excel-in `2-6` vərəqindən çıxarıldı →
**24.06.2026 – 24.07.2026**. Axundovun 26 provodkası toplandı = **120,58** —
Excel-dəki `M` ilə eynidir.

Sistem əvvəlki dövrün son tarixini **yadda saxlamalıdır** ki, növbəti dövrün
başlanğıcını özü təklif etsin. Mühasib istəsə dəyişə bilər.

---

## 5. Düstur

```
fayda(faiz, derece, bazar) = MAX(0, faiz × (bazar − derece) / derece)

vergi = fayda(faiz_adi, isci_faizi, bazar)
      + fayda(faiz_vk,  vk_faizi,   bazar)
```

Hər faiz növü **öz dərəcəsi** ilə hesablanır. Səbəb:

| Faiz növü | Dərəcə | Bazar | Nəticə |
|---|---|---|---|
| Adi | 8% | 9,25% | güzəşt **var** → vergi |
| Vaxtı keçmiş | **13%** | 9,25% | işçi bazardan **baha** ödəyir → fayda **yoxdur** |

`MAX(0, …)` vacibdir: mənfi fayda vergini **azaltmamalıdır**.

`+5` sabit yazılmır — `procstav_19` bazadan oxunur, qayda dəyişsə kod uyğunlaşır.
Bugünkü portfeldə hamısı 8% / vk 13%-dir, yəni vk hissəsi həmişə 0 çıxır. Amma
4%-lik kredit qayıtsa (vk 9% < 9,25%) kiçik fayda **var** — ona görə hesablanır.

### Rədd edilmiş variantlar

| Variant | Niyə səhvdir |
|---|---|
| Excel-in düsturu: `O = (M+N)/8×9,25`, `Q = O − M` | `N`-i artırır, amma çıxmır. `N>0` olsaydı vergini ~3 dəfə şişirdərdi. **Mühasib özü səhv olduğunu təsdiqlədi.** |
| `(M+N) × (bazar−8)/8` | `N`-i 8%-lə hesablanmış sayır, halbuki 13%-lədir |
| Qalıq × dərəcə fərqi | mühasibin metodu deyil, 6 nəfərdə səhv verir (bax §3.2) |

---

## 6. Bazar dərəcəsi

- Cari dəyər: **9,25** (Excel-də etiketi «Banklararası kredit dərəcəsi»)
- **AZN və valyuta üçün AYRI dərəcə** saxlanılır — faizləri fərqlidir
- Mühasib özü yazır, tarixi ilə (dəyişəndə yeni sətir)

---

## 7. Qurulacaq struktur

```
KreditFaizDerecesi      (Id, Tarix, ValyutaKodu, Derece, Qeyd)   ✅ QURULDU
KreditFaydaDovru        (Id, Bas, Son, Hesablanma)      ← dövr yaddaşı
OracleSorgular          → «Isci Kredit Faizi» ({BAS}/{SON})      ✅ SQL HAZIR
        ↓
IsciKreditFaydaService.HesablaAsync(bas, son)
        ↓  FIN ilə işçiyə bağlanır (əl ilə cədvəl YOXDUR)
TopluHesabla → VM9821Meblegi sahəsi HAZIR DOLU gəlir  ← MÖVCUD sahə
```

Sıra: dərəcə ✅ → sorğu ✅ → servis → dövr yaddaşı → TopluHesabla inteqrasiyası.

### 7.2 İŞÇİ BAĞI — FIN üzrə AVTOMATİK (18.08.2026 ölçüldü)

`IsciKreditMusteriKodu` cədvəli **LƏĞV EDİLDİ** — lazım deyil. BMI-nin `regnom`
cədvəlində FIN var və FinNex-dəki ilə **eynidir**:

```
Oracle: licschkre.tipzaloga=10 → SUBSTR(licschpkre,10,6) → regnom.pincode (FIN)
                                                                 ↓ FIN üzrə
FinNex: Isciler.FIN → IsciId → maaş
```

40 sətir üzərində yoxlanıldı, tutuşan hər FIN **simvol-simvol eyni**:
`11AN3Y1` (Sərvanuş), `2FATQTQ` (Qulu Zadə), `1MMWSTV` (Mahmudova),
`2GZ1KS3` (Heydərova), `1E0B33` (Najafimarganmaskan), `4XJXXL7` (Nadirova),
`6JUNK3H` (Bəkirov).

Əl ilə cədvəldən üstündür: mühasibin Exceldə etdiyi səhv (bir kodu **iki** nəfərə
yazmaq — `019703`) FIN bağında **fiziki olaraq mümkün deyil**.

**BAĞLANMAYAN SƏTİRLƏR — səssizcə atılmır:**

| Hal | Nümunə | Davranış |
|---|---|---|
| `regnom`-da FIN yanlış | `000091 NAZARİ MORTEZA` → pincode = «XX» | ekranda «işçi tapılmadı», cəmə düşmür |
| FinNex-də belə FIN yoxdur | işdən çıxmış / bizim işçi deyil | eyni |
| Oracle-da 40, FinNex-də 25 işçi | normaldır | dövr filtri onsuz da azaldır (iyul: 15) |

Ona görə Oracle sorğusunda **LEFT JOIN** məcburidir — INNER JOIN yazsaq belə
sətirlər sorğudan düşər və hesabi gəlir səssizcə əskik qalar.

### 7.1 Nəticə haraya yazılır — MÖVCUD MEXANİZM (18.08.2026 yoxlanıldı)

`VM9821Meblegi` **saxlanılan sahə DEYİL** — `TopluHesabla` formasının POST
sahəsidir (`TopluHesabla.cshtml:740` → `ferdiElaveler[i].VM9821Meblegi` →
`FerdiElaveDto`). Hesablama zamanı `MaasHesablamaService`-ə ötürülür və nəticə
`MaasDetay`-a «VM 98.2.1 Gəlirləri» sətri kimi düşür (`:1181`).

Yəni yeni cədvəl lazım deyil: servis rəqəmi hesablayır, forma sahəsi **hazır
dolu gəlir**, mühasib istəsə üstələyir. Saxlanan yalnız **dərəcə** və **dövr**
olacaq (mühasib hər dəfə yazmasın).

---

## 8. TƏSDİQLƏNDİ (18.08.2026)

**Sual idi:** vaxtı keçmiş faiz 13%-dir, bazar 9,25%. Həmin hissəyə görə hesabi
gəlir YAZILMIR (`MAX(0, …)`) — düzdürmü?

**Cavab (istifadəçi):** *«gecikmə faizi MB-dəki faizdən aşağı olduğu halda nəzərə
alınacaq»* — yəni vaxtı keçmiş faiz **yalnız öz dərəcəsi bazar dərəcəsindən AŞAĞI
olanda** hesabi gəlir yaradır. Bu, `MAX(0, …)`-ın dediyinin eynidir:

| vk dərəcəsi | Bazar | `MAX(0, faiz × (bazar − vk) / vk)` | Nəticə |
|---|---|---|---|
| 13% | 9,25% | mənfi → 0 | gəlir YOX ✅ |
| 9% | 9,25% | müsbət kiçik | gəlir VAR ✅ |

**Düstur olduğu kimi qalır — §5 dəyişmir.** `+5` sabiti yazılmır; `procstav_19`
bazadan oxunur, dərəcə dəyişsə kod özü uyğunlaşır.

---

## 9. Düzəldiləcək data

- Excel-in `Netice` vərəqində müştəri kodu **`019703`** həm Baxşaliyev Səxavət,
  həm Vəliyeva Turanə üçün yazılıb. **Doğrusu Baxşaliyevdir**; Vəliyevanın kodu
  səhvdir, mühasib düzəldəcək. Bağ cədvəli qurulanda diqqət.

---

## 10. Doğrulama izi (18.08.2026)

Sorğu `24-06-2026 … 24-07-2026` aralığı ilə işlədildi — 15 sətir. `faiz_adi`
sütunu Excel-in `M` sütunu ilə **hamısında eyni** çıxdı:

Sütun adı: **hesabi gəlir** (vergi DEYİL) — `fayda = faiz × (9,25 − 8) / 8 =
faiz × 0,15625`. Bu məbləğ 13-cü sütuna düşür, vergi/DSMF/İTSS/işsizlik isə
ondan **sonra** maaş hesablamasında tutulur.

| Müştəri | Sorğu | Excel `M` | Hesabi gəlir (×0,15625) | Excel `Netice` |
|---|---|---|---|---|
| 000054 | 120,58 | 120,58 | 18,84 | 18,84 |
| 000056 | 122,92 | 122,92 | 19,21 | 19,21 |
| 000075 | 112,51 | 112,51 | 17,58 | 17,58 |
| 000098 | 118,65 | 118,65 | 18,54 | 18,54 |
| 000060 | 22,24 | 22,24 | 3,48 | 3,48 |
| 000076 | 55,33 | 55,33 | 8,65 | 8,65 |
| 014640 | 98,78 | 98,78 | 15,43 | 15,43 |
| 016398 | 47,56 | 47,56 | 7,43 | 7,43 |
| 016085 | 7,01 | 7,01 | 1,10 | 1,10 |
| 000085 | 98,62 | 98,62 | 15,41 | 15,41 |
| 000087 | 85,07 | 85,07 | 13,29 | 13,29 |
| 000090 | 75,57 | 75,57 | 11,81 | 11,81 |
| 010164 | 92,41 | 92,41 | 14,44 | 14,44 |
| 010442 | 77,88 | 77,88 | 12,17 | 12,17 |
| 017648 | 113,95 | 113,95 | 17,80 | 17,80 |

`faiz_vk` bütün sətirlərdə **0**. 2026-cı il üzrə vaxtı keçmiş faiz sorğusu da
boş qaytardı → keçmiş aylarda düzəliş lazım deyil.

**Nəticə: düstur real data ilə sübut olunub.**
