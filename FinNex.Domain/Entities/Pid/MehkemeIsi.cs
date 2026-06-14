namespace FinNex.Domain.Entities.Pid;

public class MehkemeIsi : BaseEntity
{
    public string QeydiyyatNomresi { get; set; } = null!;   // kredit hesabı (licschkre) — kompozit açarın 1-ci hissəsi
    public string? KreditSubHesab  { get; set; }             // subschkre / K.S. (məs. "00") — kompozit açarın 2-ci hissəsi
    public string BorcluAd         { get; set; } = null!;   // Oracle-dan snapshot edilən ad
    public decimal? EsasBorc       { get; set; }             // Oracle-dan snapshot edilən əsas borc
    public decimal? MehkemeXerci   { get; set; }             // əl ilə daxil edilən məhkəmə xərci
    public string? Qerardad        { get; set; }             // proqramda redaktə olunan məhkəmə qərardadı (Oracle-da yox)
    public MehkemeIsiNov  Nov      { get; set; } = MehkemeIsiNov.Diger;
    public MehkemeIsiStatus Status { get; set; } = MehkemeIsiStatus.Hazirlanir;
    public DateTime? BaslamaTarixi { get; set; }
    public string? Qeyd            { get; set; }

    // ── Müştəri açarı (gələcəkdə Oracle/başqa data çəkmək üçün əsas açar) ──
    public string? KreditHesabi    { get; set; }   // hesab №
    public string? Subkod          { get; set; }   // subkod

    // ── Məhkəmə mərhələsi (Excel "Məhkəmə" sheet-inə uyğun) ──
    public int?      Sira                   { get; set; }   // Excel sıra №
    public string?   MehkemeStatusMetn      { get; set; }   // Excel status (BORC BAĞLANDI...) — sərbəst mətn
    public string?   KreditNovuMetn         { get; set; }   // Excel kredit növü (Daşınmaz...) — sərbəst mətn
    public DateTime? MehkemeyeVerilmeTarixi { get; set; }
    public string?   MehkemeSenedi          { get; set; }   // sənəd № + hakim

    // ── İcra mərhələsi detalları (Excel "İcrada olan işlər" sheet-inə uyğun) ──
    public decimal?  QalanBorc        { get; set; }   // qalan borc
    public DateTime? SonOdenisTarixi  { get; set; }   // son ödəniş tarixi
    public string?   Qeydiyyati       { get; set; }   // rayon / qeydiyyatı
    public string?   EmekHaqqiMelumati{ get; set; }   // əmək haqqı / ödəniş məlumatı
    public DateTime? DypSorguTarixi   { get; set; }   // DYP sorğu tarixi
    public string?   AdinaSorgu       { get; set; }   // adına sorğu (avtomobil/əmlak)
    public string?   EmlakaHebs       { get; set; }   // əmlaka həbs barədə
    public string?   Stop             { get; set; }   // stop barədə
    public string?   IcraMemuru       { get; set; }   // icra məmuru (iş səviyyəsində)
    public string?   IcraSonIsler     { get; set; }   // icraçı son işlər / son görüşmə
    public DateTime? DogumTarixi      { get; set; }   // borclunun doğum tarixi
    public string?   Zamin            { get; set; }   // zamin
    public DateTime? QetnameTarixi    { get; set; }   // qətnamə tarixi
    public string?   IsYeri           { get; set; }   // iş yeri
    public string?   IcraQeyd         { get; set; }   // icra üzrə qeyd

    public ICollection<MehkemeMerhelesi> Merheleler { get; set; } = new List<MehkemeMerhelesi>();
}

public class MehkemeMerhelesi : BaseEntity
{
    public int MehkemeIsiId        { get; set; }
    public MehkemeIsi MehkemeIsi   { get; set; } = null!;
    public MerheleTipi MerheleTipi { get; set; }
    public DateTime Tarix          { get; set; }
    public string? Hakim           { get; set; }   // məhkəmə hakiminin adı
    public string? IcraciMemur     { get; set; }   // icra məmurunun adı
    public string? Qeyd            { get; set; }
    public string? SenedYolu       { get; set; }   // FinNex_DMS relative path
}

public enum MehkemeIsiNov
{
    Ipoteka    = 1,
    Istehlak   = 2,
    KartKredit = 3,
    Diger      = 4
}

public enum MehkemeIsiStatus
{
    Hazirlanir  = 1,
    Mehkemede   = 2,
    Icra        = 3,
    Tamamlandi  = 4,
    Baghlandi   = 5
}

public enum MerheleTipi
{
    IddiaVerildi   = 1,
    MehkemeIclasi  = 2,
    QerarVerildi   = 3,
    IcraBaglandi   = 4,
    Odendi         = 5,
    Diger          = 6
}
