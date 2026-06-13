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
