# BiedApp - Detailed Design Document

## Table of Contents
1. [Project Overview](#1-project-overview)
2. [Architecture](#2-architecture)
3. [Backend Design](#3-backend-design)
4. [Frontend Design](#4-frontend-design)
5. [Communication Flow](#5-communication-flow)
6. [Configuration](#6-configuration)
7. [Data Model](#7-data-model)
8. [Security](#8-security)
9. [Deployment](#9-deployment)

---

## 1. Project Overview

### 1.1 Purpose
BiedApp is a home budget management application designed to track personal income and expenses using event sourcing pattern. The application runs entirely on a local machine without requiring external dependencies or cloud services.

### 1.2 Technology Stack

**Backend:**
- .NET 8.0
- ASP.NET Core Web API
- C# 12

**Frontend:**
- Angular 18+
- TypeScript 5+
- SCSS for styling

**Storage:**
- JSON file with AES-256 encryption
- Stored in user's AppData folder

### 1.3 Key Features
- Transaction management (CRUD operations)
- Event sourcing for complete audit trail
- Encrypted local storage
- Monthly and overall budget summaries
- Category-based expense tracking
- Statistical analysis and trends
- Data export functionality

---

## 2. Architecture

### 2.1 High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Angular Frontend                          │
│  ┌────────────┐  ┌────────────┐  ┌────────────┐           │
│  │ Components │  │  Services  │  │   Models   │           │
│  └────────────┘  └────────────┘  └────────────┘           │
└──────────────────────┬──────────────────────────────────────┘
                       │ HTTP/JSON (HTTPS on localhost:5001)
                       ↓
┌─────────────────────────────────────────────────────────────┐
│                    ASP.NET Core API                          │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  Controllers (Presentation Layer)                      │ │
│  └──────────────────────┬─────────────────────────────────┘ │
│                         ↓                                    │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  Application Layer (Use Cases, Services)               │ │
│  └──────────────────────┬─────────────────────────────────┘ │
│                         ↓                                    │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  Domain Layer (Business Logic, Aggregates, Events)    │ │
│  └──────────────────────┬─────────────────────────────────┘ │
│                         ↓                                    │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  Infrastructure Layer (Event Store, Encryption)        │ │
│  └────────────────────────────────────────────────────────┘ │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ↓
┌─────────────────────────────────────────────────────────────┐
│          Encrypted events.json (AppData folder)             │
│  Location: %LOCALAPPDATA%\BiedApp\events.json              │
└─────────────────────────────────────────────────────────────┘
```

### 2.2 Architectural Pattern

**Clean Architecture** with the following layers:

1. **Domain Layer** (Core)
   - Contains business logic
   - No dependencies on other layers
   - Defines entities, value objects, aggregates, and events

2. **Application Layer**
   - Contains application business rules
   - Implements use cases
   - Depends only on Domain layer

3. **Infrastructure Layer**
   - Implements external concerns (storage, encryption)
   - Depends on Domain and Application layers

4. **Presentation Layer** (API)
   - Exposes REST endpoints
   - Depends on all other layers

### 2.3 Design Patterns Used

#### 2.3.1 Event Sourcing
Instead of storing current state, all changes are stored as events. Current state is derived by replaying all events.

**Benefits:**
- Complete audit trail
- Ability to reconstruct state at any point in time
- Easy debugging (see all changes)
- Natural fit for undo/redo functionality

#### 2.3.2 CQRS Lite (Command Query Separation)
Commands (write operations) and Queries (read operations) are separated at the method level.

**Commands:**
- `AddTransactionCommand`
- `UpdateTransactionCommand`
- `DeleteTransactionCommand`

**Queries:**
- `GetTransactionsQuery`
- `GetBudgetSummaryQuery`

#### 2.3.3 Aggregate Pattern
`BudgetAggregate` acts as a consistency boundary, ensuring all business rules are enforced.

#### 2.3.4 Repository Pattern
`IEventStore` abstracts data access, allowing easy swapping of storage implementations.

#### 2.3.5 Value Object Pattern
`Money` and `Category` are immutable value objects with built-in validation.

---

## 3. Backend Design

### 3.1 Project Structure

```
BiedApp.sln
├── BiedApp.API/                  # Presentation Layer
│   ├── Controllers/
│   │   ├── BudgetController.cs
│   │   ├── StatisticsController.cs
│   │   └── SystemController.cs
│   ├── Program.cs
│   └── appsettings.json
│
├── BiedApp.Application/          # Application Layer
│   ├── Commands/
│   │   ├── AddTransactionCommand.cs
│   │   ├── UpdateTransactionCommand.cs
│   │   └── DeleteTransactionCommand.cs
│   ├── Queries/
│   │   └── GetTransactionsQuery.cs
│   ├── DTOs/
│   │   ├── TransactionDto.cs
│   │   ├── BudgetSummaryDto.cs
│   │   └── CategorySummaryDto.cs
│   └── Services/
│       ├── BudgetService.cs
│       └── StatisticsService.cs
│
├── BiedApp.Domain/               # Domain Layer
│   ├── Common/
│   │   └── IEvent.cs
│   ├── Events/
│   │   ├── TransactionAddedEvent.cs
│   │   ├── TransactionUpdatedEvent.cs
│   │   └── TransactionDeletedEvent.cs
│   ├── Entities/
│   │   └── Transaction.cs
│   ├── Aggregates/
│   │   └── BudgetAggregate.cs
│   ├── ValueObjects/
│   │   ├── Money.cs
│   │   └── Category.cs
│   └── Enums/
│       └── TransactionType.cs
│
└── BiedApp.Infrastructure/       # Infrastructure Layer
    ├── EventStore/
    │   ├── IEventStore.cs
    │   ├── JsonFileEventStore.cs
    │   └── InMemoryEventStore.cs
    ├── Security/
    │   └── EncryptionService.cs
    └── Configuration/
        └── EventStoreConfiguration.cs
```

### 3.2 Domain Layer

#### 3.2.1 IEvent Interface

```csharp
namespace BiedApp.Domain.Common;

public interface IEvent
{
    Guid EventId { get; init; }
    DateTime Timestamp { get; init; }
    string EventType { get; }
}
```

**Purpose:** Base interface for all domain events.

**Properties:**
- `EventId`: Unique identifier for the event
- `Timestamp`: When the event occurred (UTC)
- `EventType`: Discriminator for deserialization

#### 3.2.2 Events

**TransactionAddedEvent**
```csharp
public record TransactionAddedEvent : IEvent
{
    public Guid EventId { get; init; }
    public DateTime Timestamp { get; init; }
    public string EventType => nameof(TransactionAddedEvent);
    
    public Guid TransactionId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; }
    public string Category { get; init; }
    public string Description { get; init; }
    public TransactionType Type { get; init; }
    public DateTime Date { get; init; }
}
```

**Purpose:** Represents that a transaction was added to the budget.

**When Created:** User adds a new income or expense transaction.

**TransactionUpdatedEvent**
```csharp
public record TransactionUpdatedEvent : IEvent
{
    // Same properties as TransactionAddedEvent
}
```

**Purpose:** Represents that a transaction was modified.

**When Created:** User edits an existing transaction.

**TransactionDeletedEvent**
```csharp
public record TransactionDeletedEvent : IEvent
{
    public Guid EventId { get; init; }
    public DateTime Timestamp { get; init; }
    public string EventType => nameof(TransactionDeletedEvent);
    
    public Guid TransactionId { get; init; }
}
```

**Purpose:** Represents that a transaction was deleted.

**When Created:** User deletes a transaction.

#### 3.2.3 Value Objects

**Money**
```csharp
public record Money
{
    public decimal Amount { get; init; }
    public string Currency { get; init; }
    
    public Money(decimal amount, string currency = "PLN")
    {
        if (amount < 0)
            throw new ArgumentException("Money cannot be negative");
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency required");
            
        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }
    
    public Money Add(Money other) { /* ... */ }
    public Money Subtract(Money other) { /* ... */ }
    public Money MultiplyBy(decimal multiplier) { /* ... */ }
}
```

**Purpose:** Encapsulates monetary values with currency.

**Characteristics:**
- Immutable (record type)
- Self-validating (no negative amounts)
- Rich behavior (arithmetic operations)
- Value equality (two Money objects with same amount are equal)

**Category**
```csharp
public record Category
{
    public string Name { get; init; }
    
    public Category(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name required");
        Name = name.Trim();
    }
    
    public bool IsEssential() { /* ... */ }
}
```

**Purpose:** Represents a transaction category with validation.

**Characteristics:**
- Immutable
- Self-validating
- Domain behavior (IsEssential)

#### 3.2.4 Entities

**Transaction**
```csharp
public record Transaction
{
    public Guid Id { get; init; }
    public Money Amount { get; init; }
    public Category Category { get; init; }
    public string Description { get; init; }
    public TransactionType Type { get; init; }
    public DateTime Date { get; init; }
    
    public bool IsIncome() => Type == TransactionType.Income;
    public bool IsExpense() => Type == TransactionType.Expense;
}
```

**Purpose:** Represents a single financial transaction.

**Identity:** Identified by `Id` (Guid), not by properties.

#### 3.2.5 Aggregates

**BudgetAggregate**

```csharp
public class BudgetAggregate
{
    // Private state
    private readonly List<IEvent> _uncommittedEvents = new();
    private readonly Dictionary<Guid, Transaction> _transactions = new();
    
    // Public properties (computed from state)
    public IReadOnlyCollection<Transaction> Transactions { get; }
    public Money TotalIncome { get; }
    public Money TotalExpenses { get; }
    public Money Balance { get; }
    
    // Commands (change state)
    public void AddTransaction(Money, Category, ...) { /* ... */ }
    public void UpdateTransaction(Guid, Money, Category, ...) { /* ... */ }
    public void DeleteTransaction(Guid) { /* ... */ }
    
    // Event sourcing
    public void LoadFromHistory(IEnumerable<IEvent>) { /* ... */ }
    private void Apply(TransactionAddedEvent) { /* ... */ }
    private void Apply(TransactionUpdatedEvent) { /* ... */ }
    private void Apply(TransactionDeletedEvent) { /* ... */ }
}
```

**Purpose:** 
- Enforces business rules
- Maintains consistency
- Generates events
- Rebuilds state from events

**Responsibilities:**
1. **Validation**: Ensures all business rules are met
2. **Event Creation**: Creates events for all state changes
3. **State Management**: Maintains current state in memory
4. **Event Application**: Applies events to rebuild state

**Key Methods:**

**AddTransaction()**
```csharp
public void AddTransaction(
    Money amount,
    Category category,
    string description,
    TransactionType type,
    DateTime date)
{
    var transactionId = Guid.NewGuid();
    
    // 1. Create event
    var @event = new TransactionAddedEvent(
        transactionId,
        amount.Amount,
        amount.Currency,
        category.Name,
        description,
        type,
        date
    );
    
    // 2. Apply to state
    Apply(@event);
    
    // 3. Remember for saving
    _uncommittedEvents.Add(@event);
}
```

**Flow:**
1. Generate new transaction ID
2. Create event with all data
3. Apply event to update internal state
4. Store in uncommitted events list (to be saved later)

**LoadFromHistory()**
```csharp
public void LoadFromHistory(IEnumerable<IEvent> history)
{
    foreach (var @event in history)
    {
        ApplyEvent(@event);
    }
}

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
    }
}
```

**Flow:**
1. Receive all historical events
2. Apply each event in order
3. Rebuild current state

**Apply() Methods**
```csharp
private void Apply(TransactionAddedEvent @event)
{
    var transaction = new Transaction(
        @event.TransactionId,
        new Money(@event.Amount, @event.Currency),
        new Category(@event.Category),
        @event.Description,
        @event.Type,
        @event.Date
    );
    
    _transactions[@event.TransactionId] = transaction;
}
```

**Purpose:** 
- Updates internal state based on event
- NO validation (event already happened)
- Used both for new events and replaying history

### 3.3 Application Layer

#### 3.3.1 Commands

Commands represent user intentions to change state.

**AddTransactionCommand**
```csharp
public record AddTransactionCommand
{
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "PLN";
    public string Category { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public TransactionType Type { get; init; }
    public DateTime Date { get; init; }
    
    public void Validate()
    {
        if (Amount <= 0)
            throw new ArgumentException("Amount must be greater than zero");
        if (string.IsNullOrWhiteSpace(Category))
            throw new ArgumentException("Category is required");
        if (Date > DateTime.Now.AddDays(1))
            throw new ArgumentException("Date cannot be in the future");
    }
}
```

**Purpose:** Data container for adding a transaction with validation.

#### 3.3.2 DTOs (Data Transfer Objects)

DTOs are used to transfer data between API and frontend.

**TransactionDto**
```csharp
public record TransactionDto
{
    public Guid Id { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "PLN";
    public string Category { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public TransactionType Type { get; init; }
    public DateTime Date { get; init; }
    
    // Display properties
    public string TypeDisplay => Type == TransactionType.Income ? "Income" : "Expense";
    public string AmountDisplay => /* ... */;
}
```

**Purpose:** Flat structure optimized for API responses.

**Difference from Domain Entity:**
- Domain Entity uses Value Objects (Money, Category)
- DTO uses primitives (decimal, string)
- DTO has display properties for UI

#### 3.3.3 Services

**BudgetService**

```csharp
public class BudgetService
{
    private readonly IEventStore _eventStore;
    
    public BudgetService(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }
    
    // Commands
    public async Task AddTransactionAsync(AddTransactionCommand command)
    {
        // 1. Validate
        command.Validate();
        
        // 2. Load current state
        var budget = await GetCurrentBudgetAsync();
        
        // 3. Execute command on aggregate
        budget.AddTransaction(
            new Money(command.Amount, command.Currency),
            new Category(command.Category),
            command.Description,
            command.Type,
            command.Date
        );
        
        // 4. Save events
        await _eventStore.AppendEventsAsync(budget.UncommittedEvents);
        budget.MarkEventsAsCommitted();
    }
    
    // Queries
    public async Task<List<TransactionDto>> GetAllTransactionsAsync(
        GetTransactionsQuery? query = null)
    {
        var budget = await GetCurrentBudgetAsync();
        
        // Apply filters
        var transactions = budget.Transactions.AsEnumerable();
        if (query?.FromDate.HasValue == true)
            transactions = transactions.Where(t => t.Date >= query.FromDate.Value);
        // ... more filters
        
        // Map to DTOs
        return transactions
            .OrderByDescending(t => t.Date)
            .ThenBy(t => t.Id)
            .Select(MapToDto)
            .ToList();
    }
    
    // Helper
    private async Task<BudgetAggregate> GetCurrentBudgetAsync()
    {
        var events = await _eventStore.GetAllEventsAsync();
        var budget = new BudgetAggregate();
        budget.LoadFromHistory(events);
        return budget;
    }
}
```

**Responsibilities:**
1. **Orchestration**: Coordinates between aggregate and event store
2. **Query Execution**: Loads aggregate and extracts data
3. **Command Execution**: Validates, executes, and persists
4. **Mapping**: Converts domain entities to DTOs

**Key Flow (AddTransaction):**
```
1. API receives AddTransactionCommand
   ↓
2. BudgetService.AddTransactionAsync()
   ↓
3. Validate command
   ↓
4. Load aggregate from event store (replay all events)
   ↓
5. Execute budget.AddTransaction()
   ↓
6. Save uncommitted events to event store
   ↓
7. Return success
```

### 3.4 Infrastructure Layer

#### 3.4.1 IEventStore Interface

```csharp
public interface IEventStore
{
    Task AppendEventsAsync(IEnumerable<IEvent> events);
    Task<IEnumerable<IEvent>> GetAllEventsAsync();
    string GetFilePath();
}
```

**Purpose:** Abstract storage mechanism for events.

**Implementations:**
1. `JsonFileEventStore` - Production (encrypted JSON file)
2. `InMemoryEventStore` - Testing (in-memory list)

#### 3.4.2 JsonFileEventStore

```csharp
public class JsonFileEventStore : IEventStore
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly EncryptionService? _encryptionService;
    private readonly bool _useEncryption;
    
    public JsonFileEventStore(string filePath, EncryptionService? encryptionService)
    {
        _filePath = filePath;
        _encryptionService = encryptionService;
        _useEncryption = encryptionService != null;
        
        // Configure JSON serialization
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };
        
        EnsureFileExists();
    }
    
    public async Task AppendEventsAsync(IEnumerable<IEvent> events)
    {
        await _semaphore.WaitAsync(); // Thread-safe
        try
        {
            // 1. Read existing file
            var fileContent = await File.ReadAllTextAsync(_filePath);
            
            // 2. Decrypt if needed
            var json = _useEncryption 
                ? _encryptionService.Decrypt(fileContent) 
                : fileContent;
            
            // 3. Deserialize
            var existingEvents = JsonSerializer.Deserialize<List<JsonElement>>(json);
            
            // 4. Append new events
            foreach (var @event in events)
            {
                var eventJson = JsonSerializer.SerializeToElement(@event);
                existingEvents.Add(eventJson);
            }
            
            // 5. Serialize
            var updatedJson = JsonSerializer.Serialize(existingEvents, _jsonOptions);
            
            // 6. Encrypt if needed
            var contentToWrite = _useEncryption 
                ? _encryptionService.Encrypt(updatedJson) 
                : updatedJson;
            
            // 7. Write to file
            await File.WriteAllTextAsync(_filePath, contentToWrite);
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async Task<IEnumerable<IEvent>> GetAllEventsAsync()
    {
        // 1. Read file
        var fileContent = await File.ReadAllTextAsync(_filePath);
        
        // 2. Decrypt
        var json = _useEncryption 
            ? _encryptionService.Decrypt(fileContent) 
            : fileContent;
        
        // 3. Deserialize
        var jsonElements = JsonSerializer.Deserialize<List<JsonElement>>(json);
        
        // 4. Deserialize each event based on type
        var events = new List<IEvent>();
        foreach (var element in jsonElements)
        {
            var eventType = element.GetProperty("eventType").GetString();
            
            IEvent? @event = eventType switch
            {
                nameof(TransactionAddedEvent) => 
                    JsonSerializer.Deserialize<TransactionAddedEvent>(element),
                nameof(TransactionUpdatedEvent) => 
                    JsonSerializer.Deserialize<TransactionUpdatedEvent>(element),
                nameof(TransactionDeletedEvent) => 
                    JsonSerializer.Deserialize<TransactionDeletedEvent>(element),
                _ => null
            };
            
            if (@event != null)
                events.Add(@event);
        }
        
        return events;
    }
}
```

**Key Features:**
1. **Thread-Safe**: Uses `SemaphoreSlim` to prevent concurrent writes
2. **Encryption**: Optional AES-256 encryption
3. **Append-Only**: Never modifies existing events
4. **Type Discrimination**: Uses `eventType` property to deserialize correctly

**File Format (Unencrypted):**
```json
[
  {
    "eventId": "a1b2c3d4-...",
    "timestamp": "2025-11-01T10:30:00Z",
    "eventType": "TransactionAddedEvent",
    "transactionId": "e5f6g7h8-...",
    "amount": 150.50,
    "currency": "PLN",
    "category": "Food & Dining",
    "description": "Groceries",
    "type": 2,
    "date": "2025-11-01T00:00:00Z"
  },
  {
    "eventId": "i9j0k1l2-...",
    "timestamp": "2025-11-01T11:00:00Z",
    "eventType": "TransactionUpdatedEvent",
    "transactionId": "e5f6g7h8-...",
    "amount": 165.00,
    "currency": "PLN",
    "category": "Food & Dining",
    "description": "Groceries (updated)",
    "type": 2,
    "date": "2025-11-01T00:00:00Z"
  }
]
```

#### 3.4.3 EncryptionService

```csharp
public class EncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;
    
    public EncryptionService(string base64Key)
    {
        var keyBytes = Convert.FromBase64String(base64Key);
        _key = keyBytes;
        
        // Derive IV from key using SHA256
        using var sha256 = SHA256.Create();
        _iv = sha256.ComputeHash(keyBytes).Take(16).ToArray();
    }
    
    public string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        
        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        
        return Convert.ToBase64String(encryptedBytes);
    }
    
    public string Decrypt(string cipherText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        
        using var decryptor = aes.CreateDecryptor();
        var cipherBytes = Convert.FromBase64String(cipherText);
        var decryptedBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        
        return Encoding.UTF8.GetString(decryptedBytes);
    }
}
```

**Algorithm:** AES-256-CBC
**Key Derivation:** SHA256 of (MachineName + UserName + Salt)
**IV:** First 16 bytes of SHA256(Key)

#### 3.4.4 EventStoreConfiguration

```csharp
public static class EventStoreConfiguration
{
    public static string GetDefaultEventsFilePath()
    {
        var appDataFolder = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);
        var biedAppFolder = Path.Combine(appDataFolder, "BiedApp");
        
        if (!Directory.Exists(biedAppFolder))
            Directory.CreateDirectory(biedAppFolder);
        
        return Path.Combine(biedAppFolder, "events.json");
    }
    
    public static string GetEncryptionKey()
    {
        var machineName = Environment.MachineName;
        var userName = Environment.UserName;
        var seedString = $"BiedApp-{machineName}-{userName}-v1.0";
        
        using var sha256 = SHA256.Create();
        var keyBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(seedString));
        return Convert.ToBase64String(keyBytes);
    }
}
```

**File Locations:**
- Windows: `C:\Users\{Username}\AppData\Local\BiedApp\events.json`
- macOS: `/Users/{Username}/.local/share/BiedApp/events.json`
- Linux: `/home/{Username}/.local/share/BiedApp/events.json`

### 3.5 API Layer (Controllers)

#### 3.5.1 BudgetController

```csharp
[ApiController]
[Route("api/[controller]")]
public class BudgetController : ControllerBase
{
    private readonly BudgetService _budgetService;
    private readonly ILogger<BudgetController> _logger;
    
    // GET /api/budget/transactions
    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? category,
        [FromQuery] TransactionType? type,
        [FromQuery] int? limit)
    {
        var query = new GetTransactionsQuery { /* ... */ };
        var transactions = await _budgetService.GetAllTransactionsAsync(query);
        return Ok(transactions);
    }
    
    // POST /api/budget/transactions
    [HttpPost("transactions")]
    public async Task<IActionResult> AddTransaction(
        [FromBody] AddTransactionCommand command)
    {
        command.Validate();
        await _budgetService.AddTransactionAsync(command);
        return CreatedAtAction(nameof(GetTransactions), new { }, 
            new { message = "Transaction added" });
    }
    
    // PUT /api/budget/transactions/{id}
    [HttpPut("transactions/{id}")]
    public async Task<IActionResult> UpdateTransaction(
        Guid id, 
        [FromBody] UpdateTransactionCommand command)
    {
        var updateCommand = command with { Id = id };
        updateCommand.Validate();
        await _budgetService.UpdateTransactionAsync(updateCommand);
        return Ok(new { message = "Transaction updated" });
    }
    
    // DELETE /api/budget/transactions/{id}
    [HttpDelete("transactions/{id}")]
    public async Task<IActionResult> DeleteTransaction(Guid id)
    {
        var command = new DeleteTransactionCommand { Id = id };
        await _budgetService.DeleteTransactionAsync(command);
        return Ok(new { message = "Transaction deleted" });
    }
    
    // GET /api/budget/summary
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var summary = await _budgetService.GetBudgetSummaryAsync();
        return Ok(summary);
    }
    
    // GET /api/budget/summary/current-month
    [HttpGet("summary/current-month")]
    public async Task<IActionResult> GetCurrentMonthSummary()
    {
        var now = DateTime.Now;
        var summary = await _budgetService.GetMonthlyBudgetSummaryAsync(
            now.Year, now.Month);
        return Ok(summary);
    }
    
    // GET /api/budget/export
    [HttpGet("export")]
    public async Task<IActionResult> ExportData()
    {
        var transactions = await _budgetService.GetAllTransactionsAsync();
        var summary = await _budgetService.GetBudgetSummaryAsync();
        
        var exportData = new
        {
            exportDate = DateTime.UtcNow,
            summary,
            transactions
        };
        
        var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true });
        var bytes = Encoding.UTF8.GetBytes(json);
        return File(bytes, "application/json", 
            $"biedapp-export-{DateTime.Now:yyyyMMdd}.json");
    }
    
    // DELETE /api/budget/clear-all
    [HttpDelete("clear-all")]
    public async Task<IActionResult> ClearAllData()
    {
        var transactions = await _budgetService.GetAllTransactionsAsync();
        
        foreach (var transaction in transactions)
        {
            var command = new DeleteTransactionCommand { Id = transaction.Id };
            await _budgetService.DeleteTransactionAsync(command);
        }
        
        _logger.LogWarning("All data cleared - {Count} transactions deleted", 
            transactions.Count);
        
        return Ok(new { message = $"Deleted {transactions.Count} transactions" });
    }
}
```

**API Endpoints Summary:**

| Method | Endpoint | Description | Request Body | Response |
|--------|----------|-------------|--------------|----------|
| GET | `/api/budget/transactions` | Get all transactions (with filters) | - | `TransactionDto[]` |
| GET | `/api/budget/transactions/{id}` | Get specific transaction | - | `TransactionDto` |
| POST | `/api/budget/transactions` | Add new transaction | `AddTransactionCommand` | `201 Created` |
| PUT | `/api/budget/transactions/{id}` | Update transaction | `UpdateTransactionCommand` | `200 OK` |
| DELETE | `/api/budget/transactions/{id}` | Delete transaction | - | `200 OK` |
| GET | `/api/budget/summary` | Get overall budget summary | - | `BudgetSummaryDto` |
| GET | `/api/budget/summary/current-month` | Get current month summary | - | `BudgetSummaryDto` |
| GET | `/api/budget/summary/{year}/{month}` | Get specific month summary | - | `BudgetSummaryDto` |
| GET | `/api/budget/categories` | Get all categories | - | `string[]` |
| GET | `/api/budget/categories/summary` | Get spending by category | - | `CategorySummaryDto[]` |
| GET | `/api/budget/monthly/{year}/{month}` | Get monthly data | - | `Dictionary<string, decimal>` |
| GET | `/api/budget/export` | Export all data as JSON | - | `File` |
| DELETE | `/api/budget/clear-all` | Delete all transactions | - | `200 OK` |

#### 3.5.2 StatisticsController

```csharp
[ApiController]
[Route("api/[controller]")]
public class StatisticsController : ControllerBase
{
    private readonly StatisticsService _statisticsService;
    private readonly ILogger<StatisticsController> _logger;
    
    // GET /api/statistics/trends/balance
    [HttpGet("trends/balance")]
    public async Task<IActionResult> GetBalanceTrend()
    {
        var trend = await _statisticsService.GetLast12MonthsBalanceAsync();
        return Ok(trend);
    }
    
    // GET /api/statistics/top-expenses?top=5
    [HttpGet("top-expenses")]
    public async Task<IActionResult> GetTopExpenses([FromQuery] int top = 5)
    {
        if (top < 1 || top > 20)
            return BadRequest(new { error = "Top parameter must be between 1 and 20" });
        
        var topExpenses = await _statisticsService.GetTopExpenseCategoriesAsync(top);
        return Ok(topExpenses);
    }
    
    // GET /api/statistics/average-expenses?months=6
    [HttpGet("average-expenses")]
    public async Task<IActionResult> GetAverageExpenses([FromQuery] int months = 6)
    {
        if (months < 1 || months > 24)
            return BadRequest(new { error = "Months parameter must be between 1 and 24" });
        
        var average = await _statisticsService.GetAverageMonthlyExpensesAsync(months);
        
        return Ok(new 
        { 
            averageMonthlyExpenses = average,
            currency = "PLN",
            periodMonths = months
        });
    }
}
```

#### 3.5.3 SystemController

```csharp
[ApiController]
[Route("api/[controller]")]
public class SystemController : ControllerBase
{
    private readonly BudgetService _budgetService;
    private readonly IConfiguration _configuration;
    
    // GET /api/system/info
    [HttpGet("info")]
    public async Task<IActionResult> GetSystemInfo()
    {
        var storageLocation = _budgetService.GetStorageLocation();
        var eventCount = await _budgetService.GetEventCountAsync();
        var storageType = _configuration["Storage:Type"] ?? "File";
        
        return Ok(new
        {
            application = "BiedApp",
            version = "1.0.0",
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            storage = new
            {
                type = storageType,
                location = storageLocation,
                eventCount = eventCount,
                isEncrypted = storageType.Equals("File", StringComparison.OrdinalIgnoreCase)
            },
            system = new
            {
                machineName = Environment.MachineName,
                userName = Environment.UserName,
                osVersion = Environment.OSVersion.ToString(),
                appDataFolder = Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData)
            }
        });
    }
    
    // GET /api/system/health
    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new 
        { 
            status = "healthy",
            timestamp = DateTime.UtcNow
        });
    }
}
```

---

## 4. Frontend Design

### 4.1 Project Structure

```
BiedApp.Frontend/
├── src/
│   ├── app/
│   │   ├── core/
│   │   │   ├── models/
│   │   │   │   ├── transaction.model.ts
│   │   │   │   ├── budget-summary.model.ts
│   │   │   │   └── category-summary.model.ts
│   │   │   └── services/
│   │   │       ├── budget-api.service.ts
│   │   │       └── statistics-api.service.ts
│   │   ├── features/
│   │   │   ├── dashboard/
│   │   │   │   ├── dashboard.component.ts
│   │   │   │   ├── dashboard.component.html
│   │   │   │   └── dashboard.component.scss
│   │   │   ├── transactions/
│   │   │   │   ├── transactions.component.ts
│   │   │   │   ├── transactions.component.html
│   │   │   │   └── transactions.component.scss
│   │   │   ├── monthly-transactions/
│   │   │   │   ├── monthly-transactions.component.ts
│   │   │   │   ├── monthly-transactions.component.html
│   │   │   │   └── monthly-transactions.component.scss
│   │   │   ├── statistics/
│   │   │   │   ├── statistics.component.ts
│   │   │   │   ├── statistics.component.html
│   │   │   │   └── statistics.component.scss
│   │   │   └── settings/
│   │   │       ├── settings.component.ts
│   │   │       ├── settings.component.html
│   │   │       └── settings.component.scss
│   │   ├── shared/
│   │   │   └── pipes/
│   │   │       ├── currency-format.pipe.ts
│   │   │       └── transaction-type.pipe.ts
│   │   ├── app.component.ts
│   │   ├── app.component.html
│   │   ├── app.component.scss
│   │   ├── app.routes.ts
│   │   └── app.config.ts
│   ├── environments/
│   │   ├── environment.ts
│   │   └── environment.prod.ts
│   ├── styles.scss
│   └── index.html
├── angular.json
├── package.json
└── tsconfig.json
```

### 4.2 Core Models

#### 4.2.1 Transaction Model

```typescript
export enum TransactionType {
  Income = 1,
  Expense = 2
}

