namespace Story.Model;

/// <summary>
/// Rezultatul validării unei poveşti – colecţie de erori şi avertismente.
/// </summary>
public class ValidationResult
{
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();

    public bool IsValid => Errors.Count == 0;

    public void AddError(string message) => Errors.Add(message);
    public void AddWarning(string message) => Warnings.Add(message);

    public override string ToString()
    {
        var lines = new List<string>();
        foreach (var e in Errors) lines.Add($"[EROARE] {e}");
        foreach (var w in Warnings) lines.Add($"[AVERTISMENT] {w}");
        return string.Join(Environment.NewLine, lines);
    }
}
