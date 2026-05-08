using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Motivasya;

public interface IJetonTeklifleriService
{
    Task<IList<JetonTeklifiDto>> GetGozleyenlerAsync(int? departamentId = null);
    Task<Result> JetonVerAsync(JetonTeklifiVerDto dto, int verenUserId);
    Task<Result> ReddetAsync(int teklifId, int userId);
    Task TapshiriqTamamlandiAsync(int tapshiriqId);
    Task EvezediciQebulEdildiAsync(int evezediciTesdiqId);
    Task DavamiyyetYoxlaAsync(int davamiyyetId);
}
