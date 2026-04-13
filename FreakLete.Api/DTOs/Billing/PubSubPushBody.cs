namespace FreakLete.Api.DTOs.Billing;

public class PubSubPushBody
{
    public PubSubMessage? Message { get; set; }
    public string? Subscription { get; set; }
}

public class PubSubMessage
{
    public string? Data { get; set; }
    public string? MessageId { get; set; }
    public string? PublishTime { get; set; }
}
