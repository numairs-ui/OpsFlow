namespace OpsFlow.Domain.Interfaces;

public sealed record UploadUrlResult(string UploadUrl, string BlobUrl);

public interface IStorageProvider
{
    Task<UploadUrlResult> GetUploadUrlAsync(string blobPath, TimeSpan expiry, CancellationToken ct = default);
    Task DeleteAsync(string blobPath, CancellationToken ct = default);
}
