using System.Text.Json.Serialization;

namespace Story.Model;

/// <summary>
/// Defineşte o proprietate numerică a stării poveştii (ex: player.life, inventory.money).
/// </summary>
public class StatePropertyDefinition
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("hudLabel")]
    public string HudLabel { get; set; } = string.Empty;

    [JsonPropertyName("min")]
    public double Min { get; set; } = 0;

    [JsonPropertyName("max")]
    public double Max { get; set; } = 100;

    [JsonPropertyName("initial")]
    public double Initial { get; set; } = 0;

    [JsonPropertyName("visibleInHud")]
    public bool VisibleInHud { get; set; } = true;

    [JsonPropertyName("hudOrder")]
    public int HudOrder { get; set; } = 99;

    /// <summary>ID-ul blocului la care se sare când proprietatea atinge minimul.</summary>
    [JsonPropertyName("onMinBlock")]
    public string? OnMinBlock { get; set; }

    /// <summary>ID-ul blocului la care se sare când proprietatea atinge maximul.</summary>
    [JsonPropertyName("onMaxBlock")]
    public string? OnMaxBlock { get; set; }
}
