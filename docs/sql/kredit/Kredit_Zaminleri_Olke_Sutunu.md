# «Kredit Zaminləri» sorğusuna ölkə sütunu + ölkə siyahısı

14.08.2026 — iki dəyişiklik. Hər ikisi **yalnız SELECT**, heç nə yazılmır.

## 1. Ölkə siyahısı artıq kodda sabit deyil

Əvvəl müqavilə formalarındakı «Ölkə» açılan siyahısı kodda 5 sabit ölkə idi.
İndi BMI `countrycode` kataloqundan **canlı** oxunur — `kurval` (valyuta) ilə eyni
qayda: kataloq BMI-nindir, orada dəyişiklik olanda bizdə də dərhal görünür.

**Etməli:** `docs/sql/olke/Olke_OracleSorgu.sql` skriptini işlədin (idempotentdir).
`OLKE_SIYAHISI` adlı sorğu yaradır:

```sql
SELECT code, name FROM countrycode ORDER BY name
```

Oracle əlçatmaz olsa servis köhnə 5 ölkəyə qayıdır — forma ölkəsiz qalmır.

> **Müqaviləyə AD düşür, kod yox.** Şablon mətni «{k_olke}nın vətəndaşı»
> şəklindədir. `CODE` yalnız Oracle-dan gələn dəyəri ada çevirmək üçündür.

## 2. Zaminin ölkəsi — əl ilə seçmək lazım deyil

BMI zaminin ölkəsini onsuz da saxlayır: `odb.creditinfoguarantee.COUNTRYCODE`
(məs. `AZE`). Sorğu onu `countrycode` ilə birləşdirib **adı** qaytararsa, forma
zamini yükləyəndə ölkə də avtomatik seçilmiş gəlir.

**Etməli:** Admin → Oracle Sorğular → **«Kredit Zaminləri»** sorğusuna bir sütun
əlavə edin — ad tam olaraq **`OLKE`** olmalıdır:

```sql
c.name AS OLKE
```

və `FROM`-a kataloqu qoşun (mövcud (+) üslubu ilə):

```sql
FROM odb.creditinfoguarantee a, odb.countrycode c
WHERE a.countrycode = c.code(+)
  AND ... (mövcud şərtlər olduğu kimi qalır)
```

`(+)` vacibdir: ölkə kodu boş olan zamin sətri **itməməlidir**.

### Yoxlama

Zaminləri olan bir kredit üçün «Müqavilə hazırla» səhifəsini açın:

| Görünən | Mənası |
|---|---|
| Zaminin «Ölkə» sahəsi **seçilmiş** gəlir | ✅ sütun oxunur |
| «— seçin —» qalır | sütun hələ tapılmır — adının `OLKE` olduğunu yoxlayın |

Sütun əlavə edilməsə heç nə sınmır — sadəcə operator əvvəlki kimi əl ilə seçir.

## 3. Borcalanın ölkəsi

Kod `KreditMuqavileSatirDto.Olke` (`r.grajdanstvo`) sahəsini artıq işlədir və
onu formada öncədən seçir. Çevirmə **həm kodu, həm adı** qəbul edir, ona görə
`grajdanstvo`-nun hansı formatda saxlandığını bilmək lazım deyil:

- `"AZE"` gəlsə → `countrycode`-dan «Azərbaycan Respublikası» tapılır;
- `"Azərbaycan Respublikası"` gəlsə → olduğu kimi işlədilir;
- tanınmasa → gələn dəyər olduğu kimi qalır (məlumat itmir).

Yəni bu hissə üçün sorğu dəyişikliyi lazım deyil.
