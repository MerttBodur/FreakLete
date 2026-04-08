using FreakLete.Api.DTOs.Calculation;
using FreakLete.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreakLete.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CalculationsController : ControllerBase
{
    [HttpPost("one-rm")]
    public ActionResult<OneRmResponse> CalculateOneRm(OneRmRequest request)
    {
        var oneRm = CalculationService.CalculateOneRm(request.WeightKg, request.Reps, request.RIR);
        var rmTable = CalculationService.BuildRmTable(oneRm);

        return Ok(new OneRmResponse
        {
            OneRm = Math.Round(oneRm, 1),
            RmTable = rmTable.Select((weight, index) => new RmTableEntry
            {
                Rm = index + 1,
                Weight = Math.Round(weight, 1)
            }).ToList()
        });
    }

    [HttpPost("rsi")]
    public ActionResult<double> CalculateRsi(RsiRequest request)
    {
        var rsi = CalculationService.CalculateRsi(request.JumpHeightCm, request.GroundContactTimeSeconds);
        return Ok(Math.Round(rsi, 3));
    }

    [HttpPost("ffmi")]
    public ActionResult<FfmiResponse> CalculateFfmi(FfmiRequest request)
    {
        var (lbm, raw, normalized) = CalculationService.CalculateFfmi(
            request.WeightKg, request.HeightCm, request.BodyFatPercentage);

        return Ok(new FfmiResponse
        {
            NormalizedFfmi = Math.Round(normalized, 2),
            RawFfmi = Math.Round(raw, 2),
            LeanBodyMassKg = Math.Round(lbm, 2)
        });
    }
}
