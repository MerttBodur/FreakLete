namespace FreakLete.Api.DTOs.Billing;

public class BillingStatusResponse
{
    public string Plan { get; set; } = "free";
    public bool IsPremiumActive { get; set; }
    public DateTime? SubscriptionEndsAtUtc { get; set; }
    public int GeneralChatRemainingToday { get; set; }
    public int ProgramGenerateRemainingThisMonth { get; set; }
    public int ProgramAnalyzeRemainingThisMonth { get; set; }
    public DateTime? NutritionGuidanceNextAvailableAtUtc { get; set; }
}