export interface Transaction {
  id: string;
  amount: number;
  currency: string;
  category: string;
  description: string;
  type: TransactionType;
  date: Date;
  typeDisplay?: string;
  amountDisplay?: string;
}

export interface CreateTransactionRequest {
  amount: number;
  currency: string;
  category: string;
  description: string;
  type: TransactionType;
  date: Date;
}

export interface UpdateTransactionRequest {
  id: string;
  amount: number;
  currency: string;
  category: string;
  description: string;
  type: TransactionType;
  date: Date;
}
```

**Purpose:** TypeScript interfaces matching backend DTOs.

#### 4.2.2 Budget Summary Model

```typescript
export interface BudgetSummary {
  totalIncome: number;
  totalExpenses: number;
  balance: number;
  currency: string;
  transactionCount: number;
  incomeCount: number;
  expenseCount: number;
  totalIncomeDisplay?: string;
  totalExpensesDisplay?: string;
  balanceDisplay?: string;
  balanceStatus?: string;
}
```

### 4.3 Services

#### 4.3.1 Budget API Service

```typescript
@Injectable({
  providedIn: 'root'
})
export class BudgetApiService {
  private readonly baseUrl = `${environment.apiUrl}/budget`;

  constructor(private http: HttpClient) {}

  // Transactions
  getTransactions(
    fromDate?: Date,
    toDate?: Date,
    category?: string,
    type?: TransactionType,
    limit?: number
  ): Observable<Transaction[]> {
    let params = new HttpParams();
    
    if (fromDate) params = params.set('fromDate', fromDate.toISOString());
    if (toDate) params = params.set('toDate', toDate.toISOString());
    if (category) params = params.set('category', category);
    if (type !== undefined) params = params.set('type', type.toString());
    if (limit) params = params.set('limit', limit.toString());

    return this.http.get<Transaction[]>(
      `${this.baseUrl}/transactions`, 
      { params }
    );
  }

