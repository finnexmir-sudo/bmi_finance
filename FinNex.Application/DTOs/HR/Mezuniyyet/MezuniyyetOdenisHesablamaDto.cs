namespace FinNex.Application.DTOs.HR.Mezuniyyet
{
    /// <summary>
    /// Məzuniyyət ödənişi üçün hesablama nəticəsi + addım-addım izahat.
    /// Mühasib Detail səhifəsində bu izahatı açıb hər rəqəmin hansı məntiqdən
    /// gəldiyini görə bilir (kodlara baxmağa ehtiyac olmadan).
    /// </summary>
    public class MezuniyyetOdenisHesablamaDto
    {
        public int MezuniyyetId { get; set; }
        public int IsciId { get; set; }
        public string IsciAdSoyad { get; set; } = string.Empty;

        public DateTime BaslamaTarixi { get; set; }
        public DateTime BitmeTarixi { get; set; }

        // Ümumi göstəricilər
        public int UmumiTeqvimGun { get; set; }  // GS (cəmi, bütün period)
        public int UmumiIsGun { get; set; }      // İGS (cəmi)

        // Son 12 ayın cəmi qazancı (xam) və qeyd sayı (S_xam)
        public decimal Son12AyCemi { get; set; }
        public int Son12AyQeydSayi { get; set; }

        // Son 12 ayın DÜZƏLMİŞ (K əmsallı) cəmi — MH formulunda istifadə olunan S
        public decimal Son12AyDuzelmisCemi { get; set; }

        // Cari maaş (IsciMaliye.CariMaas)
        public decimal CariMaas { get; set; }

        // Nəticə məbləği (ümumi advance məbləği)
        public decimal CemiOdenis { get; set; }

        // ── YENİ QAYDA (ƏM md.140 — tam-dövr MAX) sahələri ──
        // YeniQayda=false olduqda aşağıdakılar doldurulmur (köhnə davranış).
        public bool    YeniQayda { get; set; }
        public decimal ACemi     { get; set; }   // Üsul A: orta əməkhaqqı (gündəlik orta × təqvim günü, tam dövr)
        public decimal BCemi     { get; set; }   // Üsul B: cari maaş (hər ay maaş/ay iş günü × düşən İŞ günü, cəm)
        public string? QalibUsul { get; set; }   // "A" (orta) / "B" (cari maaş)

        // Son 12 ayın artım əmsalları cədvəli (Muhasib Detail səhifəsində göstərmək üçün)
        public List<QazancEmsalSliceDto> QazancEmsallari { get; set; } = new();

        // Maaş tarixçəsində boşluq aşkar olunubsa — xəbərdarlıq mesajları
        public List<string> TarixceXeberdarliqlari { get; set; } = new();

        // Hər ay üçün ayrıca hesablama (məzuniyyət iki aya düşdükdə hər ikisi görünür)
        public List<MezuniyyetOdenisAySliceDto> AySliceleri { get; set; } = new();

        // Düz mətn izahat (Muhasibin oxuması üçün)
        public List<string> IzahatAddimlari { get; set; } = new();
    }

    /// <summary>
    /// Bir ay üçün məzuniyyət ödənişi hesablamasının parçası.
    /// </summary>
    public class MezuniyyetOdenisAySliceDto
    {
        public int Il { get; set; }
        public int Ay { get; set; }
        public string AyAdi { get; set; } = string.Empty;

        public int TeqvimGun { get; set; }   // GS — bu ay üçün (təqvim günü)
        public int IsGun { get; set; }       // İGS — məzuniyyət günü (PR #352, həftəsonu daxil); ödəniş və balans üçün
        public int HaqiqiIsGun { get; set; } // Faktiki iş günü (həftəsonu/bayram çıxılıb); maaş kəsintisi üçün
        public int AyIsGun { get; set; }     // Həmin ayın iş gün sayı (reference)

        // MH = S / 12 / 30.4 × GS (bu ay üçün GS)
        public decimal MH { get; set; }
        // ƏH = CariMaas / AyİşGün × İGS (bu ay üçün İGS)
        public decimal EH { get; set; }
        // Köhnə qayda: MAX(MH, ƏH) — bu ay üçün seçilən məbləğ.
        // YENİ qayda: tam-dövr MAX-ın bu aya düşən payı (təqvim gününə mütənasib) —
        // aşağı axın (vergi/bəyannamə) bunu ayın gəliri kimi istifadə edir.
        public decimal Secilen { get; set; }
        public string Qalib { get; set; } = "MH"; // MH / ƏH (hansı böyükdür; yeni qaydada dövrün qalibi)
    }

    /// <summary>
    /// Son 12 ayın qazancları üçün artım əmsalı (K) cədvəlinin bir sətri.
    /// K_i = CariStatMaas / StatMaas_i. Məzuniyyət pulunun MH hissəsi
    /// düzəlmiş cəm üzrə hesablanır.
    /// </summary>
    public class QazancEmsalSliceDto
    {
        public int Il { get; set; }
        public int Ay { get; set; }
        public string AyAdi { get; set; } = string.Empty;
        public decimal StatMaas { get; set; }         // O ayın sonundakı ştat maaşı
        public decimal Qazanc { get; set; }           // Faktiki qazanc (IsciAyliqQazanc)
        public decimal Emsal { get; set; }            // K_i = CariStat / StatMaas_i (>= 1)
        public decimal DuzelmisQazanc { get; set; }   // Qazanc × Emsal
    }

    /// <summary>
    /// Vergi və sosial tutulmaların bir məbləğ üçün kəsilişi (Preview və
    /// Detail səhifələrində yekun NET göstərmək üçün).
    /// </summary>
    public class MezuniyyetTutulmaDto
    {
        public decimal Brut { get; set; }
        // Standart + işçi güzəştinin cəmi (geri uyğunluq üçün).
        public decimal VergiGuzesti { get; set; }
        // Standart 200 AZN (brüt ≤ birinci pillə üst həddi olduqda, yoxsa 0).
        public decimal StandartGuzest { get; set; }
        // İşçinin ən böyük aktiv güzəşti (Qaçqın və s.)
        public decimal IsciGuzesti { get; set; }
        public string? IsciGuzestiAd { get; set; }
        public decimal Vergilenecek { get; set; }
        public decimal GelirVergisi { get; set; }
        public decimal DsmfIsci { get; set; }
        public decimal IssizlikIsci { get; set; }
        public decimal Itss { get; set; }
        public decimal UmumiTutulma { get; set; }
        public decimal Net { get; set; }
    }
}
