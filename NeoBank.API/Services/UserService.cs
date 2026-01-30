using Microsoft.EntityFrameworkCore;
using NeoBank.API.Data;
using NeoBank.API.DTOs;
using NeoBank.API.Models;

namespace NeoBank.API.Services;

public interface IUserService
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByCPFAsync(string cpf);
    Task<User> CreateAsync(CreateUserRequest request);
    Task<User?> AuthenticateAsync(string email, string password);
    Task<User?> UpdateAsync(Guid id, UpdateUserRequest request);
    Task<bool> DeactivateAsync(Guid id);
}

public class UserService : IUserService
{
    private readonly NeoBankDbContext _context;

    public UserService(NeoBankDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users
            .Include(u => u.Accounts)
            .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
    }

    public async Task<User?> GetByCPFAsync(string cpf)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.CPF == cpf && u.IsActive);
    }

    public async Task<User> CreateAsync(CreateUserRequest request)
    {
        // Verificar se email já existe
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            throw new InvalidOperationException("Email já cadastrado.");

        // Verificar se CPF já existe
        if (await _context.Users.AnyAsync(u => u.CPF == request.CPF))
            throw new InvalidOperationException("CPF já cadastrado.");

        var user = new User
        {
            FullName = request.FullName,
            CPF = request.CPF.Replace(".", "").Replace("-", ""),
            Email = request.Email.ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Phone = request.Phone
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }

    public async Task<User?> AuthenticateAsync(string email, string password)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email.ToLower() && u.IsActive);

        if (user == null)
            return null;

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return null;

        return user;
    }

    public async Task<User?> UpdateAsync(Guid id, UpdateUserRequest request)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null || !user.IsActive)
            return null;

        if (!string.IsNullOrWhiteSpace(request.FullName))
            user.FullName = request.FullName;

        if (!string.IsNullOrWhiteSpace(request.Phone))
            user.Phone = request.Phone;

        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return user;
    }

    public async Task<bool> DeactivateAsync(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return false;

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }
}
