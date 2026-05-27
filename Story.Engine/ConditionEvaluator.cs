using Story.Model;

namespace Story.Engine;

/// <summary>
/// Evaluează condiţii de tip AST faţă de starea curentă a jocului.
/// </summary>
public class ConditionEvaluator
{
    private readonly GameState _state;

    public ConditionEvaluator(GameState state)
    {
        _state = state;
    }

    /// <summary>
    /// Evaluează recursiv un nod de condiţie.
    /// Returnează true dacă condiţia este îndeplinită.
    /// Un nod null înseamnă că nu există condiţie → mereu adevărat.
    /// </summary>
    public bool Evaluate(ConditionDefinition? condition)
    {
        if (condition is null) return true;

        return condition switch
        {
            ComparisonCondition c => EvaluateComparison(c),
            AndCondition a => a.Conditions.All(Evaluate),
            OrCondition o => o.Conditions.Any(Evaluate),
            _ => throw new InvalidOperationException($"Tip condiţie necunoscut: {condition.GetType().Name}")
        };
    }

    private bool EvaluateComparison(ComparisonCondition c)
    {
        var actual = _state.GetValue(c.Property);
        return c.Operator switch
        {
            "<" => actual < c.Value,
            "<=" => actual <= c.Value,
            ">" => actual > c.Value,
            ">=" => actual >= c.Value,
            "==" => Math.Abs(actual - c.Value) < 1e-9,
            "!=" => Math.Abs(actual - c.Value) >= 1e-9,
            _ => throw new InvalidOperationException($"Operator necunoscut: {c.Operator}")
        };
    }
}
