using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace trackerApi.Models;

public class TrackingLogItem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public DateTimeOffset EventDate { get; set; } = DateTime.Now;

    public bool Accident { get; set; } = false;

    public bool ChangePadOrUnderware { get; set; } = false;

    public int LeakAmount { get; set; } = 1;

    public int Urgency { get; set; } = 1;

    public bool AwokeFromSleep { get; set; } = false;

    public int PainLevel { get; set; } = 1;

    public string? Notes { get; set; }

    /// <summary>
    /// Navigation property
    /// </summary>
    public User? User { get; set; }

}
