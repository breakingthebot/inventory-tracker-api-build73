// src/InventoryTracker.Api/Services/IAuthService.cs
// Defines service contracts for authentication, PBKDF2 password security, and JWT token issuance.
// Connects to: src/InventoryTracker.Api/Services/AuthService.cs, src/InventoryTracker.Api/Controllers/AuthController.cs
// Created: 2026-08-27

using InventoryTracker.Api.DTOs;

namespace InventoryTracker.Api.Services;

/// <summary>
/// Service contract for user authentication, password cryptography, and JWT token generation.
/// </summary>
public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto dto, CancellationToken cancellationToken = default);
    Task<UserDto> RegisterAsync(RegisterUserDto dto, CancellationToken cancellationToken = default);
    Task<UserDto?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default);
    string HashPassword(string password, out string salt);
    bool VerifyPassword(string password, string storedHash, string storedSalt);
}
