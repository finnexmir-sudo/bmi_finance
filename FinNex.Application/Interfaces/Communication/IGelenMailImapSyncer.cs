namespace FinNex.Application.Interfaces.Communication;

public interface IGelenMailImapSyncer
{
    Task<int> SyncNowAsync(CancellationToken ct = default);
}
