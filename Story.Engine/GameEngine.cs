using Story.Model;

namespace Story.Engine;

/// <summary>
/// Motorul central al jocului.
/// Gestionează fluxul poveştii: navigare, filtrare decizii, aplicare efecte.
/// </summary>
public class GameEngine
{
    private StoryDefinition? _story;
    private Dictionary<string, StoryBlock> _blockMap = new();
    private ConditionEvaluator? _evaluator;
    private EffectApplicator? _applicator;

    public GameState State { get; } = new();

    /// <summary>Eveniment declanşat când blocul curent se schimbă.</summary>
    public event EventHandler<BlockChangedEventArgs>? BlockChanged;

    /// <summary>Eveniment declanşat când starea jocului se modifică.</summary>
    public event EventHandler? StateChanged;

    /// <summary>Eveniment declanşat când povestea se termină.</summary>
    public event EventHandler<StoryEndedEventArgs>? StoryEnded;

    /// <summary>
    /// Încarcă o definiţie de poveste şi iniţializează jocul.
    /// </summary>
    public void LoadStory(StoryDefinition story)
    {
        _story = story;
        _blockMap = story.Blocks.ToDictionary(b => b.Id);
        _evaluator = new ConditionEvaluator(State);
        _applicator = new EffectApplicator(State);

        State.Initialize(story);
        NotifyBlockChanged();
    }

    /// <summary>
    /// Returnează blocul curent sau null dacă nu există.
    /// </summary>
    public StoryBlock? GetCurrentBlock()
    {
        if (string.IsNullOrEmpty(State.CurrentBlockId)) return null;
        return _blockMap.TryGetValue(State.CurrentBlockId, out var b) ? b : null;
    }

    /// <summary>
    /// Returnează lista deciziilor disponibile (filtrate după condiţii).
    /// </summary>
    public List<DecisionDefinition> GetAvailableDecisions()
    {
        var block = GetCurrentBlock();
        if (block is null || _evaluator is null) return new();

        return block.Decisions
            .Where(d => _evaluator.Evaluate(d.Condition))
            .ToList();
    }

    /// <summary>
    /// Execută o decizie: aplică efectele şi navighează la blocul următor.
    /// </summary>
    public void MakeDecision(DecisionDefinition decision)
    {
        if (_applicator is null || _story is null) return;

        // Aplică efectele şi verifică redirect-uri automate
        var redirects = _applicator.ApplyEffects(decision.Effects);
        StateChanged?.Invoke(this, EventArgs.Empty);

        // Dacă un redirect automat există, merge acolo
        if (redirects.Count > 0)
        {
            NavigateTo(redirects[0]);
            return;
        }

        NavigateTo(decision.TargetBlock);
    }

    /// <summary>
    /// Navighează direct la un bloc prin ID.
    /// </summary>
    public void NavigateTo(string blockId)
    {
        if (!_blockMap.TryGetValue(blockId, out var block))
        {
            // Bloc negăsit – semnal de eroare
            return;
        }

        State.CurrentBlockId = blockId;

        if (block.IsFinal)
        {
            State.IsFinished = true;
            StoryEnded?.Invoke(this, new StoryEndedEventArgs(block));
            return;
        }

        NotifyBlockChanged();
    }

    /// <summary>
    /// Resetează jocul la start.
    /// </summary>
    public void Restart()
    {
        if (_story is null) return;
        LoadStory(_story);
    }

    /// <summary>
    /// Returnează un bloc după ID.
    /// </summary>
    public StoryBlock? GetBlock(string id)
    {
        return _blockMap.TryGetValue(id, out var b) ? b : null;
    }

    /// <summary>
    /// Returnează toate blocurile.
    /// </summary>
    public IReadOnlyDictionary<string, StoryBlock> AllBlocks => _blockMap;

    public StoryDefinition? CurrentStory => _story;

    private void NotifyBlockChanged()
    {
        var block = GetCurrentBlock();
        if (block is null) return;
        BlockChanged?.Invoke(this, new BlockChangedEventArgs(block));
    }
}

public class BlockChangedEventArgs : EventArgs
{
    public StoryBlock Block { get; }
    public BlockChangedEventArgs(StoryBlock block) => Block = block;
}

public class StoryEndedEventArgs : EventArgs
{
    public StoryBlock FinalBlock { get; }
    public StoryEndedEventArgs(StoryBlock block) => FinalBlock = block;
}
