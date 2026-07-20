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

    // Kredit Pul Axını (Maturity Ladder) — gözlənilən ödənişlər müddət qutularında.
    Task<MuhasibatMaturityDto> MaturityAsync(DateTime? tarix = null);

    // Kredit Keyfiyyəti & Ehtiyat — təsnifat, ehtiyat örtüyü, girov/LTV, restrukt.
    Task<MuhasibatKeyfiyyetDto> KreditKeyfiyyetAsync(DateTime? tarix = null);

    // Yerləşdirilmiş vəsaitlər — bank yerləşdirmələri (arh_licsch_rs): kontragent/valyuta/faiz/müddət.
    Task<MuhasibatYerlesdirmeDto> YerlesdirmeAsync(DateTime? tarix = null);

    // IFRS 9 ECL — roll-rate stage-keçid modeli (Excel metodologiyasının proqram versiyası).
    // Cari portfelə (arh_licschkre) tarixi risk faizini tətbiq edib gözlənilən kredit itkisini hesablayır.
    Task<MuhasibatIfrs9Dto> Ifrs9EclAsync(DateTime? tarix = null);

    // AMB MHBS 9 — Cədvəl A1: IFRS 9 ECL nəticəsini AMB kredit-növü kateqoriyalarına aqreqasiya edir.
    Task<MuhasibatAmbA1Dto> AmbA1Async(DateTime? tarix = null);

    // AMB MHBS 9 — Cədvəl A1.1: kredit qalığının mərhələlərarası dəyişməsi (roll-forward,
    // dövr əvvəli → dövr sonu snapshot müqayisəsi).
    Task<MuhasibatAmbA1_1Dto> AmbA1_1Async(DateTime? tarix = null);

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
