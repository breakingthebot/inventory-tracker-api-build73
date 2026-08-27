// src/InventoryTracker.Api/Models/User.cs
// Domain entity representing an authenticated user operator with PBKDF2 password hashes and RBAC roles.
// Connects to: src/InventoryTracker.Api/Models/UserRole.cs, src/InventoryTracker.Api/Data/InventoryDbContext.cs
// Created: 2026-08-27

namespace InventoryTracker.Api.Models;

/// <summary>
/// Domain entity representing an authenticated user account.
/// </summary>
public class User
{
    /// <summary>
    /// Unique database primary key identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Unique login username.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// User email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Full display name of the operator.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Cryptographic PBKDF2 password hash.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Per-user cryptographic salt for hash hardening.
    /// </summary>
    public string Salt { get; set; } = string.Empty;

    /// <summary>
    /// Assigned RBAC security role.
    /// </summary>
    public UserRole Role { get; set; } = UserRole.Clerk;

    /// <summary>
    /// Indicates whether the user account is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Account creation timestamp.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp of most recent successful login.
    /// </summary>
    public DateTime? LastLoginAtUtc { get; set; }
}
