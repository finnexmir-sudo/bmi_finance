namespace FinNex.Application.DTOs.Pid;

// PID — "Bildirişə düşənlər" (Oracle sorğusu → borcalan + zaminlər).
// Word bildiriş (borcalana + zaminə) generasiyası üçün lazım olan sahələr.

public class BildirisZaminDto
{
    public int     Sira  { get; set; }        // 1..5 — Word üçün hansı zamin
    public string  Ad    { get; set; } = "";
    public string? Unvan { get; set; }
}

public class BildirisSetirDto
{
    // Açar — Word generasiyası zamanı sətri yenidən tapmaq üçün
    public string? Sk  { get; set; }           // subschkre
    public string? Hes { get; set; }           // licschkre

    // Borcalan
    public string  Ad        { get; set; } = "";
    public string? BorcUnvan { get; set; }

    // Kredit / borc
    public DateTime? VTar         { get; set; }   // v_tar (date_open)
    public int?      MuddedAy     { get; set; }   // mudded_ay
    public decimal?  VerKr        { get; set; }   // ver_kr (verilmiş kredit)
    public decimal?  FaizDerecesi { get; set; }   // illik faiz dərəcəsi (%) — Word sssss9
    public decimal?  Esas         { get; set; }   // esas
    public decimal?  Vk           { get; set; }   // vk (vaxtı keçmiş əsas)
    public decimal?  Faiz         { get; set; }   // faiz
    public decimal?  VkFaiz       { get; set; }   // vk_faiz
    public decimal?  ToplamVkBorc { get; set; }   // toplam_VK_borc
    public decimal?  UmumiBorc    { get; set; }   // umumi_borc
    public decimal?  Ayliq        { get; set; }   // ayliq
    public decimal?  Nisbet       { get; set; }   // nisbet_1_5
    public string?   Item01       { get; set; }
    public string?   Item02       { get; set; }
    public DateTime? SonOdTar     { get; set; }   // son_od_tar
    public decimal?  SonOdMeb     { get; set; }   // son_od_meb

    public List<BildirisZaminDto> Zaminlar { get; set; } = new();
}

public class BildirisViewDto
{
    public bool    SorguTapildi { get; set; }
    public string? Xeta         { get; set; }
    public List<BildirisSetirDto> Setirler { get; set; } = new();
}
