using FinNex.Domain.Entities.Goaml;

namespace FinNex.Application.DTOs.Goaml
{
    /// <summary>
    /// "Bax" (sırf oxunuş) ekranı üçün — GoamlTesdiqDto-nun bütün KYC sahələrini
    /// miras alır, üstəlik siyahı/audit sahələrini (Id/Status/Təsdiqləyən/Yaradılma
    /// tarixi) əlavə edir. GoamlEmeliyyatListDto-dakı sahələrlə EYNİ məna daşıyır.
    /// </summary>
    public class GoamlEmeliyyatDetalDto : GoamlTesdiqDto
    {
        public int Id { get; set; }
        public GoamlEmeliyyatStatusu Status { get; set; }
        public string? TesdiqleyenAdi { get; set; }
        public DateTime YaradilmaTarixi { get; set; }
    }
}
