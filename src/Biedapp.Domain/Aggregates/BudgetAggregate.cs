using Biedapp.Domain.Aggregates;
using Biedapp.Domain.Enums;
using Biedapp.Domain.Events;
using Biedapp.Domain.ValueObjects;

namespace Biedapp.Domain.Aggregates;
public sealed class BudgetAggregate
{
    private readonly List<IEvent> _uncommittedEvents = [];
    private readonly Dictionary<Guid, Transaction> _transactions = [];

    public IReadOnlyCollection<Transaction> Transactions => _transactions.Values.ToList();
    public IReadOnlyCollection<IEvent> UncommittedEvents => _uncommittedEvents.AsReadOnly();

    public Money TotalIncome
    {
        get
        {
            decimal total = _transactions.Values
                .Where(t => t.IsIncome())
                .Sum(t => t.Amount.Amount);
            return new Money(total);
        }
    }

    public Money TotalExpenses
    {
        get
        {
            var total = _transactions.Values
                .Where(t => t.IsExpense())
                .Sum(t => t.Amount.Amount);
            return new Money(total);
        }
    }

    public Money Balance => TotalIncome.Subtract(TotalExpenses);

    public int TransactionCount => _transactions.Count;

    #region Commands
    public void AddTransaction(
        Money amount,
        Category category,
        string description,
        TransactionType type,
        DateTime date)
    {
        var transactionId = Guid.NewGuid();

        var @event = new TransactionAddedEvent(
            transactionId,
            amount.Amount,
            amount.Currency,
            category.Name,
            description,
            type,
            date);

        Apply(@event);
        _uncommittedEvents.Add(@event);
    }

    public void UpdateTransaction(
        Guid transactionId,
        Money amount,
        Category category,
        string description,
        TransactionType type,
        DateTime date)
    {
        if (!_transactions.ContainsKey(transactionId))
            throw new InvalidOperationException($"Transaction {transactionId} not found");

        var @event = new TransactionUpdatedEvent(
            transactionId,
            amount.Amount,
            amount.Currency,
            category.Name,
            description,
            type,
            date);

        Apply(@event);
        _uncommittedEvents.Add(@event);
    }

    public void DeleteTransaction(Guid transactionId)
    {
        if (!_transactions.ContainsKey(transactionId))
            throw new InvalidOperationException($"Transaction {transactionId} not found");

        var @event = new TransactionDeletedEvent(transactionId);

        Apply(@event);
        _uncommittedEvents.Add(@event);
    }
    #endregion

    public void LoadFromHistory(IEnumerable<IEvent> history)
    {
        foreach (var @event in history)
        {
            ApplyEvent(@event);
        }
    }

    public void MarkEventsAsCommitted()
    {
        _uncommittedEvents.Clear();
    }

    #region Apply
    private void ApplyEvent(IEvent @event)
    {
        switch (@event)
        {
            case TransactionAddedEvent e:
                Apply(e);
                break;
            case TransactionUpdatedEvent e:
                Apply(e);
                break;
            case TransactionDeletedEvent e:
                Apply(e);
                break;
            default:
                throw new InvalidOperationException($"Unknown event type: {@event.GetType().Name}");
        }
    }

    private void Apply(TransactionAddedEvent @event)
    {
        var transaction = new Transaction(
            @event.TransactionId,
            new Money(@event.Amount, @event.Currency),
            new Category(@event.Category),
            @event.Description,
            @event.Type,
            @event.Date);

        _transactions[@event.TransactionId] = transaction;
    }

    private void Apply(TransactionUpdatedEvent @event)
    {
        Transaction transaction = new(
            @event.TransactionId,
            new Money(@event.Amount, @event.Currency),
            new Category(@event.Category),
            @event.Description,
            @event.Type,
            @event.Date);

        _transactions[@event.TransactionId] = transaction;
    }

    private void Apply(TransactionDeletedEvent @event)
    {
        _transactions.Remove(@event.TransactionId);
    }
    #endregion

    #region Get
    public IEnumerable<Transaction> GetTransactionsByCategory(string categoryName)
    {
        return _transactions.Values
            .Where(t => t.Category.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(t => t.Date);
    }

    public IEnumerable<Transaction> GetTransactionsByType(TransactionType type)
    {
        return _transactions.Values
            .Where(t => t.Type == type)
            .OrderByDescending(t => t.Date);
    }

    public IEnumerable<Transaction> GetTransactionsByDateRange(DateTime from, DateTime to)
    {
        return _transactions.Values
            .Where(t => t.Date >= from && t.Date <= to)
            .OrderByDescending(t => t.Date);
    }

    public IEnumerable<string> GetAllCategories()
    {
        return _transactions.Values
            .Select(t => t.Category.Name)
            .Distinct()
            .OrderBy(c => c);
    }
    #endregion
}