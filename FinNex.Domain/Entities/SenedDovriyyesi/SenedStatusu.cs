namespace FinNex.Domain.Entities.SenedDovriyyesi
{
    public enum SenedStatusu
    {
        Yeni = 1,
        Yoxlanilir = 2,
        Tesdiq = 3,
        Arxiv = 4
    }
    public enum MexfilikSeviyesi
    {
        Public = 1,
        Internal = 2,
        Confidential = 3,
        Strict = 4
    }
    public enum PrincipalType
    {
        User = 1,
        Role = 2,
        Sobe = 3
    }
    public enum AccessPermission
    {
        Oxu = 1,
        Yukle = 2,
        Redakte = 3,
        Sil = 4,
        TamHuquq = 5
    }
}
