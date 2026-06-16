using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using OpsFlow.Domain.Interfaces;

namespace OpsFlow.Infrastructure.Azure;

internal sealed class AzureBlobStorageProvider(BlobServiceClient blobClient, IConfiguration configuration)
    : IStorageProvider
{
    private readonly string _containerName = configuration["AZURE_BLOB_CONTAINER_NAME"] ?? "task-photos";

    public Task<UploadUrlResult> GetUploadUrlAsync(string blobPath, TimeSpan expiry, CancellationToken ct = default)
    {
        var container = blobClient.GetBlobContainerClient(_containerName);
        var blob = container.GetBlobClient(blobPath);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _containerName,
            BlobName = blobPath,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.Add(expiry),
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Write | BlobSasPermissions.Create);

        var uploadUrl = blob.GenerateSasUri(sasBuilder).ToString();
        var blobUrl = blob.Uri.ToString();

        return Task.FromResult(new UploadUrlResult(uploadUrl, blobUrl));
    }

    public async Task DeleteAsync(string blobPath, CancellationToken ct = default)
    {
        var container = blobClient.GetBlobContainerClient(_containerName);
        await container.GetBlobClient(blobPath).DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: ct);
    }
}
