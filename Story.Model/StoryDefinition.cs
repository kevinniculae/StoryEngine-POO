using System.Text.Json.Serialization;

namespace Story.Model;

/// <summary>
/// Rădăcina poveștii – conține toate datele serializate în JSON.
/// </summary>
public class StoryDefinition
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("startBlock")]
    public string StartBlock { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    [JsonPropertyName("properties")]
    public List<StatePropertyDefinition> Properties { get; set; } = new();

    [JsonPropertyName("blocks")]
    public List<StoryBlock> Blocks { get; set; } = new();
}
