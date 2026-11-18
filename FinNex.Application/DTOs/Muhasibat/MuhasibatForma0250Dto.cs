namespace FinNex.Application.DTOs.Muhasibat
{
    /// <summary>
    /// Forma 0250 — General Ledger (CBAR). Bir KOD×valyuta sətri. Sahə adları
    /// Oracle sorğusunun öz sütun aliaslarına uyğundur (rate, closing_base,
    /// closing_local, credit_turnover, debit_turnover, opening_local,
    /// currencytype, kod) — mənbə sorğu "Excel görüntü sırası (KOD ən axırda)"
    /// şərhini daşıyır, ona görə Excel ixracında sütun sırası bu DTO-dakı sıra
    /// ilə EYNİ saxlanmalıdır (18.11.2026).
    /// </summary>
    public class MuhasibatForma0250SatirDto
    {
        /// <summary>Son günün məzənnəsi. AZN üçün həmişə null (mənbə: "rate"-də AZN→null).</summary>
        public decimal? Rate { get; set; }

        /// <summary>AZN kitab dəyərinin cari kursa bölünməsi (valyuta ekvivalenti). AZN üçün 0.</summary>
        public decimal? ClosingBase { get; set; }

        public decimal? ClosingLocal { get; set; }
        public decimal? CreditTurnover { get; set; }
        public decimal? DebitTurnover { get; set; }
        public decimal? OpeningLocal { get; set; }

        public string CurrencyType { get; set; } = "";
        public string Kod { get; set; } = "";
    }

    /// <summary>
    /// Forma 0250 nəticəsi — dövr (bas/son) + KOD×valyuta sətirləri.
    /// </summary>
    public class MuhasibatForma0250Dto
    {
        public DateTime Bas { get; set; }
        public DateTime Son { get; set; }

        public bool Ugurlu { get; set; } = true;
        public string? Xeta { get; set; }

        public List<MuhasibatForma0250SatirDto> Setirler { get; set; } = new();
    }
}