  addTransaction(request: CreateTransactionRequest): Observable<any> {
    return this.http.post(`${this.baseUrl}/transactions`, request);
  }

  updateTransaction(id: string, request: UpdateTransactionRequest): Observable<any> {
    return this.http.put(`${this.baseUrl}/transactions/${id}`, request);
  }

  deleteTransaction(id: string): Observable<any> {
    return this.http.delete(`${this.baseUrl}/transactions/${id}`);
  }

  // Summary
  getBudgetSummary(): Observable<BudgetSummary> {
    return this.http.get<BudgetSummary>(`${this.baseUrl}/summary`);
  }

  getCurrentMonthSummary(): Observable<BudgetSummary> {
    return this.http.get<BudgetSummary>(`${this.baseUrl}/summary/current-month`);
  }

  getMonthlySummary(year: number, month: number): Observable<BudgetSummary> {
    return this.http.get<BudgetSummary>(`${this.baseUrl}/summary/${year}/${month}`);
  }

  // Categories
  getCategories(): Observable<string[]> {
    return this.http.get<string[]>(`${this.baseUrl}/categories`);
  }

  getCategorySummary(type?: TransactionType): Observable<CategorySummary[]> {
    let params = new HttpParams();
    if (type !== undefined) {
      params = params.set('type', type.toString());
    }
    return this.http.get<CategorySummary[]>(
      `${this.baseUrl}/categories/summary`, 
      { params }
    );
  }

