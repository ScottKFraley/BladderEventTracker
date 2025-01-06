using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace trackerApi.Models;

public class User
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid(); // Generate Guid in .NET if not provided

    [Required]
    [MaxLength(50)]
    public required string Username { get; set; }

    [Required]
    public required string PasswordHash { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
