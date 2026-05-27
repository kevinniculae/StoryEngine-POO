using System.Text.Json.Serialization;

namespace Story.Model;

/// <summary>
/// O decizie disponibilă utilizatorului la un bloc narativ.
/// </summary>
public class DecisionDefinition
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("targetBlock")]
    public string TargetBlock { get; set; } = string.Empty;

    /// <summary>Calea relativă spre icoana deciziei (ex: images/icon_key.png).</summary>
    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    /// <summary>Condiţia de afişare (AST JSON). Null = mereu afişată.</summary>
    [JsonPropertyName("condition")]
    public ConditionDefinition? Condition { get; set; }

    [JsonPropertyName("effects")]
    public List<EffectDefinition> Effects { get; set; } = new();
}
