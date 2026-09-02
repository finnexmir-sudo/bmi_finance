namespace FinNex.Application.DTOs.Kredit.Arayis;

/// <summary>
/// «Borcalan təmizlik arayışı» axtarış sətri — BMI `frmborcalantemizlik`
/// cədvəlinin qarşılığı. Axtarış QEYDİYYAT KODU (regnom) üzrədir.
/// Mənbə: Oracle, YALNIZ SELECT.
/// </summary>
public class BorcalanArayisSatirDto
{
    public string? Adi        { get; set; }   // r.name_regnom — borcalanın adı
    public string? HesabNo    { get; set; }   // t.licschkre
    public string? Ks         { get; set; }   // t.subschkre
    public DateTime? Tarix    { get; set; }   // t.date_open — kredit müqaviləsinin tarixi
    public decimal? Kredit    { get; set; }   // t.summakre — MÜQAVİLƏ məbləği (qalıq DEYİL)
    public decimal? Qaliq     { get; set; }   // t.summa — cari əsas qalıq
    public string? MuqavileNo { get; set; }   // k.nomer_lsk — müqavilə nömrəsi

    /// <summary>
    /// Valyuta hesab nömrəsindən çıxarılır: `substr(licschkre, 7, 2)`
    /// 00→AZN, 01→USD, 02→AVRO (BMI ilə eyni). `kod_valuti` sütunu İSTİFADƏ
    /// EDİLMİR — o, INTEGER-dir və mətn müqayisəsində tələyə düşür (CLAUDE.md).
    /// </summary>
    public string? Valyuta    { get; set; }
}

/// <summary>
/// «Zamin təmizlik arayışı» axtarış sətri — BMI `zaminarayis` cədvəlinin
/// qarşılığı. Axtarış ZAMİNİN FİN kodu üzrədir (`creditinfoguarantee.pincode`).
/// </summary>
public class ZaminArayisSatirDto
{
    public string? Adi       { get; set; }   // r.name_regnom — borcalan
    public string? HesabNo   { get; set; }   // t.licschkre
    public string? Ks        { get; set; }   // t.subschkre
    public string? Zamin     { get; set; }   // g.guarantee_name
    public DateTime? Tarix   { get; set; }   // t.date_open
    public decimal? Kredit   { get; set; }   // t.summakre
    public decimal? Qaliq    { get; set; }   // t.summa
    public string? Valyuta   { get; set; }
}
