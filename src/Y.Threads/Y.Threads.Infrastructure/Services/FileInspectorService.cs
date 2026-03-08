using MimeDetective;
using Y.Core.SharedKernel;
using Y.Threads.Domain.Services;
using Y.Threads.Domain.ValueObjects;

namespace Y.Threads.Infrastructure.Services;
internal sealed class FileInspectorService : IFileInspectorService
{
    private readonly IContentInspector _contentInspector;

    public FileInspectorService(IContentInspector contentInspector)
    {
        _contentInspector = contentInspector;
    }

    public Result<FileInspectionResult> InspectFileStream(Stream stream)
    {
        var inspect = _contentInspector.Inspect(stream);

        var mimeResults = inspect.ByMimeType();
        var mime = mimeResults.FirstOrDefault()?.MimeType;

        if (string.IsNullOrWhiteSpace(mime))
        {
            return Result.Failure<FileInspectionResult>(InspectionErrors.UnableToDetermineMime);
        }

        var extensionResults = inspect.ByFileExtension();
        var extension = extensionResults.FirstOrDefault()?.Extension;

        if (string.IsNullOrWhiteSpace(mime) || string.IsNullOrWhiteSpace(extension))
        {
            return Result.Failure<FileInspectionResult>(InspectionErrors.UnableToDetermineExtension);
        }

        return Result.Success(new FileInspectionResult(mime, extension));
    }
}

public static class InspectionErrors
{
    public static Error UnableToDetermineMime => new("UNABLE_TO_DETERMINE_MIME", "Unable to determine file mime type");
    public static Error UnableToDetermineExtension => new("UNABLE_TO_DETERMINE_EXTENSION", "Unable to determine file extension");
}
