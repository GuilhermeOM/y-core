using Y.Core.SharedKernel;
using Y.Threads.Domain.ValueObjects;

namespace Y.Threads.Domain.Services;
public interface IFileInspectorService
{
    Result<FileInspectionResult> InspectFileStream(Stream stream);
}
