using Story.Model;

namespace Story.Engine;

/// <summary>
/// Aplică efectele unei decizii asupra stării jocului.
/// </summary>
public class EffectApplicator
{
    private readonly GameState _state;

    public EffectApplicator(GameState state)
    {
        _state = state;
    }

    /// <summary>
    /// Aplică lista de efecte a unei decizii.
    /// Returnează lista de ID-uri de blocuri spre care trebuie redirect
    /// (dacă o proprietate atinge min/max).
    /// </summary>
    public List<string> ApplyEffects(IEnumerable<EffectDefinition> effects)
    {
        foreach (var effect in effects)
        {
            var current = _state.GetValue(effect.Property);
            var newValue = effect.Type switch
            {
                EffectType.ADD => current + effect.Value,
                EffectType.SET => effect.Value,
                _ => current
            };
            _state.SetValue(effect.Property, newValue);
        }

        return CheckRedirects();
    }

    private List<string> CheckRedirects()
    {
        var redirects = new List<string>();

        foreach (var prop in _state.GetAllDefinitions())
        {
            var value = _state.GetValue(prop.Key);

            if (!string.IsNullOrEmpty(prop.OnMinBlock) && Math.Abs(value - prop.Min) < 1e-9)
                redirects.Add(prop.OnMinBlock!);
            else if (!string.IsNullOrEmpty(prop.OnMaxBlock) && Math.Abs(value - prop.Max) < 1e-9)
                redirects.Add(prop.OnMaxBlock!);
        }

        return redirects;
    }
}
