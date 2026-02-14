namespace CMMS.Core.Interfaces;

public interface IUpdateService
{
    UpdateInfo? GetAvailableUpdate();
    void SetAvailableUpdate(UpdateInfo? update);
    Task DownloadUpdateAsync(UpdateInfo update, CancellationToken ct = default);
    void LaunchUpdater(string updateZipPath);
}
