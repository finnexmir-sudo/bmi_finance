using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Goaml;

namespace FinNex.Application.Interfaces.Goaml
{
    /// <summary>GOAML — "Əməliyyatlar" tabı.</summary>
    public interface IGoamlService
    {
        /// <summary>Oracle-dan (DOC_DNA_ALL_TAB+RISK) namizəd əməliyyatları oxuyur.
        /// Oracle-a HEÇ NƏ yazılmır.</summary>
        Task<Result<List<GoamlNamizedSetirDto>>> NamizedAxtarAsync(GoamlAxtarisDto dto, CancellationToken ct = default);

        /// <summary>Operator seçdiyi namizədi təsdiqləyir — bizim ÖZ SQL Server-imizə
        /// yazılır (Oracle-a YOX). Eyni namizəd (filial+tarix+recnum) ikinci dəfə
        /// təsdiqlənə bilməz.</summary>
        Task<Result> TesdiqleAsync(GoamlTesdiqDto dto, int isciId);

        Task<Result<List<GoamlEmeliyyatListDto>>> HamisiniGetirAsync();

        Task<Result<GoamlTesdiqDto>> DetalGetirAsync(int id);
    }
}