  // Data management
  exportData(): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/export`, { responseType: 'blob' });
  }

  clearAllData(): Observable<any> {
    return this.http.delete(`${this.baseUrl}/clear-all`);
  }
}
```

**Purpose:** Encapsulates all HTTP communication with backend API.

**Key Features:**
- Type-safe with TypeScript interfaces
- Uses RxJS Observables
- Handles query parameters
- Centralized API URL configuration

### 4.4 Components

#### 4.4.1 Dashboard Component

```typescript
@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, CurrencyFormatPipe],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent implements OnInit {
  summary: BudgetSummary | null = null;
  monthlySummary: BudgetSummary | null = null;
  recentTransactions: Transaction[] = [];
  expensesByCategory: CategorySummary[] = [];
  loading = true;
  showAddModal = false;
  
  constructor(private budgetApi: BudgetApiService) {}

  ngOnInit(): void {
    this.loadDashboardData();
  }

  loadDashboardData(): void {
    this.loading = true;
    
    // Load multiple datasets in parallel
    forkJoin({
      summary: this.budgetApi.getBudgetSummary(),
      monthlySummary: this.budgetApi.getCurrentMonthSummary(),
      recentTransactions: this.budgetApi.getTransactions(
        undefined, undefined, undefined, undefined, 5),
      expensesByCategory: this.budgetApi.getCategorySummary(
        TransactionType.Expense)
    }).subscribe({
      next: (data) => {
        this.summary = data.summary;
        this.monthlySummary = data.monthlySummary;
        this.recentTransactions = data.recentTransactions;
        this.expensesByCategory = data.expensesByCategory.slice(0, 5);
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading dashboard data:', err);
        this.loading = false;
      }
    });
  }

  // Computed properties
  get monthlyRevenueUsedPercentage(): number {
    if (!this.monthlySummary || this.monthlySummary.totalIncome === 0) 
      return 0;
    return Math.min(
      (this.monthlySummary.totalExpenses / this.monthlySummary.totalIncome) * 100, 
      100
    );
  }

  get monthlyRevenueLeft(): number {
    if (!this.monthlySummary) return 0;
    return Math.max(
      this.monthlySummary.totalIncome - this.monthlySummary.totalExpenses, 
      0
    );
  }
}
```

**Responsibilities:**
1. Load dashboard data from API
2. Display summary cards
3. Show recent transactions
4. Display expense breakdown
5. Handle add transaction modal

#### 4.4.2 Transactions Component

```typescript
@Component({
  selector: 'app-transactions',
  standalone: true,
  imports: [CommonModule, FormsModule, CurrencyFormatPipe],
  templateUrl: './transactions.component.html',
  styleUrls: ['./transactions.component.scss']
})
export class TransactionsComponent implements OnInit {
  transactions: Transaction[] = [];
  categories: string[] = [];
  loading = false;
  showForm = false;
  editingId: string | null = null;
  
  formData: CreateTransactionRequest = {
    amount: 0,
    currency: 'PLN',
    category: '',
    description: '',
    type: TransactionType.Expense,
    date: new Date()
  };

  constructor(private budgetApi: BudgetApiService) {}

  ngOnInit(): void {
    this.loadTransactions();
    this.loadCategories();
  }

  loadTransactions(): void {
    this.loading = true;
    this.budgetApi.getTransactions().subscribe({
      next: (data) => {
        this.transactions = data;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading transactions:', err);
        this.loading = false;
      }
    });
  }

  saveTransaction(): void {
    if (this.editingId) {
      // Update
      this.budgetApi.updateTransaction(this.editingId, {
        ...this.formData,
        id: this.editingId
      }).subscribe({
        next: () => {
          this.loadTransactions();
          this.closeForm();
        }
      });
    } else {
      // Add new
      this.budgetApi.addTransaction(this.formData).subscribe({
        next: () => {
          this.loadTransactions();
          this.closeForm();
        }
      });
    }
  }

  deleteTransaction(id: string): void {
    if (confirm('Are you sure you want to delete this transaction?')) {
      this.budgetApi.deleteTransaction(id).subscribe({
        next: () => this.loadTransactions()
      });
    }
  }
}
```

**Responsibilities:**
1. Display all transactions in a list
2. CRUD operations (Create, Read, Update, Delete)
3. Modal form for add/edit
4. Category selection with autocomplete

#### 4.4.3 Monthly Transactions Component

```typescript
interface MonthlyGroup {
  year: number;
  month: number;
  monthName: string;
  transactions: Transaction[];
  summary: {
    income: number;
    expenses: number;
    balance: number;
  };
}

@Component({
  selector: 'app-monthly-transactions',
  standalone: true,
  imports: [CommonModule, FormsModule, CurrencyFormatPipe],
  templateUrl: './monthly-transactions.component.html',
  styleUrls: ['./monthly-transactions.component.scss']
})
export class MonthlyTransactionsComponent implements OnInit {
  currentDate = new Date();
  currentYear = this.currentDate.getFullYear();
  currentMonth = this.currentDate.getMonth() + 1;
  
  monthlyGroups: MonthlyGroup[] = [];
  loading = false;

  constructor(private budgetApi: BudgetApiService) {}

  ngOnInit(): void {
    this.loadMonthlyTransactions();
  }

  loadMonthlyTransactions(): void {
    this.loading = true;
    const monthsToShow = 3;
    const promises: Promise<MonthlyGroup>[] = [];

    // Load last 3 months
    for (let i = 0; i < monthsToShow; i++) {
      const date = new Date(this.currentYear, this.currentMonth - 1 - i, 1);
      const year = date.getFullYear();
      const month = date.getMonth() + 1;

      const promise = this.loadMonthData(year, month);
      promises.push(promise);
    }

    Promise.all(promises).then(groups => {
      this.monthlyGroups = groups;
      this.loading = false;
    });
  }

  private async loadMonthData(year: number, month: number): Promise<MonthlyGroup> {
    const startDate = new Date(year, month - 1, 1);
    const endDate = new Date(year, month, 0);

    const transactions = await this.budgetApi.getTransactions(
      startDate, endDate
    ).toPromise() || [];
    
    const income = transactions
      .filter(t => t.type === TransactionType.Income)
      .reduce((sum, t) => sum + t.amount, 0);
    
    const expenses = transactions
      .filter(t => t.type === TransactionType.Expense)
      .reduce((sum, t) => sum + t.amount, 0);

    return {
      year,
      month,
      monthName: startDate.toLocaleDateString('en-US', { 
        month: 'long', 
        year: 'numeric' 
      }),
      transactions,
      summary: { income, expenses, balance: income - expenses }
    };
  }

  previousMonth(): void {
    if (this.currentMonth === 1) {
      this.currentMonth = 12;
      this.currentYear--;
    } else {
      this.currentMonth--;
    }
    this.loadMonthlyTransactions();
  }

  nextMonth(): void {
    const now = new Date();
    const canGoNext = this.currentYear < now.getFullYear() || 
                     (this.currentYear === now.getFullYear() && 
                      this.currentMonth < now.getMonth() + 1);
    
    if (canGoNext) {
      if (this.currentMonth === 12) {
        this.currentMonth = 1;
        this.currentYear++;
      } else {
        this.currentMonth++;
      }
      this.loadMonthlyTransactions();
    }
  }
}
```

**Responsibilities:**
1. Group transactions by month
2. Display monthly summaries
3. Navigate between months
4. Show transactions in sections

#### 4.4.4 Statistics Component

```typescript
@Component({
  selector: 'app-statistics',
  standalone: true,
  imports: [CommonModule, CurrencyFormatPipe],
  templateUrl: './statistics.component.html',
  styleUrls: ['./statistics.component.scss']
})
export class StatisticsComponent implements OnInit {
  loading = true;
  balanceTrend: { month: string; balance: number }[] = [];
  topExpenses: CategorySummary[] = [];
  expensesByCategory: CategorySummary[] = [];
  incomeByCategory: CategorySummary[] = [];
  averageExpenses = 0;
  selectedPeriod = 6;
  selectedMonthsPeriod = 3;

  constructor(
    private statisticsApi: StatisticsApiService,
    private budgetApi: BudgetApiService
  ) {}

  ngOnInit(): void {
    this.loadStatistics();
  }

  loadStatistics(): void {
    this.loading = true;

    forkJoin({
      balanceTrend: this.statisticsApi.getBalanceTrend(),
      topExpenses: this.statisticsApi.getTopExpenses(5),
      expensesByCategory: this.budgetApi.getCategorySummary(
        TransactionType.Expense),
      incomeByCategory: this.budgetApi.getCategorySummary(
        TransactionType.Income),
      averageExpenses: this.statisticsApi.getAverageExpenses(this.selectedPeriod)
    }).subscribe({
      next: (data) => {
        this.balanceTrend = Object.entries(data.balanceTrend).map(
          ([month, balance]) => ({ month, balance })
        );
        this.topExpenses = data.topExpenses;
        this.expensesByCategory = data.expensesByCategory;
        this.incomeByCategory = data.incomeByCategory;
        this.averageExpenses = data.averageExpenses.averageMonthlyExpenses;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading statistics:', err);
        this.loading = false;
      }
    });
  }

  onPeriodChange(months: number): void {
    this.selectedPeriod = months;
    this.statisticsApi.getAverageExpenses(months).subscribe({
      next: (data) => {
        this.averageExpenses = data.averageMonthlyExpenses;
      }
    });
  }

  getMaxBalance(): number {
    const balances = this.balanceTrend.map(item => Math.abs(item.balance));
    return Math.max(...balances, 1);
  }
}
```

**Responsibilities:**
1. Display balance trends (12 months)
2. Show top expense categories
3. Display category breakdowns
4. Calculate average expenses
5. Period selection (3/6/12 months)

#### 4.4.5 Settings Component

```typescript
interface SystemInfo {
  application: string;
  version: string;
  environment: string;
  storage: {
    type: string;
    location: string;
    eventCount: number;
    isEncrypted: boolean;
  };
  system: {
    machineName: string;
    userName: string;
    osVersion: string;
    appDataFolder: string;
  };
}

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.scss']
})
export class SettingsComponent implements OnInit {
  systemInfo: SystemInfo | null = null;
  loading = true;

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.loadSystemInfo();
  }

  loadSystemInfo(): void {
    this.http.get<SystemInfo>(`${environment.apiUrl}/system/info`).subscribe({
      next: (data) => {
        this.systemInfo = data;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading system info:', err);
        this.loading = false;
      }
    });
  }

  exportData(): void {
    this.http.get(`${environment.apiUrl}/budget/export`, { 
      responseType: 'blob' 
    }).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `biedapp-export-${new Date().toISOString().split('T')[0]}.json`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
      },
      error: (err) => {
        console.error('Error exporting data:', err);
        alert('Failed to export data');
      }
    });
  }

  clearData(): void {
    if (confirm('⚠️ WARNING: This will delete ALL your transactions!\n\n' +
                'This action cannot be undone. Are you absolutely sure?')) {
      this.http.delete(`${environment.apiUrl}/budget/clear-all`).subscribe({
        next: (response: any) => {
          alert(`Success! ${response.message}`);
          this.loadSystemInfo();
        },
        error: (err) => {
          console.error('Error clearing data:', err);
          alert('Failed to clear data');
        }
      });
    }
  }
}
```

**Responsibilities:**
1. Display system information
2. Show storage location and stats
3. Export data as JSON file
4. Clear all data functionality

### 4.5 Shared Components

#### 4.5.1 Currency Format Pipe

```typescript
@Pipe({
  name: 'currencyFormat',
  standalone: true
})
export class CurrencyFormatPipe implements PipeTransform {
  transform(value: number, currency: string = 'PLN'): string {
    if (value === null || value === undefined) {
      return '0.00 PLN';
    }
    return `${value.toFixed(2)} ${currency}`;
  }
}
```

**Usage in template:**
```html
<span>{{ transaction.amount | currencyFormat: transaction.currency }}</span>
<!-- Output: 150.50 PLN -->
```

### 4.6 Routing

```typescript
// app.routes.ts
export const routes: Routes = [
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  { path: 'dashboard', component: DashboardComponent },
  { path: 'transactions', component: TransactionsComponent },
  { path: 'transactions/monthly', component: MonthlyTransactionsComponent },
  { path: 'statistics', component: StatisticsComponent },
  { path: 'settings', component: SettingsComponent },
  { path: '**', redirectTo: '/dashboard' }
];
```

**Route Structure:**
- `/` → Redirects to `/dashboard`
- `/dashboard` → Main dashboard with summary
- `/transactions` → All transactions list view
- `/transactions/monthly` → Monthly grouped view
- `/statistics` → Analytics and trends
- `/settings` → System information and data management

---

## 5. Communication Flow

### 5.1 Complete Request Flow (Add Transaction Example)

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. USER ACTION                                                   │
│    User fills form and clicks "Save"                            │
└────────────────────────┬────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────────┐
│ 2. ANGULAR COMPONENT (TransactionsComponent)                    │
│    saveTransaction() {                                          │
│      this.budgetApi.addTransaction(this.formData)              │
│    }                                                            │
└────────────────────────┬────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────────┐
│ 3. ANGULAR SERVICE (BudgetApiService)                           │
│    addTransaction(request: CreateTransactionRequest) {          │
│      return this.http.post('/api/budget/transactions', request)│
│    }                                                            │
└────────────────────────┬────────────────────────────────────────┘
                         ↓ HTTP POST with JSON body
┌─────────────────────────────────────────────────────────────────┐
│ 4. ASP.NET CORE API (BudgetController)                          │
│    [HttpPost("transactions")]                                   │
│    public async Task<IActionResult> AddTransaction(            │
│        [FromBody] AddTransactionCommand command)               │
│    {                                                            │
│      command.Validate();                                        │
│      await _budgetService.AddTransactionAsync(command);        │
│      return CreatedAtAction(...);                              │
│    }                                                            │
└────────────────────────┬────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────────┐
│ 5. APPLICATION SERVICE (BudgetService)                          │
│    public async Task AddTransactionAsync(                       │
│        AddTransactionCommand command)                          │
│    {                                                            │
│      // 5.1: Load current state                                │
│      var budget = await GetCurrentBudgetAsync();               │
│                                                                  │
│      // 5.2: Execute command on aggregate                      │
│      budget.AddTransaction(...);                               │
│                                                                  │
│      // 5.3: Save events                                       │
│      await _eventStore.AppendEventsAsync(                      │
│          budget.UncommittedEvents);                            │
│    }                                                            │
└────────────────────────┬────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────────┐
│ 5.1: GET CURRENT STATE (BudgetService.GetCurrentBudgetAsync)   │
│      var events = await _eventStore.GetAllEventsAsync();       │
│      var budget = new BudgetAggregate();                        │
│      budget.LoadFromHistory(events);                            │
│      return budget;                                             │
└────────────────────────┬────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────────┐
│ 6. INFRASTRUCTURE (JsonFileEventStore.GetAllEventsAsync)       │
│    {                                                            │
│      // 6.1: Read file                                          │
│      var fileContent = await File.ReadAllTextAsync(_filePath); │
│                                                                  │
│      // 6.2: Decrypt                                            │
│      var json = _encryptionService.Decrypt(fileContent);       │
│                                                                  │
│      // 6.3: Deserialize                                        │
│      var jsonElements = JsonSerializer.Deserialize<>(json);    │
│                                                                  │
│      // 6.4: Convert to events based on type                   │
│      foreach (var element in jsonElements) {                   │
│        var eventType = element.GetProperty("eventType");       │
│        IEvent @event = eventType switch {                      │
│          "TransactionAddedEvent" => Deserialize<...>(),        │
│          ...                                                    │
│        };                                                       │
│        events.Add(@event);                                      │
│      }                                                          │
│      return events;                                             │
│    }                                                            │
└────────────────────────┬────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────────┐
│ 7. DOMAIN (BudgetAggregate.LoadFromHistory)                    │
│    public void LoadFromHistory(IEnumerable<IEvent> history) {  │
│      _transactions.Clear();                                     │
│      foreach (var @event in history) {                         │
│        ApplyEvent(@event); // Rebuild state from each event    │
│      }                                                          │
│    }                                                            │
│                                                                  │
│    Example: 10 events in history                               │
│    Event 1: TransactionAdded → Apply() → _transactions[id1]   │
│    Event 2: TransactionAdded → Apply() → _transactions[id2]   │
│    Event 3: TransactionUpdated → Apply() → _transactions[id1] │
│    ...                                                          │
│    Event 10: Current state reconstructed                       │
└────────────────────────┬────────────────────────────────────────┘
                         ↓ (Returns BudgetAggregate with current state)
┌─────────────────────────────────────────────────────────────────┐
│ 5.2: EXECUTE COMMAND (BudgetAggregate.AddTransaction)          │
│    public void AddTransaction(Money, Category, ...) {          │
│      var transactionId = Guid.NewGuid();                        │
│                                                                  │
│      // Create event                                            │
│      var @event = new TransactionAddedEvent(                   │
│        transactionId,                                           │
│        amount.Amount,                                           │
│        amount.Currency,                                         │
│        category.Name,                                           │
│        description,                                             │
│        type,                                                    │
│        date                                                     │
│      );                                                         │
│                                                                  │
│      // Apply to state (adds to _transactions dictionary)      │
│      Apply(@event);                                             │
│                                                                  │
│      // Remember for saving                                     │
│      _uncommittedEvents.Add(@event);                            │
│    }                                                            │
└────────────────────────┬────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────────┐
│ 5.3: SAVE EVENTS (IEventStore.AppendEventsAsync)               │
│    await _eventStore.AppendEventsAsync(budget.UncommittedEvents)│
└────────────────────────┬────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────────┐
│ 8. INFRASTRUCTURE (JsonFileEventStore.AppendEventsAsync)       │
│    {                                                            │
│      await _semaphore.WaitAsync(); // Thread-safe              │
│      try {                                                      │
│        // 8.1: Read existing file                              │
│        var fileContent = await File.ReadAllTextAsync(...);     │
│                                                                  │
│        // 8.2: Decrypt                                          │
│        var json = _encryptionService.Decrypt(fileContent);     │
│                                                                  │
│        // 8.3: Deserialize existing events                     │
│        var existingEvents = JsonSerializer.Deserialize<>(...); │
│                                                                  │
│        // 8.4: Append new events                               │
│        foreach (var @event in events) {                        │
│          var eventJson = JsonSerializer.SerializeToElement();  │
│          existingEvents.Add(eventJson);                         │
│        }                                                        │
│                                                                  │
│        // 8.5: Serialize                                        │
│        var updatedJson = JsonSerializer.Serialize(...);        │
│                                                                  │
│        // 8.6: Encrypt                                          │
│        var encrypted = _encryptionService.Encrypt(updatedJson);│
│                                                                  │
│        // 8.7: Write to file                                   │
│        await File.WriteAllTextAsync(_filePath, encrypted);     │
│      }                                                          │
│      finally {                                                  │
│        _semaphore.Release();                                    │
│      }                                                          │
│    }                                                            │
└────────────────────────┬────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────────┐
│ 9. DISK STORAGE                                                 │
│    File: %LOCALAPPDATA%\BiedApp\events.json                    │
│    Content: Encrypted Base64 string                            │
│                                                                  │
│    [Before encryption]                                          │
│    [                                                            │
│      { "eventType": "TransactionAddedEvent", ... },            │
│      { "eventType": "TransactionAddedEvent", ... },            │
│      { "eventType": "TransactionAddedEvent", ... } ← NEW!      │
│    ]                                                            │
│                                                                  │
│    [After encryption]                                           │
│    kJ8HvN2pL9xQwR3tY5zM8aB4cD6eF7gH9iJ0kL1mN2oP3qR...        │
└────────────────────────┬────────────────────────────────────────┘
                         ↓ Returns 201 Created
┌─────────────────────────────────────────────────────────────────┐
│ 10. API RESPONSE (BudgetController)                            │
│     return CreatedAtAction(nameof(GetTransactions),            │
│       new { },                                                  │
│       new { message = "Transaction added successfully" });     │
└────────────────────────┬────────────────────────────────────────┘
                         ↓ HTTP 201 Created with JSON body
┌─────────────────────────────────────────────────────────────────┐
│ 11. ANGULAR SERVICE (BudgetApiService)                         │
│     Observable completes successfully                          │
└────────────────────────┬────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────────┐
│ 12. ANGULAR COMPONENT (TransactionsComponent)                  │
│     .subscribe({                                               │
│       next: () => {                                            │
│         this.loadTransactions(); // Refresh list               │
│         this.closeForm();                                      │
│       }                                                         │
│     });                                                         │
└────────────────────────┬────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────────┐
│ 13. UI UPDATE                                                   │
│     - Modal closes                                              │
│     - Transaction list refreshes                               │
│     - New transaction appears in list                          │
│     - Success!                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### 5.2 Query Flow (Get Transactions Example)

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. USER ACTION                                                   │
│    User navigates to Transactions page                          │
└────────────────────────┬────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────────┐
│ 2. ANGULAR COMPONENT (TransactionsComponent.ngOnInit)          │
│    this.loadTransactions();                                     │
└────────────────────────┬────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────────┐
│ 3. ANGULAR SERVICE (BudgetApiService)                           │
│    getTransactions(): Observable<Transaction[]> {               │
│      return this.http.get<Transaction[]>(                       │
│        `${this.baseUrl}/transactions`);                         │
│    }                                                            │
└────────────────────────┬────────────────────────────────────────┘
                         ↓ HTTP GET /api/budget/transactions
┌─────────────────────────────────────────────────────────────────┐
│ 4. API CONTROLLER (BudgetController)                            │
│    [HttpGet("transactions")]                                    │
│    public async Task<IActionResult> GetTransactions(           │
│        [FromQuery] GetTransactionsQuery query)                 │
│    {                                                            │
│      var transactions = await _budgetService                   │
│        .GetAllTransactionsAsync(query);                        │
│      return Ok(transactions);                                  │
│    }                                                            │
└────────────────────────┬────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────────┐
│ 5. APPLICATION SERVICE (BudgetService)                          │
│    public async Task<List<TransactionDto>>                     │
│        GetAllTransactionsAsync(GetTransactionsQuery? query)    │
│    {                                                            │
│      // 5.1: Load aggregate (replay all events)                │
│      var budget = await GetCurrentBudgetAsync();               │
│                                                                  │
│      // 5.2: Get transactions from aggregate                   │
│      var transactions = budget.Transactions.AsEnumerable();    │
│                                                                  │
│      // 5.3: Apply filters                                     │
│      if (query?.FromDate.HasValue)                             │
│        transactions = transactions.Where(                       │
│          t => t.Date >= query.FromDate.Value);                 │
│                                                                  │
│      // 5.4: Sort                                              │
│      transactions = transactions                                │
│        .OrderByDescending(t => t.Date)                         │
│        .ThenBy(t => t.Id);                                     │
│                                                                  │
│      // 5.5: Map to DTOs                                       │
│      return transactions.Select(t => new TransactionDto {     │
│        Id = t.Id,                                              │
│        Amount = t.Amount.Amount,                               │
│        Currency = t.Amount.Currency,                           │
│        Category = t.Category.Name,                             │
│        Description = t.Description,                            │
│        Type = t.Type,                                          │
│        Date = t.Date                                           │
│      }).ToList();                                              │
│    }                                                            │
└────────────────────────┬────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────────┐
│ 6. EVENT SOURCING FLOW                                          │
│    GetCurrentBudgetAsync() {                                    │
│      // Read all events from storage                           │
│      var events = await _eventStore.GetAllEventsAsync();       │
│                                                                  │
│      // Create new aggregate                                    │
│      var budget = new BudgetAggregate();                        │
│                                                                  │
│      // Replay all events to rebuild state                     │
│      budget.LoadFromHistory(events);                            │
│                                                                  │
│      // Now budget contains current state                      │
│      return budget;                                             │
│    }                                                            │
└────────────────────────┬────────────────────────────────────────┘
                         ↓ Returns List<TransactionDto>
┌─────────────────────────────────────────────────────────────────┐
│ 7. API RESPONSE                                                 │
│    HTTP 200 OK                                                  │
│    Content-Type: application/json                              │
│    [                                                            │
│      {                                                          │
│        "id": "...",                                            │
│        "amount": 150.50,                                       │
│        "currency": "PLN",                                      │
│        "category": "Food & Dining",                            │
│        "description": "Groceries",                             │
│        "type": 2,                                              │
│        "date": "2025-11-01T00:00:00Z"                         │
│      },                                                         │
│      ...                                                        │
│    ]                                                            │
└────────────────────────┬────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────────┐
│ 8. ANGULAR SERVICE                                              │
│    Observable emits Transaction[]                              │
└────────────────────────┬────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────────┐
│ 9. ANGULAR COMPONENT                                            │
│    .subscribe({                                                │
│      next: (data) => {                                         │
│        this.transactions = data;                               │
│        this.loading = false;                                   │
│      }                                                          │
│    });                                                          │
└────────────────────────┬────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────────┐
│ 10. UI UPDATE                                                   │
│     - Transactions list rendered                               │
│     - Each transaction displayed with details                  │
│     - Formatted with pipes (currency, date)                    │
└─────────────────────────────────────────────────────────────────┘
```

### 5.3 Data Flow Summary

**Write Operations (Commands):**
```
User Input → Component → Service → HTTP → Controller → 
Application Service → Domain Aggregate → Event Creation → 
Event Store → Encrypted File → Disk
```

**Read Operations (Queries):**
```
User Request → Component → Service → HTTP → Controller → 
Application Service → Event Store → Read Events → 
Replay Events → Rebuild State → Map to DTO → HTTP Response → 
Component → UI Display
```

---

## 6. Configuration

### 6.1 Backend Configuration

#### 6.1.1 appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "BiedApp": "Debug"
    }
  },
  "AllowedHosts": "*",
  "Urls": "http://localhost:5000;https://localhost:5001",
  "Storage": {
    "Type": "File",
    "Description": "Options: File (encrypted JSON) or InMemory (testing)"
  }
}
```

**Configuration Options:**

| Key | Value | Description |
|-----|-------|-------------|
| `Logging:LogLevel:Default` | `Information` | Default log level |
| `Logging:LogLevel:Microsoft.AspNetCore` | `Warning` | ASP.NET Core framework logs |
| `Urls` | `http://localhost:5000;https://localhost:5001` | API URLs |
| `Storage:Type` | `File` or `InMemory` | Storage implementation |

#### 6.1.2 appsettings.Development.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  },
  "Storage": {
    "Type": "File"
  }
}
```

**Development-specific settings:**
- More verbose logging (Debug level)
- Uses File storage for persistence during development

#### 6.1.3 Program.cs Configuration

```csharp
var builder = WebApplication.CreateBuilder(args);

