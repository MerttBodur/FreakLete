using System.ComponentModel.DataAnnotations;

namespace FreakLete.Api.DTOs.Calculation;

public class OneRmRequest
{
    [Range(1, 500)]
    public int WeightKg { get; set; }

    [Range(1, 30)]
    public int Reps { get; set; }

    [Range(0, 10)]
    public int RIR { get; set; }
}
