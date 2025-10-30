namespace Biedapp.Domain.ValueObjects;
public sealed record Money
{
    public decimal Amount { get; init; }
    public string Currency { get; init; }
    public Money(decimal amount, string currency = DomainConstants.DefaultCurrencyCode)
    {
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required", nameof(currency));

        Amount = amount;
        Currency = currency;
    }

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot add {other.Currency} to {Currency}");

        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot subtract {other.Currency} from {Currency}");

        return new Money(Amount - other.Amount, Currency);
    }

    public Money MultiplyBy(decimal multiplier)
    {
        if (multiplier <= 0)
            throw new InvalidOperationException($"Cannot multiply by {multiplier}. The number must be greater than 0.");

        return new Money(Amount * multiplier, Currency);
    }

    public override string ToString() => $"{Amount:N2} {Currency}";
}
