using Story.Model;

namespace Story.Editor.WinForms;

/// <summary>
/// Context partajat al sesiunii de editare.
/// Conţine povestea curentă şi metadate despre starea editorului.
/// </summary>
public class EditorContext
{
    public StoryDefinition Story { get; set; } = new();

    /// <summary>Directorul temporar de lucru (după extragere ZIP).</summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>Calea ZIP originală (pentru salvare).</summary>
    public string? OriginalZipPath { get; set; }

    public bool IsDirty { get; set; } = false;

    public void MarkDirty() => IsDirty = true;
    public void MarkClean() => IsDirty = false;

    /// <summary>Creează un proiect nou gol.</summary>
    public void NewStory(string title, string startBlock)
    {
        Story = new StoryDefinition
        {
            Title = title,
            StartBlock = startBlock,
            Version = "1.0"
        };
        WorkingDirectory = Path.Combine(Path.GetTempPath(), "StoryEditor_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(WorkingDirectory);
        Directory.CreateDirectory(Path.Combine(WorkingDirectory, "images"));
        OriginalZipPath = null;
        IsDirty = true;
    }

    /// <summary>Returnează calea absolută pentru o resursă relativă.</summary>
    public string? ResolveResource(string? relativePath)
    {
        if (string.IsNullOrEmpty(relativePath) || WorkingDirectory is null) return null;
        var full = Path.Combine(WorkingDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(full) ? full : null;
    }
}