// 1. Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 2. Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// 3. Configure Event Store (based on appsettings)
var storageType = builder.Configuration["Storage:Type"] ?? "File";

builder.Services.AddSingleton<IEventStore>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<Program>>();

    return storageType.ToLower() switch
    {
        "memory" or "inmemory" => 
        {
            logger.LogInformation("Using InMemory event store");
            return new InMemoryEventStore();
        },
        "file" or _ => 
        {
            var eventsFilePath = EventStoreConfiguration.GetDefaultEventsFilePath();
            var encryptionKey = EventStoreConfiguration.GetEncryptionKey();
            var encryptionService = new EncryptionService(encryptionKey);
            
            logger.LogInformation("Using JSON File event store at: {FilePath}", 
                eventsFilePath);
            
            return new JsonFileEventStore(eventsFilePath, encryptionService);
        }
    };
});

// 4. Register Application Services
builder.Services.AddScoped<BudgetService>();
builder.Services.AddScoped<StatisticsService>();

// 5. Build and configure pipeline
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAngular");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

**Configuration Flow:**
1. Read configuration from appsettings.json
2. Based on `Storage:Type`, instantiate correct IEventStore implementation
3. Register services with dependency injection
4. Configure middleware pipeline

### 6.2 Frontend Configuration

#### 6.2.1 environment.ts (Development)

