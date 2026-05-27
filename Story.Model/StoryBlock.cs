using System.Text.Json.Serialization;

namespace Story.Model;

/// <summary>
/// Un bloc narativ – o etapă a poveştii cu text şi decizii posibile.
/// </summary>
public class StoryBlock
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    /// <summary>Calea relativă spre imaginea de fundal (ex: images/forest.jpg).</summary>
    [JsonPropertyName("backgroundImage")]
    public string? BackgroundImage { get; set; }

    /// <summary>Dacă este true, blocul reprezintă un final al poveştii.</summary>
    [JsonPropertyName("isFinal")]
    public bool IsFinal { get; set; } = false;

    [JsonPropertyName("decisions")]
    public List<DecisionDefinition> Decisions { get; set; } = new();
}
