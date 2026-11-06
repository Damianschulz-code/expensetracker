namespace ExpenseTracker;

/// <summary>Одна запись о расходе.</summary>
public record Expense(Guid Id, DateTime Date, string Category, decimal Amount, string Note);
