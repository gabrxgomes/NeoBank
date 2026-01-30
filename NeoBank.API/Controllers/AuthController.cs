using Microsoft.AspNetCore.Mvc;
using NeoBank.API.DTOs;
using NeoBank.API.Services;

namespace NeoBank.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ITokenService _tokenService;

    public AuthController(IUserService userService, ITokenService tokenService)
    {
        _userService = userService;
        _tokenService = tokenService;
    }

    /// <summary>
    /// Registra um novo usuário
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] CreateUserRequest request)
    {
        try
        {
            var user = await _userService.CreateAsync(request);
            var token = _tokenService.GenerateToken(user);

            var response = new AuthResponse(
                user.Id,
                user.FullName,
                user.Email,
                token,
                DateTime.UtcNow.AddHours(8)
            );

            return CreatedAtAction(nameof(Register), response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Realiza login do usuário
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userService.AuthenticateAsync(request.Email, request.Password);

        if (user == null)
            return Unauthorized(new { error = "Email ou senha inválidos." });

        var token = _tokenService.GenerateToken(user);

        var response = new AuthResponse(
            user.Id,
            user.FullName,
            user.Email,
            token,
            DateTime.UtcNow.AddHours(8)
        );

        return Ok(response);
    }
}
