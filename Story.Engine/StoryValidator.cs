using Story.Model;

namespace Story.Engine;

/// <summary>
/// Validează o definiție de poveste conform regulilor din cerințe.
/// Acoperă: structură, proprietăți, blocuri, referințe, resurse grafice,
/// accesibilitate graf.
/// </summary>
public class StoryValidator
{
    // Extensii de imagine acceptate
    private static readonly HashSet<string> ValidImageExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp" };

    public ValidationResult Validate(StoryDefinition story,
        IEnumerable<string>? availableResources = null)
    {
        var result   = new ValidationResult();
        var resSet   = availableResources?.ToHashSet(StringComparer.OrdinalIgnoreCase);

        ValidateMetadata(story, result);
        ValidateProperties(story, result);
        ValidateBlocks(story, result);
        ValidateReferences(story, result);
        ValidateResources(story, result, resSet);
        ValidateAccessibility(story, result);

        return result;
    }

    // ── 1. Metadata ──────────────────────────────────────────────────────
    private void ValidateMetadata(StoryDefinition story, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(story.Title))
            result.AddError("Titlul poveștii este obligatoriu.");

        if (string.IsNullOrWhiteSpace(story.StartBlock))
            result.AddError("Blocul de start (startBlock) este obligatoriu.");
    }

    // ── 2. Proprietăți ───────────────────────────────────────────────────
    private void ValidateProperties(StoryDefinition story, ValidationResult result)
    {
        var keys = new HashSet<string>();
        foreach (var prop in story.Properties)
        {
            if (string.IsNullOrWhiteSpace(prop.Key))
            {
                result.AddError("O proprietate nu are cheie.");
                continue;
            }
            if (!keys.Add(prop.Key))
                result.AddError($"Proprietatea '{prop.Key}' este duplicată.");

            if (prop.Min > prop.Max)
                result.AddError($"'{prop.Key}': min ({prop.Min}) > max ({prop.Max}).");

            if (prop.Initial < prop.Min || prop.Initial > prop.Max)
                result.AddError(
                    $"'{prop.Key}': valoarea inițială ({prop.Initial}) " +
                    $"nu este în [{prop.Min}, {prop.Max}].");

            if (prop.HudOrder < 0)
                result.AddWarning($"'{prop.Key}': hudOrder negativ ({prop.HudOrder}).");
        }
    }

    // ── 3. Blocuri ───────────────────────────────────────────────────────
    private void ValidateBlocks(StoryDefinition story, ValidationResult result)
    {
        var ids = new HashSet<string>();
        foreach (var block in story.Blocks)
        {
            if (string.IsNullOrWhiteSpace(block.Id))
            {
                result.AddError("Un bloc nu are ID.");
                continue;
            }
            if (!ids.Add(block.Id))
                result.AddError($"Blocul '{block.Id}' este duplicat.");

            if (string.IsNullOrWhiteSpace(block.Text))
                result.AddWarning($"Blocul '{block.Id}' nu are text narativ.");

            if (!block.IsFinal && block.Decisions.Count == 0)
                result.AddWarning(
                    $"Blocul '{block.Id}' nu este final și nu are decizii (dead-end).");
        }

        if (!string.IsNullOrWhiteSpace(story.StartBlock)
            && !ids.Contains(story.StartBlock))
            result.AddError(
                $"Blocul de start '{story.StartBlock}' nu există în lista de blocuri.");
    }

    // ── 4. Referințe ─────────────────────────────────────────────────────
    private void ValidateReferences(StoryDefinition story, ValidationResult result)
    {
        var blockIds = story.Blocks.Select(b => b.Id).ToHashSet();
        var propKeys = story.Properties.Select(p => p.Key).ToHashSet();

        // Redirect-uri din proprietăți
        foreach (var prop in story.Properties)
        {
            if (!string.IsNullOrEmpty(prop.OnMinBlock)
                && !blockIds.Contains(prop.OnMinBlock!))
                result.AddError(
                    $"Proprietatea '{prop.Key}': onMinBlock " +
                    $"'{prop.OnMinBlock}' nu există.");

            if (!string.IsNullOrEmpty(prop.OnMaxBlock)
                && !blockIds.Contains(prop.OnMaxBlock!))
                result.AddError(
                    $"Proprietatea '{prop.Key}': onMaxBlock " +
                    $"'{prop.OnMaxBlock}' nu există.");
        }

        // Decizii
        foreach (var block in story.Blocks)
        {
            foreach (var dec in block.Decisions)
            {
                if (string.IsNullOrWhiteSpace(dec.TargetBlock))
                {
                    result.AddError(
                        $"O decizie din blocul '{block.Id}' nu are targetBlock.");
                    continue;
                }
                if (!blockIds.Contains(dec.TargetBlock))
                    result.AddError(
                        $"Decizia '{dec.Text}' din blocul '{block.Id}' " +
                        $"referă blocul '{dec.TargetBlock}' care nu există.");

                foreach (var ef in dec.Effects)
                    if (!propKeys.Contains(ef.Property))
                        result.AddError(
                            $"Decizia '{dec.Text}' din '{block.Id}': " +
                            $"efectul referă proprietatea '{ef.Property}' care nu există.");

                if (dec.Condition is not null)
                    ValidateConditionRefs(
                        dec.Condition, dec.Text, block.Id, propKeys, result);
            }
        }
    }

    private void ValidateConditionRefs(
        ConditionDefinition cond,
        string decText, string blockId,
        HashSet<string> propKeys,
        ValidationResult result)
    {
        switch (cond)
        {
            case ComparisonCondition c:
                if (!propKeys.Contains(c.Property))
                    result.AddError(
                        $"Decizia '{decText}' din '{blockId}': " +
                        $"condiția referă proprietatea '{c.Property}' care nu există.");
                var validOps = new HashSet<string>
                    { "<", "<=", ">", ">=", "==", "!=" };
                if (!validOps.Contains(c.Operator))
                    result.AddError(
                        $"Decizia '{decText}' din '{blockId}': " +
                        $"operator necunoscut '{c.Operator}'.");
                break;
            case AndCondition a:
                if (a.Conditions.Count == 0)
                    result.AddWarning(
                        $"Decizia '{decText}' din '{blockId}': " +
                        $"condiție AND fără sub-condiții.");
                foreach (var sub in a.Conditions)
                    ValidateConditionRefs(sub, decText, blockId, propKeys, result);
                break;
            case OrCondition o:
                if (o.Conditions.Count == 0)
                    result.AddWarning(
                        $"Decizia '{decText}' din '{blockId}': " +
                        $"condiție OR fără sub-condiții.");
                foreach (var sub in o.Conditions)
                    ValidateConditionRefs(sub, decText, blockId, propKeys, result);
                break;
        }
    }

    // ── 5. Resurse grafice ───────────────────────────────────────────────
    /// <summary>
    /// Verifică imaginile referite în blocuri și decizii:
    /// - extensie compatibilă
    /// - existența în arhivă (dacă availableResources este furnizat)
    /// </summary>
    private void ValidateResources(
        StoryDefinition story,
        ValidationResult result,
        HashSet<string>? availableResources)
    {
        var allRefs = new List<(string path, string context)>();

        foreach (var block in story.Blocks)
        {
            if (!string.IsNullOrEmpty(block.BackgroundImage))
                allRefs.Add((block.BackgroundImage!, $"blocul '{block.Id}'"));

            foreach (var dec in block.Decisions)
                if (!string.IsNullOrEmpty(dec.Icon))
                    allRefs.Add((dec.Icon!, $"decizia '{dec.Text}' din '{block.Id}'"));
        }

        foreach (var (path, ctx) in allRefs)
        {
            // 1. Verifică extensia
            var ext = Path.GetExtension(path);
            if (!ValidImageExtensions.Contains(ext))
                result.AddError(
                    $"Resursa '{path}' din {ctx} " +
                    $"are extensie incompatibilă ('{ext}'). " +
                    $"Acceptate: {string.Join(", ", ValidImageExtensions)}");

            // 2. Verifică existența în arhivă (opțional, dacă lista e furnizată)
            if (availableResources is not null
                && !availableResources.Contains(path)
                && !availableResources.Contains(path.Replace('/', '\\')))
                result.AddError(
                    $"Resursa '{path}' din {ctx} " +
                    $"nu există în arhiva ZIP.");
        }
    }

    // ── 6. Accesibilitate graf ────────────────────────────────────────────
    /// <summary>
    /// Verifică prin BFS că toate blocurile sunt accesibile din blocul de start.
    /// Blocurile inaccesibile sunt raportate ca avertismente.
    /// </summary>
    private void ValidateAccessibility(StoryDefinition story, ValidationResult result)
    {
        if (story.Blocks.Count == 0) return;
        if (string.IsNullOrEmpty(story.StartBlock)) return;

        var blockMap = story.Blocks.ToDictionary(b => b.Id);
        if (!blockMap.ContainsKey(story.StartBlock)) return;

        // BFS din blocul de start
        var visited = new HashSet<string>();
        var queue   = new Queue<string>();

        visited.Enqueue(story.StartBlock, queue);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!blockMap.TryGetValue(current, out var block)) continue;

            // Urmărește toate destinațiile deciziilor
            foreach (var dec in block.Decisions)
            {
                if (!string.IsNullOrEmpty(dec.TargetBlock)
                    && blockMap.ContainsKey(dec.TargetBlock)
                    && !visited.Contains(dec.TargetBlock))
                {
                    visited.Enqueue(dec.TargetBlock, queue);
                }
            }

            // Urmărește și redirect-urile din proprietăți
            foreach (var prop in story.Properties)
            {
                if (!string.IsNullOrEmpty(prop.OnMinBlock)
                    && blockMap.ContainsKey(prop.OnMinBlock!)
                    && !visited.Contains(prop.OnMinBlock!))
                    visited.Enqueue(prop.OnMinBlock!, queue);

                if (!string.IsNullOrEmpty(prop.OnMaxBlock)
                    && blockMap.ContainsKey(prop.OnMaxBlock!)
                    && !visited.Contains(prop.OnMaxBlock!))
                    visited.Enqueue(prop.OnMaxBlock!, queue);
            }
        }

        // Raportează blocurile inaccesibile
        foreach (var block in story.Blocks)
        {
            if (!visited.Contains(block.Id))
                result.AddWarning(
                    $"Blocul '{block.Id}' este inaccesibil " +
                    $"(nu se poate ajunge la el din blocul de start '{story.StartBlock}').");
        }
    }
}

// ── Extension helper ──────────────────────────────────────────────────────────
internal static class QueueExtensions
{
    public static void Enqueue(this HashSet<string> visited, string id, Queue<string> queue)
    {
        if (visited.Add(id))
            queue.Enqueue(id);
    }
}
