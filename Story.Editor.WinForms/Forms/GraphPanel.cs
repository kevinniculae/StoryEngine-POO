using Story.Model;

namespace Story.Editor.WinForms.Forms;

/// <summary>
/// Control custom care desenează graful poveștii ca noduri + săgeți.
/// Suportă pan (drag), zoom cu scroll, și selecție de noduri prin click.
/// </summary>
public class GraphPanel : Panel
{
    // ── date ──────────────────────────────────────────────────────────────
    private StoryDefinition? _story;

    // ── layout ────────────────────────────────────────────────────────────
    private readonly Dictionary<string, RectangleF> _nodeRects = new();
    private const int NODE_W   = 160;
    private const int NODE_H   = 54;
    private const int COL_GAP  = 60;
    private const int ROW_GAP  = 30;

    // ── pan & zoom ────────────────────────────────────────────────────────
    private PointF _pan        = new(20, 20);
    private float  _zoom       = 1.0f;
    private const float ZOOM_MIN = 0.3f;
    private const float ZOOM_MAX = 2.5f;
    private Point  _lastMouse;
    private bool   _panning;

    // ── selection ─────────────────────────────────────────────────────────
    private string? _selectedId;
    public event EventHandler<string>? NodeSelected;

    // ── colors ────────────────────────────────────────────────────────────
    private static readonly Color BG         = Color.FromArgb(12, 12, 20);
    private static readonly Color GRID_COLOR = Color.FromArgb(22, 22, 34);
    private static readonly Color NODE_BG    = Color.FromArgb(30, 28, 46);
    private static readonly Color NODE_START = Color.FromArgb(28, 55, 28);
    private static readonly Color NODE_FINAL = Color.FromArgb(55, 20, 20);
    private static readonly Color NODE_SEL   = Color.FromArgb(45, 42, 75);
    private static readonly Color NODE_BORD  = Color.FromArgb(60, 57, 90);
    private static readonly Color NODE_BORD_START = Color.FromArgb(80, 175, 80);
    private static readonly Color NODE_BORD_FINAL = Color.FromArgb(175, 70, 70);
    private static readonly Color NODE_BORD_SEL   = Color.FromArgb(180, 160, 255);
    private static readonly Color TEXT_ID    = Color.FromArgb(255, 215, 100);
    private static readonly Color TEXT_DEC   = Color.FromArgb(140, 135, 120);
    private static readonly Color ARROW      = Color.FromArgb(90, 85, 130);
    private static readonly Color ARROW_SEL  = Color.FromArgb(180, 160, 255);

    public GraphPanel()
    {
        DoubleBuffered  = true;
        BackColor       = BG;
        Cursor          = Cursors.Hand;
        MinimumSize     = new Size(200, 200);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  PUBLIC API
    // ══════════════════════════════════════════════════════════════════════
    public void LoadStory(StoryDefinition story)
    {
        _story = story;
        LayoutNodes();
        Invalidate();
    }

    public void Refresh(StoryDefinition story)
    {
        _story = story;
        LayoutNodes();
        Invalidate();
    }

    public void SelectNode(string? id)
    {
        _selectedId = id;
        Invalidate();
    }

    public void FitAll()
    {
        if (_nodeRects.Count == 0) return;
        float minX = _nodeRects.Values.Min(r => r.X);
        float minY = _nodeRects.Values.Min(r => r.Y);
        float maxX = _nodeRects.Values.Max(r => r.Right);
        float maxY = _nodeRects.Values.Max(r => r.Bottom);

        float graphW = maxX - minX;
        float graphH = maxY - minY;
        float scaleX = (Width  - 60) / Math.Max(graphW, 1);
        float scaleY = (Height - 60) / Math.Max(graphH, 1);
        _zoom = Math.Clamp(Math.Min(scaleX, scaleY), ZOOM_MIN, ZOOM_MAX);

        float centerX = (Width  - graphW * _zoom) / 2f;
        float centerY = (Height - graphH * _zoom) / 2f;
        _pan = new PointF(centerX - minX * _zoom, centerY - minY * _zoom);
        Invalidate();
    }

    // ══════════════════════════════════════════════════════════════════════
    //  LAYOUT — BFS level-based
    // ══════════════════════════════════════════════════════════════════════
    private void LayoutNodes()
    {
        _nodeRects.Clear();
        if (_story is null || _story.Blocks.Count == 0) return;

        // BFS from start block
        var levels   = new Dictionary<string, int>();
        var queue    = new Queue<string>();
        var startId  = _story.StartBlock;

        if (!_story.Blocks.Any(b => b.Id == startId))
            startId = _story.Blocks[0].Id;

        levels[startId] = 0;
        queue.Enqueue(startId);

        var blockMap = _story.Blocks.ToDictionary(b => b.Id);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!blockMap.TryGetValue(current, out var block)) continue;

            foreach (var dec in block.Decisions)
            {
                if (!string.IsNullOrEmpty(dec.TargetBlock) && !levels.ContainsKey(dec.TargetBlock))
                {
                    levels[dec.TargetBlock] = levels[current] + 1;
                    queue.Enqueue(dec.TargetBlock);
                }
            }
        }

