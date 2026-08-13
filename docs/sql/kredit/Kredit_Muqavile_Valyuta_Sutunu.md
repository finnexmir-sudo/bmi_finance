# «Kredit Müqavilə» Oracle sorğusuna valyuta sütunu

**Nə üçün:** Word şablonları yalnız AZN üçündür — `{k_val}` sabit «AZN» yazılır və
məbləğin sözlə yazılışı (`KreditSozeCevir.MebleghSoze`) «manat»/«qəpik» sözlərini
sabit əlavə edir. Valyutalı kreditdə hər ikisi səhv olardı və **heç bir xəta
verməzdi** — sənəd səssizcə yanlış çıxardı.

Qoruyucu artıq koddadır, amma mənbə sütunu sorğuya əlavə edilməlidir.

## 13.08.2026 vəziyyəti

```
xarici_valyutada_kredit = 0  →  açıq portfeldəki 310 kreditin HAMISI
```

Yəni bu dəyişiklik **bu gün heç bir krediti bloklamır** — gələcək üçün qoruyucudur.

`summakre` / `summa` fərqi valyuta ilə bağlı DEYİL (əvvəlki şübhə bağlandı):
- `summakre` = müqavilə məbləği → müqaviləyə **bu** düşür (`{k_meb}`);
- `summa` = cari əsas qalıq (amortizasiya ilə azalır).

Yeni verilən kreditdə ikisi bərabərdir (son 30 gün: 4 kredit, 4-ü də bərabər).

## Nə etməli

**Admin → Oracle Sorğular → «Kredit Müqavilə»** sorğusunu redaktə edin.
SELECT siyahısına bir sütun əlavə edin — **ad tam olaraq `XARICI_VALYUTA`
olmalıdır** (kod sütunu adına görə oxuyur, böyük/kiçik hərf fərqi əhəmiyyətsizdir):

```sql
t.xarici_valyutada_kredit AS XARICI_VALYUTA
```

Məsələn, mövcud SELECT belədirsə:

```sql
SELECT r.name_regnom AS ADI,
       t.subschkre  AS KS,
       ...
       t.summakre   AS MEBLEG,
```

sona (və ya istənilən yerə) əlavə edin:

```sql
       t.xarici_valyutada_kredit AS XARICI_VALYUTA,
```

`FROM` hissəsinə toxunmayın — sütun onsuz da `odb.licschkre` (alias `t`)
cədvəlindədir. Yalnız SELECT siyahısı dəyişir.

## Yoxlama

Sorğunu yenilədikdən sonra **Kredit → Müqavilələr → (bir kredit seç) → Hazırla**
səhifəsini açın:

| Görünən | Mənası |
|---|---|
| Sarı xəbərdarlıq **YOXDUR** | ✅ sütun oxunur, qoruyucu işləyir |
| Sarı xəbərdarlıq **VAR** | ❌ sütun hələ tapılmır — adı `XARICI_VALYUTA` olduğunu yoxlayın |

Xəbərdarlıq mətni:

> Valyuta yoxlaması aktiv deyil: «Kredit Müqavilə» Oracle sorğusunda
> XARICI_VALYUTA sütunu yoxdur.

## Davranış

| `xarici_valyutada_kredit` | Nəticə |
|---|---|
| `0` | Müqavilə normal hazırlanır |
| `1` (və ya ≠0) | Forma açılmır / POST bloklanır: *«Bu kredit xarici valyutadadır — mövcud şablonlar yalnız AZN üçündür. Müqavilə hazırlanmadı, nömrə ayrılmadı.»* |
| sütun yoxdur (`null`) | **Bloklamır** (modul dayanmasın deyə), amma sarı xəbərdarlıq göstərilir |

Yoxlama **iki yerdədir**: forma açılanda (`Hazirla` / `ZaminlikHazirla` GET) və
sənəd yaradılanda (`MenzilYarat` / `ZaminlikYarat` POST). POST-dakı yoxlama
nömrə ayrılmasından ƏVVƏLdir — bloklanan halda sayğac artmır, məktub yazılmır.

## Valyutalı kredit lazım olsa (gələcək)

Blok təsadüfi deyil — şablonlar hazır olmadan icazə verilməməlidir. Lazım olanda
üç yer birlikdə dəyişməlidir:

1. `{k_val}` — hazırda `KreditMuqavileController`-də sabit `"AZN"`;
2. `KreditSozeCevir.MebleghSoze` — «manat»/«qəpik» sözləri sabitdir (sətir 63, 72);
3. `kurval`-da beynəlxalq qısaltma (USD/EUR) **yoxdur** — yalnız kod (`00`) və ad
   (`ABŞ DOLLARI`). Kod → qısaltma xəritəsi lazım olacaq.
