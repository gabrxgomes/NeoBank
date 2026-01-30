using Microsoft.EntityFrameworkCore;
using NeoBank.API.Data;
using NeoBank.API.DTOs;
using NeoBank.API.Models;

namespace NeoBank.API.Services;

public interface IAccountService
{
    Task<Account?> GetByIdAsync(Guid id);
    Task<Account?> GetByAccountNumberAsync(string accountNumber);
    Task<List<Account>> GetByUserIdAsync(Guid userId);
    Task<Account> CreateAsync(Guid userId, CreateAccountRequest request);
    Task<bool> UpdateBalanceAsync(Guid accountId, decimal amount);
    Task<bool> DeactivateAsync(Guid accountId);
}

public class AccountService : IAccountService
{
    private readonly NeoBankDbContext _context;

    public AccountService(NeoBankDbContext context)
    {
        _context = context;
    }

    public async Task<Account?> GetByIdAsync(Guid id)
    {
        return await _context.Accounts
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == id && a.IsActive);
    }

    public async Task<Account?> GetByAccountNumberAsync(string accountNumber)
    {
        return await _context.Accounts
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber && a.IsActive);
    }

    public async Task<List<Account>> GetByUserIdAsync(Guid userId)
    {
        return await _context.Accounts
            .Where(a => a.UserId == userId && a.IsActive)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<Account> CreateAsync(Guid userId, CreateAccountRequest request)
    {
        // Gerar número de conta único
        var accountNumber = await GenerateAccountNumberAsync();

        var account = new Account
        {
            UserId = userId,
            AccountNumber = accountNumber,
            Type = request.Type,
            Balance = request.InitialDeposit,
            CreditLimit = request.Type == AccountType.Checking ? 500 : 0
        };

        _context.Accounts.Add(account);

        // Se houver depósito inicial, criar transação
        if (request.InitialDeposit > 0)
        {
            var transaction = new Transaction
            {
                Type = TransactionType.Deposit,
                Amount = request.InitialDeposit,
                Description = "Depósito inicial",
                Status = TransactionStatus.Completed,
                ToAccountId = account.Id,
                ProcessedAt = DateTime.UtcNow
            };
            _context.Transactions.Add(transaction);
        }

        await _context.SaveChangesAsync();
        return account;
    }

    public async Task<bool> UpdateBalanceAsync(Guid accountId, decimal amount)
    {
        var account = await _context.Accounts.FindAsync(accountId);
        if (account == null || !account.IsActive)
            return false;

        account.Balance += amount;
        account.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeactivateAsync(Guid accountId)
    {
        var account = await _context.Accounts.FindAsync(accountId);
        if (account == null)
            return false;

        if (account.Balance != 0)
            throw new InvalidOperationException("Não é possível encerrar conta com saldo.");

        account.IsActive = false;
        account.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    private async Task<string> GenerateAccountNumberAsync()
    {
        string accountNumber;
        do
        {
            accountNumber = new Random().Next(10000000, 99999999).ToString();
        } while (await _context.Accounts.AnyAsync(a => a.AccountNumber == accountNumber));

        return accountNumber;
    }
}
