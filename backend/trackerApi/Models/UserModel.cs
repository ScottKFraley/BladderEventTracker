using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace trackerApi.Models;

public class User
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
    [MaxLength(50)]
    public required string Username { get; set; }

    [Required]
    public required string PasswordHash { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// For navigation.
    /// </summary>
    public ICollection<TrackingLogItem>? TrackingLogs { get; set; }

}

/// <summary>
/// A login DTO. May want, if not need to change this to a User instance.
/// </summary>
public record LoginDto(string Username, string Password);
