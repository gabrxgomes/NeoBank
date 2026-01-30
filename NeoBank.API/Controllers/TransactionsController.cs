using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoBank.API.DTOs;
using NeoBank.API.Services;

namespace NeoBank.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly IAccountService _accountService;

    public TransactionsController(ITransactionService transactionService, IAccountService accountService)
    {
        _transactionService = transactionService;
        _accountService = accountService;
    }

    /// <summary>
    /// Realiza um depósito em conta
    /// </summary>
    [HttpPost("deposit")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Deposit([FromBody] DepositRequest request)
    {
        try
        {
            var transaction = await _transactionService.DepositAsync(request);

            var response = new TransactionResponse(
                transaction.Id,
                transaction.Type.ToString(),
                transaction.Amount,
                transaction.Description,
                transaction.Status.ToString(),
                transaction.CreatedAt,
                transaction.ProcessedAt,
                transaction.FromAccountId,
                transaction.ToAccountId
            );

            return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Realiza um saque de conta
    /// </summary>
    [HttpPost("withdraw")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Withdraw([FromBody] WithdrawalRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var transaction = await _transactionService.WithdrawAsync(request, userId);

            var response = new TransactionResponse(
                transaction.Id,
                transaction.Type.ToString(),
                transaction.Amount,
                transaction.Description,
                transaction.Status.ToString(),
                transaction.CreatedAt,
                transaction.ProcessedAt,
                transaction.FromAccountId,
                transaction.ToAccountId
            );

            return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, response);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Realiza uma transferência entre contas
    /// </summary>
    [HttpPost("transfer")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Transfer([FromBody] TransferRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var transaction = await _transactionService.TransferAsync(request, userId);

            var response = new TransactionResponse(
                transaction.Id,
                transaction.Type.ToString(),
                transaction.Amount,
                transaction.Description,
                transaction.Status.ToString(),
                transaction.CreatedAt,
                transaction.ProcessedAt,
                transaction.FromAccountId,
                transaction.ToAccountId
            );

            return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, response);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtém detalhes de uma transação
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransaction(Guid id)
    {
        var transaction = await _transactionService.GetByIdAsync(id);

        if (transaction == null)
            return NotFound(new { error = "Transação não encontrada." });

        // Verificar se o usuário tem acesso à transação
        var userId = GetCurrentUserId();
        var userAccounts = await _accountService.GetByUserIdAsync(userId);
        var userAccountIds = userAccounts.Select(a => a.Id).ToList();

        if (!userAccountIds.Contains(transaction.FromAccountId ?? Guid.Empty) &&
            !userAccountIds.Contains(transaction.ToAccountId ?? Guid.Empty))
        {
            return Forbid();
        }

        var response = new TransactionResponse(
            transaction.Id,
            transaction.Type.ToString(),
            transaction.Amount,
            transaction.Description,
            transaction.Status.ToString(),
            transaction.CreatedAt,
            transaction.ProcessedAt,
            transaction.FromAccountId,
            transaction.ToAccountId
        );

        return Ok(response);
    }

    /// <summary>
    /// Obtém o extrato de uma conta
    /// </summary>
    [HttpGet("statement/{accountId:guid}")]
    [ProducesResponseType(typeof(StatementResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStatement(
        Guid accountId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var account = await _accountService.GetByIdAsync(accountId);
            if (account == null)
                return NotFound(new { error = "Conta não encontrada." });

            if (account.UserId != GetCurrentUserId())
                return Forbid();

            var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
            var end = endDate ?? DateTime.UtcNow;

            var statement = await _transactionService.GetStatementAsync(accountId, start, end);
            return Ok(statement);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdClaim!);
    }
}
