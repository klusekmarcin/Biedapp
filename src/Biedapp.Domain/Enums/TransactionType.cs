using System.Text.Json.Serialization;

namespace Biedapp.Domain.Enums;
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TransactionType
{
    Income,
    Expense,
}