        // Assign level 999 to unreachable blocks
        foreach (var b in _story.Blocks)
            if (!levels.ContainsKey(b.Id))
                levels[b.Id] = 999;

        // Group by level
        var byLevel = levels
            .GroupBy(kv => kv.Value)
            .OrderBy(g => g.Key)
            .ToList();

        float x = 0;
        foreach (var levelGroup in byLevel)
        {
            var ids = levelGroup.Select(kv => kv.Key).ToList();
            float totalH = ids.Count * NODE_H + (ids.Count - 1) * ROW_GAP;
            float startY = 0;
            for (int i = 0; i < ids.Count; i++)
            {
                float y = startY + i * (NODE_H + ROW_GAP);
                _nodeRects[ids[i]] = new RectangleF(x, y, NODE_W, NODE_H);
            }
            x += NODE_W + COL_GAP;
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  PAINT
    // ══════════════════════════════════════════════════════════════════════
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode     = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        // Apply pan & zoom transform
        g.TranslateTransform(_pan.X, _pan.Y);
        g.ScaleTransform(_zoom, _zoom);

        DrawGrid(g);
        if (_story is null) return;

        var blockMap = _story.Blocks.ToDictionary(b => b.Id);

        // Draw edges first (behind nodes)
        foreach (var block in _story.Blocks)
        {
            if (!_nodeRects.TryGetValue(block.Id, out var fromRect)) continue;
            foreach (var dec in block.Decisions)
            {
                if (string.IsNullOrEmpty(dec.TargetBlock)) continue;
                if (!_nodeRects.TryGetValue(dec.TargetBlock, out var toRect)) continue;
                bool highlighted = block.Id == _selectedId || dec.TargetBlock == _selectedId;
                DrawEdge(g, fromRect, toRect, highlighted);
            }
        }

        // Draw nodes
        foreach (var block in _story.Blocks)
        {
            if (!_nodeRects.TryGetValue(block.Id, out var rect)) continue;
            bool isStart = block.Id == _story.StartBlock;
            bool isFinal = block.IsFinal;
            bool isSel   = block.Id == _selectedId;
            DrawNode(g, rect, block, isStart, isFinal, isSel);
        }

        // Help text if empty
        if (_story.Blocks.Count == 0)
        {
            using var f = new Font("Segoe UI", 11f, FontStyle.Italic);
            g.DrawString("(niciun bloc)", f, Brushes.DimGray, 20, 20);
        }
    }

    private void DrawGrid(Graphics g)
    {
        using var pen = new Pen(GRID_COLOR, 1f / _zoom);
        // Visible area in graph coords
        float left   = -_pan.X / _zoom;
        float top    = -_pan.Y / _zoom;
        float right  = left + Width  / _zoom;
        float bottom = top  + Height / _zoom;
        float step   = 40;

        for (float gx = (float)Math.Floor(left / step) * step; gx < right; gx += step)
            g.DrawLine(pen, gx, top, gx, bottom);
        for (float gy = (float)Math.Floor(top / step) * step; gy < bottom; gy += step)
            g.DrawLine(pen, left, gy, right, gy);
    }

