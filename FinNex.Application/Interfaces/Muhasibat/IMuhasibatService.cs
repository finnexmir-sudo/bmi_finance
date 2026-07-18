using FinNex.Application.DTOs.Muhasibat;

namespace FinNex.Application.Interfaces.Muhasibat;

public interface IMuhasibatService
{
    // Günlük İcmal (executive) — bütün bölmələrin əsas göstəriciləri bir səhifədə.
    Task<MuhasibatIcmalDto> GunlukIcmalAsync(DateTime? tarix = null);

    // Balans İcmalı — verilmiş tarixə (default: dünən / son iş günü).
    Task<MuhasibatBalansDto> BalansAsync(DateTime? tarix = null);

    // Depozitlər — portfel, TOP-10, valyuta bölgüsü.
    Task<MuhasibatDepozitDto> DepozitAsync(DateTime? tarix = null);

    // Kredit portfeli — cari vəziyyət (tip/təyinat/valyuta/gecikmə bölgüsü, NPL).
    Task<MuhasibatKreditDto> KreditPortfelAsync(DateTime? tarix = null);

    // Mənfəət/Zərər (P&L) — tarix aralığında gəlir/xərc, NII, NIM, Cost/Income.
    Task<MuhasibatMenfeetDto> MenfeetAsync(DateTime? bas = null, DateTime? son = null);

    // Likvidlik — likvid aktivlər + sadə likvidlik nisbətləri.
    Task<MuhasibatLikvidlikDto> LikvidlikAsync(DateTime? tarix = null);

    // Valyuta əməliyyatları — tarix aralığında alış/satış, spred, açıq mövqe.
    Task<MuhasibatValyutaDto> ValyutaAsync(DateTime? bas = null, DateTime? son = null);

    // Rezident / qeyri-rezident — hesab qalıqlarının rezidentlik bölgüsü.
    Task<MuhasibatRezidentDto> RezidentAsync(DateTime? tarix = null);

    // Drill-down — bir kart/sətrin arxasındakı hesab (sətir) detalı.
    // sahe: balans / balans-valyuta / balans-menfeet / likvidlik / depozit / kredit / valyuta / rezident.
    // Cem müvafiq kartdakı rəqəmlə üst-üstə düşür (eyni sorğu + eyni təsnifat).
    Task<MuhasibatDetalDto> DetalAsync(string sahe, string madde,
        DateTime? tarix = null, DateTime? bas = null, DateTime? son = null);
}
