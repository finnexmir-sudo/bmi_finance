namespace FinNex.Application.DTOs.HR.IcraciNo;

// "İcraçı Nömrələri" səhifəsi — işçi + təyin olunmuş icraçı nömrəsi (oxumaq üçün)
public class IcraciNoSetirDto
{
    public int     IsciId      { get; set; }
    public string  AdSoyad     { get; set; } = "";
    public string? Vezife      { get; set; }
    public string? Departament { get; set; }
    public bool    Aktivdir    { get; set; }
    public int?    IcraciNo    { get; set; }
}

// Toplu yadda saxlama — hər işçi üçün təyin olunan icraçı nömrəsi
public class IcraciNoTeyinDto
{
    public int  IsciId   { get; set; }
    public int? IcraciNo { get; set; }
}
