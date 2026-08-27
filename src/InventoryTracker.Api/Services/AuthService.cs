// src/InventoryTracker.Api/Services/AuthService.cs
// Implementation of PBKDF2 password hashing, salt generation, and JWT token issuance.
// Connects to: src/InventoryTracker.Api/Data/InventoryDbContext.cs, src/InventoryTracker.Api/Models/User.cs
// Created: 2026-08-27

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using InventoryTracker.Api.Data;
using InventoryTracker.Api.DTOs;
using InventoryTracker.Api.Models;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace InventoryTracker.Api.Services;

/// <summary>
/// Service providing password hashing, login authentication, and JWT bearer token issuance.
/// </summary>
public class AuthService : IAuthService
{
    private readonly InventoryDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(InventoryDbContext context, IConfiguration configuration, ILogger<AuthService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto dto, CancellationToken cancellationToken = default)
    {
        var normalizedUsername = dto.Username.Trim().ToLowerInvariant();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == normalizedUsername, cancellationToken);

        if (user == null || !user.IsActive)
        {
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        if (!VerifyPassword(dto.Password, user.PasswordHash, user.Salt))
        {
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        user.LastLoginAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        var token = GenerateJwtToken(user, out var expiresInSeconds);

        _logger.LogInformation("User logged in: {Username} (Role: {Role})", user.Username, user.Role);

        return new LoginResponseDto
        {
            Token = token,
            TokenType = "Bearer",
            ExpiresInSeconds = expiresInSeconds,
            User = MapToDto(user)
        };
    }

    public async Task<UserDto> RegisterAsync(RegisterUserDto dto, CancellationToken cancellationToken = default)
    {
        var normalizedUsername = dto.Username.Trim().ToLowerInvariant();
        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();

        var usernameExists = await _context.Users.AnyAsync(u => u.Username.ToLower() == normalizedUsername, cancellationToken);
        if (usernameExists)
        {
            throw new InvalidOperationException($"Username '{dto.Username}' is already taken.");
        }

        var emailExists = await _context.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);
        if (emailExists)
        {
            throw new InvalidOperationException($"Email '{dto.Email}' is already registered.");
        }

        var hash = HashPassword(dto.Password, out var salt);

        var user = new User
        {
            Username = dto.Username.Trim(),
            Email = normalizedEmail,
            FullName = dto.FullName.Trim(),
            PasswordHash = hash,
            Salt = salt,
            Role = dto.Role,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _context.Users.AddAsync(user, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User registered: {Username} (Role: {Role})", user.Username, user.Role);
        return MapToDto(user);
    }

    public async Task<UserDto?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        return user == null ? null : MapToDto(user);
    }

    public async Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _context.Users
            .AsNoTracking()
            .OrderBy(u => u.Username)
            .ToListAsync(cancellationToken);

        return users.Select(MapToDto).ToList();
    }

    public string HashPassword(string password, out string salt)
    {
        var saltBytes = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(saltBytes);
        salt = Convert.ToBase64String(saltBytes);

        return ComputeHash(password, saltBytes);
    }

    public bool VerifyPassword(string password, string storedHash, string storedSalt)
    {
        var saltBytes = Convert.FromBase64String(storedSalt);
        var computedHash = ComputeHash(password, saltBytes);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedHash),
            Encoding.UTF8.GetBytes(storedHash));
    }

    private static string ComputeHash(string password, byte[] saltBytes)
    {
        var hashBytes = KeyDerivation.Pbkdf2(
            password: password,
            salt: saltBytes,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100000,
            numBytesRequested: 32);

        return Convert.ToBase64String(hashBytes);
    }

    private string GenerateJwtToken(User user, out int expiresInSeconds)
    {
        var secretKey = _configuration["Jwt:SecretKey"] ?? "InventoryTrackerApiSecretKey_Production_SuperSecret_2026_Key!";
        var issuer = _configuration["Jwt:Issuer"] ?? "InventoryTrackerApi";
        var audience = _configuration["Jwt:Audience"] ?? "InventoryTrackerClients";
        expiresInSeconds = 86400; // 24 Hours

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.GivenName, user.FullName),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddSeconds(expiresInSeconds),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private static UserDto MapToDto(User u) => new()
    {
        Id = u.Id,
        Username = u.Username,
        Email = u.Email,
        FullName = u.FullName,
        Role = u.Role,
        IsActive = u.IsActive,
        CreatedAtUtc = u.CreatedAtUtc,
        LastLoginAtUtc = u.LastLoginAtUtc
    };
}
