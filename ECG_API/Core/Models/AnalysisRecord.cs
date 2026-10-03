namespace ECG_API.Core.Models;

public class AnalysisRecord
{
    public int Id { get; private set; }
    public DateTime CreatedAtUtc { get; set; }
    public required string OriginalFileName { get; set; }
    public required string PredictedDiagnosis { get; set; }
    public float Confidence { get; set; }
    public string? ResultImageFileName { get; set; }
}
