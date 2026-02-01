using Y.Core.SharedKernel;

namespace Y.Threads.Domain.Services;
public interface IFileInspectorService
{
    Result<(string Mime, string Extension)> InspectFileStream(Stream stream);
}
