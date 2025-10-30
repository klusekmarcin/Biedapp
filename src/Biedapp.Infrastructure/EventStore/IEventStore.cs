using Biedapp.Domain.Events;

namespace Biedapp.Infrastructure.EventStore;
public interface IEventStore
{
    Task AppendEventsAsync(IEnumerable<IEvent> events);
    Task<IEnumerable<IEvent>> GetAllEventsAsync();
    string GetFilePath();
}