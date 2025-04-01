using AbyssScan.Core.Interfaces;
using AbyssScan.Core.Models;

public interface IDirectoryScanner
{
    Task<IEnumerable<string>> ScanAsync(
        string selectedUserAgent, string baseUrl, IEnumerable<string> paths, int depth, 
        CancellationToken cancellationToken, IThrottlingService throttlingService);
    Task<IEnumerable<string>> ScanAsync(
        string selectedUserAgent, string baseUrl, int depth, CancellationToken cancellationToken, 
        IThrottlingService throttlingService);
    IEnumerable<string> GetFoundDirectories();
}
