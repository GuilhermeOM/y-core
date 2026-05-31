using System.Net;
using Y.Core.SharedKernel;

namespace Y.Threads.Domain.Errors;

public static class ThreadErrors
{
    public static Error ThreadNotFound => new(HttpStatusCode.NotFound, "THREAD_NOT_FOUND", "Thread not found");

}
