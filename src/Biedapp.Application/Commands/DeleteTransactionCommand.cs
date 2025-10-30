namespace Biedapp.Application.Commands;

public record DeleteTransactionCommand
{
    public Guid Id { get; init; }

    public void Validate()
    {
        if (Id == Guid.Empty)
            throw new ArgumentException("Transaction ID is required", nameof(Id));
    }
}
