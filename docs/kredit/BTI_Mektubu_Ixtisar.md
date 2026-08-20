# Daşınmaz müqaviləsi — BTİ məktubu müvəqqəti ixtisar edildi

**Tarix:** 20.08.2026 · **Status:** kod yazılıb, **build edilməyib** (mühitdə `dotnet` yoxdur)

---

## Qərar

İstifadəçi: «daşınmaz müqaviləsində məktub hazırlanırdı, onu **hələlik** ixtisara
salmalıyıq — nə hazırlansın, nə də nömrə götürsün məktub nömrəsi».

«Hələlik» olduğu üçün **kod silinmədi** — konfiqurasiya açarına bağlandı
(CLAUDE.md — «İşlək funksiyanı silmək yalnız açıq icazə ilə»).

## Açar

```jsonc
// FinNex.UI/appsettings.json
"KreditMuqavile": {
    "NomreYaz": false,
    "BtiMektubu": false      // ← 20.08.2026-dan söndürülü
}
```

Kodda default də `false`-dur (`config.GetValue("KreditMuqavile:BtiMektubu", false)`),
yəni açar silinsə də davranış dəyişmir. **Geri qaytarmaq = `true` yazmaq**; kod
dəyişikliyi lazım deyil.

## Söndürülü olduqda nə dəyişir (`MenzilYarat`)

| Yer | Əvvəl | İndi |
|---|---|---|
| `.zip` məzmunu | Kredit + İpoteka + **BTİ məktubu** + Zaminliklər | Kredit + İpoteka + Zaminliklər |
| `MektubQeydiyyatiAsync` | çağırılır → `XaricMektub`-a sətir, nömrə yeyilir | **çağırılmır** |
| `{mekno}` tokeni | `2026-N` | boş sətir |
| `BTI_salinma(_Tek).docx` mövcudluğu | yoxlanılır, yoxdursa müqavilə **bloklanır** | yoxlanılmır |
| Həmin şablonda `{i_mno}` yoxlaması | tətbiq olunur | tətbiq olunmur |

**Nömrə niyə məhz orada söndürülüb:** nömrə bir dəfə veriləndən sonra geri
qaytarılmır (CLAUDE.md — «Jurnal Nömrəsi Geri Qaytarılmır — Silinmişlər DƏ
Sayılır»). Sənədi yaratmayıb nömrəni götürsək jurnalda **sənədsiz sətir** qalardı.

**Şablon yoxlamaları niyə çıxarıldı:** hazırlanmayan sənədin şablonu yoxdur deyə
kredit müqaviləsi bloklanmamalıdır. Açar `true` olanda hər iki yoxlama avtomatik
geri qayıdır — şərtlər silinmədi, `if (_btiMektubu)` altına salındı.

## TOXUNULMAYAN

- **AVTOMOBİL (DYP) məktubu** — `AvtomobilYarat` axını tam əvvəlki kimidir,
  bu açardan asılı deyil.
- `XaricMektub` jurnalının özü, `IXaricMektubService`, jurnal səhifəsindən
  əl ilə məktub yaratmaq — heç biri dəyişmədi.
- `Erazi`, `ObyektTipi` və digər forma sahələri **qaldı** — onlar ipoteka
  müqaviləsinə də düşür (`{i_erazi}`, `{i_diger_cixaris_melumati}`).
  Yalnız iki etiket mətnindən «BTİ məktubu» sözü çıxarıldı, çünki artıq
  hazırlanmayan sənədə istinad edirdi.

## Fayllar

| Fayl | Dəyişiklik |
|---|---|
| `FinNex.UI/Areas/User/Controllers/KreditMuqavileController.cs` | `_btiMektubu` sahəsi + 4 şərt (`MenzilYarat`) |
| `FinNex.UI/appsettings.json` | `KreditMuqavile:BtiMektubu` |
| `FinNex.UI/Areas/User/Views/KreditMuqavile/Hazirla.cshtml` | 2 etiket mətni |

## YOXLANMAYAN

- ⚠️ **Build edilməyib** — mühitdə `dotnet` yoxdur.
- Real daşınmaz müqaviləsi hazırlanıb `.zip`-də BTİ məktubunun **olmadığı**
  (kredit + ipoteka + zaminliklər qaldığı) yoxlanmalıdır.
- `XaricMektub` jurnalında yeni sətir **yaranmadığı** yoxlanmalıdır.
  `NomreYaz=false` olduğu üçün onsuz da yazılmırdı — `NomreYaz` `true`
  ediləndə bu yoxlama xüsusilə vacibdir.
