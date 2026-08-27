// tests/InventoryTracker.Tests/Controllers/AuthControllerTests.cs
// Unit tests for AuthController login, registration, and user listing endpoints.
// Connects to: src/InventoryTracker.Api/Controllers/AuthController.cs
// Created: 2026-08-27

using InventoryTracker.Api.Controllers;
using InventoryTracker.Api.DTOs;
using InventoryTracker.Api.Models;
using InventoryTracker.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InventoryTracker.Tests.Controllers;

public class AuthControllerTests
{
    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithToken()
    {
        // Arrange
        var mockService = new Mock<IAuthService>();
        var loginDto = new LoginRequestDto { Username = "admin", Password = "AdminPass123!" };
        var responseDto = new LoginResponseDto
        {
            Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
            TokenType = "Bearer",
            ExpiresInSeconds = 86400,
            User = new UserDto { Id = 1, Username = "admin", Role = UserRole.Admin }
        };

        mockService.Setup(s => s.LoginAsync(loginDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        var controller = new AuthController(mockService.Object);

        // Act
        var result = await controller.Login(loginDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<ApiResponse<LoginResponseDto>>(okResult.Value);
        Assert.True(envelope.Success);
        Assert.Equal("admin", envelope.Data?.User.Username);
    }

    [Fact]
    public async Task Register_ValidDto_ReturnsCreatedAtAction()
    {
        // Arrange
        var mockService = new Mock<IAuthService>();
        var registerDto = new RegisterUserDto
        {
            Username = "newclerk",
            Email = "clerk@test.com",
            FullName = "New Clerk",
            Password = "Password123!",
            Role = UserRole.Clerk
        };
        var userDto = new UserDto
        {
            Id = 2,
            Username = "newclerk",
            Role = UserRole.Clerk
        };

        mockService.Setup(s => s.RegisterAsync(registerDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userDto);

        var controller = new AuthController(mockService.Object);

        // Act
        var result = await controller.Register(registerDto, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var envelope = Assert.IsType<ApiResponse<UserDto>>(createdResult.Value);
        Assert.True(envelope.Success);
        Assert.Equal("newclerk", envelope.Data?.Username);
    }
}
