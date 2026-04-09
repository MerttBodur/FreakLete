using System.ComponentModel.DataAnnotations;

namespace FreakLete.Api.DTOs.Billing;

public class GooglePlayPurchaseSyncRequest
{
    [Required, MaxLength(100)]
    public string ProductId { get; set; } = "";

    [MaxLength(50)]
    public string? BasePlanId { get; set; }

    [Required, MaxLength(500)]
    public string PurchaseToken { get; set; } = "";

    [MaxLength(200)]
    public string? OrderId { get; set; }

    public int PurchaseState { get; set; }

    public bool IsAcknowledged { get; set; }

    public string? RawPayloadJson { get; set; }
}
