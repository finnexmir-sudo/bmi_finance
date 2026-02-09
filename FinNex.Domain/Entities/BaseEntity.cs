public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime YaradilmaTarixi { get; set; } = DateTime.Now;

    // İcracı ID-ləri artıq int (çünki AppUser-i int etdik)
    public int? YaradanIcraciId { get; set; }
    public int? YenileyenIcraciId { get; set; }
    public int? SilenIcraciId { get; set; }

    public DateTime? YenilenmeTarixi { get; set; }
    public bool Silinib { get; set; } = false;
    public DateTime? SilinmeTarixi { get; set; }
}

