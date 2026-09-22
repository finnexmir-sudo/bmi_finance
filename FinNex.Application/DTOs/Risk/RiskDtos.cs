using FinNex.Application.DTOs.Oracle;

namespace FinNex.Application.DTOs.Risk;

// Risk departamentinə təyin olunmuş bir hesabat (OracleSorgular-dan)
public class RiskHesabatDto
{
    public int     Id       { get; set; }
    public string  Ad       { get; set; } = "";
    public string? Mahiyyet { get; set; }
}

// Hesabatın SQL-ində olan parametr token-ləri
public class RiskParametrler
{
    public bool BasTarix { get; set; }   // {BASTARIX}
    public bool SonTarix { get; set; }   // {SONTARIX}
    public bool Tarix    { get; set; }   // {TARIX}
    public bool Hedd     { get; set; }   // {HEDD}
    public bool Il       { get; set; }   // {IL}

    public bool VarMi => BasTarix || SonTarix || Tarix || Hedd || Il;
}

// İstifadəçinin daxil etdiyi parametr dəyərləri (xam)
public class RiskParametrDeyer
{
    public string? BasTarix { get; set; }   // yyyy-MM-dd
    public string? SonTarix { get; set; }
    public string? Tarix    { get; set; }
    public string? Hedd     { get; set; }
    public string? Il       { get; set; }
}

// Dashboard — tək rəqəmli göstərici kartı ([KPI] tag-li sorğudan)
public class RiskKpiDto
{
    public int     Id     { get; set; }
    public string  Ad     { get; set; } = "";
    public string? Alt    { get; set; }              // Mahiyyət-in tag-dan sonrakı hissəsi
    public string  Deyer  { get; set; } = "0";       // formatlanmış rəqəm və ya mətn
    public bool    Reqem  { get; set; } = true;      // dəyər rəqəmdir?
    public string? Xeta   { get; set; }
}

// Dashboard — qrafik ([PIE]/[BAR]/[LINE] tag-li sorğudan: sütun0=etiket, sonuncu=dəyər)
public class RiskChartDto
{
    public int           Id         { get; set; }
    public string        Ad         { get; set; } = "";
    public string        Tip        { get; set; } = "bar";   // bar|pie|line
    public List<string>  Etiketler  { get; set; } = new();
    public List<decimal> Deyerler   { get; set; } = new();
    public string?       Xeta       { get; set; }
    public bool          Bosdur => Etiketler.Count == 0 && string.IsNullOrEmpty(Xeta);
}

// Dashboard — bütün widget-lər + adi hesabat kartları
public class RiskPanelDto
{
    public List<RiskKpiDto>      Kpiler      { get; set; } = new();
    public List<RiskChartDto>    Qrafikler   { get; set; } = new();
    public List<RiskHesabatDto>  Hesabatlar  { get; set; } = new();   // tag-siz (klikləyəndə cədvəl)
    public bool Bosdur => Kpiler.Count == 0 && Qrafikler.Count == 0 && Hesabatlar.Count == 0;
}

// Bir hesabatın icra nəticəsi — dinamik sütunlar + sətirlər
public class RiskNeticeDto
{
    public int             Id           { get; set; }
    public string          Ad           { get; set; } = "";
    public string?         Mahiyyet     { get; set; }
    public OracleNetice    Netice       { get; set; } = new();
    public int             Say          { get; set; }
    public RiskParametrler Parametrler  { get; set; } = new();
    public RiskParametrDeyer Deyerler   { get; set; } = new();
    public bool            IcraOlundu   { get; set; }   // parametrlər tam olub icra edildimi
    public string?         Xeta         { get; set; }

    // Drill-down: sətrə klik → başqa hesabatı aç (Mahiyyətdə {DRILL:ad|param|sütun})
    public int?            DrillId      { get; set; }   // açılacaq hesabat id
    public string?         DrillParam   { get; set; }   // ötürüləcək parametr (il/t/h/bt/st)
    public int             DrillSutun   { get; set; } = -1; // hansı sütunun dəyəri ötürülür
    public bool            DrillVar => DrillId.HasValue && !string.IsNullOrEmpty(DrillParam) && DrillSutun >= 0;
}

// ── Məlumat Bazası — "Axtarılanlar" siyahısının bank müştəriləri ilə yoxlanması ──

// Excel-dən oxunan bir axtarış sətri (Ad Soyad Ata adı / VÖEN / FİN)
public class AxtarisSetriDto
{
    public int     Sira        { get; set; }
    public string? AdSoyadAta  { get; set; }
    public string? Voen        { get; set; }
    public string? Fin         { get; set; }

    /// <summary>
    /// Excel şablonundakı «novu» sütunu (22.09.2026 — istifadəçinin real faylı).
    /// ⚠️ AXTARIŞDA İŞTİRAK ETMİR — yalnız oxunur, cədvəldə və Excel ixracında
    /// göstərilir. Uyğunluq hələ yalnız ad/VÖEN/FİN üzrədir. Bu sütun axtarışa
    /// təsir etməlidirsə (məs. fiziki/hüquqi ayırmaq), qayda əvvəlcə
    /// istifadəçidən soruşulmalıdır — özbaşına fərz etmə.
    /// </summary>
    public string? Novu        { get; set; }
}

// Bir axtarış sətrinin bank bazasında tapılan uyğunluğu (varsa)
public class AxtarisUygunlugDto
{
    public string  Menbe     { get; set; } = "";   // hansı Oracle mənbəyindən tapıldı (məs. "Müştəri qeydiyyatı")
    public string? Regnom    { get; set; }          // VÖEN / qeydiyyat nömrəsi
    public string? AdSoyad   { get; set; }
    public string? UygunSahe { get; set; }          // "AD" | "VOEN" | "FIN" — hansı sahəyə görə tapıldı
}

// Bir axtarış sətrinin tam nəticəsi
public class AxtarisNeticeSetriDto
{
    public AxtarisSetriDto Axtarilan { get; set; } = new();
    public bool Tapildi => Uygunluqlar.Count > 0;
    public List<AxtarisUygunlugDto> Uygunluqlar { get; set; } = new();
}

// Bütöv icra nəticəsi (Excel yükləmədən sonra)
public class AxtarisNeticeDto
{
    public List<AxtarisNeticeSetriDto> Setirler   { get; set; } = new();
    public int     UmumiSay   { get; set; }
    public int     TapilanSay { get; set; }
    public string? Xeta       { get; set; }

    /// <summary>
    /// Axtarış İKİ ADDIMDIR (22.09.2026, istifadəçi qərarı: «həmin exceli tabledə
    /// göstərsin və sonra bazada axtarmaq işlərinə getsin buton ilə»).
    ///   false → Excel oxundu, cədvəldə göstərilir, BMI-yə hələ sorğu getməyib;
    ///   true  → «Bazada axtar» basılıb, `Uygunluqlar` doludur.
    /// Bayraq olmasa ekran «tapılmadı» ilə «hələ axtarılmayıb» halını AYIRD EDƏ
    /// BİLMİR — ikisi də boş `Uygunluqlar` deməkdir.
    /// </summary>
    public bool    AxtarisEdildi { get; set; }

    /// <summary>Excel-in necə oxunduğu (vərəq, başlıq sətri, sütun xəritəsi) — ekranda göstərilir.</summary>
    public string? Menbe { get; set; }
}
