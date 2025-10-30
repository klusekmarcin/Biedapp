using Biedapp.Domain.Events;
using Biedapp.Infrastructure.Security;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Biedapp.Infrastructure.EventStore;
public class JsonFileEventStore : IEventStore
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly EncryptionService? _encryptionService;
    private readonly bool _useEncryption;

    public JsonFileEventStore(string filePath, EncryptionService encryptionService = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        _filePath = filePath;
        _encryptionService = encryptionService;
        _useEncryption = encryptionService != null;

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        EnsureFileExists();
    }

    private void EnsureFileExists()
    {
        string directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!File.Exists(_filePath))
        {
            string emptyJson = "[]";

            if (_useEncryption && _encryptionService != null)
            {
                string encrypted = _encryptionService.Encrypt(emptyJson);
                File.WriteAllText(_filePath, encrypted);
            }
            else
            {
                File.WriteAllText(_filePath, emptyJson);
            }
        }
    }

    public async Task AppendEventsAsync(IEnumerable<IEvent> events)
    {
        if (events == null || !events.Any())
            return;

        await _semaphore.WaitAsync();
        try
        {
            // Read existing content
            string fileContent = await File.ReadAllTextAsync(_filePath);

            // Decrypt if encryption is enabled
            string json = _useEncryption && _encryptionService != null
                ? _encryptionService.Decrypt(fileContent)
                : fileContent;

            // Deserialize existing events
            List<JsonElement> existingEvents = JsonSerializer.Deserialize<List<JsonElement>>(json, _jsonOptions) ?? [];

            // Append new events
            foreach (IEvent @event in events)
            {
                JsonElement eventJson = JsonSerializer.SerializeToElement(@event, _jsonOptions);
                existingEvents.Add(eventJson);
            }

            // Serialize back to JSON
            string updatedJson = JsonSerializer.Serialize(existingEvents, _jsonOptions);

            // Encrypt if encryption is enabled
            string contentToWrite = _useEncryption && _encryptionService != null
                ? _encryptionService.Encrypt(updatedJson)
                : updatedJson;

            // Write to file
            await File.WriteAllTextAsync(_filePath, contentToWrite);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to append events to {_filePath}", ex);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<IEnumerable<IEvent>> GetAllEventsAsync()
    {
        try
        {
            if (!File.Exists(_filePath))
                return [];

            string fileContent = await File.ReadAllTextAsync(_filePath);

            if (string.IsNullOrWhiteSpace(fileContent))
                return [];

            // Decrypt if encryption is enabled
            string json = _useEncryption && _encryptionService != null
                ? _encryptionService.Decrypt(fileContent)
                : fileContent;

            // Deserialize to JSON elements
            List<JsonElement> jsonElements = JsonSerializer.Deserialize<List<JsonElement>>(json, _jsonOptions);

            if (jsonElements == null || jsonElements.Count == 0)
                return [];

            // Deserialize each event based on its type
            List<IEvent> events = [];
            foreach (JsonElement element in jsonElements)
            {
                try
                {
                    string eventType = element.GetProperty("eventType").GetString();

                    IEvent @event = eventType switch
                    {
                        nameof(TransactionAddedEvent) =>
                            JsonSerializer.Deserialize<TransactionAddedEvent>(element.GetRawText(), _jsonOptions),
                        nameof(TransactionUpdatedEvent) =>
                            JsonSerializer.Deserialize<TransactionUpdatedEvent>(element.GetRawText(), _jsonOptions),
                        nameof(TransactionDeletedEvent) =>
                            JsonSerializer.Deserialize<TransactionDeletedEvent>(element.GetRawText(), _jsonOptions),
                        _ => null
                    };

                    if (@event != null)
                        events.Add(@event);
                }
                catch (Exception ex)
                {
                    // Log and skip corrupted events
                    Console.WriteLine($"Warning: Failed to deserialize event: {ex.Message}");
                }
            }

            return events;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to read events from {_filePath}", ex);
        }
    }

    public string GetFilePath() => _filePath;
}
