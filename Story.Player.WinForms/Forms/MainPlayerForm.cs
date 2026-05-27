using Story.Engine;
using Story.Model;
using Story.Persistence;

namespace Story.Player.WinForms.Forms;

public class MainPlayerForm : Form
{
    // ── engine & persistence ──────────────────────────────────────────────
    private readonly GameEngine          _engine    = new();
    private readonly StoryRepository     _storyRepo = new();
    private readonly ImageRepository     _imageRepo = new();
    private readonly SaveStateRepository _saveRepo  = new();

    // ── UI controls ───────────────────────────────────────────────────────
    private MenuStrip  _menu             = null!;
    private Label      _titleLabel       = null!;
    private Label      _blockIdLabel     = null!;
    private Panel      _hudPanel         = null!;
    private PictureBox _backgroundPicture = null!;
    private RichTextBox _storyText       = null!;
    private Panel      _decisionsPanel   = null!;
    private Panel      _endPanel         = null!;
    private Label      _endLabel         = null!;

    // ── fade animation ─────────────────────────────────────────────────
    private readonly System.Windows.Forms.Timer _fadeTimer =
        new() { Interval = 14 };                    // ~70fps
    private double      _fadeAlpha   = 1.0;
    private StoryBlock? _pendingBlock;

    public MainPlayerForm()
    {
        InitializeComponent();
        _fadeTimer.Tick += FadeTick;
        WireEngineEvents();
        ShowWelcome();
    }

    // ══════════════════════════════════════════════════════════════════════
    //  UI CONSTRUCTION
    // ══════════════════════════════════════════════════════════════════════
    private void InitializeComponent()
    {
        Text           = "Story Player";
        Size           = new Size(1024, 768);
        MinimumSize    = new Size(800, 600);
        StartPosition  = FormStartPosition.CenterScreen;
        BackColor      = ThemeManager.BgPrimary;
        ForeColor      = ThemeManager.TextPrimary;
        Font           = new Font("Segoe UI", 10f);
        DoubleBuffered = true;

        BuildMenu();

        _titleLabel = new Label
        {
            Dock      = DockStyle.Top,
            Height    = 44,
            TextAlign = ContentAlignment.MiddleCenter,
            Font      = new Font("Palatino Linotype", 16f, FontStyle.Bold | FontStyle.Italic),
            ForeColor = ThemeManager.TextAccent,
            BackColor = ThemeManager.BgSecondary,
            Text      = "Story Player"
        };

        _blockIdLabel = new Label
        {
            Dock      = DockStyle.Top,
            Height    = 18,
            TextAlign = ContentAlignment.MiddleCenter,
            Font      = new Font("Consolas", 8f),
            ForeColor = ThemeManager.TextBlockId,
            BackColor = ThemeManager.BgSecondary
        };

        _hudPanel = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 50,
            BackColor = ThemeManager.BgHud,
            Padding   = new Padding(12, 7, 12, 7)
        };

        _backgroundPicture = new PictureBox
        {
            Dock      = DockStyle.Top,
            Height    = 230,
            SizeMode  = PictureBoxSizeMode.Zoom,
            BackColor = ThemeManager.BgSecondary,
            Visible   = false
        };

        _storyText = new RichTextBox
        {
            Dock        = DockStyle.Fill,
            ReadOnly    = true,
            BackColor   = ThemeManager.BgPrimary,
            ForeColor   = ThemeManager.TextPrimary,
            Font        = new Font("Palatino Linotype", 12f),
            BorderStyle = BorderStyle.None,
            ScrollBars  = RichTextBoxScrollBars.Vertical,
            WordWrap    = true,
            Padding     = new Padding(20)
        };

