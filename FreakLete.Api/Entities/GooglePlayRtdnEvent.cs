namespace FreakLete.Api.Entities;

public class GooglePlayRtdnEvent
{
    public int Id { get; set; }
    public string MessageId { get; set; } = string.Empty;
    public string PurchaseTokenFingerprint { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public int NotificationType { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public string ProcessingState { get; set; } = string.Empty;
}
