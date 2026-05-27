using Story.Model;

namespace Story.Engine;

/// <summary>
/// Starea curentă a jocului la runtime.
/// Conţine valorile proprietăţilor şi blocul curent.
/// </summary>
public class GameState
{
    private readonly Dictionary<string, double> _values = new();
    private readonly Dictionary<string, StatePropertyDefinition> _definitions = new();

    public string CurrentBlockId { get; set; } = string.Empty;
    public bool IsFinished { get; set; } = false;

    /// <summary>
    /// Iniţializează starea pornind de la definiţiile proprietăţilor din poveste.
    /// </summary>
    public void Initialize(StoryDefinition story)
    {
        _values.Clear();
        _definitions.Clear();
        foreach (var prop in story.Properties)
        {
            _definitions[prop.Key] = prop;
            _values[prop.Key] = prop.Initial;
        }
        CurrentBlockId = story.StartBlock;
        IsFinished = false;
    }

    /// <summary>Returnează valoarea curentă a unei proprietăţi.</summary>
    public double GetValue(string key)
    {
        return _values.TryGetValue(key, out var v) ? v : 0.0;
    }

    /// <summary>Setează valoarea, limitând-o la [min, max].</summary>
    public void SetValue(string key, double value)
    {
        if (_definitions.TryGetValue(key, out var def))
            _values[key] = Math.Max(def.Min, Math.Min(def.Max, value));
        else
            _values[key] = value;
    }

    /// <summary>Returnează definiţia unei proprietăţi sau null.</summary>
    public StatePropertyDefinition? GetDefinition(string key)
    {
        return _definitions.TryGetValue(key, out var d) ? d : null;
    }

    /// <summary>Toate proprietăţile vizibile în HUD, ordonate.</summary>
    public IEnumerable<(StatePropertyDefinition Def, double Value)> GetHudProperties()
    {
        return _definitions.Values
            .Where(d => d.VisibleInHud)
            .OrderBy(d => d.HudOrder)
            .Select(d => (d, GetValue(d.Key)));
    }

    /// <summary>
    /// Toate definiţiile (inclusiv cele invizibile în HUD).
    /// Folosit de EffectApplicator pentru redirect-uri.
    /// </summary>
    public IEnumerable<StatePropertyDefinition> GetAllDefinitions()
    {
        return _definitions.Values;
    }

    /// <summary>Toate perechile cheie-valoare pentru salvare.</summary>
    public Dictionary<string, double> GetAllValues() => new(_values);

    /// <summary>Restaurează valorile dintr-un save.</summary>
    public void RestoreValues(Dictionary<string, double> saved)
    {
        foreach (var kv in saved)
            _values[kv.Key] = kv.Value;
    }
}
