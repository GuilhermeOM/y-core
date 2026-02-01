using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Y.Threads.Domain.Options;

namespace Y.Threads.Domain;
public static class DependencyInjection
{
    public static IServiceCollection AddDomain(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddOptions(configuration);
    }

    public static IServiceCollection AddOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<BlobStorageOptions>().Bind(configuration.GetSection("Options:BlobStorage"));

        return services;
    }
}
