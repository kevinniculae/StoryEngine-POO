using System.Text.Json;
using System.Text.Json.Serialization;

namespace Story.Model;

/// <summary>
/// Nod de condiţie – poate fi COMPARISON, AND, sau OR.
/// Reprezintă un AST (Abstract Syntax Tree) serializat în JSON.
/// </summary>
[JsonConverter(typeof(ConditionDefinitionConverter))]
public abstract class ConditionDefinition
{
    [JsonPropertyName("type")]
    public abstract string Type { get; }
}

/// <summary>Condiţie simplă: compară o proprietate cu o valoare numerică.</summary>
public class ComparisonCondition : ConditionDefinition
{
    public override string Type => "COMPARISON";

    [JsonPropertyName("property")]
    public string Property { get; set; } = string.Empty;

    [JsonPropertyName("operator")]
    public string Operator { get; set; } = "==";

    [JsonPropertyName("value")]
    public double Value { get; set; }
}

/// <summary>Condiţie compusă AND – toate sub-condiţiile trebuie să fie adevărate.</summary>
public class AndCondition : ConditionDefinition
{
    public override string Type => "AND";

    [JsonPropertyName("conditions")]
    public List<ConditionDefinition> Conditions { get; set; } = new();
}

/// <summary>Condiţie compusă OR – cel puţin o sub-condiţie trebuie să fie adevărată.</summary>
public class OrCondition : ConditionDefinition
{
    public override string Type => "OR";

    [JsonPropertyName("conditions")]
    public List<ConditionDefinition> Conditions { get; set; } = new();
}

/// <summary>
/// Convertor JSON polimorfic pentru ConditionDefinition.
/// Citeşte câmpul "type" şi instanţiază tipul corespunzător.
/// </summary>
public class ConditionDefinitionConverter : JsonConverter<ConditionDefinition>
{
    public override ConditionDefinition? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty("type", out var typeProp))
            throw new JsonException("Condiţia nu are câmpul 'type'.");

        var type = typeProp.GetString()?.ToUpperInvariant();
        var json = root.GetRawText();

        return type switch
        {
            "COMPARISON" => JsonSerializer.Deserialize<ComparisonCondition>(json, options),
            "AND" => JsonSerializer.Deserialize<AndCondition>(json, options),
            "OR" => JsonSerializer.Deserialize<OrCondition>(json, options),
            _ => throw new JsonException($"Tip de condiţie necunoscut: {type}")
        };
    }

    public override void Write(Utf8JsonWriter writer, ConditionDefinition value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case ComparisonCondition c:
                JsonSerializer.Serialize(writer, c, options);
                break;
            case AndCondition a:
                JsonSerializer.Serialize(writer, a, options);
                break;
            case OrCondition o:
                JsonSerializer.Serialize(writer, o, options);
                break;
            default:
                throw new JsonException($"Tip necunoscut: {value.GetType().Name}");
        }
    }
}
