namespace Y.Threads.Domain.ValueObjects;

public sealed record FileUpload(
    Guid BlobId,
    Stream Data,
    string Path,
    string Mime,
    string Extension,
    string Description = "");

public sealed record FileUploadResult(
    Guid BlobId,
    string Url,
    string Path,
    string Mime,
    string Description);
