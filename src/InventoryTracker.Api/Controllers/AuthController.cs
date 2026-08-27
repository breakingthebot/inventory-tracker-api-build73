// src/InventoryTracker.Api/Controllers/AuthController.cs
// REST controller for JWT authentication, user registration, profile queries, and role verification.
// Connects to: src/InventoryTracker.Api/Services/IAuthService.cs, src/InventoryTracker.Api/DTOs/AuthDtos.cs
// Created: 2026-08-27

using System.Security.Claims;
using InventoryTracker.Api.DTOs;
using InventoryTracker.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryTracker.Api.Controllers;

/// <summary>
/// Manages user authentication, JWT token issuance, operator registration, and profile lookups.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Authenticates user credentials and returns a signed JWT Bearer access token.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto, CancellationToken cancellationToken)
    {
        var response = await _authService.LoginAsync(dto, cancellationToken);
        return Ok(ApiResponse<LoginResponseDto>.Ok(response, "Authentication successful."));
    }

    /// <summary>
    /// Registers a new operator account with role assignment.
    /// </summary>
    [HttpPost("register")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto dto, CancellationToken cancellationToken)
    {
        var created = await _authService.RegisterAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetProfile), ApiResponse<UserDto>.Ok(created, "User account created successfully."));
    }

    /// <summary>
    /// Retrieves the profile details of the currently authenticated user from token claims.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<object>.Fail("Invalid token claims."));
        }

        var profile = await _authService.GetUserByIdAsync(userId, cancellationToken);
        if (profile == null)
        {
            return NotFound(ApiResponse<object>.Fail("User profile not found."));
        }

        return Ok(ApiResponse<UserDto>.Ok(profile));
    }

    /// <summary>
    /// Retrieves a list of all operator user accounts (Admin only).
    /// </summary>
    [HttpGet("users")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<UserDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        var users = await _authService.GetUsersAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<UserDto>>.Ok(users, $"Retrieved {users.Count} user accounts."));
    }
}
