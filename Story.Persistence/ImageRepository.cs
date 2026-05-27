using System.Drawing;
using System.IO.Compression;

namespace Story.Persistence;

/// <summary>
/// Gestionează încărcarea şi cache-ul imaginilor din arhiva poveştii.
/// Imaginile sunt stocate în memorie după prima citire.
/// </summary>
public class ImageRepository : IDisposable
{
    private readonly Dictionary<string, Image> _cache = new();
    private string? _zipPath;
    private string? _extractedDir;

    /// <summary>
    /// Configurează repository-ul pentru o arhivă ZIP.
    /// </summary>
    public void SetZipSource(string zipPath)
    {
        Clear();
        _zipPath = zipPath;
    }

    /// <summary>
    /// Configurează repository-ul pentru un director deja extras.
    /// </summary>
    public void SetDirectorySource(string directory)
    {
        Clear();
        _extractedDir = directory;
    }

    /// <summary>
    /// Returnează o imagine după calea sa relativă (ex: images/forest.jpg).
    /// Returnează null dacă nu există sau nu poate fi citită.
    /// </summary>
    public Image? GetImage(string? relativePath)
    {
        if (string.IsNullOrEmpty(relativePath)) return null;

        if (_cache.TryGetValue(relativePath, out var cached))
            return cached;

        Image? image = null;

        if (_extractedDir is not null)
        {
            var fullPath = Path.Combine(_extractedDir, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(fullPath))
            {
                try { image = Image.FromFile(fullPath); }
                catch { /* nu poate fi citit */ }
            }
        }
        else if (_zipPath is not null)
        {
            try
            {
                using var archive = ZipFile.OpenRead(_zipPath);
                var entry = archive.GetEntry(relativePath);
                if (entry is not null)
                {
                    using var stream = entry.Open();
                    using var ms = new MemoryStream();
                    stream.CopyTo(ms);
                    ms.Position = 0;
                    image = Image.FromStream(ms);
                }
            }
            catch { /* nu poate fi citit */ }
        }

        if (image is not null)
            _cache[relativePath] = image;

        return image;
    }

    /// <summary>
    /// Eliberează cache-ul de imagini.
    /// </summary>
    public void Clear()
    {
        foreach (var img in _cache.Values)
            img.Dispose();
        _cache.Clear();
        _zipPath = null;
        _extractedDir = null;
    }

    public void Dispose() => Clear();
}
