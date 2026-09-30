namespace FinNex.Domain.Entities.HR
{
    /// <summary>
    /// Əmək məzuniyyəti hüququ hesablamasının (M.114/116/117/119) qanuni
    /// ədədləri — TƏK sətirli parametr cədvəli (30.09.2026, istifadəçi
    /// tələbi: "qanun dəyişsə kod dəyişməli olmasın").
    ///
    /// Əvvəllər `MezuniyyetHuquqService`-də HARDCODE idi. Defolt dəyərlər
    /// həmin kodun EYNİSİDİR — bu cədvəl boş/tapılmayanda da servis eyni
    /// nəticəni verir (bax aşağıdakı statik <see cref="Defolt"/> nüsxəsi).
    ///
    /// ⚠️ Vəzifəyə görə 21/30 seçimi BURADA DEYİL — o, `Vezife.EsasMezuniyyetGunu`
    /// sütunundadır (artıq əvvəldən DB-dən idarə olunurdu). Bu cədvəldəki
    /// <see cref="EsasGunAdi"/> yalnız vəzifə 30 GÜN TƏYİN ETMƏYƏNDƏ işlənən
    /// default (21) ədədidir.
    /// </summary>
    public class MezuniyyetHuquqParametrleri : BaseEntity
    {
        // ── Əsas gün (M.114 / M.119) ──────────────────────────────────
        /// <summary>Vəzifə 30 gün təyin etməyəndə işlənən default əsas gün.</summary>
        public int EsasGunAdi { get; set; } = 21;
        /// <summary>Əlil işçi üçün əsas gün (M.119) — vəzifədən asılı olmayaraq.</summary>
        public int EsasGunElil { get; set; } = 42;

        // ── Staj əlavəsi (M.116.3) — 3 pilləli, ARTAN sırada olmalıdır ──
        // ⚠️ `decimal` — `double` YAZMA. Sistem az-Latn-AZ mədəniyyətində işləyir,
        // yalnız `decimal`/`decimal?` `FlexibleDecimalModelBinder`-dən keçir
        // (bax FinNex.UI/Configurations/FlexibleDecimalModelBinder.cs) — `double`
        // formda "5.5" yazılsa mədəniyyətin minlik ayırıcısı kimi oxunub 55 ola
        // bilər, HEÇ BİR XƏTA ÇIXMADAN (code-review, 30.09.2026).
        public decimal StajHedd1Il { get; set; } = 5;
        public int StajHedd1Gun { get; set; } = 2;
        public decimal StajHedd2Il { get; set; } = 10;
        public int StajHedd2Gun { get; set; } = 4;
        public decimal StajHedd3Il { get; set; } = 15;
        public int StajHedd3Gun { get; set; } = 6;

        // ── Uşaq əlavəsi (M.117) ──────────────────────────────────────
        /// <summary>Adi uşaq üçün yaş həddi (bu yaşınadək sayılır).</summary>
        public int UsaqYasHeddi { get; set; } = 14;
        /// <summary>Əlil uşaq üçün yaş həddi (M.117.3 il-sonu qoruması da bunu işlədir).</summary>
        public int EngelliUsaqYasHeddi { get; set; } = 18;
        /// <summary>Bu sayda (dəqiq) uşaqda <see cref="UsaqGun2"/> verilir.</summary>
        public int UsaqSayi2GunHeddi { get; set; } = 2;
        public int UsaqGun2 { get; set; } = 2;
        /// <summary>Bu sayda VƏ ÇOX uşaqda (və ya əlil uşaq varsa) <see cref="UsaqGun5"/> verilir.</summary>
        public int UsaqSayi5GunHeddi { get; set; } = 3;
        public int UsaqGun5 { get; set; } = 5;

        /// <summary>
        /// Kodda əvvəlki HARDCODE dəyərlərlə tam eyni default nüsxə — DB sətri
        /// tapılmayanda (məs. migration hələ işlədilməyib) servis bunu işlədir,
        /// hesablama HEÇ VAXT boş/sıfır parametrlə getmir.
        /// </summary>
        public static MezuniyyetHuquqParametrleri Defolt => new();
    }
}
