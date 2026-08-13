using FinNex.Domain.Entities.Kredit;

namespace FinNex.Application.DTOs.Kredit.Muqavile;

/// <summary>Bir sayğacın (növ + il) cari vəziyyəti — idarəetmə/köçürmə ekranı üçün.</summary>
public class MuqavileSayghacDto
{
    public MuqavileNomreNovu Novu     { get; set; }
    public string            NovuAdi  { get; set; } = "";   // ekranda görünən ad
    public string            OracleSutun { get; set; } = ""; // BMI qarşılığı (izah üçün)
    public int               Il       { get; set; }
    public int               SonNomre { get; set; }         // son VERİLMİŞ nömrə

    public int Novbeti => SonNomre + 1;
}

/// <summary>
/// BMI sayğaclarının köçürülməsi üçün bir sətir: Oracle-dakı dəyər, ondan
/// hesablanan FinNex SonNomre və nəticədə veriləcək növbəti nömrə.
/// </summary>
public class MuqavileSayghacKocurmeSetirDto
{
    public MuqavileNomreNovu Novu        { get; set; }
    public string            NovuAdi     { get; set; } = "";
    public string            OracleSutun { get; set; } = "";
    public int               Il          { get; set; }

    // Oracle-dakı xam dəyər
    public int  OracleDeyer { get; set; }

    // Oracle bu sayğacda NÖVBƏTİ nömrəni saxlayır? (kr_zaminlik/kr_menzil → bəli,
    // kr_zaminler → xeyr, o SONUNCUNU saxlayır). Çevrilmə buna görə fərqlənir.
    public bool OracleNovbetiSaxlayir { get; set; }

    // FinNex-ə yazılacaq dəyər
    public int  YeniSonNomre { get; set; }

    // Mövcud FinNex dəyəri (sətir yoxdursa null)
    public int? FinNexSonNomre { get; set; }

    public int  Novbeti   => YeniSonNomre + 1;
    public bool Kocurulub => FinNexSonNomre.HasValue && FinNexSonNomre.Value == YeniSonNomre;
}

/// <summary>Köçürmə ekranının bütün vəziyyəti.</summary>
public class MuqavileSayghacKocurmeDto
{
    public List<MuqavileSayghacKocurmeSetirDto> Setirler { get; set; } = new();

    public IEnumerable<int> Iller => Setirler.Select(x => x.Il).Distinct().OrderByDescending(x => x);
    public int TamOlmayan => Setirler.Count(x => !x.Kocurulub);
}

/// <summary>Köçürmənin nəticəsi.</summary>
public class MuqavileSayghacKocurmeNeticeDto
{
    public int Il      { get; set; }
    public int Yazilan { get; set; }   // yeni yaradılan və ya yenilənən sayğac sayı
    public int Kecilen { get; set; }   // artıq eyni dəyərdə olduğu üçün toxunulmayan
}
