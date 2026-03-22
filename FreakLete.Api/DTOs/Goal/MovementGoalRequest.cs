using System.ComponentModel.DataAnnotations;

namespace FreakLete.Api.DTOs.Goal;

public class MovementGoalRequest
{
    [Required, MaxLength(200)]
    public string MovementName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string MovementCategory { get; set; } = string.Empty;

    [MaxLength(100)]
    public string GoalMetricLabel { get; set; } = string.Empty;

    public double TargetValue { get; set; }

    [MaxLength(50)]
    public string Unit { get; set; } = string.Empty;
}
