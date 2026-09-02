# Kredit Arayışları — BMI «Kredit DP → Arayışlar» portu (02.09.2026)

## BMI-də 7 bənd var idi, 4-ü işləyirdi

| Bənd | BMI forması | Vəziyyət |
|---|---|---|
| DYP arayış | `DYP` | ✅ köçürüldü |
| **BTİ arayış** | — | ❌ **ölü menyu** — handler, forma, sorğu, şablon YOXDUR |
| Borcalan temizlik arayışı | `frmborcalantemizlik` | ✅ köçürüldü |
| Zamin təmizlik arayışı | `zaminarayis` | ✅ köçürüldü |
| **Qeydiyyata düşmə** | — | ❌ **ölü menyu** |
| **İcarə məktubu** | — | ❌ **ölü menyu** |
| Saipa girovdan çıxma | `frmcarsmektubcix` | ✅ köçürüldü (2 rejim) |

Üç bəndin arxasında BMI-də heç nə yoxdur — `Form1.Designer.cs`-də menyu elementi
var, `Click +=` yoxdur, bütün repo-da nə forma sinfi, nə SQL, nə şablon tapıldı.
Lazım olsa **sıfırdan** qurulmalıdır (mətn və məlumat mənbəyi istifadəçidən).

## Ortaq sxem

```
giriş → (bəzən Oracle axtarışı) → Qeydiyyat № → XaricMektub jurnalı → Word (.docx yüklənir)
```

| Qat | FinNex-də |
|---|---|
| Oracle oxuma | `IKreditArayisService` → `IOracleService` (**yalnız SELECT**) |
| SQL mənbəyi | Admin → Oracle Sorğular: «Arayış Borcalan», «Arayış Zamin» |
| Nömrə + jurnal | `IXaricMektubService.YaratAsync` (il üzrə `max+1`) |
| Word | `KreditWordService.Doldur` (OpenXML, token əvəzləmə) |
| Şablonlar | `wwwroot/Files/Word/Kredit/Arayis/` |

## Səhifələr

| Ünvan | Nə edir | Axtarış |
|---|---|---|
| `/User/KreditArayis/Dyp` | Avtomobilin girovdan çıxması | yoxdur (əl ilə) |
| `/User/KreditArayis/Borcalan` | Borcun bağlandığı arayışı | qeydiyyat kodu (regnom) |
| `/User/KreditArayis/Zamin` | Zaminliyin bitdiyi arayışı | zaminin FİN kodu |
| `/User/KreditArayis/Saipa` | Saipa — girovdan çıxma / texpasport dəyişmə | yoxdur |

**Giriş:** Admin · KreditAdmin · Admin panelindən təyin edilmiş «kredit baxan işçi»
(`IKreditBaxanIsciService.BaxaBilerMiAsync`). Kredit müraciətləri ilə **eyni**
siyahıdır — ikinci siyahı qurulmadı, yoxsa biri köhnə qalar.

## Şablonlar və tokenlər

| Fayl | BMI-dəki adı | Tokenlər |
|---|---|---|
| `DYP_arayis_girovdan_cixma.docx` | `DYP arayış girovda çıxma1.doc` | `{mekNo} {mektarixi} {borcalan} {muqtar} {muqNo} {avtoNo} {marka} {avtoil} {muh} {ban} {reng}` |
| `Borcalan_temizlik_arayisi.docx` | `borcalan arayis.docx` | `{mekNo} {mektarixi} {muqtar} {borcalan} {muqno} {mebleg}` |
| `Zamin_temizlik_arayisi.docx` | `Zaminarayis1.docx` | `{mekNo} {mektarixi} {muqtar} {borcalan} {zamin} {mebleg}` |
| `Saipa_girovdan_cixma.docx` | `carsgirovcix1.docx` | `{mekNo} {muqtar} {avtoNo} {avtoil} {muh} {ban} {reng}` |
| `Saipa_texpasport_deyisme.docx` | `Cars Şehadetneme deyismesi1.docx` | yuxarıdakılar + `{texpNo}` |

⚠️ **`{muqno}` ilə `{muqNo}` FƏRQLİ TOKENLƏRDİR** — borcalan şablonunda kiçik `n`,
DYP-də böyük `N`. BMI-də də belədir. Birini o birinə «uyğunlaşdırmaq» olmaz:
şablonda olmayan token səssizcə itir, xəta çıxmır.

⚠️ **`{krtar}`** — BMI zamin arayışında bu tokeni doldururdu (üstəlik zaminin adı
ilə, səhv görünürdü), amma `Zaminarayis1.docx`-də belə token **ümumiyyətlə yoxdur**
(02.09.2026 yoxlanıldı). Ona görə burada da yazılmır.

⚠️ **`{mektarixi}` `Saipa_girovdan_cixma.docx`-də YOXDUR** — həmin sənəddə
məktubun tarixi çap olunmur, yalnız `{muqtar}` var. Token yenə də göndərilir ki,
şablona sonradan əlavə edilsə kod dəyişməsin. Şablonun səhvi olub-olmadığı
**istifadəçidən soruşulub, cavab gözlənilir**.

## ÖNİZLƏMƏ REJİMİ — `KreditArayis:NomreYaz`

```jsonc
"KreditArayis": { "NomreYaz": false }   // defolt
```

- **`false`** — sənəd hazırlanır, jurnala **heç nə yazılmır**, nömrənin yanında
  «(ÖNİZLƏMƏ)» yazılır;
