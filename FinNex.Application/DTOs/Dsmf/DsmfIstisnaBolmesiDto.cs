namespace FinNex.Application.DTOs.Dsmf;

// DSMF Vahid Hesabat Forması — Əlavə №4, Bölmə 3/4/5 (29.10.2026). Hər bölmə
// Hissə 3-dən FƏRQLİ struktur daşıyır — işçi-sətri DEYİL, QANUN MADDƏSİ-sətridir
// (hər sətir bir VM/qanun bəndi, ay üzrə "işçi sayı" + "cəlb olunmayan məbləğ").
public class DsmfIstisnaBolmesiDto
{
    public int BolmeNo { get; set; } // 3, 4 və ya 5
    public string Basliq { get; set; } = "";

    public int Il { get; set; }
    public int Rub { get; set; }
    public int Ay1 { get; set; }
    public int Ay2 { get; set; }
    public int Ay3 { get; set; }
    public string Ay1Adi { get; set; } = "";
    public string Ay2Adi { get; set; } = "";
    public string Ay3Adi { get; set; } = "";

    public List<DsmfIstisnaSetirDto> Setirler { get; set; } = new();
}

public class DsmfIstisnaSetirDto
{
    public int SiraNo { get; set; }

    /// <summary>Qanun maddəsinin tam mətni ("5.2.12 - Sığortaedənin ...").</summary>
    public string Gostərici { get; set; } = "";

    public int Ay1IsciSayi { get; set; }
    public decimal Ay1Mebleg { get; set; }
    public int Ay2IsciSayi { get; set; }
    public decimal Ay2Mebleg { get; set; }
    public int Ay3IsciSayi { get; set; }
    public decimal Ay3Mebleg { get; set; }
}
