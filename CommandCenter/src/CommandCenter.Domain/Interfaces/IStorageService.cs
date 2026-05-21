using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CommandCenter.Domain.Interfaces;

public interface IStorageService
{
    Task<string> UploadAsync(Stream content, string objectName, string contentType, CancellationToken ct = default);
    Task<Stream> DownloadAsync(string objectName, CancellationToken ct = default);
    Task DeleteAsync(string objectName, CancellationToken ct = default);
    string GetGcsUri(string objectName);
}