- **`true`** — Qeydiyyat № ayrılır, `XaricMektub` jurnalına sətir düşür.

Nömrə bir dəfə veriləndən sonra **geri qaytarılmır** — yoxlamadan sonra `true` edin.

## BMI-DƏN QƏSDƏN FƏRQLƏNƏN YERLƏR

### 1. Nömrə yarışı aradan qaldırıldı
BMI nömrəni `max+1` ilə hesablayıb **Word-ə yazır**, sonra jurnala INSERT edirdi —
özü də `qey_nom` sütununu **yazmadan** (onu Oracle təyin edirdi). Yəni sənəddəki
nömrə jurnaldakından fərqlənə bilərdi, iki nəfər eyni anda işləsə üst-üstə düşürdü.
Burada `YaratAsync` nömrəni ayırır, jurnala yazır və **elə həmin nömrəni** qaytarır.

### 2. Oracle-a yazı yoxdur
BMI dördü də `odb.xaric_mektub`-a INSERT edirdi. Həmin jurnal 12.08.2026-da
FinNex-ə köçürülüb; Oracle burada **yalnız oxunur** (CLAUDE.md).

### 3. Borcalan arayışında `{muqtar}`
BMI-də bu xana səhifə açılanda **bugünkü tarixlə** dolurdu, kreditin real tarixi
isə istifadə olunmayan başqa xanada qalırdı. Şablon mətni «{borcalan} ilə {muqtar}
il tarixində … bağlanmış» deyir — yəni ora **müqavilə tarixi** düşməlidir. Burada
Oracle-dan gələn `date_open` ilə öncədən doldurulur, operator dəyişə bilər.
**Bu, davranış dəyişikliyidir — istifadəçi təsdiqi gözlənilir.**

### 4. Zamin axtarışında `like` → `=`
BMI `lower(g.pincode) like lower('...')` yazırdı, amma forma heç vaxt `%`
göndərmirdi. `like` qalsaydı istifadəçi `%` yazıb bütün zaminlikləri çəkə bilərdi.

### 5. Saipa-nın Access bazası KÖÇÜRÜLMƏDİ
BMI «Girovdan çıxma» rejimində əlavə olaraq lokal `AtlasCars.accdb` faylındakı
`Cars_mektublar` cədvəlinə də yazırdı (yol kodda sabit: `C:\BMI_\BMI\bin\Debug\`).
**Bu hissə köçürülmədi** — fayl kiminsə kompüterindədir və istifadədə olub-olmadığı
istifadəçidən soruşulub, cavab gözlənilir. Lazım olsa FinNex-də cədvəl kimi
qurulacaq (ayrıca miqrasiya).

### 6. SQL inyeksiyası bağlandı
BMI-də hər sorğu TextBox mətnini birbaşa SQL-ə yapışdırırdı. Burada SQL Admin
panelində saxlanılır, axtarış dəyəri servisdə təmizlənir (regnom → yalnız rəqəm,
FİN → yalnız hərf/rəqəm) və `{REGNOM}` / `{PINCODE}` yer tutucusu əvəz olunur.

## Quraşdırma sırası

1. **VS-də build.** (Bu kod build EDİLMƏYİB — mühitdə `dotnet` yoxdur.)
2. `docs/sql/kredit/Arayis_OracleSorgular.sql` işlədin → Admin → Oracle Sorğular-da
   «Arayış Borcalan» və «Arayış Zamin» görünməlidir (aktiv).
3. **DYP şablonu:** `DYP arayış girovda çıxma1.doc` **köhnə `.doc` formatındadır** —
   `KreditWordService` (OpenXML) onu aça bilmir. Word-də açıb
   **«Farklı kaydet → Word Belgesi (.docx)»** edin və faylı
   `wwwroot/Files/Word/Kredit/Arayis/DYP_arayis_girovdan_cixma.docx`
   adı ilə qoyun. O olmadan DYP səhifəsi «Şablon tapılmadı» deyir və **nömrə
   ayırmır** (jurnala heç nə yazılmır).
4. Hər dörd səhifəni **önizləmə rejimində** (`NomreYaz=false`) sınayın — sənədin
   içindəki bütün xanaları BMI-dən çıxan sənədlə tutuşdurun.
5. Nəticə düz olanda `appsettings.json → KreditArayis:NomreYaz = true`.
6. Real rejimdə bir sənəd hazırlayın və **Xaric məktub jurnalında** sətrin
   yarandığını, nömrənin sənəddəki ilə eyni olduğunu yoxlayın.

## Yoxlama siyahısı

- [ ] Sorğular Admin panelində görünür və aktivdir
- [ ] Borcalan: qeydiyyat kodu ilə axtarış nəticə verir, «Seç» forma doldurur
- [ ] Zamin: FİN ilə axtarış nəticə verir
- [ ] Məbləğ sözlə düzgün yazılır (AZN → «manat/qəpik», USD → «dollar/sent»)
- [ ] Tarix sözlə düzgün yazılır («02 Sentyabr 2026-cı»)
- [ ] Saipa: rejim dəyişəndə texpasport xanası görünür/gizlənir
- [ ] Texpasport rejimində şəhadətnamə № boş olsa **nömrə ayrılmır**
- [ ] Şablon tapılmayanda **nömrə ayrılmır** (bütün 4 səhifədə)
- [ ] `NomreYaz=true` edildikdən sonra jurnalda sətir yaranır və nömrələr uyğundur
