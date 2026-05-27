using System.IO.Compression;
using System.Text.Json;
using Story.Model;

namespace Story.Persistence;

/// <summary>
/// Gestionează încărcarea și salvarea poveștilor ca arhive ZIP.
/// Fișierul principal JSON se numește story.json.
/// Imaginile sunt în directorul images/ din arhivă.
/// </summary>
public class StoryRepository
{
    private const string StoryJsonEntry = "story.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Încarcă o poveste dintr-un fișier ZIP.
    /// </summary>
    public StoryDefinition Load(string zipPath)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        var entry = archive.GetEntry(StoryJsonEntry)
            ?? throw new FileNotFoundException(
                $"'{StoryJsonEntry}' nu a fost găsit în arhivă.");

        using var stream = entry.Open();
        return JsonSerializer.Deserialize<StoryDefinition>(stream, JsonOptions)
            ?? throw new InvalidDataException("JSON-ul poveștii este invalid.");
    }

    /// <summary>
    /// Salvează o poveste ca arhivă ZIP.
    /// imageSourcePaths: cheie = cale relativă în arhivă, valoare = cale absolută pe disc.
    /// </summary>
    public void Save(StoryDefinition story, string zipPath,
        Dictionary<string, string>? imageSourcePaths = null)
    {
        if (File.Exists(zipPath))
            File.Delete(zipPath);

        using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);

        // story.json
        var jsonEntry = archive.CreateEntry(StoryJsonEntry, CompressionLevel.Optimal);
        using (var writer = new StreamWriter(jsonEntry.Open()))
            writer.Write(JsonSerializer.Serialize(story, JsonOptions));

        // imagini
        if (imageSourcePaths is not null)
            foreach (var (rel, abs) in imageSourcePaths)
                if (File.Exists(abs))
                    archive.CreateEntryFromFile(abs, rel, CompressionLevel.Optimal);
    }

    /// <summary>
    /// Extrage arhiva ZIP într-un director temporar și returnează calea.
    /// </summary>
    public string ExtractToTemp(string zipPath)
    {
        var tempDir = Path.Combine(
            Path.GetTempPath(), "StoryEngine_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        ZipFile.ExtractToDirectory(zipPath, tempDir, overwriteFiles: true);
        return tempDir;
    }

    /// <summary>
    /// Creează un ZIP dintr-un director (editor workflow).
    /// </summary>
    public void PackFromDirectory(string sourceDir, string zipPath)
    {
        if (File.Exists(zipPath))
            File.Delete(zipPath);
        ZipFile.CreateFromDirectory(sourceDir, zipPath,
            CompressionLevel.Optimal, includeBaseDirectory: false);
    }

    /// <summary>
    /// Citește story.json dintr-un director deja extras.
    /// </summary>
    public StoryDefinition LoadFromDirectory(string directory)
    {
        var jsonPath = Path.Combine(directory, StoryJsonEntry);
        if (!File.Exists(jsonPath))
            throw new FileNotFoundException(
                $"story.json nu a fost găsit în {directory}");

        return JsonSerializer.Deserialize<StoryDefinition>(
                File.ReadAllText(jsonPath), JsonOptions)
            ?? throw new InvalidDataException("JSON-ul poveștii este invalid.");
    }

    /// <summary>
    /// Salvează story.json într-un director (editor workflow).
    /// </summary>
    public void SaveToDirectory(StoryDefinition story, string directory)
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(
            Path.Combine(directory, StoryJsonEntry),
            JsonSerializer.Serialize(story, JsonOptions));
    }

    /// <summary>
    /// Returnează căile relative ale imaginilor din arhivă.
    /// </summary>
    public List<string> GetImagePaths(string zipPath)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        return archive.Entries
            .Where(e => e.FullName.StartsWith("images/",
                StringComparison.OrdinalIgnoreCase))
            .Select(e => e.FullName)
            .ToList();
    }

    /// <summary>
    /// Returnează lista căilor relative ale TUTUROR fișierelor din arhivă
    /// (pentru validare resurse grafice).
    /// </summary>
    public List<string> GetAllEntryPaths(string zipPath)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        return archive.Entries
            .Select(e => e.FullName.Replace('\\', '/'))
            .ToList();
    }

    /// <summary>
    /// Returnează lista fișierelor dintr-un director extras
    /// (pentru validare în Editor).
    /// </summary>
    public List<string> GetAllFilePaths(string directory)
    {
        if (!Directory.Exists(directory)) return new();
        return Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(directory, f).Replace('\\', '/'))
            .ToList();
    }
}
