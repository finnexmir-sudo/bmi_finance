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

        // Son 12 ayın cəmi qazancı və qeyd sayı (S)
        public decimal Son12AyCemi { get; set; }
        public int Son12AyQeydSayi { get; set; }

        // Cari maaş (IsciMaliye.CariMaas)
        public decimal CariMaas { get; set; }

        // Nəticə məbləği (ümumi advance məbləği)
        public decimal CemiOdenis { get; set; }

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

        public int TeqvimGun { get; set; }   // GS — bu ay üçün
        public int IsGun { get; set; }       // İGS — bu ay üçün
        public int AyIsGun { get; set; }     // Həmin ayın iş gün sayı (reference)

        // MH = S / 12 / 30.4 × GS (bu ay üçün GS)
        public decimal MH { get; set; }
        // ƏH = CariMaas / AyİşGün × İGS (bu ay üçün İGS)
        public decimal EH { get; set; }
        // MAX(MH, ƏH) — bu ay üçün seçilən məbləğ
        public decimal Secilen { get; set; }
        public string Qalib { get; set; } = "MH"; // MH / ƏH (hansı böyükdür)
    }

    /// <summary>
    /// Vergi və sosial tutulmaların bir məbləğ üçün kəsilişi (Preview və
    /// Detail səhifələrində yekun NET göstərmək üçün).
    /// </summary>
    public class MezuniyyetTutulmaDto
    {
        public decimal Brut { get; set; }
        public decimal VergiGuzesti { get; set; }
        public decimal Vergilenecek { get; set; }
        public decimal GelirVergisi { get; set; }
        public decimal DsmfIsci { get; set; }
        public decimal IssizlikIsci { get; set; }
        public decimal Itss { get; set; }
        public decimal UmumiTutulma { get; set; }
        public decimal Net { get; set; }
    }
}