```typescript
export const environment = {
  production: false,
  apiUrl: 'https://localhost:5001/api'
};
```

#### 6.2.2 environment.prod.ts (Production)

```typescript
export const environment = {
  production: true,
  apiUrl: 'https://localhost:5001/api' // Or production URL
};
```

**Usage in Services:**
```typescript
@Injectable({ providedIn: 'root' })
export class BudgetApiService {
  private readonly baseUrl = `${environment.apiUrl}/budget`;
  // ...
}
```

#### 6.2.3 angular.json

```json
{
  "projects": {
    "BiedApp.Frontend": {
      "architect": {
        "serve": {
          "options": {
            "port": 4200,
            "open": true,
            "proxyConfig": "proxy.conf.json"
          }
        },
        "build": {
          "options": {
            "outputPath": "dist/bied-app-frontend",
            "index": "src/index.html",
            "main": "src/main.ts",
            "styles": [
              "src/styles.scss"
            ]
          }
        }
      }
    }
  }
}
```

#### 6.2.4 app.config.ts

```typescript
export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withInterceptorsFromDi()),
    provideAnimations()
  ]
};
```

**Providers:**
- `provideRouter`: Enables routing
- `provideHttpClient`: Enables HTTP requests
- `provideAnimations`: Enables Angular animations

### 6.3 Storage Configuration

#### 6.3.1 File Storage Location

**Automatic Location Detection:**
```csharp
public static string GetDefaultEventsFilePath()
{
    var appDataFolder = Environment.GetFolderPath(
        Environment.SpecialFolder.LocalApplicationData);
    var biedAppFolder = Path.Combine(appDataFolder, "BiedApp");
    
    if (!Directory.Exists(biedAppFolder))
        Directory.CreateDirectory(biedAppFolder);
    
    return Path.Combine(biedAppFolder, "events.json");
}
```

**Locations by OS:**
- **Windows**: `C:\Users\{Username}\AppData\Local\BiedApp\events.json`
- **macOS**: `/Users/{Username}/.local/share/BiedApp/events.json`
- **Linux**: `/home/{Username}/.local/share/BiedApp/events.json`

#### 6.3.2 Encryption Configuration

**Key Generation:**
```csharp
public static string GetEncryptionKey()
{
    var machineName = Environment.MachineName;
    var userName = Environment.UserName;
    var seedString = $"BiedApp-{machineName}-{userName}-v1.0";
    
    using var sha256 = SHA256.Create();
    var keyBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(seedString));
    return Convert.ToBase64String(keyBytes);
}
```

**Key Characteristics:**
- **Machine-Specific**: Different machines generate different keys
- **User-Specific**: Different users generate different keys
- **Deterministic**: Same machine + user = same key
- **256-bit**: SHA256 generates 32 bytes (256 bits)

**Encryption Algorithm:**
- **Algorithm**: AES-256-CBC
- **Key Size**: 256 bits (32 bytes)
- **IV Size**: 128 bits (16 bytes, derived from key)
- **Mode**: CBC (Cipher Block Chaining)
- **Padding**: PKCS7

---

## 7. Data Model

### 7.1 Domain Model (Event Sourcing)

#### 7.1.1 Event Schema

**Base Event Structure:**
```json
{
  "eventId": "GUID",
  "timestamp": "ISO 8601 DateTime (UTC)",
  "eventType": "String (discriminator)"
}
```

**TransactionAddedEvent:**
```json
{
  "eventId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "timestamp": "2025-11-01T10:30:00.000Z",
  "eventType": "TransactionAddedEvent",
  "transactionId": "f1e2d3c4-b5a6-7980-dcba-fe0987654321",
  "amount": 150.50,
  "currency": "PLN",
  "category": "Food & Dining",
  "description": "Weekly groceries",
  "type": 2,
  "date": "2025-11-01T00:00:00.000Z"
}
```

**TransactionUpdatedEvent:**
```json
{
  "eventId": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
  "timestamp": "2025-11-01T11:15:00.000Z",
  "eventType": "TransactionUpdatedEvent",
  "transactionId": "f1e2d3c4-b5a6-7980-dcba-fe0987654321",
  "amount": 165.00,
  "currency": "PLN",
  "category": "Food & Dining",
  "description": "Weekly groceries (updated)",
  "type": 2,
  "date": "2025-11-01T00:00:00.000Z"
}
```

**TransactionDeletedEvent:**
```json
{
  "eventId": "c3d4e5f6-a7b8-9012-cdef-123456789012",
  "timestamp": "2025-11-01T12:00:00.000Z",
  "eventType": "TransactionDeletedEvent",
  "transactionId": "f1e2d3c4-b5a6-7980-dcba-fe0987654321"
}
```

#### 7.1.2 Complete Event Stream Example

```json
[
  {
    "eventId": "e1-...",
    "timestamp": "2025-10-01T08:00:00Z",
    "eventType": "TransactionAddedEvent",
    "transactionId": "t1-...",
    "amount": 5000.00,
    "currency": "PLN",
    "category": "Salary",
    "description": "October salary",
    "type": 1,
    "date": "2025-10-01T00:00:00Z"
  },
  {
    "eventId": "e2-...",
    "timestamp": "2025-10-05T10:30:00Z",
    "eventType": "TransactionAddedEvent",
    "transactionId": "t2-...",
    "amount": 150.50,
    "currency": "PLN",
    "category": "Food & Dining",
    "description": "Groceries",
    "type": 2,
    "date": "2025-10-05T00:00:00Z"
  },
  {
    "eventId": "e3-...",
    "timestamp": "2025-10-05T11:00:00Z",
    "eventType": "TransactionUpdatedEvent",
    "transactionId": "t2-...",
    "amount": 165.00,
    "currency": "PLN",
    "category": "Food & Dining",
    "description": "Groceries (corrected)",
    "type": 2,
    "date": "2025-10-05T00:00:00Z"
  },
  {
    "eventId": "e4-...",
    "timestamp": "2025-10-10T14:20:00Z",
    "eventType": "TransactionAddedEvent",
    "transactionId": "t3-...",
    "amount": 80.00,
    "currency": "PLN",
    "category": "Transportation",
    "description": "Monthly bus pass",
    "type": 2,
    "date": "2025-10-10T00:00:00Z"
  }
]
```

**State Reconstruction:**

| Event # | Event Type | Action | Resulting State |
|---------|------------|--------|-----------------|
| 1 | TransactionAdded | Add transaction t1 (Salary +5000) | Balance: +5000 PLN |
| 2 | TransactionAdded | Add transaction t2 (Groceries -150.50) | Balance: +4849.50 PLN |
| 3 | TransactionUpdated | Update transaction t2 (Groceries -165) | Balance: +4835 PLN |
| 4 | TransactionAdded | Add transaction t3 (Bus pass -80) | Balance: +4755 PLN |

### 7.2 API DTOs

#### 7.2.1 TransactionDto

```typescript
interface TransactionDto {
  id: string;                    // GUID
  amount: number;                // Decimal
  currency: string;              // "PLN"
  category: string;              // "Food & Dining"
  description: string;           // Free text
  type: TransactionType;         // 1 (Income) or 2 (Expense)
  date: Date;                    // ISO 8601 DateTime
  typeDisplay?: string;          // "Income" or "Expense"
  amountDisplay?: string;        // "150.50 PLN"
}
```

#### 7.2.2 BudgetSummaryDto

```typescript
interface BudgetSummaryDto {
  totalIncome: number;           // Sum of all income transactions
  totalExpenses: number;         // Sum of all expense transactions
  balance: number;               // totalIncome - totalExpenses
  currency: string;              // "PLN"
  transactionCount: number;      // Total count
  incomeCount: number;           // Count of income transactions
  expenseCount: number;          // Count of expense transactions
  totalIncomeDisplay?: string;   // Formatted for display
  totalExpensesDisplay?: string; // Formatted for display
  balanceDisplay?: string;       // Formatted for display
  balanceStatus?: string;        // "Positive" or "Negative"
}
```

#### 7.2.3 CategorySummaryDto

```typescript
interface CategorySummaryDto {
  category: string;              // Category name
  totalAmount: number;           // Sum for this category
  transactionCount: number;      // Count for this category
  currency: string;              // "PLN"
  percentage: number;            // % of total (0-100)
  totalAmountDisplay?: string;   // Formatted amount
  percentageDisplay?: string;    // Formatted percentage
}
```

### 7.3 Database Schema (N/A)

**Note:** BiedApp does not use a traditional database. All data is stored as events in a JSON file.

**Advantages:**
- No database installation required
- Simple backup (copy one file)
- Complete audit trail
- Easy to inspect data

**Trade-offs:**
- Limited query capabilities (must replay events)
- Not suitable for high concurrency
- No built-in indexing

---

## 8. Security

### 8.1 Data Encryption

#### 8.1.1 Encryption Algorithm

**AES-256-CBC (Advanced Encryption Standard with Cipher Block Chaining)**

**Parameters:**
- Key Size: 256 bits (32 bytes)
- Block Size: 128 bits (16 bytes)
- Mode: CBC (Cipher Block Chaining)
- Padding: PKCS7
#### 8.1.2 Key Derivation

```csharp
public static string GetEncryptionKey()
{
    // Components
    var machineName = Environment.MachineName;      // e.g., "DESKTOP-ABC123"
    var userName = Environment.UserName;            // e.g., "JohnDoe"
    var version = "v1.0";                           // Application version
    
    // Seed string
    var seedString = $"BiedApp-{machineName}-{userName}-{version}";
    // Example: "BiedApp-DESKTOP-ABC123-JohnDoe-v1.0"
    
    // Hash with SHA256 to generate 256-bit key
    using var sha256 = SHA256.Create();
    var keyBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(seedString));
    
    // Convert to Base64 for storage/transmission
    return Convert.ToBase64String(keyBytes);
    // Example output: "xK8pL3mN9qR5sT7uV2wX4yZ6aB8cD0eF1gH3iJ5kL7mN9oP="
}
```

**Key Characteristics:**

| Property | Value | Description |
|----------|-------|-------------|
| **Deterministic** | Yes | Same machine + user = same key always |
| **Machine-Specific** | Yes | Different on each computer |
| **User-Specific** | Yes | Different for each OS user |
| **Length** | 256 bits | Maximum AES security |
| **Algorithm** | SHA256 | Industry-standard hashing |
| **Storage** | Not stored | Regenerated each time |

**Security Implications:**

