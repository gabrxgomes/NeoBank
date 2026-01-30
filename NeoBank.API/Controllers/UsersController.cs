using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoBank.API.DTOs;
using NeoBank.API.Services;

namespace NeoBank.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Obtém o perfil do usuário autenticado
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetCurrentUserId();
        var user = await _userService.GetByIdAsync(userId);

        if (user == null)
            return NotFound(new { error = "Usuário não encontrado." });

        var response = new UserResponse(
            user.Id,
            user.FullName,
            user.CPF,
            user.Email,
            user.Phone,
            user.CreatedAt,
            user.IsActive
        );

        return Ok(response);
    }

    /// <summary>
    /// Atualiza os dados do usuário autenticado
    /// </summary>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserRequest request)
    {
        var userId = GetCurrentUserId();
        var user = await _userService.UpdateAsync(userId, request);

        if (user == null)
            return NotFound(new { error = "Usuário não encontrado." });

        var response = new UserResponse(
            user.Id,
            user.FullName,
            user.CPF,
            user.Email,
            user.Phone,
            user.CreatedAt,
            user.IsActive
        );

        return Ok(response);
    }

    /// <summary>
    /// Desativa a conta do usuário autenticado
    /// </summary>
    [HttpDelete("me")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateAccount()
    {
        var userId = GetCurrentUserId();
        var result = await _userService.DeactivateAsync(userId);

        if (!result)
            return NotFound(new { error = "Usuário não encontrado." });

        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdClaim!);
    }
}
