using Biedapp.Domain.Enums;

namespace Biedapp.Application.Queries;
public record GetTransactionsQuery
{
    public DateOnly? FromDate { get; init; }
    public DateOnly? ToDate { get; init; }
    public string? Category { get; init; }
    public TransactionType? Type { get; init; }
    public int? Limit { get; init; }
}