        _endPanel = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 50,
            BackColor = ThemeManager.BgSecondary,
            Visible   = false
        };
        _endLabel = new Label
        {
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = ThemeManager.TextAccent
        };
        _endPanel.Controls.Add(_endLabel);

        _decisionsPanel = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 180,
            BackColor = ThemeManager.BgSecondary,
            Padding   = new Padding(14, 8, 14, 8),
            AutoScroll = true
        };

        // Assemble
        Controls.Add(_storyText);
        Controls.Add(_backgroundPicture);
        Controls.Add(_hudPanel);
        Controls.Add(_blockIdLabel);
        Controls.Add(_titleLabel);
        Controls.Add(_decisionsPanel);
        Controls.Add(_endPanel);
        Controls.Add(_menu);
        MainMenuStrip = _menu;
    }

    private void BuildMenu()
    {
        _menu = new MenuStrip
        {
            BackColor  = Color.FromArgb(10, 10, 18),
            ForeColor  = Color.FromArgb(200, 195, 180),
            RenderMode = ToolStripRenderMode.Professional
        };

        ToolStripMenuItem I(string t, EventHandler h)
        {
            var it = new ToolStripMenuItem(t) { ForeColor = Color.FromArgb(200, 195, 180) };
            it.Click += h;
            return it;
        }

        var file = new ToolStripMenuItem("Fișier") { ForeColor = Color.FromArgb(200, 195, 180) };
        file.DropDownItems.Add(I("Deschide poveste...", OnOpen));
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(I("Salvează starea...", OnSave));
        file.DropDownItems.Add(I("Încarcă starea...", OnLoadSave));
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(I("Restart", (_, _) =>
        {
            if (_engine.CurrentStory is not null) _engine.Restart();
        }));
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(I("Ieșire", (_, _) => Close()));

        var view = new ToolStripMenuItem("Vizualizare") { ForeColor = Color.FromArgb(200, 195, 180) };

        // Sunet toggle
        var soundItem = new ToolStripMenuItem("🔊 Sunet activ")
        {
            ForeColor = Color.FromArgb(200, 195, 180),
            Checked   = StorySound.Enabled,
            CheckOnClick = true
        };
        soundItem.CheckedChanged += (_, _) =>
        {
            StorySound.Enabled = soundItem.Checked;
            soundItem.Text     = soundItem.Checked ? "🔊 Sunet activ" : "🔇 Sunet inactiv";
        };

        // Temă toggle
        var themeItem = new ToolStripMenuItem("☀ Temă luminoasă")
        {
            ForeColor    = Color.FromArgb(200, 195, 180),
            CheckOnClick = true
        };
        themeItem.CheckedChanged += (_, _) =>
        {
            ThemeManager.Toggle();
            themeItem.Text = ThemeManager.IsDark ? "☀ Temă luminoasă" : "🌙 Temă întunecată";
            ApplyTheme();
        };

        view.DropDownItems.Add(soundItem);
        view.DropDownItems.Add(themeItem);

        _menu.Items.Add(file);
        _menu.Items.Add(view);
        MainMenuStrip = _menu;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  THEME
    // ══════════════════════════════════════════════════════════════════════
    private void ApplyTheme()
    {
        BackColor                = ThemeManager.BgPrimary;
        _titleLabel.BackColor    = ThemeManager.BgSecondary;
        _titleLabel.ForeColor    = ThemeManager.TextAccent;
        _blockIdLabel.BackColor  = ThemeManager.BgSecondary;
        _blockIdLabel.ForeColor  = ThemeManager.TextBlockId;
        _hudPanel.BackColor      = ThemeManager.BgHud;
        _backgroundPicture.BackColor = ThemeManager.BgSecondary;
        _storyText.BackColor     = ThemeManager.BgPrimary;
        _storyText.ForeColor     = ThemeManager.TextPrimary;
        _decisionsPanel.BackColor = ThemeManager.BgSecondary;
        _endPanel.BackColor      = ThemeManager.BgSecondary;
        _endLabel.ForeColor      = ThemeManager.TextAccent;

        RefreshHud();

        // Re-render decisions with new theme
        var block = _engine.GetCurrentBlock();
        if (block is not null) RefreshDecisions();

        Invalidate(true);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  WELCOME
    // ══════════════════════════════════════════════════════════════════════
    private void ShowWelcome()
    {
        _storyText.Clear();
        A("\n\n", Color.Transparent, new Font("Segoe UI", 10f));
        A("Story Player\n\n",
            ThemeManager.TextAccent,
            new Font("Palatino Linotype", 22f, FontStyle.Bold | FontStyle.Italic),
            HorizontalAlignment.Center);
        A("Motor de povești interactive\n\n",
            ThemeManager.TextSecond,
            new Font("Palatino Linotype", 13f),
            HorizontalAlignment.Center);
        A("Fișier  →  Deschide poveste...",
            ThemeManager.TextSecond,
            new Font("Segoe UI", 11f),
            HorizontalAlignment.Center);
    }

    private void A(string text, Color color, Font font,
        HorizontalAlignment align = HorizontalAlignment.Left)
    {
        _storyText.SelectionAlignment = align;
        _storyText.SelectionFont      = font;
        _storyText.SelectionColor     = color == Color.Transparent
            ? ThemeManager.TextSecond : color;
        _storyText.AppendText(text);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  ENGINE EVENTS
    // ══════════════════════════════════════════════════════════════════════
    private void WireEngineEvents()
    {
        _engine.BlockChanged += (_, e) => SafeInvoke(() => StartFade(e.Block));
        _engine.StateChanged += (_, _) => SafeInvoke(RefreshHud);
        _engine.StoryEnded   += (_, e) => SafeInvoke(() => ShowEnd(e.FinalBlock));
    }

    // ══════════════════════════════════════════════════════════════════════
    //  FADE ANIMATION
    // ══════════════════════════════════════════════════════════════════════
    private void StartFade(StoryBlock block)
    {
        _pendingBlock      = block;
        _fadeAlpha         = 0.0;
        _storyText.ForeColor = ThemeManager.BgPrimary; // invisible
        _fadeTimer.Start();
        StorySound.PlayNavigate();
    }

    private void FadeTick(object? sender, EventArgs e)
    {
        _fadeAlpha = Math.Min(1.0, _fadeAlpha + 0.06);

        // Interpolate color from BgPrimary (hidden) to TextPrimary (visible)
        var bg   = ThemeManager.BgPrimary;
        var fg   = ThemeManager.TextPrimary;
        int r    = (int)(bg.R + (fg.R - bg.R) * _fadeAlpha);
        int g2   = (int)(bg.G + (fg.G - bg.G) * _fadeAlpha);
        int b    = (int)(bg.B + (fg.B - bg.B) * _fadeAlpha);
        _storyText.ForeColor = Color.FromArgb(
            Math.Clamp(r, 0, 255), Math.Clamp(g2, 0, 255), Math.Clamp(b, 0, 255));

        if (_fadeAlpha >= 1.0)
        {
            _fadeTimer.Stop();
            _storyText.ForeColor = ThemeManager.TextPrimary;
            if (_pendingBlock is not null)
            {
                RenderBlock(_pendingBlock);
                _pendingBlock = null;
            }
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  RENDERING
    // ══════════════════════════════════════════════════════════════════════
    private void RenderBlock(StoryBlock block)
    {
        _endPanel.Visible      = false;
        _decisionsPanel.Visible = true;
        _blockIdLabel.Text     = $"[ {block.Id} ]";

        _storyText.Clear();
        _storyText.SelectionIndent = 24;
        _storyText.SelectionFont   = new Font("Palatino Linotype", 12f);
        _storyText.SelectionColor  = ThemeManager.TextPrimary;
        _storyText.AppendText("\n" + block.Text + "\n");

        // Background image
        if (!string.IsNullOrEmpty(block.BackgroundImage))
        {
            var img = _imageRepo.GetImage(block.BackgroundImage);
            _backgroundPicture.Image   = img;
            _backgroundPicture.Visible = img is not null;
        }
        else
        {
            _backgroundPicture.Image   = null;
            _backgroundPicture.Visible = false;
        }

        RefreshHud();
        RefreshDecisions();
    }

    // ── HUD ───────────────────────────────────────────────────────────────
    private void RefreshHud()
    {
        _hudPanel.Controls.Clear();
        int x = 8;

        foreach (var (def, value) in _engine.State.GetHudProperties())
        {
            var ratio    = def.Max - def.Min > 0
                ? (value - def.Min) / (def.Max - def.Min) : 1.0;
            var barColor = ratio <= 0.25 ? Color.FromArgb(200, 65, 65)
                         : ratio <= 0.50 ? Color.FromArgb(210, 140, 40)
                         :                 Color.FromArgb(70, 175, 85);

            var card = new Panel
            {
                Location  = new Point(x, 5),
                Size      = new Size(136, 38),
                BackColor = ThemeManager.IsDark
                    ? Color.FromArgb(22, 20, 35)
                    : Color.FromArgb(210, 205, 185)
            };

            var lbl = new Label
            {
                Text      = def.HudLabel,
                Location  = new Point(4, 2),
                Size      = new Size(128, 14),
                Font      = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = ThemeManager.TextSecond,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var barBg = new Panel
            {
                Location  = new Point(4, 18),
                Size      = new Size(128, 10),
                BackColor = ThemeManager.IsDark
                    ? Color.FromArgb(35, 33, 48)
                    : Color.FromArgb(185, 180, 165)
            };

            int fillW = Math.Max(2, (int)(128 * ratio));
            var fill  = new Panel
            {
                Location  = Point.Empty,
                Size      = new Size(fillW, 10),
                BackColor = barColor
            };
            barBg.Controls.Add(fill);

            var valLbl = new Label
            {
                Text      = $"{(int)value} / {(int)def.Max}",
                Location  = new Point(4, 17),
                Size      = new Size(128, 12),
                Font      = new Font("Consolas", 7f, FontStyle.Bold),
                ForeColor = ThemeManager.IsDark ? Color.White : Color.FromArgb(40, 35, 25),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter
            };

            card.Controls.Add(barBg);
            card.Controls.Add(valLbl);
            card.Controls.Add(lbl);
            _hudPanel.Controls.Add(card);
            x += 144;
        }
    }

    // ── Decisions ─────────────────────────────────────────────────────────
    private void RefreshDecisions()
    {
        _decisionsPanel.Controls.Clear();
        var decisions = _engine.GetAvailableDecisions();

        if (decisions.Count == 0)
        {
            _decisionsPanel.Controls.Add(new Label
            {
                Text      = "Nu există decizii disponibile.",
                Location  = new Point(10, 10),
                AutoSize  = true,
                Font      = new Font("Segoe UI", 10f, FontStyle.Italic),
                ForeColor = ThemeManager.TextSecond
            });
            return;
        }

        int y = 8;
        for (int i = 0; i < decisions.Count; i++)
        {
            var btn = MakeDecisionCard(decisions[i], i + 1, y);
            _decisionsPanel.Controls.Add(btn);
            y += btn.Height + 6;
        }

        _decisionsPanel.Height = Math.Min(Math.Max(y + 14, 80), 260);
    }

    private Panel MakeDecisionCard(DecisionDefinition dec, int index, int y)
    {
        var card = new Panel
        {
            Location  = new Point(8, y),
            Size      = new Size(_decisionsPanel.ClientSize.Width - 24, 40),
            BackColor = ThemeManager.BgDecision,
            Cursor    = Cursors.Hand
        };

        var badge = new Label
        {
            Text      = index.ToString(),
            Location  = new Point(0, 0),
            Size      = new Size(34, 40),
            TextAlign = ContentAlignment.MiddleCenter,
            Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = ThemeManager.TextAccent,
            BackColor = ThemeManager.BgBadge
        };

        int textLeft = 42;
        if (!string.IsNullOrEmpty(dec.Icon))
        {
            var img = _imageRepo.GetImage(dec.Icon);
            if (img is not null)
            {
                var ico = new PictureBox
                {
                    Location  = new Point(40, 8),
                    Size      = new Size(24, 24),
                    Image     = new Bitmap(img, 24, 24),
                    SizeMode  = PictureBoxSizeMode.StretchImage,
                    BackColor = Color.Transparent
                };
                card.Controls.Add(ico);
                textLeft = 70;
            }
        }

        var txt = new Label
        {
            Text      = dec.Text,
            Location  = new Point(textLeft, 0),
            Size      = new Size(card.Width - textLeft - 28, 40),
            TextAlign = ContentAlignment.MiddleLeft,
            Font      = new Font("Segoe UI", 10.5f),
            ForeColor = ThemeManager.TextPrimary,
            BackColor = Color.Transparent,
            Cursor    = Cursors.Hand
        };

        var arrow = new Label
        {
            Text      = "›",
            Location  = new Point(card.Width - 26, 0),
            Size      = new Size(22, 40),
            TextAlign = ContentAlignment.MiddleCenter,
            Font      = new Font("Segoe UI", 15f),
            ForeColor = ThemeManager.ArrowNormal,
            BackColor = Color.Transparent
        };

        card.Controls.Add(badge);
        card.Controls.Add(txt);
        card.Controls.Add(arrow);

        void SetHover(bool on)
        {
            card.BackColor  = on ? ThemeManager.BgDecHover  : ThemeManager.BgDecision;
            arrow.ForeColor = on ? ThemeManager.ArrowHover  : ThemeManager.ArrowNormal;
            txt.ForeColor   = on ? ThemeManager.TextAccent  : ThemeManager.TextPrimary;
        }

        var captured = dec;
        void Click(object? s, EventArgs e)
        {
            StorySound.PlayDecision();
            _engine.MakeDecision(captured);
        }

        foreach (Control c in new Control[] { card, badge, txt, arrow })
        {
            c.Click      += Click;
            c.MouseEnter += (_, _) => SetHover(true);
            c.MouseLeave += (_, _) => SetHover(false);
        }

        return card;
    }

    // ── End ───────────────────────────────────────────────────────────────
    private void ShowEnd(StoryBlock block)
    {
        _blockIdLabel.Text      = $"[ {block.Id} ]  —  FINAL";
        _decisionsPanel.Visible = false;

        _storyText.Clear();
        _storyText.SelectionIndent = 24;
        _storyText.SelectionFont   = new Font("Palatino Linotype", 12f);
        _storyText.SelectionColor  = ThemeManager.TextPrimary;
        _storyText.AppendText("\n" + block.Text + "\n");

        bool goodEnd    = !block.Id.Contains("death") && !block.Id.Contains("fail");
        _endLabel.Text  = goodEnd
            ? "  Poveste terminată!   ·   Fișier → Restart"
            : "  Joc terminat   ·   Fișier → Restart";
        _endLabel.ForeColor = goodEnd
            ? ThemeManager.TextAccent
            : Color.FromArgb(200, 75, 75);
        _endPanel.Visible = true;
        RefreshHud();

        if (goodEnd) StorySound.PlayGoodEnd();
        else         StorySound.PlayBadEnd();
    }

    // ══════════════════════════════════════════════════════════════════════
    //  MENU HANDLERS
    // ══════════════════════════════════════════════════════════════════════
    private void OnOpen(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title  = "Deschide poveste",
            Filter = "Povești (*.zip)|*.zip|Toate fișierele|*.*"
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        try
        {
            var story = _storyRepo.Load(dlg.FileName);
            _imageRepo.SetZipSource(dlg.FileName);
            _engine.LoadStory(story);
            _titleLabel.Text = story.Title;
            Text             = $"Story Player  —  {story.Title}";
            _storyText.Clear();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Eroare la încărcare:\n{ex.Message}", "Eroare",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnSave(object? sender, EventArgs e)
    {
        if (_engine.CurrentStory is null) { MessageBox.Show("Nicio poveste încărcată."); return; }
        using var dlg = new SaveFileDialog
        {
            Title = "Salvează starea", Filter = "Salvare (*.save.json)|*.save.json", DefaultExt = "save.json"
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        try
        {
            _saveRepo.Save(_engine.State, dlg.FileName);
            MessageBox.Show("Starea a fost salvată.", "Salvat",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Eroare:\n{ex.Message}", "Eroare",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnLoadSave(object? sender, EventArgs e)
    {
        if (_engine.CurrentStory is null) { MessageBox.Show("Nicio poveste încărcată."); return; }
        using var dlg = new OpenFileDialog
        {
            Title = "Încarcă starea", Filter = "Salvare (*.save.json)|*.save.json"
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        try
        {
            _saveRepo.Load(_engine.State, dlg.FileName);
            var block = _engine.GetBlock(_engine.State.CurrentBlockId);
            if (block is not null) RenderBlock(block);
            RefreshHud();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Eroare:\n{ex.Message}", "Eroare",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  HELPERS
    // ══════════════════════════════════════════════════════════════════════
    private void SafeInvoke(Action action)
    {
        if (IsHandleCreated && InvokeRequired) Invoke(action);
        else action();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _fadeTimer.Dispose();
        _imageRepo.Dispose();
        base.OnFormClosed(e);
    }
}
