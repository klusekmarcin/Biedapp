using Biedapp.Domain.Events;

namespace Biedapp.Infrastructure.EventStore;
public class InMemoryEventStore : IEventStore
{
    private readonly List<IEvent> _events = [];

    public Task AppendEventsAsync(IEnumerable<IEvent> events)
    {
        _events.AddRange(events);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<IEvent>> GetAllEventsAsync()
    {
        return Task.FromResult<IEnumerable<IEvent>>(_events);
    }

    public string GetFilePath() => "In-Memory";
}
