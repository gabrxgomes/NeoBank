using System.ComponentModel.DataAnnotations;

namespace NeoBank.API.DTOs;

// Request para criar usuário
public record CreateUserRequest(
    [Required][MaxLength(100)] string FullName,
    [Required][MaxLength(11)] string CPF,
    [Required][EmailAddress] string Email,
    [Required][MinLength(6)] string Password,
    string? Phone
);

// Request para login
public record LoginRequest(
    [Required][EmailAddress] string Email,
    [Required] string Password
);

// Response de autenticação
public record AuthResponse(
    Guid UserId,
    string FullName,
    string Email,
    string Token,
    DateTime ExpiresAt
);

// Response de usuário
public record UserResponse(
    Guid Id,
    string FullName,
    string CPF,
    string Email,
    string? Phone,
    DateTime CreatedAt,
    bool IsActive
);

// Request para atualizar usuário
public record UpdateUserRequest(
    string? FullName,
    string? Phone
);
