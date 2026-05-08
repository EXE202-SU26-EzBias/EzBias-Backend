namespace EzBias.Application.Features.Payments;

public interface ISePayClient
{
    Task<(bool Success, string? Error, IReadOnlyList<SePayTransaction> Transactions, int? RetryAfterSeconds)> GetTransactionsAsync(string accountNumber, int limit, CancellationToken ct);
}

public record SePayTransaction(string Id, decimal AmountIn, string TransactionContent, string? ReferenceNumber, string? AccountNumber, DateTimeOffset? TransactionDate);