    private void DrawNode(Graphics g, RectangleF rect, StoryBlock block,
        bool isStart, bool isFinal, bool isSelected)
    {
        // Background
        var bgColor = isSelected ? NODE_SEL
                    : isStart   ? NODE_START
                    : isFinal   ? NODE_FINAL
                    : NODE_BG;
        using var bgBrush = new SolidBrush(bgColor);
        g.FillRectangle(bgBrush, rect);

        // Border
        var borderColor = isSelected ? NODE_BORD_SEL
                        : isStart   ? NODE_BORD_START
                        : isFinal   ? NODE_BORD_FINAL
                        : NODE_BORD;
        using var borderPen = new Pen(borderColor, isSelected ? 2.5f : 1.5f);
        g.DrawRectangle(borderPen, rect.X, rect.Y, rect.Width, rect.Height);

        // Top accent line
        using var accentPen = new Pen(borderColor, 3f);
        g.DrawLine(accentPen, rect.X, rect.Y, rect.Right, rect.Y);

        // Icon + ID
        string icon = isFinal ? "🏁" : isStart ? "▶" : "▷";
        using var idFont = new Font("Segoe UI", 8f, FontStyle.Bold);
        using var idBrush = new SolidBrush(TEXT_ID);
        g.DrawString($"{icon}  {block.Id}", idFont, idBrush,
            rect.X + 6, rect.Y + 6);

        // Decision count
        using var decFont  = new Font("Segoe UI", 7.5f);
        using var decBrush = new SolidBrush(TEXT_DEC);
        string decText = block.Decisions.Count == 0
            ? (isFinal ? "— final —" : "⚠ no decisions")
            : $"{block.Decisions.Count} decizie{(block.Decisions.Count != 1 ? "i" : "")}";
        g.DrawString(decText, decFont, decBrush, rect.X + 6, rect.Y + 30);
    }

    private void DrawEdge(Graphics g, RectangleF from, RectangleF to, bool highlighted)
    {
        // Connection points: right-center → left-center
        var p1 = new PointF(from.Right, from.Y + from.Height / 2f);
        var p2 = new PointF(to.Left,   to.Y   + to.Height   / 2f);

        // Self-loop
        if (from == to)
        {
            using var selfPen = new Pen(highlighted ? ARROW_SEL : ARROW, 1.5f);
            g.DrawEllipse(selfPen, from.Right - 10, from.Top - 20, 30, 20);
            return;
        }

        // Bezier curve
        float dx     = Math.Abs(p2.X - p1.X) * 0.5f;
        var cp1 = new PointF(p1.X + dx, p1.Y);
        var cp2 = new PointF(p2.X - dx, p2.Y);

        using var pen = new Pen(highlighted ? ARROW_SEL : ARROW, highlighted ? 2f : 1.2f);
        pen.EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor;
        g.DrawBezier(pen, p1, cp1, cp2, p2);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  MOUSE: pan + click
    // ══════════════════════════════════════════════════════════════════════
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Middle || e.Button == MouseButtons.Right)
        {
            _panning   = true;
            _lastMouse = e.Location;
            Cursor     = Cursors.SizeAll;
        }
        else if (e.Button == MouseButtons.Left)
        {
            var graphPt = ScreenToGraph(e.Location);
            string? hit = null;
            foreach (var kv in _nodeRects)
                if (kv.Value.Contains(graphPt)) { hit = kv.Key; break; }

            _selectedId = hit;
            if (hit is not null) NodeSelected?.Invoke(this, hit);
            Invalidate();
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_panning)
        {
            _pan = new PointF(
                _pan.X + e.X - _lastMouse.X,
                _pan.Y + e.Y - _lastMouse.Y);
            _lastMouse = e.Location;
            Invalidate();
        }
        else
        {
            // Hover cursor hint
            var graphPt = ScreenToGraph(e.Location);
            bool over = _nodeRects.Values.Any(r => r.Contains(graphPt));
            Cursor = over ? Cursors.Hand : Cursors.SizeAll;
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _panning = false;
        Cursor   = Cursors.Hand;
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        float oldZoom = _zoom;
        _zoom = Math.Clamp(_zoom + e.Delta / 1200f, ZOOM_MIN, ZOOM_MAX);

        // Zoom toward mouse position
        _pan = new PointF(
            e.X - (e.X - _pan.X) * (_zoom / oldZoom),
            e.Y - (e.Y - _pan.Y) * (_zoom / oldZoom));
        Invalidate();
    }

    private PointF ScreenToGraph(Point screen) =>
        new((screen.X - _pan.X) / _zoom, (screen.Y - _pan.Y) / _zoom);
}
