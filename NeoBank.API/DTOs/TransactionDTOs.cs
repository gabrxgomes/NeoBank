using System.ComponentModel.DataAnnotations;
using NeoBank.API.Models;

namespace NeoBank.API.DTOs;

// Request para depósito
public record DepositRequest(
    [Required] Guid AccountId,
    [Required][Range(0.01, double.MaxValue)] decimal Amount,
    string? Description
);

// Request para saque
public record WithdrawalRequest(
    [Required] Guid AccountId,
    [Required][Range(0.01, double.MaxValue)] decimal Amount,
    string? Description
);

// Request para transferência
public record TransferRequest(
    [Required] Guid FromAccountId,
    [Required] Guid ToAccountId,
    [Required][Range(0.01, double.MaxValue)] decimal Amount,
    string? Description
);

// Request para PIX
public record PixRequest(
    [Required] Guid FromAccountId,
    [Required] string PixKey,  // CPF, Email, Telefone ou Chave Aleatória
    [Required][Range(0.01, double.MaxValue)] decimal Amount,
    string? Description
);

// Response de transação
public record TransactionResponse(
    Guid Id,
    string Type,
    decimal Amount,
    string? Description,
    string Status,
    DateTime CreatedAt,
    DateTime? ProcessedAt,
    Guid? FromAccountId,
    Guid? ToAccountId
);

// Response de extrato
public record StatementResponse(
    Guid AccountId,
    string AccountNumber,
    decimal CurrentBalance,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    List<TransactionResponse> Transactions
);
