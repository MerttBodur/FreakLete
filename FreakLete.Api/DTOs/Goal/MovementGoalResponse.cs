namespace FreakLete.Api.DTOs.Goal;

public class MovementGoalResponse
{
    public int Id { get; set; }
    public string MovementName { get; set; } = string.Empty;
    public string MovementCategory { get; set; } = string.Empty;
    public string GoalMetricLabel { get; set; } = string.Empty;
    public double TargetValue { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
