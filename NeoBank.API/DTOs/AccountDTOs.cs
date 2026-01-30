using System.ComponentModel.DataAnnotations;
using NeoBank.API.Models;

namespace NeoBank.API.DTOs;

// Request para criar conta
public record CreateAccountRequest(
    [Required] AccountType Type,
    decimal InitialDeposit = 0
);

// Response de conta
public record AccountResponse(
    Guid Id,
    string AccountNumber,
    string Agency,
    string Type,
    decimal Balance,
    decimal CreditLimit,
    bool IsActive,
    DateTime CreatedAt
);

// Response resumido de conta (para listagens)
public record AccountSummaryResponse(
    Guid Id,
    string AccountNumber,
    string Agency,
    string Type,
    decimal Balance
);
