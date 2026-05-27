using System.Text.Json.Serialization;

namespace Story.Model;

/// <summary>
/// Tipul de efect aplicat la alegerea unei decizii.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EffectType
{
    /// <summary>Adaugă delta la valoarea curentă.</summary>
    ADD,
    /// <summary>Setează direct o valoare.</summary>
    SET
}

/// <summary>
/// Un efect care modifică o proprietate de stare la alegerea unei decizii.
/// </summary>
public class EffectDefinition
{
    [JsonPropertyName("type")]
    public EffectType Type { get; set; } = EffectType.ADD;

    [JsonPropertyName("property")]
    public string Property { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public double Value { get; set; }
}
