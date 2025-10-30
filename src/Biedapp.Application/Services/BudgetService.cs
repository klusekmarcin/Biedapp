using Biedapp.Application.Commands;
using Biedapp.Application.DTOs;
using Biedapp.Application.Interfaces;
using Biedapp.Application.Queries;
using Biedapp.Domain;
using Biedapp.Domain.Aggregates;
using Biedapp.Domain.Enums;
using Biedapp.Domain.Events;
using Biedapp.Domain.ValueObjects;
using Biedapp.Infrastructure.EventStore;

namespace Biedapp.Application.Services;
public class BudgetService : IBudgetService
{
    private readonly IEventStore _eventStore;

    public BudgetService(IEventStore eventStore)
    {
        ArgumentNullException.ThrowIfNull(eventStore);
        _eventStore = eventStore;
    }

    #region Private Helpers

    private async Task<BudgetAggregate> GetCurrentBudgetAsync()
    {
        IEnumerable<IEvent> events = await _eventStore.GetAllEventsAsync();
        BudgetAggregate budget = new();
        budget.LoadFromHistory(events);
        return budget;
    }

    #endregion

    #region Commands

    public async Task AddTransactionAsync(AddTransactionCommand command)
    {
        command.Validate();

        BudgetAggregate budget = await GetCurrentBudgetAsync();

        budget.AddTransaction(
            new Money(command.Amount, command.Currency),
            new Category(command.Category),
            command.Description,
            command.Type,
            command.Date);

        await _eventStore.AppendEventsAsync(budget.UncommittedEvents);
        budget.MarkEventsAsCommitted();
    }

    public async Task UpdateTransactionAsync(UpdateTransactionCommand command)
    {
        command.Validate();

        var budget = await GetCurrentBudgetAsync();

        budget.UpdateTransaction(
            command.Id,
            new Money(command.Amount, command.Currency),
            new Category(command.Category),
            command.Description,
            command.Type,
            command.Date);

        await _eventStore.AppendEventsAsync(budget.UncommittedEvents);
        budget.MarkEventsAsCommitted();
    }

    public async Task DeleteTransactionAsync(DeleteTransactionCommand command)
    {
        command.Validate();

        BudgetAggregate budget = await GetCurrentBudgetAsync();
        budget.DeleteTransaction(command.Id);

        await _eventStore.AppendEventsAsync(budget.UncommittedEvents);
        budget.MarkEventsAsCommitted();
    }

    #endregion

    #region Queries

