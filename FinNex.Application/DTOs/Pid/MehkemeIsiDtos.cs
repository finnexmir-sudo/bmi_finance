using FinNex.Domain.Entities.Pid;

namespace FinNex.Application.DTOs.Pid;

public class MehkemeIsiListDto
{
    public int Id                  { get; set; }
    public string QeydiyyatNomresi { get; set; } = null!;
    public string BorcluAd         { get; set; } = null!;
    public decimal? EsasBorc       { get; set; }
    public decimal? MehkemeXerci   { get; set; }
    public MehkemeIsiNov Nov       { get; set; }
    public MehkemeIsiStatus Status { get; set; }
    public DateTime? BaslamaTarixi { get; set; }
    public int MerheleCount        { get; set; }
    public DateTime YaradilmaTarixi { get; set; }
}

public class MehkemeIsiDetailDto
{
    public int Id                  { get; set; }
    public string QeydiyyatNomresi { get; set; } = null!;
    public string BorcluAd         { get; set; } = null!;
    public decimal? EsasBorc       { get; set; }
    public decimal? MehkemeXerci   { get; set; }
    public MehkemeIsiNov Nov       { get; set; }
    public MehkemeIsiStatus Status { get; set; }
    public DateTime? BaslamaTarixi { get; set; }
    public string? Qeyd            { get; set; }
    // Müştəri açarı + İcra detalları
    public string? KreditHesabi      { get; set; }
    public string? Subkod            { get; set; }
    public decimal?  QalanBorc        { get; set; }
    public DateTime? SonOdenisTarixi  { get; set; }
    public string?   Qeydiyyati       { get; set; }
    public string?   EmekHaqqiMelumati{ get; set; }
    public DateTime? DypSorguTarixi   { get; set; }
    public string?   AdinaSorgu       { get; set; }
    public string?   EmlakaHebs       { get; set; }
    public string?   Stop             { get; set; }
    public string?   IcraMemuru       { get; set; }
    public string?   IcraSonIsler     { get; set; }
    public DateTime? DogumTarixi      { get; set; }
    public string?   Zamin            { get; set; }
    public DateTime? QetnameTarixi    { get; set; }
    public string?   IsYeri           { get; set; }
    public string?   IcraQeyd         { get; set; }
    public string?   Qerardad         { get; set; }   // məhkəmə qətnaməsi (mətn, əl ilə — yalnız Detal)
    public decimal?  FaizBorcu        { get; set; }   // faiz_meblegi + vk_faiz_meblegi (Oracle canlı)
    public string?   TeminatMetn      { get; set; }   // girovun_novu (Oracle canlı) — Təminat
    public string?   StatusMetn       { get; set; }   // item_01 (Oracle canlı) — Status
    public bool      MaliyyeCanli     { get; set; }   // maliyyə Oracle canlısındandırmı (yoxsa əl ilə/snapshot)
    public DateTime YaradilmaTarixi { get; set; }
    public IList<MehkemeMerheleDto> Merheleler { get; set; } = new List<MehkemeMerheleDto>();
    public IList<ZaminIcraDto> Zaminler { get; set; } = new List<ZaminIcraDto>();
    public IList<MehkemeXerciDto> Xercler { get; set; } = new List<MehkemeXerciDto>();
    public IList<MehkemeSenedDto> Senedler { get; set; } = new List<MehkemeSenedDto>();
}

public class MehkemeMerheleDto
{
    public int Id                  { get; set; }
    public MerheleTipi MerheleTipi { get; set; }
    public DateTime Tarix          { get; set; }
    public string? Hakim           { get; set; }
    public string? IcraciMemur     { get; set; }
    public string? Qeyd            { get; set; }
    public string? SenedYolu       { get; set; }
}

public class MehkemeIsiCreateDto
{
    public string QeydiyyatNomresi { get; set; } = null!;
    public string BorcluAd         { get; set; } = null!;
    public decimal? EsasBorc       { get; set; }
    public decimal? MehkemeXerci   { get; set; }
    public MehkemeIsiNov Nov       { get; set; }
    public MehkemeIsiStatus Status { get; set; } = MehkemeIsiStatus.Hazirlanir;
    public DateTime? BaslamaTarixi { get; set; }
    public string? Qeyd            { get; set; }
}

public class MehkemeIsiUpdateDto
{
    public string? Qerardad        { get; set; }   // məhkəmə qətnaməsi (mətn)
    public decimal? MehkemeXerci   { get; set; }
    public MehkemeIsiNov Nov       { get; set; }
    public MehkemeIsiStatus Status { get; set; }
    public DateTime? BaslamaTarixi { get; set; }
    public string? Qeyd            { get; set; }
    // Müştəri açarı + İcra detalları
    public string? KreditHesabi      { get; set; }
    public string? Subkod            { get; set; }
    public decimal?  QalanBorc        { get; set; }
    public DateTime? SonOdenisTarixi  { get; set; }
    public string?   Qeydiyyati       { get; set; }
    public string?   EmekHaqqiMelumati{ get; set; }
    public DateTime? DypSorguTarixi   { get; set; }
    public string?   AdinaSorgu       { get; set; }
    public string?   EmlakaHebs       { get; set; }
    public string?   Stop             { get; set; }
    public string?   IcraMemuru       { get; set; }
    public string?   IcraSonIsler     { get; set; }
    public DateTime? DogumTarixi      { get; set; }
    public string?   Zamin            { get; set; }
    public DateTime? QetnameTarixi    { get; set; }
    public string?   IsYeri           { get; set; }
    public string?   IcraQeyd         { get; set; }
}

public class MehkemeMerheleCreateDto
{
    public int MehkemeIsiId        { get; set; }
    public MerheleTipi MerheleTipi { get; set; }
    public DateTime Tarix          { get; set; }
    public string? Hakim           { get; set; }
    public string? IcraciMemur     { get; set; }
    public string? Qeyd            { get; set; }
}

public class MehkemeXerciDto
{
    public int Id          { get; set; }
    public decimal Mebleg  { get; set; }
    public DateTime? Tarix { get; set; }
    public string? Mehkeme { get; set; }
}

public class MehkemeXerciCreateDto
{
    public int MehkemeIsiId { get; set; }
    public decimal Mebleg   { get; set; }
    public DateTime? Tarix  { get; set; }
    public string? Mehkeme  { get; set; }
}

public class MehkemeSenedDto
{
    public int Id                { get; set; }
    public string Ad             { get; set; } = "";
    public MehkemeSenedNovu Novu { get; set; }
    public string FaylYolu       { get; set; } = "";
    public DateTime? Tarix       { get; set; }
}

public class MehkemeSenedCreateDto
{
    public int MehkemeIsiId      { get; set; }
    public string? Ad            { get; set; }
    public MehkemeSenedNovu Novu { get; set; }
}

public class OracleAxtarNeticesi
{
    public bool Tapildi            { get; set; }
    public string? BorcluAd       { get; set; }
    public decimal? EsasBorc      { get; set; }
    public string? Xeta           { get; set; }
}
