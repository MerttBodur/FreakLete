namespace FreakLete.Api.DTOs.Calculation;

public class OneRmResponse
{
    public double OneRm { get; set; }
    public List<RmTableEntry> RmTable { get; set; } = [];
}

public class RmTableEntry
{
    public int Rm { get; set; }
    public double Weight { get; set; }
}
