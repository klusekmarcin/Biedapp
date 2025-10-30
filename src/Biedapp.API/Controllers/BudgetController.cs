using Biedapp.Application.Commands;
using Biedapp.Application.DTOs;
using Biedapp.Application.Interfaces;
using Biedapp.Application.Queries;
using Biedapp.Application.Services;
using Biedapp.Domain.Enums;

using Microsoft.AspNetCore.Mvc;

namespace Biedapp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class BudgetController : ControllerBase
{
    private readonly IBudgetService _budgetService;
    private readonly ILogger<BudgetController> _logger;

    public BudgetController(
        IBudgetService budgetService,
        ILogger<BudgetController> logger)
    {
        _budgetService = budgetService;
        _logger = logger;
    }

    /// <summary>
    /// Get all transactions with optional filters
    /// </summary>
    [HttpGet("transactions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? category,
        [FromQuery] TransactionType? type,
        [FromQuery] int? limit)
    {
        try
        {
            GetTransactionsQuery query = new()
            {
                FromDate = fromDate,
                ToDate = toDate,
                Category = category,
                Type = type,
                Limit = limit
            };

            List<TransactionDto> transactions = await _budgetService.GetAllTransactionsAsync(query);

            _logger.LogInformation("Retrieved {Count} transactions", transactions.Count);

            return Ok(transactions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transactions");
            return StatusCode(500, new { error = "Failed to retrieve transactions" });
        }
    }

    /// <summary>
    /// Get a specific transaction by ID
    /// </summary>
    [HttpGet("transactions/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransaction(Guid id)
    {
        try
        {
            TransactionDto transaction = await _budgetService.GetTransactionByIdAsync(id);

            if (transaction == null)
            {
                _logger.LogWarning("Transaction {Id} not found", id);
                return NotFound(new { error = $"Transaction {id} not found" });
            }

            return Ok(transaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transaction {Id}", id);
            return StatusCode(500, new { error = "Failed to retrieve transaction" });
        }
    }

    /// <summary>
    /// Add a new transaction
    /// </summary>
    [HttpPost("transactions")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddTransaction([FromBody] AddTransactionCommand command)
    {
        try
        {
            command.Validate();
            await _budgetService.AddTransactionAsync(command);

            _logger.LogInformation(
                "Added transaction: {Amount} {Currency} - {Category}",
                command.Amount,
                command.Currency,
                command.Category);

            return CreatedAtAction(
                nameof(GetTransactions),
                new { },
                new { message = "Transaction added successfully" });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid transaction data");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding transaction");
            return StatusCode(500, new { error = "Failed to add transaction" });
        }
    }

    /// <summary>
    /// Update an existing transaction
    /// </summary>
    [HttpPut("transactions/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTransaction(Guid id, [FromBody] UpdateTransactionCommand command)
    {
        try
        {
            // Ensure ID in URL matches ID in body
            if (command.Id != Guid.Empty && command.Id != id)
            {
                return BadRequest(new { error = "Transaction ID mismatch" });
            }

            // Create command with correct ID
            UpdateTransactionCommand updateCommand = command with { Id = id };
            updateCommand.Validate();

            await _budgetService.UpdateTransactionAsync(updateCommand);

            _logger.LogInformation("Updated transaction {Id}", id);

            return Ok(new { message = "Transaction updated successfully" });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid transaction data for update");
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            _logger.LogWarning("Transaction {Id} not found for update", id);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating transaction {Id}", id);
            return StatusCode(500, new { error = "Failed to update transaction" });
        }
    }

    /// <summary>
    /// Delete a transaction
    /// </summary>
    [HttpDelete("transactions/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTransaction(Guid id)
    {
        try
        {
            DeleteTransactionCommand command = new() { Id = id };
            command.Validate();

            await _budgetService.DeleteTransactionAsync(command);

            _logger.LogInformation("Deleted transaction {Id}", id);

            return Ok(new { message = "Transaction deleted successfully" });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            _logger.LogWarning("Transaction {Id} not found for deletion", id);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting transaction {Id}", id);
            return StatusCode(500, new { error = "Failed to delete transaction" });
        }
    }

    /// <summary>
    /// Get budget summary (income, expenses, balance)
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary()
    {
        try
        {
            BudgetSummaryDto summary = await _budgetService.GetBudgetSummaryAsync();

            _logger.LogInformation(
                "Retrieved budget summary - Balance: {Balance}",
                summary.Balance);

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving budget summary");
            return StatusCode(500, new { error = "Failed to retrieve budget summary" });
        }
    }

    /// <summary>
    /// Get spending summary by category
    /// </summary>
    [HttpGet("categories/summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategorySummary([FromQuery] TransactionType? type = null)
    {
        try
        {
            List<CategorySummaryDto> summary = await _budgetService.GetCategorySummaryAsync(type);

            _logger.LogInformation(
                "Retrieved category summary - {Count} categories",
                summary.Count);

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving category summary");
            return StatusCode(500, new { error = "Failed to retrieve category summary" });
        }
    }

    /// <summary>
    /// Get all available categories
    /// </summary>
    [HttpGet("categories")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories()
    {
        try
        {
            List<string> categories = await _budgetService.GetAllCategoriesAsync();
            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving categories");
            return StatusCode(500, new { error = "Failed to retrieve categories" });
        }
    }

    /// <summary>
    /// Get monthly income and expenses for a specific month
    /// </summary>
    [HttpGet("monthly/{year}/{month}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMonthlyData(int year, int month)
    {
        try
        {
            if (year < 2000 || year > 2100)
                return BadRequest(new { error = "Invalid year" });

            if (month < 1 || month > 12)
                return BadRequest(new { error = "Invalid month" });

            Dictionary<string, decimal> data = await _budgetService.GetMonthlyIncomeExpensesAsync(year, month);

            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving monthly data for {Year}-{Month}", year, month);
            return StatusCode(500, new { error = "Failed to retrieve monthly data" });
        }
    }

    /// <summary>
    /// Export all data as JSON
    /// </summary>
    [HttpGet("export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportData()
    {
        try
        {
            List<TransactionDto> transactions = await _budgetService.GetAllTransactionsAsync();
            BudgetSummaryDto summary = await _budgetService.GetBudgetSummaryAsync();
            List<string> categories = await _budgetService.GetAllCategoriesAsync();

            var exportData = new
            {
                exportDate = DateTime.UtcNow,
                summary = summary,
                transactions = transactions,
                categories = categories
            };

            string json = System.Text.Json.JsonSerializer.Serialize(exportData,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
            return File(bytes, "application/json", $"biedapp-export-{DateTime.Now:yyyyMMdd}.json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting data");
            return StatusCode(500, new { error = "Failed to export data" });
        }
    }

    /// <summary>
    /// Clear all transactions (deletes events file)
    /// </summary>
    [HttpDelete("clear-all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ClearAllData()
    {
        try
        {
            // Get all transactions
            List<TransactionDto> transactions = await _budgetService.GetAllTransactionsAsync();

            // Delete each transaction (creates delete events)
            foreach (TransactionDto transaction in transactions)
            {
                DeleteTransactionCommand command = new() { Id = transaction.Id };
                await _budgetService.DeleteTransactionAsync(command);
            }

            _logger.LogWarning("All data cleared - {Count} transactions deleted", transactions.Count);

            return Ok(new { message = $"Successfully deleted {transactions.Count} transactions" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing data");
            return StatusCode(500, new { error = "Failed to clear data" });
        }
    }

    /// <summary>
    /// Get budget summary for current month
    /// </summary>
    [HttpGet("summary/current-month")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentMonthSummary()
    {
        try
        {
            DateTime now = DateTime.Now;
            BudgetSummaryDto summary = await _budgetService.GetMonthlyBudgetSummaryAsync(now.Year, now.Month);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current month summary");
            return StatusCode(500, new { error = "Failed to retrieve summary" });
        }
    }

    /// <summary>
    /// Get budget summary for specific month
    /// </summary>
    [HttpGet("summary/{year}/{month}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMonthlySummary(int year, int month)
    {
        try
        {
            if(!DateTime.TryParse($"{year}-{month}-01T00:00:00Z", out _))
            {
                return BadRequest(new { error = "Invalid year or month" });
            }

            BudgetSummaryDto summary = await _budgetService.GetMonthlyBudgetSummaryAsync(year, month);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving monthly summary");
            return StatusCode(500, new { error = "Failed to retrieve summary" });
        }
    }
}
