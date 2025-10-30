namespace Biedapp.Domain.ValueObjects;
public record Category
{
    private static readonly HashSet<string> PredefinedCategories =
    [
        "Food & Dining",
        "Transportation",
        "Shopping",
        "Entertainment",
        "Bills & Utilities",
        "Healthcare",
        "Education",
        "Personal Care",
        "Gifts & Donations",
        "Investments",
        "Salary",
        "Business Income",
        "Other Income",
        "Other Expenses"
    ];

    public string Name { get; init; }

    public Category(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name is required", nameof(name));

        Name = name.Trim();
    }

    public bool IsEssential() => Name switch
    {
        "Food & Dining" => true,
        "Bills & Utilities" => true,
        "Healthcare" => true,
        "Transportation" => true,
        _ => false
    };

    public static IReadOnlySet<string> GetPredefinedCategories() => PredefinedCategories;

    public override string ToString() => Name;
}
