using Biedapp.Infrastructure.Configuration;
using Biedapp.Infrastructure.EventStore;
using Biedapp.Infrastructure.Models;
using Biedapp.Infrastructure.Security;

using Microsoft.AspNetCore.Cors.Infrastructure;

namespace Biedapp.API.Extensions;
internal static class IServiceCollectionExtensions
{
    private const string _storageTypAppSettingName = "AppSettings:Storage:Type";
    private static EventStoreTypes _defaultStorageType => EventStoreTypes.InMemory;

    public static IServiceCollection AddEventStoreSupport(this IServiceCollection services, IConfiguration configuration)
    {
        string storageType = configuration[_storageTypAppSettingName];

        ILogger<Program> logger = services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();

        if (string.IsNullOrWhiteSpace(storageType))
        {
            storageType = _defaultStorageType.ToString();
            logger.LogWarning("Storage type didn't find in app settings file '{appSettingName}'. Using '{defaultStorageType}' as default storage.", _storageTypAppSettingName, _defaultStorageType);
        }

        if (!Enum.TryParse(storageType, true, out EventStoreTypes eventStoreType))
        {
            logger.LogWarning("Storage type value: '{eventStoreType}' not supported. Set default value '{defaultValue}'.", storageType, _defaultStorageType);
            eventStoreType = _defaultStorageType;
        }

        services.AddSingleton<IEventStore>(sp =>
        {
            switch (eventStoreType)
            {
                case EventStoreTypes.InMemory:
                    logger.LogInformation("Using InMemory event store (data will be lost on restart)");
                    return new InMemoryEventStore();
                case EventStoreTypes.File:
                    string eventsFilePath = EventStoreConfiguration.GetDefaultEventsFilePath();
                    string encryptionKey = EventStoreConfiguration.GetEncryptionKey();
                    EncryptionService encryptionService = new(encryptionKey);

                    logger.LogInformation("Using JSON File event store at: {FilePath}", eventsFilePath);
                    logger.LogInformation("Data is encrypted with machine-specific key");
                    return new JsonFileEventStore(eventsFilePath, encryptionService);
                default:
                    throw new ArgumentOutOfRangeException(nameof(eventStoreType), $"Not expected storage type value: {eventStoreType}");
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
