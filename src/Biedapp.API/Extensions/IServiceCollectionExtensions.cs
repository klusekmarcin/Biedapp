using Biedapp.Infrastructure.Configuration;
using Biedapp.Infrastructure.EventStore;
using Biedapp.Infrastructure.Security;

using Microsoft.AspNetCore.Cors.Infrastructure;

namespace Biedapp.API.Extensions;
internal static class IServiceCollectionExtensions
{
    private const string _storageTypAppSettingName = "AppSettings:Storage:Type";
    private const string _defaultStorageType = "File";

    public static IServiceCollection AddEventStoreSupport(this IServiceCollection services, IConfiguration configuration)
    {
        string storageType = configuration[_storageTypAppSettingName] ?? _defaultStorageType;

        services.AddSingleton<IEventStore>(sp =>
        {
            ILogger<Program> logger = sp.GetRequiredService<ILogger<Program>>();

            switch (storageType.ToLower())
            {
                case "memory" or "inmemory":
                    logger.LogInformation("Using InMemory event store (data will be lost on restart)");
                    return new InMemoryEventStore();
                case "file":
                default:
                    string eventsFilePath = EventStoreConfiguration.GetDefaultEventsFilePath();
                    string encryptionKey = EventStoreConfiguration.GetEncryptionKey();
                    EncryptionService encryptionService = new(encryptionKey);

                    logger.LogInformation("Using JSON File event store at: {FilePath}", eventsFilePath);
                    logger.LogInformation("Data is encrypted with machine-specific key");
                    return new JsonFileEventStore(eventsFilePath, encryptionService);
            };
        });

        return services;
    }

    public static IServiceCollection AddCorsSupport(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddFrontendClientCorsPolicy();
        });

        return services;
    }

    private static void AddFrontendClientCorsPolicy(this CorsOptions options)
    {
        options.AddPolicy(ApiConstants.FrontendClientCorsPolicyName, policy =>
        {
            policy.WithOrigins(
                "http://localhost:4200",
                "https://localhost:4200",
                "https://localhost:52472",
                "https://localhost:52472",
                "http://localhost:63839",
                "https://localhost:63839")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
        });
    }
}
