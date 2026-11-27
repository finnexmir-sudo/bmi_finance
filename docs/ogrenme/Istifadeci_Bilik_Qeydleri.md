# İstifadəçinin Texniki Bilik Qeydləri

> Bu fayl CLAUDE.md-nin bir hissəsi DEYİL — layihənin öz qaydalarını daşımır,
> sırf istifadəçinin öyrənmə vəziyyətini izləyir. Claude bunu öz təşəbbüsü ilə
> yeniləyir (bax CLAUDE.md → "Bilik izlənməsi — öyrənmə jurnalı", 26.11.2026).
> **Tarixçə deyil, STATUS cədvəlidir** — hər mövzu üçün yalnız SON vəziyyət
> saxlanılır, köhnə sətir yenilənir.

**Status şərhləri:** 🟢 Bilir (sərbəst işlədir) · 🟡 Çətinlik çəkir (kömək lazımdır) · 🔴 Unutmuşdu (əvvəl bilib, indi yadına salmaq lazım gəldi) · ⚪ Hələ yoxlanmayıb

## Ümumi qeyd (26.11.2026 — izləmənin başlanğıcı)

İzləmə bu gündən başlayır. Əvvəlki sessiyaların (bu layihənin CLAUDE.md-dəki
onlarla qeydi) göstərdiyi odur ki, istifadəçi **biznes məntiqini** (AML/GOAML
qaydaları, DSMF hesabat strukturu, kredit/mühasibat prosesləri) çox dərindən
bilir və real datanı Oracle-da özü yoxlayıb səhvləri tapa bilir — bu, güclü
domen bilgisi və sərt QA refleksidir. AMMA bu qeydlər əsasən "Claude yazdı,
istifadəçi testdə tapdı" formasındadır — istifadəçinin ÖZÜ kod yazması yeni
başlayan bir təcrübədir, ona görə aşağıdaki cədvəldə **kod yazma** bacarığı
hələ "⚪ Hələ yoxlanmayıb" ilə başlayır, **domen/oxuma** bacarığı isə mövcud
dəlilə görə qeyd olunur.

## Status Cədvəli

| Mövzu | Status | Son yoxlanma | Qeyd |
|---|---|---|---|
| AML/GOAML biznes qaydaları (KYC, hesabat növləri, Tip təsnifatı) | 🟢 | 26.11.2026 | Real Oracle datasını özü yoxlayıb, çoxsaylı məntiq səhvini (CON/SIG mənbəyi, valyuta kodu və s.) özü tapıb |
| DSMF hesabat strukturu (Əlavə №1/4, bölmələr) | 🟢 | 20.11.2026 | Rəsmi şablonun özünü oxuyub çarpaz-referens qeydlərini tapıb |
| Oracle SQL oxuma/analiz (sorğu nəticəsini şərh etmək) | 🟢 | 24.11.2026 | PL/SQL Developer-də özü sorğu işlədib nəticəni şərh edib |
| Oracle SQL YAZMA (özü sıfırdan sorğu qurmaq) | ⚪ | — | Hələ sınanmayıb |
| C# — metod gövdəsi yazmaq | ⚪ | — | Hələ sınanmayıb |
| C# — əsas anlayışlar (`async/await`, DI, LINQ) | 🟡 | 27.11.2026 | Hələ yazmayıb, amma `Dictionary<int,decimal>.GetValueOrDefault(isci.Id)` + `[i]` indeksləməsi olan bir düsturu oxuyub, öz çıxardığı sətri (`AzadGuzestCemi`-nin `xst[i]` əlavəsi) kodla sözbəsöz tutuşdurub düzgün təsdiqlədi — ilk oxuma dəlili, hələ YAZMA yoxdur |
| Razor/CSS/JS (görüntü qatı) | ⚪ | — | Hələ sınanmayıb |
| Git əsasları (commit, branch, push) | ⚪ | — | Hələ sınanmayıb |
| Arxitektura oxuma (layihənin Clean Architecture qatları) | ⚪ | — | Hələ sınanmayıb |

## Növbəti addım

Birinci praktiki tapşırıq (kiçik, təcrid olunmuş bir SQL sorğusu və ya C#
metodu) üçün uyğun məqam gözlənilir — real iş zamanı, süni "dərs" kimi deyil.
