using FinNex.Application.DTOs.Communication.IsciMail;

namespace FinNex.Application.Interfaces.Communication;

public interface IIsciMailService
{
    Task<int> GonderAsync(IsciMailCreateDto dto, int gonderenIsciId);
    Task<int> TaslagKaydetAsync(IsciMailCreateDto dto, int gonderenIsciId);
    Task TaslagiGonderAsync(int mailId, int gonderenIsciId);
    Task<List<IsciMailDto>> InboxGetirAsync(int isciId);
    Task<List<IsciMailDto>> GonderilenlerGetirAsync(int isciId);
    Task<List<IsciMailDto>> TaslaqlarGetirAsync(int isciId);
    Task<IsciMailDetayDto?> DetayGetirAsync(int mailId, int isciId);
    Task SilAsync(int mailId, int isciId);
    Task<int> OxunmamisSayiAsync(int isciId);
}
