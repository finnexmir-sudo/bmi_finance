namespace FinNex.Application.DTOs.Goaml;

/// <summary>13.11.2026, DİAQNOSTİKA (müvəqqəti) — tətbiqin Oracle hesabının
/// (FOXPRO) HANSI Oracle nüsxəsinə/sxeminə bağlandığını istifadəçinin öz
/// PL/SQL Developer sessiyası ilə tutuşdurmaq üçün. "BLR" üçün
/// `malumatin_novu` sorğusu app-dan sükutla 0 sətir qaytarır, istifadəçinin
/// şəxsi sessiyasında isə 8/4 verir — bu DTO həmin fərqin FOXPRO-nun özündə
/// (fərqli DB/sxem, fərqli görmə şərti) olub-olmadığını göstərir. Heç bir
/// sahə `null`/boş qalarsa, uyğun sorğu Oracle-da istisna ilə uğursuz olub
/// (<see cref="Xetalar"/>-da səbəbi var).</summary>
public class GoamlOracleDiaqnostikaDto
{
    /// <summary>`select global_name from global_name` — PL/SQL Developer-in
    /// öz pəncərəsində EYNİ sorğunu işlədib bu sahə ilə tutuşdur.</summary>
    public string? GlobalName { get; set; }

    /// <summary>`select sys_context('userenv','db_name') from dual`.</summary>
    public string? DbName { get; set; }

    /// <summary>`select sys_context('userenv','host') from dual` — Oracle
    /// serverinin öz host adı (tətbiqin çalışdığı server YOX).</summary>
    public string? DbHost { get; set; }

    /// <summary>`select sys_context('userenv','instance_name') from dual`.</summary>
    public string? InstanceName { get; set; }

    /// <summary>`select user from dual` — tətbiqin bağlandığı Oracle
    /// istifadəçisi (gözlənilən: FOXPRO).</summary>
    public string? OracleUser { get; set; }

    /// <summary>`select count(*) from odb.malumatin_novu` — cədvəlin ÜMUMİ
    /// sətir sayı, FOXPRO-nun gördüyü qədər.</summary>
    public long? MalumatinNovuUmumiSay { get; set; }

    /// <summary>`select count(*) from odb.malumatin_novu where isare='BLR'`.</summary>
    public long? MalumatinNovuBlrSay { get; set; }

    /// <summary>Tapılıbsa, "BLR" üçün reportType/reportReason (əsl ixrac
    /// kodunun çağırdığı EYNİ sorğu, ayrıca icra olunur).</summary>
    public string? BlrReportTypeVeReason { get; set; }

    /// <summary>Hər sorğunun ÖZ uğursuzluq mətni (varsa) — "Sorğu adı: ORA-xxxxx ..."
    /// formasında, boşdursa hamısı uğurla keçib.</summary>
    public List<string> Xetalar { get; set; } = new();
}
