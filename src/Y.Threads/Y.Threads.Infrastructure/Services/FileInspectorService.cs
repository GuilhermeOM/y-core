using MimeDetective;
using Y.Core.SharedKernel;
using Y.Threads.Domain.Services;

namespace Y.Threads.Infrastructure.Services;
internal sealed class FileInspectorService : IFileInspectorService
{
    private readonly IContentInspector _contentInspector;

    public FileInspectorService(IContentInspector contentInspector)
    {
        _contentInspector = contentInspector;
    }

    public Result<(string Mime, string Extension)> InspectFileStream(Stream stream)
    {
        var inspect = _contentInspector.Inspect(stream);

        var mimeResults = inspect.ByMimeType();
        var mime = mimeResults.FirstOrDefault()?.MimeType;

        if (string.IsNullOrWhiteSpace(mime))
        {
            return Result.Failure<(string Mime, string Extension)>(InspectionErrors.UnableToDetermineMime);
        }

        var extensionResults = inspect.ByFileExtension();
        var extension = extensionResults.FirstOrDefault()?.Extension;

        if (string.IsNullOrWhiteSpace(mime) || string.IsNullOrWhiteSpace(extension))
        {
            return Result.Failure<(string Mime, string Extension)>(InspectionErrors.UnableToDetermineExtension);
        }

        return Result.Success((mime, extension));
    }
}

public static class InspectionErrors
{
    public static Error UnableToDetermineMime => new("UNABLE_TO_DETERMINE_MIME", "Unable to determine file mime type");
    public static Error UnableToDetermineExtension => new("UNABLE_TO_DETERMINE_EXTENSION", "Unable to determine file extension");
}
