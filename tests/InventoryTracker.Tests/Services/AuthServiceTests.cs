// tests/InventoryTracker.Tests/Services/AuthServiceTests.cs
// Unit tests for AuthService password cryptography, salt hashing, registration, and JWT token issuance.
// Connects to: src/InventoryTracker.Api/Services/AuthService.cs
// Created: 2026-08-27

using InventoryTracker.Api.Data;
using InventoryTracker.Api.DTOs;
using InventoryTracker.Api.Models;
using InventoryTracker.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InventoryTracker.Tests.Services;

public class AuthServiceTests
{
    private static InventoryDbContext CreateInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var context = new InventoryDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static IConfiguration CreateMockConfiguration()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Jwt:SecretKey", "InventoryTrackerTestSecretKey_2026_ForTesting_LongSecretKey!" },
            { "Jwt:Issuer", "InventoryTrackerApi" },
            { "Jwt:Audience", "InventoryTrackerClients" }
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    [Fact]
    public void HashPassword_GeneratesSaltAndVerifiesCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("TestDb_Auth_Hash");
        var config = CreateMockConfiguration();
        var loggerMock = new Mock<ILogger<AuthService>>();
        var service = new AuthService(context, config, loggerMock.Object);

        // Act
        var hash = service.HashPassword("SecurePassword123!", out var salt);
        var isValid = service.VerifyPassword("SecurePassword123!", hash, salt);
        var isInvalid = service.VerifyPassword("WrongPassword!", hash, salt);

        // Assert
        Assert.NotEmpty(hash);
        Assert.NotEmpty(salt);
        Assert.True(isValid);
        Assert.False(isInvalid);
    }

    [Fact]
    public async Task RegisterAsync_ValidData_CreatesUserWithRole()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("TestDb_Auth_Register");
        var config = CreateMockConfiguration();
        var loggerMock = new Mock<ILogger<AuthService>>();
        var service = new AuthService(context, config, loggerMock.Object);

        var dto = new RegisterUserDto
        {
            Username = "superadmin",
            Email = "superadmin@inventory.local",
            FullName = "Super Admin",
            Password = "Password123!",
            Role = UserRole.Admin
        };

        // Act
        var result = await service.RegisterAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("superadmin", result.Username);
        Assert.Equal(UserRole.Admin, result.Role);

        var saved = await context.Users.FirstOrDefaultAsync(u => u.Username == "superadmin");
        Assert.NotNull(saved);
        Assert.Equal(UserRole.Admin, saved.Role);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsJwtTokenAndUser()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("TestDb_Auth_Login");
        var config = CreateMockConfiguration();
        var loggerMock = new Mock<ILogger<AuthService>>();
        var service = new AuthService(context, config, loggerMock.Object);

        var hash = service.HashPassword("ManagerPass123!", out var salt);
        context.Users.Add(new User
        {
            Username = "testmanager",
            Email = "manager@test.com",
            FullName = "Test Manager",
            PasswordHash = hash,
            Salt = salt,
            Role = UserRole.WarehouseManager,
            IsActive = true
        });
        await context.SaveChangesAsync();

        var loginDto = new LoginRequestDto
        {
            Username = "testmanager",
            Password = "ManagerPass123!"
        };

        // Act
        var result = await service.LoginAsync(loginDto);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);
        Assert.Equal("Bearer", result.TokenType);
        Assert.Equal("testmanager", result.User.Username);
        Assert.Equal(UserRole.WarehouseManager, result.User.Role);
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("TestDb_Auth_InvalidLogin");
        var config = CreateMockConfiguration();
        var loggerMock = new Mock<ILogger<AuthService>>();
        var service = new AuthService(context, config, loggerMock.Object);

        var hash = service.HashPassword("CorrectPassword123!", out var salt);
        context.Users.Add(new User
        {
            Username = "user1",
            Email = "user1@test.com",
            PasswordHash = hash,
            Salt = salt,
            Role = UserRole.Clerk,
            IsActive = true
        });
        await context.SaveChangesAsync();

        var loginDto = new LoginRequestDto
        {
            Username = "user1",
            Password = "WrongPassword!"
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.LoginAsync(loginDto));
    }
}
