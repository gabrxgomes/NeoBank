using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoBank.API.DTOs;
using NeoBank.API.Services;

namespace NeoBank.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountsController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountsController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    /// <summary>
    /// Lista todas as contas do usuário autenticado
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<AccountResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAccounts()
    {
        var userId = GetCurrentUserId();
        var accounts = await _accountService.GetByUserIdAsync(userId);

        var response = accounts.Select(a => new AccountResponse(
            a.Id,
            a.AccountNumber,
            a.Agency,
            a.Type.ToString(),
            a.Balance,
            a.CreditLimit,
            a.IsActive,
            a.CreatedAt
        )).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Obtém detalhes de uma conta específica
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAccount(Guid id)
    {
        var account = await _accountService.GetByIdAsync(id);

        if (account == null)
            return NotFound(new { error = "Conta não encontrada." });

        if (account.UserId != GetCurrentUserId())
            return Forbid();

        var response = new AccountResponse(
            account.Id,
            account.AccountNumber,
            account.Agency,
            account.Type.ToString(),
            account.Balance,
            account.CreditLimit,
            account.IsActive,
            account.CreatedAt
        );

        return Ok(response);
    }

    /// <summary>
    /// Cria uma nova conta para o usuário autenticado
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var account = await _accountService.CreateAsync(userId, request);

            var response = new AccountResponse(
                account.Id,
                account.AccountNumber,
                account.Agency,
                account.Type.ToString(),
                account.Balance,
                account.CreditLimit,
                account.IsActive,
                account.CreatedAt
            );

            return CreatedAtAction(nameof(GetAccount), new { id = account.Id }, response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Encerra uma conta (saldo deve ser zero)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CloseAccount(Guid id)
    {
        var account = await _accountService.GetByIdAsync(id);

        if (account == null)
            return NotFound(new { error = "Conta não encontrada." });

        if (account.UserId != GetCurrentUserId())
            return Forbid();

        try
        {
            await _accountService.DeactivateAsync(id);
            return NoContent();
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
