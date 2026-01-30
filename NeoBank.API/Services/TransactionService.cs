using Microsoft.EntityFrameworkCore;
using NeoBank.API.Data;
using NeoBank.API.DTOs;
using NeoBank.API.Models;

namespace NeoBank.API.Services;

public interface ITransactionService
{
    Task<Transaction?> GetByIdAsync(Guid id);
    Task<List<Transaction>> GetByAccountIdAsync(Guid accountId, DateTime? startDate = null, DateTime? endDate = null);
    Task<Transaction> DepositAsync(DepositRequest request);
    Task<Transaction> WithdrawAsync(WithdrawalRequest request, Guid userId);
    Task<Transaction> TransferAsync(TransferRequest request, Guid userId);
    Task<StatementResponse> GetStatementAsync(Guid accountId, DateTime startDate, DateTime endDate);
}

public class TransactionService : ITransactionService
{
    private readonly NeoBankDbContext _context;
    private readonly IAccountService _accountService;

    public TransactionService(NeoBankDbContext context, IAccountService accountService)
    {
        _context = context;
        _accountService = accountService;
    }

    public async Task<Transaction?> GetByIdAsync(Guid id)
    {
        return await _context.Transactions
            .Include(t => t.FromAccount)
            .Include(t => t.ToAccount)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<List<Transaction>> GetByAccountIdAsync(Guid accountId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.Transactions
            .Where(t => t.FromAccountId == accountId || t.ToAccountId == accountId);

        if (startDate.HasValue)
            query = query.Where(t => t.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(t => t.CreatedAt <= endDate.Value);

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<Transaction> DepositAsync(DepositRequest request)
    {
        var account = await _accountService.GetByIdAsync(request.AccountId);
        if (account == null)
            throw new InvalidOperationException("Conta não encontrada.");

        using var dbTransaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var transaction = new Transaction
            {
                Type = TransactionType.Deposit,
                Amount = request.Amount,
                Description = request.Description ?? "Depósito em conta",
                Status = TransactionStatus.Completed,
                ToAccountId = request.AccountId,
                ProcessedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);
            await _accountService.UpdateBalanceAsync(request.AccountId, request.Amount);

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            return transaction;
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }

    public async Task<Transaction> WithdrawAsync(WithdrawalRequest request, Guid userId)
    {
        var account = await _accountService.GetByIdAsync(request.AccountId);
        if (account == null)
            throw new InvalidOperationException("Conta não encontrada.");

        if (account.UserId != userId)
            throw new UnauthorizedAccessException("Você não tem permissão para sacar desta conta.");

        var availableBalance = account.Balance + account.CreditLimit;
        if (request.Amount > availableBalance)
            throw new InvalidOperationException("Saldo insuficiente.");

        using var dbTransaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var transaction = new Transaction
            {
                Type = TransactionType.Withdrawal,
                Amount = request.Amount,
                Description = request.Description ?? "Saque em conta",
                Status = TransactionStatus.Completed,
                FromAccountId = request.AccountId,
                ProcessedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);
            await _accountService.UpdateBalanceAsync(request.AccountId, -request.Amount);

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            return transaction;
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }

    public async Task<Transaction> TransferAsync(TransferRequest request, Guid userId)
    {
        if (request.FromAccountId == request.ToAccountId)
            throw new InvalidOperationException("Conta de origem e destino não podem ser iguais.");

        var fromAccount = await _accountService.GetByIdAsync(request.FromAccountId);
        if (fromAccount == null)
            throw new InvalidOperationException("Conta de origem não encontrada.");

        if (fromAccount.UserId != userId)
            throw new UnauthorizedAccessException("Você não tem permissão para transferir desta conta.");

        var toAccount = await _accountService.GetByIdAsync(request.ToAccountId);
        if (toAccount == null)
            throw new InvalidOperationException("Conta de destino não encontrada.");

        var availableBalance = fromAccount.Balance + fromAccount.CreditLimit;
        if (request.Amount > availableBalance)
            throw new InvalidOperationException("Saldo insuficiente.");

        using var dbTransaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var transaction = new Transaction
            {
                Type = TransactionType.Transfer,
                Amount = request.Amount,
                Description = request.Description ?? $"Transferência para conta {toAccount.AccountNumber}",
                Status = TransactionStatus.Completed,
                FromAccountId = request.FromAccountId,
                ToAccountId = request.ToAccountId,
                ProcessedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);

            // Debitar da conta de origem
            await _accountService.UpdateBalanceAsync(request.FromAccountId, -request.Amount);

            // Creditar na conta de destino
            await _accountService.UpdateBalanceAsync(request.ToAccountId, request.Amount);

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            return transaction;
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }

    public async Task<StatementResponse> GetStatementAsync(Guid accountId, DateTime startDate, DateTime endDate)
    {
        var account = await _accountService.GetByIdAsync(accountId);
        if (account == null)
            throw new InvalidOperationException("Conta não encontrada.");

        var transactions = await GetByAccountIdAsync(accountId, startDate, endDate);

        var transactionResponses = transactions.Select(t => new TransactionResponse(
            t.Id,
            t.Type.ToString(),
            t.Amount,
            t.Description,
            t.Status.ToString(),
            t.CreatedAt,
            t.ProcessedAt,
            t.FromAccountId,
            t.ToAccountId
        )).ToList();

        return new StatementResponse(
            accountId,
            account.AccountNumber,
            account.Balance,
            startDate,
            endDate,
            transactionResponses
        );
    }
}
