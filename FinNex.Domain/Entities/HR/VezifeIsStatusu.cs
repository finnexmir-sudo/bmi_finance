namespace FinNex.Domain.Entities.HR
{
    public enum VezifeIsStatusuTipi
    {
        Icrachi = 1,
        MesulSexs = 2
    }

    /// <summary>
    /// Vəzifə ADINA görə "İşdə statusu" təsnifatı (icraçı / məsul şəxs) —
    /// mühasibin şəxsi vərəq Excel-indəki eyni adlı sahə üçün.
    ///
    /// <c>Vezife</c> cədvəli DEPARTAMENT üzrə sətirlənir (eyni ad bir neçə
    /// departamentdə ayrı sətir kimi ola bilər), amma bu təsnifat departamentdən
    /// ASILI DEYİL — ona görə ayrıca, VəzifəAdı üzrə TƏKRARSIZ cədvəldədir.
    /// HR "HR → Vəzifələr → İşdə statusları" səhifəsində bunu bir dəfə qurur;
    /// yeni Vəzifə adı əlavə olunanda siyahıya "İcraçı" ilə default düşür.
    /// </summary>
    public class VezifeIsStatusu : BaseEntity
    {
        public string VezifeAdi { get; set; } = null!;
        public VezifeIsStatusuTipi Status { get; set; } = VezifeIsStatusuTipi.Icrachi;
    }
}