    public async Task<List<TransactionDto>> GetAllTransactionsAsync(GetTransactionsQuery? query = null)
    {
        BudgetAggregate budget = await GetCurrentBudgetAsync();
        IEnumerable<Transaction> transactions = budget.Transactions.AsEnumerable();

        // Apply filters
        if (query != null)
        {
            if (query.FromDate.HasValue)
                transactions = transactions.Where(t => t.Date >= query.FromDate.Value);

            if (query.ToDate.HasValue)
                transactions = transactions.Where(t => t.Date <= query.ToDate.Value);

            if (!string.IsNullOrWhiteSpace(query.Category))
                transactions = transactions.Where(t =>
                    t.Category.Name.Equals(query.Category, StringComparison.OrdinalIgnoreCase));

            if (query.Type.HasValue)
                transactions = transactions.Where(t => t.Type == query.Type.Value);

            if (query.Limit.HasValue)
                transactions = transactions.Take(query.Limit.Value);
        }

        return transactions
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Id)
            .Select(t => new TransactionDto
            {
                Id = t.Id,
                Amount = t.Amount.Amount,
                Currency = t.Amount.Currency,
                Category = t.Category.Name,
                Description = t.Description,
                Type = t.Type,
                Date = t.Date
            })
            .ToList();
    }

    public async Task<TransactionDto?> GetTransactionByIdAsync(Guid id)
    {
        BudgetAggregate budget = await GetCurrentBudgetAsync();
        Transaction transaction = budget.Transactions.FirstOrDefault(t => t.Id == id);

        if (transaction == null)
            return null;

        return new TransactionDto
        {
            Id = transaction.Id,
            Amount = transaction.Amount.Amount,
            Currency = transaction.Amount.Currency,
            Category = transaction.Category.Name,
            Description = transaction.Description,
            Type = transaction.Type,
            Date = transaction.Date
        };
    }

    public async Task<BudgetSummaryDto> GetBudgetSummaryAsync()
    {
        BudgetAggregate budget = await GetCurrentBudgetAsync();

        int incomeCount = budget.Transactions.Count(t => t.Type == TransactionType.Income);
        int expenseCount = budget.Transactions.Count(t => t.Type == TransactionType.Expense);

        return new BudgetSummaryDto
        {
            TotalIncome = budget.TotalIncome.Amount,
            TotalExpenses = budget.TotalExpenses.Amount,
            Balance = budget.Balance.Amount,
            Currency = DomainConstants.DefaultCurrencyCode,
            TransactionCount = budget.TransactionCount,
            IncomeCount = incomeCount,
            ExpenseCount = expenseCount
        };
    }

    public async Task<List<CategorySummaryDto>> GetCategorySummaryAsync(TransactionType? type = null)
    {
        BudgetAggregate budget = await GetCurrentBudgetAsync();
        IEnumerable<Transaction> transactions = budget.Transactions.AsEnumerable();

        if (type.HasValue)
            transactions = transactions.Where(t => t.Type == type.Value);

        decimal totalAmount = transactions.Sum(t => t.Amount.Amount);

        List<CategorySummaryDto> categorySummaries = transactions
            .GroupBy(t => t.Category.Name)
            .Select(g => new CategorySummaryDto
            {
                Category = g.Key,
                TotalAmount = g.Sum(t => t.Amount.Amount),
                TransactionCount = g.Count(),
                Currency = "PLN",
                Percentage = totalAmount > 0
                    ? (g.Sum(t => t.Amount.Amount) / totalAmount) * 100
                    : 0
            })
            .OrderByDescending(c => c.TotalAmount)
            .ToList();

        return categorySummaries;
    }

    public async Task<List<string>> GetAllCategoriesAsync()
    {
        BudgetAggregate budget = await GetCurrentBudgetAsync();
        List<string> categories = budget.GetAllCategories().ToList();

        // Add predefined categories that haven't been used yet
        IReadOnlySet<string> predefinedCategories = Category.GetPredefinedCategories();
        foreach (string category in predefinedCategories)
        {
            if (!categories.Contains(category))
                categories.Add(category);
        }

        return categories.OrderBy(c => c).ToList();
    }

    public async Task<Dictionary<string, decimal>> GetMonthlyIncomeExpensesAsync(int year, int month)
    {
        BudgetAggregate budget = await GetCurrentBudgetAsync();
        DateTime startDate = new DateTime(year, month, 1);
        DateTime endDate = startDate.AddMonths(1).AddDays(-1);

        IEnumerable<Transaction> transactions = budget.GetTransactionsByDateRange(startDate, endDate);

        decimal income = transactions
            .Where(t => t.Type == TransactionType.Income)
            .Sum(t => t.Amount.Amount);

        decimal expenses = transactions
            .Where(t => t.Type == TransactionType.Expense)
            .Sum(t => t.Amount.Amount);

        return new Dictionary<string, decimal>
        {
            { "Income", income },
            { "Expenses", expenses },
            { "Balance", income - expenses }
        };
    }

    public async Task<BudgetSummaryDto> GetMonthlyBudgetSummaryAsync(int year, int month)
    {
        BudgetAggregate budget = await GetCurrentBudgetAsync();
        DateTime startDate = new(year, month, 1);
        DateTime endDate = startDate.AddMonths(1).AddDays(-1);

        List<Transaction> monthlyTransactions = budget.GetTransactionsByDateRange(startDate, endDate).ToList();

        decimal totalIncome = monthlyTransactions
            .Where(t => t.Type == TransactionType.Income)
            .Sum(t => t.Amount.Amount);

        decimal totalExpenses = monthlyTransactions
            .Where(t => t.Type == TransactionType.Expense)
            .Sum(t => t.Amount.Amount);

        int incomeCount = monthlyTransactions.Count(t => t.Type == TransactionType.Income);
        int expenseCount = monthlyTransactions.Count(t => t.Type == TransactionType.Expense);

        return new BudgetSummaryDto
        {
            TotalIncome = totalIncome,
            TotalExpenses = totalExpenses,
            Balance = totalIncome - totalExpenses,
            Currency = DomainConstants.DefaultCurrencyCode,
            TransactionCount = monthlyTransactions.Count,
            IncomeCount = incomeCount,
            ExpenseCount = expenseCount
        };
    }

    #endregion

    #region System Info

    public string GetStorageLocation()
    {
        return _eventStore.GetFilePath();
    }

    public async Task<int> GetEventCountAsync()
    {
        IEnumerable<IEvent> events = await _eventStore.GetAllEventsAsync();
        return events.Count();
    }

    #endregion
}
