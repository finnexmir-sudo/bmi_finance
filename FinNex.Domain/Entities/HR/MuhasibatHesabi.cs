namespace FinNex.Domain.Entities.HR
{
    // Mühasibat (proводka) hesabları — açar→hesab nömrəsi.
    // Genişlənən ayar: avans Debet hesabı indi, maaş hesabları sonra
    // (yalnız yeni sətir əlavə olunur, sxem dəyişmir).
    // Məs: Acar="AvansDebet", HesabNomresi="25052000010000300000".
    public class MuhasibatHesabi : BaseEntity
    {
        public string Acar         { get; set; } = null!;   // unikal açar
        public string Ad           { get; set; } = null!;   // izah (UI üçün)
        public string HesabNomresi { get; set; } = null!;   // 20 rəqəmli hesab
        public bool   Aktiv        { get; set; } = true;
    }
}
