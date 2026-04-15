namespace DrivingSchoolApp.Models;

public sealed class LessonSignature
{
    public string SignedByName { get; set; } = string.Empty;
    public DateTimeOffset SignedAt { get; set; }
    public List<SignatureStrokeData> Strokes { get; set; } = new();
}

public sealed class SignatureStrokeData
{
    public float LineWidth { get; set; } = 4f;
    public List<SignaturePointData> Points { get; set; } = new();
}

public sealed class SignaturePointData
{
    public float X { get; set; }
    public float Y { get; set; }
}
