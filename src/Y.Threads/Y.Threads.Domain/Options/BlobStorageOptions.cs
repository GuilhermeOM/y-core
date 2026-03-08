namespace Y.Threads.Domain.Options;
public sealed record BlobStorageOptions
{
    public string BaseUrl { get; set; } = string.Empty; 
}
