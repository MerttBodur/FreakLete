using System.ComponentModel.DataAnnotations;

namespace FreakLete.Api.DTOs.Calculation;

public class RsiRequest
{
    [Range(0.1, 200)]
    public double JumpHeightCm { get; set; }

    [Range(0.01, 5)]
    public double GroundContactTimeSeconds { get; set; }
}
