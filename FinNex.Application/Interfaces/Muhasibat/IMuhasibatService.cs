using FinNex.Application.DTOs.Muhasibat;

namespace FinNex.Application.Interfaces.Muhasibat;

public interface IMuhasibatService
{
    // Balans İcmalı — verilmiş tarixə (default: dünən / son iş günü).
    Task<MuhasibatBalansDto> BalansAsync(DateTime? tarix = null);

    // Depozitlər — portfel, TOP-10, valyuta bölgüsü.
    Task<MuhasibatDepozitDto> DepozitAsync(DateTime? tarix = null);
}
