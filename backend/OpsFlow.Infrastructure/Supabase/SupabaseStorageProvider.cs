using Microsoft.Extensions.Configuration;
using OpsFlow.Domain.Interfaces;
using Supabase.Storage;

namespace OpsFlow.Infrastructure.Supabase;

internal sealed class SupabaseStorageProvider(
    global::Supabase.Client supabase,
    IConfiguration configuration) : IStorageProvider
{
    private readonly string _bucket = configuration["SUPABASE_STORAGE_BUCKET"] ?? "task-photos";

    public async Task<UploadUrlResult> GetUploadUrlAsync(string blobPath, TimeSpan expiry, CancellationToken ct = default)
    {
        // Cast to concrete type — CreateUploadSignedUrl and GetPublicUrl are not on the interface
        var fileApi = (StorageFileApi)supabase.Storage.From(_bucket);
        var signed = await fileApi.CreateUploadSignedUrl(blobPath);
        var publicUrl = fileApi.GetPublicUrl(blobPath, null);
        return new UploadUrlResult(signed.SignedUrl.ToString(), publicUrl);
    }

    public async Task DeleteAsync(string blobPath, CancellationToken ct = default)
    {
        var fileApi = (StorageFileApi)supabase.Storage.From(_bucket);
        await fileApi.Remove(new List<string> { blobPath });
    }
}