✅ **Pros:**
- No key storage required
- Automatic per-user encryption
- Simple implementation
- No key management overhead

⚠️ **Cons:**
- Data cannot be moved between machines
- Data cannot be shared between users
- Changing machine name or username breaks encryption
- No key rotation capability

#### 8.1.3 Initialization Vector (IV)

```csharp
public EncryptionService(string base64Key)
{
    var keyBytes = Convert.FromBase64String(base64Key);
    _key = keyBytes;
    
    // Derive IV from key (first 16 bytes of SHA256(key))
    using var sha256 = SHA256.Create();
    _iv = sha256.ComputeHash(keyBytes).Take(16).ToArray();
}
```

**IV Characteristics:**
- **Size**: 128 bits (16 bytes)
- **Derivation**: First 16 bytes of SHA256(key)
- **Deterministic**: Same key = same IV
- **Security Note**: In production, consider unique IV per encryption

#### 8.1.4 Encryption Process

```csharp
public string Encrypt(string plainText)
{
    // 1. Create AES instance
    using var aes = Aes.Create();
    aes.Key = _key;                    // 256-bit key
    aes.IV = _iv;                      // 128-bit IV
    aes.Mode = CipherMode.CBC;         // Cipher Block Chaining
    aes.Padding = PaddingMode.PKCS7;   // PKCS#7 padding
    
    // 2. Create encryptor
    using var encryptor = aes.CreateEncryptor();
    
    // 3. Convert plaintext to bytes
    var plainBytes = Encoding.UTF8.GetBytes(plainText);
    
    // 4. Encrypt
    var encryptedBytes = encryptor.TransformFinalBlock(
        plainBytes, 0, plainBytes.Length);
    
    // 5. Convert to Base64 for storage
    return Convert.ToBase64String(encryptedBytes);
}
```

**Encryption Flow:**
```
Plaintext JSON
    ↓
UTF-8 Encoding
    ↓
Byte Array [plain bytes]
    ↓
AES-256-CBC Encryption
    ↓
Byte Array [encrypted bytes]
    ↓
Base64 Encoding
    ↓
Encrypted String (stored in file)
```

#### 8.1.5 Decryption Process

```csharp
public string Decrypt(string cipherText)
{
    try
    {
        // 1. Create AES instance
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        
        // 2. Create decryptor
        using var decryptor = aes.CreateDecryptor();
        
        // 3. Convert Base64 to bytes
        var cipherBytes = Convert.FromBase64String(cipherText);
        
        // 4. Decrypt
        var decryptedBytes = decryptor.TransformFinalBlock(
            cipherBytes, 0, cipherBytes.Length);
        
        // 5. Convert bytes to string
        return Encoding.UTF8.GetString(decryptedBytes);
    }
    catch (CryptographicException)
    {
        // Wrong key or corrupted data
        return "[]"; // Return empty event list
    }
    catch (FormatException)
    {
        // Not encrypted (plain text file)
        return "[]";
    }
}
```

**Decryption Flow:**
```
Encrypted String (from file)
    ↓
Base64 Decoding
    ↓
Byte Array [encrypted bytes]
    ↓
AES-256-CBC Decryption
    ↓
Byte Array [plain bytes]
    ↓
UTF-8 Decoding
    ↓
Plaintext JSON
```

#### 8.1.6 Example: Before and After Encryption

**Before Encryption (events.json - Plain JSON):**
```json
[
  {
    "eventId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "timestamp": "2025-11-01T10:30:00.000Z",
    "eventType": "TransactionAddedEvent",
    "transactionId": "f1e2d3c4-b5a6-7980-dcba-fe0987654321",
    "amount": 150.50,
    "currency": "PLN",
    "category": "Food & Dining",
    "description": "Weekly groceries",
    "type": 2,
    "date": "2025-11-01T00:00:00.000Z"
  }
]
```

**After Encryption (events.json - Encrypted):**
```
kJ8HvN2pL9xQwR3tY5zM8aB4cD6eF7gH9iJ0kL1mN2oP3qR4sT5uV6wX7yZ8aB9cD0eF1gH2iJ3kL4m
N5oP6qR7sT8uV9wX0yZ1aB2cD3eF4gH5iJ6kL7mN8oP9qR0sT1uV2wX3yZ4aB5cD6eF7gH8iJ9kL0mN1o
P2qR3sT4uV5wX6yZ7aB8cD9eF0gH1iJ2kL3mN4oP5qR6sT7uV8wX9yZ0aB1cD2eF3gH4iJ5kL6mN7oP8q
... (Base64 encoded encrypted data)
```

### 8.2 API Security

#### 8.2.1 HTTPS Configuration

**launchSettings.json:**
```json
{
  "profiles": {
    "BiedApp.API": {
      "commandName": "Project",
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "https://localhost:5001;http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

**HTTPS Enforcement:**
```csharp
// In Program.cs
app.UseHttpsRedirection(); // Redirects HTTP to HTTPS
```

**Certificate:**
- Development: Self-signed certificate (created by .NET SDK)
- Production: Would require valid SSL certificate

#### 8.2.2 CORS Configuration

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",   // Angular dev server
                "https://localhost:4200")  // Angular dev server (HTTPS)
            .AllowAnyHeader()              // Allow all headers
            .AllowAnyMethod()              // Allow all HTTP methods
            .AllowCredentials();           // Allow cookies/credentials
    });
});

// Apply CORS
app.UseCors("AllowAngular");
```

**CORS Policy Details:**

| Setting | Value | Purpose |
|---------|-------|---------|
| **Origins** | `localhost:4200` | Only Angular app can make requests |
| **Headers** | Any | Accept any request headers |
| **Methods** | Any | Allow GET, POST, PUT, DELETE, etc. |
| **Credentials** | True | Allow cookies and authentication headers |

**Security Considerations:**
- ✅ Restricted to specific origin (localhost:4200)
- ✅ Prevents other websites from accessing API
- ⚠️ In production, update to actual domain

#### 8.2.3 Input Validation

**Command Validation:**
```csharp
public record AddTransactionCommand
{
    public decimal Amount { get; init; }
    public string Category { get; init; } = string.Empty;
    public DateTime Date { get; init; }
    
    public void Validate()
    {
        // Amount validation
        if (Amount <= 0)
            throw new ArgumentException(
                "Amount must be greater than zero", 
                nameof(Amount));
        
        if (Amount > 1_000_000)
            throw new ArgumentException(
                "Amount exceeds maximum allowed", 
                nameof(Amount));
        
        // Category validation
        if (string.IsNullOrWhiteSpace(Category))
            throw new ArgumentException(
                "Category is required", 
                nameof(Category));
        
        if (Category.Length > 100)
            throw new ArgumentException(
                "Category name too long (max 100 characters)", 
                nameof(Category));
        
        // Date validation
        if (Date > DateTime.Now.AddDays(1))
            throw new ArgumentException(
                "Date cannot be in the future", 
                nameof(Date));
        
        if (Date < new DateTime(2000, 1, 1))
            throw new ArgumentException(
                "Date is too far in the past", 
                nameof(Date));
    }
}
```

**Controller Validation:**
```csharp
[HttpPost("transactions")]
public async Task<IActionResult> AddTransaction(
    [FromBody] AddTransactionCommand command)
{
    try
    {
        // Validate command
        command.Validate();
        
        // Execute
        await _budgetService.AddTransactionAsync(command);
        
        return CreatedAtAction(nameof(GetTransactions), 
            new { }, 
            new { message = "Transaction added successfully" });
    }
    catch (ArgumentException ex)
    {
        // Return 400 Bad Request with error message
        return BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        // Log error
        _logger.LogError(ex, "Error adding transaction");
        
        // Return 500 Internal Server Error
        return StatusCode(500, 
            new { error = "Failed to add transaction" });
    }
}
```

**Validation Layers:**

```
Layer 1: Client-Side (Angular)
  ↓ HTML5 validation + Angular validators
Layer 2: API (Controller)
  ↓ Model binding validation
Layer 3: Command (Application)
  ↓ Command.Validate() method
Layer 4: Domain (Aggregate)
  ↓ Business rule enforcement
```

### 8.3 Data Privacy

#### 8.3.1 Local Storage Only

**Data Location:**
```
%LOCALAPPDATA%\BiedApp\events.json

Example:
C:\Users\JohnDoe\AppData\Local\BiedApp\events.json
```

**Privacy Features:**
- ✅ Data never leaves the local machine
- ✅ No cloud synchronization
- ✅ No telemetry or tracking
- ✅ Encrypted with user-specific key
- ✅ Protected by OS user permissions

**File Permissions (Windows):**
```
Owner: JohnDoe (Full Control)
SYSTEM: Full Control
Administrators: Full Control
Other Users: No Access
```

#### 8.3.2 No Authentication Required

**Design Decision:**
BiedApp is designed for single-user, local-only use. No authentication is implemented because:

1. **OS-Level Security**: Windows/macOS/Linux user accounts provide security
2. **Encrypted Storage**: Data is encrypted and only accessible to the user
3. **No Network Exposure**: API only listens on localhost
4. **Simplicity**: No password management, no user registration

**Access Control:**
```
Physical Access to Machine
    ↓
OS User Authentication (Windows/macOS/Linux)
    ↓
File System Permissions
    ↓
Encryption (AES-256)
    ↓
BiedApp Data
```

#### 8.3.3 Data Isolation

**Per-User Data:**
Each OS user gets their own encrypted data file:

```
User: Alice
  └─ C:\Users\Alice\AppData\Local\BiedApp\events.json
     Encrypted with: SHA256("BiedApp-MACHINE-Alice-v1.0")

User: Bob
  └─ C:\Users\Bob\AppData\Local\BiedApp\events.json
     Encrypted with: SHA256("BiedApp-MACHINE-Bob-v1.0")
```

**Consequences:**
- Alice cannot read Bob's data (different encryption key)
- Bob cannot read Alice's data (different encryption key)
- Each user sees only their own budget

### 8.4 Security Best Practices

#### 8.4.1 Implemented

✅ **HTTPS Enforcement**
- All API communication over HTTPS
- HTTP redirects to HTTPS

✅ **Input Validation**
- Multi-layer validation (client, API, domain)
- Type-safe commands
- Range checks and format validation

✅ **Data Encryption**
- AES-256 encryption for stored data
- Automatic encryption/decryption

✅ **CORS Policy**
- Restricted to specific origin
- Prevents unauthorized API access

✅ **Error Handling**
- Proper HTTP status codes
- No sensitive information in error messages
- Logging for debugging

✅ **Thread Safety**
- SemaphoreSlim for file access
- Prevents concurrent write conflicts

#### 8.4.2 Considerations for Production

⚠️ **Current Limitations:**

1. **IV Reuse**: Same IV for all encryptions
   - **Risk**: Pattern exposure in large datasets
   - **Solution**: Generate unique IV per encryption, store with data

2. **No Key Rotation**: Key never changes
   - **Risk**: Long-term key compromise
   - **Solution**: Implement key rotation mechanism

3. **No Backup Encryption**: Export files are plain JSON
   - **Risk**: Exported data is unencrypted
   - **Solution**: Encrypt exports with password

4. **No Rate Limiting**: API has no rate limits
   - **Risk**: Potential DOS attacks
   - **Solution**: Implement rate limiting middleware

5. **No Authentication**: API is open to localhost
   - **Risk**: Any process on machine can access
   - **Solution**: Add API key or token authentication

**Recommended Enhancements:**

```csharp
// 1. Unique IV per encryption
public string Encrypt(string plainText)
{
    using var aes = Aes.Create();
    aes.Key = _key;
    aes.GenerateIV(); // Generate unique IV
    
    var iv = aes.IV;
    var encryptor = aes.CreateEncryptor();
    var encrypted = /* ... */;
    
    // Prepend IV to encrypted data
    var combined = iv.Concat(encrypted).ToArray();
    return Convert.ToBase64String(combined);
}

// 2. API Key Authentication
[ServiceFilter(typeof(ApiKeyAuthFilter))]
public class BudgetController : ControllerBase
{
    // ...
}

// 3. Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
    });
});
```

---

## 9. Deployment

### 9.1 Development Environment

#### 9.1.1 Prerequisites

