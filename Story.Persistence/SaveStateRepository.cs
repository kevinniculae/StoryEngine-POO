using System.Text.Json;
using Story.Engine;

namespace Story.Persistence;

/// <summary>
/// Salvează şi restaurează starea jocului (save/load).
/// </summary>
public class SaveStateRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public void Save(GameState state, string filePath)
    {
        var data = new SaveData
        {
            CurrentBlockId = state.CurrentBlockId,
            IsFinished = state.IsFinished,
            Values = state.GetAllValues()
        };

        var json = JsonSerializer.Serialize(data, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    public void Load(GameState state, string filePath)
    {
        var json = File.ReadAllText(filePath);
        var data = JsonSerializer.Deserialize<SaveData>(json)
            ?? throw new InvalidDataException("Fişierul de salvare este invalid.");

        state.CurrentBlockId = data.CurrentBlockId;
        state.IsFinished = data.IsFinished;
        state.RestoreValues(data.Values);
    }

    private class SaveData
    {
        public string CurrentBlockId { get; set; } = string.Empty;
        public bool IsFinished { get; set; }
        public Dictionary<string, double> Values { get; set; } = new();
    }
}
