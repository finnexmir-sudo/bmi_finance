namespace FinNex.Application.DTOs.Kredit.Muqavile;

/// <summary>
/// Kredit müqaviləsi hazırlanması üçün verilmiş kreditlərin siyahı sətri.
/// Mənbə: Oracle (BMI) — odb.licschkre + regnom + srokpogprockre + creditinfo (yalnız SELECT).
/// BMI-dəki Form2 (Anakredit) "kataloqgetir_zamin_ayliq" sorğusunun qarşılığıdır.
/// </summary>
public class KreditMuqavileSatirDto
{
    // Borcalan
    public string? Adi { get; set; }                 // r.name_regnom
    public string? Ks { get; set; }                  // t.subschkre
    public string? SubQeyd { get; set; }             // substr(licschkre,10,6)||subschkre
    public string? HesabNo { get; set; }             // t.licschkre
    public string? Fin { get; set; }                 // y.pincode
    public string? Mobil { get; set; }               // r.telefon
    public string? Unvan { get; set; }               // r.registrac
    public string? Olke { get; set; }                // r.grajdanstvo
    public string? SeriyaNo { get; set; }            // r.passport
    public string? VerenOrqan { get; set; }          // r.senedi_veren_orqaninin_adi
    public DateTime? SenedVerilmeTarixi { get; set; } // r.senedin_verilme_tarixi

    // Hüquqi şəxs (r.yurik = 1 → true). Hüquqi şəxsdə vəsiqə/FİN yerinə VÖEN yazılır.
    public bool HuquqiSexs { get; set; }             // r.yurik == 1
    public string? Voen { get; set; }                // r.inn_regnom

    // Kredit
    public DateTime? VerilmeTarixi { get; set; }     // t.date_open
    public string? Teyinat { get; set; }             // t.naznackredita
    // DİQQƏT — iki məbləğin mənası (13.08.2026-da BMI datası ilə təsdiqləndi):
    //   summakre = MÜQAVİLƏ məbləği (verilən kredit) → müqaviləyə BU düşür
    //   summa    = cari ƏSAS QALIQ (amortizasiya ilə azalır)
    // Yeni verilən kreditdə ikisi bərabərdir (yoxlama: son 30 gün, 4/4 bərabər),
    // köhnə kreditlərdə summa/summakre ~0,27-yə enir. Valyuta ekvivalenti DEYİL.
    // {k_meb} üçün `Mebleg` işlədilməlidir — `MeblegAzn` işlədilsə müqavilədə
    // kreditin cari qalığı yazılardı (10 000 AZN-lik kredit üçün 2 724 AZN).
    public decimal? Mebleg { get; set; }             // t.summakre
    public decimal? MeblegAzn { get; set; }          // t.summa

    // Valyutalı kreditmi? Mənbə: t.xarici_valyutada_kredit (0/1).
    // null = sütun sorğuya əlavə edilməyib (yoxlama aparıla bilmir) — bu halda
    // forma bloklanmır, amma ekranda xəbərdarlıq göstərilir.
    // Şablonlar yalnız AZN üçündür ({k_val} sabit "AZN", məbləğ sözlə "manat/qəpik"),
    // ona görə valyutalı kreditdə müqavilə hazırlanmasına icazə verilmir.
    public bool? XariciValyuta { get; set; }          // t.xarici_valyutada_kredit
    public decimal? Ayliq { get; set; }              // graphpogkre — aylıq ödəniş
    public string? Fifd { get; set; }                // k.fifd
    public decimal? Faiz { get; set; }               // t.procstavkre
    public decimal? VkFaiz { get; set; }             // t.procstav_19
    public decimal? EhtiyatFaiz { get; set; }        // t.procstavrez
    public string? Muddet { get; set; }              // t.srok
    public string? CariHesab { get; set; }           // m.az||m.nr||m.bank||m.licsch

    // Girov / təminat (creditinfo)
    public decimal? GirovDeyeri { get; set; }        // t.summa_zaloga
    public string? GirovUnvan { get; set; }          // y.anyinfotodisting
    public string? TeminatNo { get; set; }           // y.registryno
    public DateTime? CixarisTarixi { get; set; }     // y.registrydate
}
