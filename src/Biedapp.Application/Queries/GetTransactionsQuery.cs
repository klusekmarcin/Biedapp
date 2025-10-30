using Biedapp.Domain.Enums;

namespace Biedapp.Application.Queries;
public record GetTransactionsQuery
{
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public string? Category { get; init; }
    public TransactionType? Type { get; init; }
    public int? Limit { get; init; }
}
