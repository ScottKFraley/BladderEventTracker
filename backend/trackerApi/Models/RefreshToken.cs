using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace trackerApi.Models;

public class RefreshToken
{
    /// <summary>
    /// The row Id.
    /// </summary>
    /// <remarks>
    /// The database is set up to auto-gen a new rowId.
    /// </remarks>
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(500)]
    public required string Token { get; set; }

    [Required]
    public DateTimeOffset ExpiresAt { get; set; }

    [Required]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Required]
    public bool IsRevoked { get; set; } = false;

    [MaxLength(200)]
    public string? DeviceInfo { get; set; }

    /// <summary>
    /// Navigation property
    /// </summary>
    public User? User { get; set; }
}