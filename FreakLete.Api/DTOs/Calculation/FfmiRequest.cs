using System.ComponentModel.DataAnnotations;

namespace FreakLete.Api.DTOs.Calculation;

public class FfmiRequest
{
	[Range(0.1, 500)]
	public double WeightKg { get; set; }

	[Range(1, 300)]
	public double HeightCm { get; set; }

	[Range(0.1, 99.9)]
	public double BodyFatPercentage { get; set; }
}