**Required Software:**
- .NET 8.0 SDK or later
- Node.js 18+ and npm
- Angular CLI 18+
- Visual Studio 2022 or VS Code
- Git (for version control)

**Installation Steps:**

1. **Install .NET SDK**
```bash
# Download from: https://dotnet.microsoft.com/download
# Verify installation:
dotnet --version
# Expected: 8.0.x or later
```

2. **Install Node.js and npm**
```bash
# Download from: https://nodejs.org/
# Verify installation:
node --version  # Expected: v18.x.x or later
npm --version   # Expected: 9.x.x or later
```

3. **Install Angular CLI**
```bash
npm install -g @angular/cli
ng version
# Expected: Angular CLI 18.x.x or later
```

#### 9.1.2 Building and Running

**Backend (API):**
```bash
# Navigate to API project
cd BiedApp.API

# Restore dependencies
dotnet restore

# Build
dotnet build

# Run
dotnet run
# API will be available at:
# - http://localhost:5000
# - https://localhost:5001
# - Swagger UI: https://localhost:5001/swagger
```

**Frontend (Angular):**
```bash
# Navigate to frontend project
cd BiedApp.Frontend

# Install dependencies
npm install

# Run development server
ng serve
# Angular app will be available at:
# - http://localhost:4200
```

**Running Both Simultaneously:**

**Option 1: Two Terminals**
```bash
# Terminal 1 (Backend)
cd BiedApp.API
dotnet run

# Terminal 2 (Frontend)
cd BiedApp.Frontend
ng serve
```

**Option 2: Visual Studio**
1. Right-click solution → Properties
2. Multiple startup projects
3. Set both `BiedApp.API` and `BiedApp.Frontend` to **Start**
4. Click OK
5. Press F5 to start debugging

#### 9.1.3 Development Workflow

**Typical Development Cycle:**

1. **Start Backend**
   - Run `dotnet run` in API project
   - Swagger UI opens automatically
   - Test API endpoints

2. **Start Frontend**
   - Run `ng serve` in Frontend project
   - Navigate to http://localhost:4200
   - Hot reload enabled (changes reflect automatically)

3. **Make Changes**
   - Backend: Edit C# files → Auto-reload on save
   - Frontend: Edit TypeScript/HTML/SCSS → Hot reload

4. **Test**
   - Use browser dev tools (F12)
   - Check Network tab for API calls
   - Check Console for errors

5. **Commit**
   - Git add/commit changes
   - Push to repository

### 9.2 Production Build

#### 9.2.1 Backend Build

**Release Build:**
```bash
cd BiedApp.API

# Build in Release mode
dotnet build --configuration Release

# Publish (creates self-contained package)
dotnet publish --configuration Release --output ./publish

# Output location: BiedApp.API/publish/
```

**Published Files:**
```
publish/
├── BiedApp.API.dll
├── BiedApp.API.exe (Windows only)
├── BiedApp.Application.dll
├── BiedApp.Domain.dll
├── BiedApp.Infrastructure.dll
├── appsettings.json
├── appsettings.Production.json
└── [Other dependencies]
```

**Self-Contained Deployment (Optional):**
```bash
# Includes .NET runtime (no .NET installation required)
dotnet publish --configuration Release \
  --output ./publish \
  --runtime win-x64 \
  --self-contained true

# Platform options:
# - win-x64 (Windows 64-bit)
# - win-x86 (Windows 32-bit)
# - linux-x64 (Linux 64-bit)
# - osx-x64 (macOS 64-bit)
```

#### 9.2.2 Frontend Build

**Production Build:**
```bash
cd BiedApp.Frontend

# Build for production
ng build --configuration production

# Output location: dist/bied-app-frontend/
```

**Build Output:**
```
dist/bied-app-frontend/
├── index.html
├── main.[hash].js
├── polyfills.[hash].js
├── runtime.[hash].js
├── styles.[hash].css
└── assets/
```

**Build Optimizations:**
- Minification (reduces file size)
- Tree-shaking (removes unused code)
- AOT compilation (Ahead-Of-Time)
- CSS optimization
- Asset optimization

### 9.3 Deployment Options

#### 9.3.1 Option 1: Single Executable (Recommended for BiedApp)

**Create Desktop Application:**

1. **Backend**: Publish as self-contained
```bash
cd BiedApp.API
dotnet publish -c Release -r win-x64 --self-contained true -o ./dist
```

2. **Frontend**: Build production assets
```bash
cd BiedApp.Frontend
ng build --configuration production
```

3. **Combine**: Copy frontend build to API wwwroot
```bash
# Create wwwroot in API publish folder
mkdir ./dist/wwwroot

# Copy Angular build
cp -r ./BiedApp.Frontend/dist/bied-app-frontend/* ./dist/wwwroot/
```

4. **Configure API to serve static files**
```csharp
// In Program.cs
app.UseDefaultFiles();
app.UseStaticFiles();

// Fallback to index.html for SPA routing
app.MapFallbackToFile("index.html");
```

5. **Run**
```bash
./dist/BiedApp.API.exe
# Opens on https://localhost:5001
# Serves Angular app and API
```

**Distribution:**
- Package `./dist/` folder
- Create installer (optional): Use Inno Setup or NSIS
- Distribute as ZIP file

#### 9.3.2 Option 2: Windows Service

**Install as Windows Service:**

1. **Install package**
```bash
dotnet add package Microsoft.Extensions.Hosting.WindowsServices
```

2. **Update Program.cs**
```csharp
var builder = WebApplication.CreateBuilder(args);

// Add Windows Service support
builder.Host.UseWindowsService();

// Rest of configuration...
```

3. **Publish**
```bash
dotnet publish -c Release -r win-x64 --self-contained true
```

4. **Install Service**
```powershell
# Run as Administrator
sc create BiedApp binPath="C:\Path\To\BiedApp.API.exe"
sc start BiedApp
```

5. **Manage Service**
```powershell
# Stop service
sc stop BiedApp

# Uninstall service
sc delete BiedApp
```

#### 9.3.3 Option 3: Docker Container (Advanced)

**Dockerfile (Backend):**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["BiedApp.API/BiedApp.API.csproj", "BiedApp.API/"]
COPY ["BiedApp.Application/BiedApp.Application.csproj", "BiedApp.Application/"]
COPY ["BiedApp.Domain/BiedApp.Domain.csproj", "BiedApp.Domain/"]
COPY ["BiedApp.Infrastructure/BiedApp.Infrastructure.csproj", "BiedApp.Infrastructure/"]
RUN dotnet restore "BiedApp.API/BiedApp.API.csproj"
COPY . .
WORKDIR "/src/BiedApp.API"
RUN dotnet build "BiedApp.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BiedApp.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BiedApp.API.dll"]
```

**Build and Run:**
```bash
# Build image
docker build -t biedapp-api .

# Run container
docker run -d -p 5001:5001 \
  -v ${HOME}/.local/share/BiedApp:/app/data \
  biedapp-api
```

### 9.4 Configuration for Production

#### 9.4.1 appsettings.Production.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "BiedApp": "Information"
    }
  },
  "Storage": {
    "Type": "File"
  },
  "Urls": "https://localhost:5001"
}
```

#### 9.4.2 Environment Variables

```bash
# Set environment
export ASPNETCORE_ENVIRONMENT=Production

# Or in Windows
set ASPNETCORE_ENVIRONMENT=Production

# Or in appsettings
{
  "ASPNETCORE_ENVIRONMENT": "Production"
}
```

### 9.5 Backup and Restore

#### 9.5.1 Backup

**Manual Backup:**
```bash
# Copy events.json file
cp %LOCALAPPDATA%\BiedApp\events.json \
   D:\Backups\BiedApp\events.json.backup
```

**Automated Backup Script (PowerShell):**
```powershell
# BackupBiedApp.ps1
$source = "$env:LOCALAPPDATA\BiedApp\events.json"
$destination = "D:\Backups\BiedApp\events_$(Get-Date -Format 'yyyyMMdd_HHmmss').json"

if (Test-Path $source) {
    Copy-Item $source $destination
    Write-Host "Backup created: $destination"
} else {
    Write-Host "Source file not found: $source"
}
```

**Schedule with Task Scheduler:**
1. Open Task Scheduler
2. Create Basic Task
3. Trigger: Daily at 2:00 AM
4. Action: Start Program
5. Program: `powershell.exe`
6. Arguments: `-File "C:\Path\To\BackupBiedApp.ps1"`

#### 9.5.2 Restore

**Using Export Feature:**
```typescript
// In Angular
exportData() {
  this.budgetApi.exportData().subscribe(blob => {
    // Download as JSON file
    saveAs(blob, 'biedapp-backup.json');
  });
}
```

**Manual Restore:**
```bash
# Stop BiedApp

# Replace events.json with backup
cp D:\Backups\BiedApp\events.json.backup \
   %LOCALAPPDATA%\BiedApp\events.json

# Start BiedApp
```

### 9.6 Troubleshooting

#### 9.6.1 Common Issues

**Issue: API won't start - Port already in use**
```
Solution:
1. Check what's using the port:
   netstat -ano | findstr :5001
   
2. Kill the process:
   taskkill /PID <process_id> /F
   
3. Or change port in appsettings.json:
   "Urls": "https://localhost:5002"
```

**Issue: Frontend can't connect to API**
```
Solution:
1. Check CORS configuration
2. Verify API is running (check https://localhost:5001/swagger)
3. Check environment.ts has correct apiUrl
4. Check browser console for CORS errors
```

**Issue: Events.json is corrupted**
```
Solution:
1. Stop BiedApp
2. Rename events.json to events.json.corrupted
3. Create new empty file: echo [] > events.json
4. Start BiedApp (will create fresh file)
5. Optionally restore from backup
```

**Issue: Cannot decrypt events.json (wrong machine/user)**
```
Solution:
1. Export data from original machine using Export feature
2. Import on new machine (future feature)
3. Or manually copy and re-encrypt:
   - Decrypt on original machine
   - Copy plain JSON
   - Create new encrypted file on new machine
```

---

## 10. Future Enhancements

### 10.1 Planned Features

1. **Import/Export Improvements**
   - Import from CSV
   - Export to Excel
   - Scheduled backups to cloud (OneDrive, Dropbox)

2. **Budgeting Features**
   - Budget limits per category
   - Alerts when exceeding budget
   - Budget vs actual comparison

3. **Recurring Transactions**
   - Monthly bills
   - Salary automation
   - Subscription tracking

4. **Multi-Currency Support**
   - Support multiple currencies
   - Exchange rate tracking
   - Currency conversion

5. **Advanced Analytics**
   - Spending trends over time
   - Category predictions
   - Savings goals tracking

6. **Mobile App**
   - iOS/Android apps
   - Sync via cloud or local network
   - QR code for receipt scanning

### 10.2 Architectural Improvements

1. **Snapshots**
   - Periodic state snapshots to speed up event replay
   - Reduce time to load large event streams

2. **Read Model (Full CQRS)**
   - Separate read database (SQLite)
   - Updated via event handlers
   - Faster queries

3. **Event Versioning**
   - Support for event schema evolution
   - Backward compatibility

4. **Multi-User Support**
   - Shared family budgets
   - Permission-based access
   - Sync mechanism

---

## Appendix

### A. Glossary

| Term | Definition |
|------|------------|
| **Aggregate** | A cluster of domain objects treated as a single unit for data changes |
| **Command** | An instruction to change the system state |
| **CQS** | Command Query Separation - separating commands from queries |
| **CQRS** | Command Query Responsibility Segregation - separate models for reads and writes |
| **DTO** | Data Transfer Object - simple object for transferring data between layers |
| **Entity** | An object with a unique identity |
| **Event** | A record of something that happened in the past |
| **Event Sourcing** | Storing all changes as a sequence of events |
| **Value Object** | An immutable object defined by its attributes, not identity |

### B. References

- **Clean Architecture**: Robert C. Martin (Uncle Bob)
- **Domain-Driven Design**: Eric Evans
- **Event Sourcing**: Greg Young, Martin Fowler
- **CQRS Pattern**: Greg Young
- **ASP.NET Core Documentation**: https://docs.microsoft.com/aspnet/core
- **Angular Documentation**: https://angular.io/docs

### C. Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2025-11-02 | Initial release with all core features |

---

**End of Document**