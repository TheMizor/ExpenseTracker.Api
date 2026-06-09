namespace ExpenseTracker.Api.DTOs;

public record CreateTransactionRequest(decimal Amount, DateOnly Date, string Label, int CategoryId);
public record UpdateTransactionRequest(decimal Amount, DateOnly Date, string Label, int CategoryId);
public record TransactionResponse(int Id, decimal Amount, DateOnly Date, string Label, int CategoryId);