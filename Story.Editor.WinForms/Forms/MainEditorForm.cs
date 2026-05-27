using Story.Editor.WinForms.Dialogs;
using Story.Engine;
using Story.Model;
using Story.Persistence;

namespace Story.Editor.WinForms.Forms;

public class MainEditorForm : Form
{
    // ── state ─────────────────────────────────────────────────────────────
    private readonly EditorContext  _ctx       = new();
    private readonly StoryRepository _repo     = new();
    private readonly StoryValidator _validator = new();

    // ── main layout ───────────────────────────────────────────────────────
    private MenuStrip  _menu        = null!;
    private TreeView   _treeView    = null!;
    private TabControl _tabs        = null!;
    private Panel      _editPanel   = null!;   // inside "Editare" tab
    private GraphTabPanel _graphTab = null!;   // inside "Graf" tab
    private Label      _statusLabel = null!;
    private Label      _validLabel  = null!;
    private Label      _statsLabel  = null!;

    // ── right sidebar ─────────────────────────────────────────────────────
    private PictureBox  _previewBox = null!;
    private RichTextBox _validLog   = null!;

    // ── edit sub-panels ───────────────────────────────────────────────────
    private Panel _welcomePanel = null!;
    private Panel _metaPanel    = null!;
    private Panel _propPanel    = null!;
    private Panel _blockPanel   = null!;

    public MainEditorForm()
    {
        InitializeComponent();
        ShowWelcome();
    }

    // ══════════════════════════════════════════════════════════════════════
    //  UI CONSTRUCTION
    // ══════════════════════════════════════════════════════════════════════
    private void InitializeComponent()
    {
        Text           = "Story Editor";
        Size           = new Size(1300, 840);
        MinimumSize    = new Size(1000, 650);
        StartPosition  = FormStartPosition.CenterScreen;
        BackColor      = Color.FromArgb(24, 24, 36);
        ForeColor      = Color.FromArgb(210, 205, 190);
        Font           = new Font("Segoe UI", 9.5f);
        DoubleBuffered = true;

        BuildMenu();

        // ── Status bar ─────────────────────────────────────────────
        var statusBar = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 26,
            BackColor = Color.FromArgb(12, 12, 20)
        };
        _statusLabel = new Label
        {
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(90, 145, 195),
            Padding   = new Padding(8, 0, 0, 0),
            Font      = new Font("Segoe UI", 9f)
        };
        _validLabel = new Label
        {
            Dock      = DockStyle.Right,
            Width     = 210,
            TextAlign = ContentAlignment.MiddleRight,
            Padding   = new Padding(0, 0, 10, 0),
            Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(80, 75, 65)
        };
        _statsLabel = new Label
        {
            Dock      = DockStyle.Right,
            Width     = 200,
            TextAlign = ContentAlignment.MiddleRight,
            Padding   = new Padding(0, 0, 220, 0),
            Font      = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(70, 65, 55)
        };
        statusBar.Controls.Add(_statusLabel);
        statusBar.Controls.Add(_statsLabel);
        statusBar.Controls.Add(_validLabel);

        // ── Main split: left tree | right area ─────────────────────
        var mainSplit = new SplitContainer
        {
            Dock             = DockStyle.Fill,
            Orientation      = Orientation.Vertical,
            SplitterDistance = 220,
            BackColor        = Color.FromArgb(24, 24, 36),
            SplitterWidth    = 3
        };
        BuildLeftPanel(mainSplit.Panel1);

        // ── Right split: TabControl | sidebar ──────────────────────
        var rightSplit = new SplitContainer
        {
            Dock             = DockStyle.Fill,
            Orientation      = Orientation.Vertical,
            SplitterDistance = 570,
            BackColor        = Color.FromArgb(24, 24, 36),
            SplitterWidth    = 3
        };
        BuildTabControl(rightSplit.Panel1);
        BuildSidebar(rightSplit.Panel2);
        mainSplit.Panel2.Controls.Add(rightSplit);

        Controls.Add(mainSplit);
        Controls.Add(statusBar);
        Controls.Add(_menu);
        MainMenuStrip = _menu;

        BuildEditPanels();
    }

    // ── Left: tree + toolbar ──────────────────────────────────────────────
    private void BuildLeftPanel(Panel host)
    {
        _treeView = new TreeView
        {
            Dock          = DockStyle.Fill,
            BackColor     = Color.FromArgb(14, 14, 22),
            ForeColor     = Color.FromArgb(200, 195, 180),
            BorderStyle   = BorderStyle.None,
            Font          = new Font("Segoe UI", 9.5f),
            ItemHeight    = 22,
            Indent        = 14,
            ShowLines     = true,
            HideSelection = false
        };
        _treeView.AfterSelect += OnTreeSelect;

        var toolbar = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 36,
            BackColor = Color.FromArgb(12, 12, 20),
            Padding   = new Padding(6, 5, 6, 5)
        };
        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        var bBlock = SmBtn("+ Bloc", Color.FromArgb(38, 62, 40));
        var bProp  = SmBtn("+ Prop", Color.FromArgb(38, 44, 75));
        var bDel   = SmBtn("✕",     Color.FromArgb(72, 38, 38));
        bBlock.Click += OnAddBlock;
        bProp.Click  += OnAddProperty;
        bDel.Click   += OnDeleteNode;
        flow.Controls.Add(bBlock);
        flow.Controls.Add(bProp);
        flow.Controls.Add(bDel);
        toolbar.Controls.Add(flow);

        host.Controls.Add(_treeView);
        host.Controls.Add(toolbar);
    }

    // ── TabControl: "Editare" + "Graf" ────────────────────────────────────
    private void BuildTabControl(Panel host)
    {
        _tabs = new TabControl
        {
            Dock      = DockStyle.Fill,
            Font      = new Font("Segoe UI", 9.5f),
            Appearance = TabAppearance.Normal
        };

        // Tab 1: Editare
        var tabEdit = new TabPage("  ✎  Editare  ")
        {
            BackColor = Color.FromArgb(24, 24, 36),
            ForeColor = Color.FromArgb(210, 205, 190),
            Padding   = new Padding(0)
        };
        _editPanel = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.FromArgb(24, 24, 36),
            Padding   = new Padding(10, 8, 10, 8)
        };
        tabEdit.Controls.Add(_editPanel);

        // Tab 2: Graf
        var tabGraph = new TabPage("  ◈  Graf  ")
        {
            BackColor = Color.FromArgb(12, 12, 20),
            ForeColor = Color.FromArgb(210, 205, 190),
            Padding   = new Padding(0)
        };
        _graphTab = new GraphTabPanel();
        _graphTab.Graph.NodeSelected += OnGraphNodeSelected;
        tabGraph.Controls.Add(_graphTab);

        _tabs.TabPages.Add(tabEdit);
        _tabs.TabPages.Add(tabGraph);
        _tabs.SelectedIndexChanged += (_, _) =>
        {
            if (_tabs.SelectedIndex == 1 && _ctx.Story.Blocks.Count > 0)
                _graphTab.Reload(_ctx.Story);
        };

        host.Controls.Add(_tabs);
    }

    private void OnGraphNodeSelected(object? sender, string blockId)
    {
        // Sincronizează selecția în TreeView și deschide panoul de editare
        var block = _ctx.Story.Blocks.FirstOrDefault(b => b.Id == blockId);
        if (block is null) return;

        // Caută nodul în tree
        foreach (TreeNode root in _treeView.Nodes)
            foreach (TreeNode child in root.Nodes)
                foreach (TreeNode leaf in child.Nodes)
                    if (leaf.Tag is StoryBlock b && b.Id == blockId)
                    {
                        _treeView.SelectedNode = leaf;
                        break;
                    }

        _tabs.SelectedIndex = 0; // Trece la tab editare
        ShowBlockPanel(block);
    }

    // ── Sidebar: preview + validation ─────────────────────────────────────
    private void BuildSidebar(Panel host)
    {
        host.BackColor = Color.FromArgb(16, 16, 26);
        host.Padding   = new Padding(6);

        var lbl1 = SideLabel("Preview imagine");
        _previewBox = new PictureBox
        {
            Dock      = DockStyle.Top,
            Height    = 175,
            SizeMode  = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(10, 10, 18)
        };

        var lbl2 = SideLabel("Jurnal validare");
        var bVal = SmBtn("Validează", Color.FromArgb(38, 62, 40));
        bVal.Dock   = DockStyle.Top;
        bVal.Height = 26;
        bVal.Click += OnValidate;

        _validLog = new RichTextBox
        {
            Dock        = DockStyle.Fill,
            ReadOnly    = true,
            BackColor   = Color.FromArgb(10, 10, 18),
            ForeColor   = Color.FromArgb(150, 145, 130),
            BorderStyle = BorderStyle.None,
            Font        = new Font("Consolas", 8.5f),
            ScrollBars  = RichTextBoxScrollBars.Vertical
        };

        host.Controls.Add(_validLog);
        host.Controls.Add(bVal);
        host.Controls.Add(lbl2);
        host.Controls.Add(_previewBox);
        host.Controls.Add(lbl1);
    }

    private void BuildMenu()
    {
        _menu = new MenuStrip
        {
            BackColor  = Color.FromArgb(12, 12, 20),
            ForeColor  = Color.FromArgb(200, 195, 180),
            RenderMode = ToolStripRenderMode.Professional
        };

        ToolStripMenuItem M(string t) =>
            new(t) { ForeColor = Color.FromArgb(200, 195, 180) };
        ToolStripMenuItem I(string t, EventHandler h)
        {
            var it = new ToolStripMenuItem(t) { ForeColor = Color.FromArgb(200, 195, 180) };
            it.Click += h;
            return it;
        }

        var file = M("Fișier");
        file.DropDownItems.Add(I("Poveste nouă",   OnNew));
        file.DropDownItems.Add(I("Deschide...",    OnOpen));
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(I("Salvează",       OnSave));
        file.DropDownItems.Add(I("Salvează ca...", OnSaveAs));
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(I("Ieșire",         (_, _) => Close()));

        var story = M("Poveste");
        story.DropDownItems.Add(I("Validare",       OnValidate));
        story.DropDownItems.Add(I("+ Adaugă bloc",  OnAddBlock));
        story.DropDownItems.Add(I("+ Adaugă prop.", OnAddProperty));
        story.DropDownItems.Add(new ToolStripSeparator());
        story.DropDownItems.Add(I("◈ Afișează graf", (_, _) =>
        {
            _tabs.SelectedIndex = 1;
            _graphTab.Load(_ctx.Story);
        }));

        _menu.Items.Add(file);
        _menu.Items.Add(story);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  EDIT PANELS
    // ══════════════════════════════════════════════════════════════════════
    private void BuildEditPanels()
    {
        _welcomePanel = MakeWelcomePanel();
        _metaPanel    = MakeMetaPanel();
        _propPanel    = new Panel { Dock = DockStyle.Fill, Visible = false, BackColor = Color.FromArgb(24, 24, 36) };
        _blockPanel   = new Panel { Dock = DockStyle.Fill, Visible = false, BackColor = Color.FromArgb(24, 24, 36) };

        _editPanel.Controls.Add(_welcomePanel);
        _editPanel.Controls.Add(_metaPanel);
        _editPanel.Controls.Add(_propPanel);
        _editPanel.Controls.Add(_blockPanel);
    }

    private Panel MakeWelcomePanel()
    {
        var p   = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(24, 24, 36) };
        var rtb = new RichTextBox
        {
            Dock        = DockStyle.Fill,
            ReadOnly    = true,
            BackColor   = Color.FromArgb(24, 24, 36),
            BorderStyle = BorderStyle.None,
            Font        = new Font("Palatino Linotype", 13f)
        };

        void A(string text, Color color, Font font,
               HorizontalAlignment align = HorizontalAlignment.Center)
        {
            rtb.SelectionAlignment = align;
            rtb.SelectionFont      = font;
            rtb.SelectionColor     = color;
            rtb.AppendText(text);
        }

        A("\n\n", Color.White, new Font("Segoe UI", 10f));
        A("Story Editor\n\n",
            Color.FromArgb(255, 215, 100),
            new Font("Palatino Linotype", 22f, FontStyle.Bold | FontStyle.Italic));
        A("Creează sau deschide o poveste.\n\n",
            Color.FromArgb(130, 125, 110),
            new Font("Segoe UI", 11f));
        A("Fișier → Poveste nouă\nFișier → Deschide...",
            Color.FromArgb(80, 75, 65),
            new Font("Segoe UI", 10f));

        p.Controls.Add(rtb);
        return p;
    }

    // ── Meta panel ────────────────────────────────────────────────────────
    private Panel MakeMetaPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Visible = false, BackColor = Color.FromArgb(24, 24, 36) };

        var header = SectionHeader("Metadate poveste");
        var layout = new TableLayoutPanel
        {
            Dock      = DockStyle.Top,
            AutoSize  = true,
            ColumnCount = 2,
            Padding   = new Padding(0, 6, 0, 12)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var tTitle   = Txt(); var tAuthor  = Txt();
        var tDesc    = Txt(); var tStart   = Txt();
        var tVersion = Txt();

        Row(layout, "Titlu:",       tTitle);
        Row(layout, "Autor:",       tAuthor);
        Row(layout, "Descriere:",   tDesc);
        Row(layout, "Bloc start:",  tStart);
        Row(layout, "Versiune:",    tVersion);

        var btnApply = Btn("Aplică modificările", Color.FromArgb(38, 62, 40));
        btnApply.Dock   = DockStyle.Top;
        btnApply.Height = 30;
        btnApply.Click += (_, _) =>
        {
            _ctx.Story.Title       = tTitle.Text.Trim();
            _ctx.Story.Author      = tAuthor.Text.Trim();
            _ctx.Story.Description = tDesc.Text.Trim();
            _ctx.Story.StartBlock  = tStart.Text.Trim();
            _ctx.Story.Version     = tVersion.Text.Trim();
            _ctx.MarkDirty();
            UpdateTitle(); RebuildTree(); LiveValidate();
            RefreshGraph();
            Status("Metadate actualizate.");
        };

        panel.Tag = new[] { tTitle, tAuthor, tDesc, tStart, tVersion };
        panel.Controls.Add(btnApply);
        panel.Controls.Add(layout);
        panel.Controls.Add(header);
        return panel;
    }

    private void ShowMeta()
    {
        HideAll();
        if (_metaPanel.Tag is TextBox[] boxes)
        {
            boxes[0].Text = _ctx.Story.Title;
            boxes[1].Text = _ctx.Story.Author;
            boxes[2].Text = _ctx.Story.Description;
            boxes[3].Text = _ctx.Story.StartBlock;
            boxes[4].Text = _ctx.Story.Version;
        }
        _metaPanel.Visible = true;
        _previewBox.Image  = null;
    }

    // ── Property panel ────────────────────────────────────────────────────
    private void ShowPropertyPanel(StatePropertyDefinition prop)
    {
        HideAll();
        _propPanel.Controls.Clear();

        var header = SectionHeader($"Proprietate: {prop.Key}");
        var layout = new TableLayoutPanel
        {
            Dock      = DockStyle.Top,
            AutoSize  = true,
            ColumnCount = 2,
            Padding   = new Padding(0, 6, 0, 12)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var tKey   = Txt(prop.Key);
        var tLabel = Txt(prop.HudLabel);
        var tMin   = Txt(prop.Min.ToString());
        var tMax   = Txt(prop.Max.ToString());
        var tInit  = Txt(prop.Initial.ToString());
        var chkVis = new CheckBox
        {
            Checked   = prop.VisibleInHud,
            Text      = "vizibil în HUD",
            ForeColor = Color.FromArgb(200, 195, 180),
            BackColor = Color.Transparent
        };
        var tOrder = Txt(prop.HudOrder.ToString());
        var tOnMin = Txt(prop.OnMinBlock ?? "");
        var tOnMax = Txt(prop.OnMaxBlock ?? "");

        Row(layout, "Cheie:",            tKey);
        Row(layout, "Etichetă HUD:",     tLabel);
        Row(layout, "Min:",              tMin);
        Row(layout, "Max:",              tMax);
        Row(layout, "Valoare inițială:", tInit);
        Row(layout, "Vizibil HUD:",      chkVis);
        Row(layout, "Ordine HUD:",       tOrder);
        Row(layout, "Bloc la Min:",      tOnMin);
        Row(layout, "Bloc la Max:",      tOnMax);

        var btnApply = Btn("Aplică", Color.FromArgb(38, 62, 40));
        btnApply.Dock   = DockStyle.Top;
        btnApply.Height = 30;
        btnApply.Click += (_, _) =>
        {
            prop.Key         = tKey.Text.Trim();
            prop.HudLabel    = tLabel.Text.Trim();
            if (double.TryParse(tMin.Text,   out var mn))  prop.Min      = mn;
            if (double.TryParse(tMax.Text,   out var mx))  prop.Max      = mx;
            if (double.TryParse(tInit.Text,  out var ini)) prop.Initial  = ini;
            prop.VisibleInHud = chkVis.Checked;
            if (int.TryParse(tOrder.Text,    out var ord)) prop.HudOrder = ord;
            prop.OnMinBlock  = string.IsNullOrWhiteSpace(tOnMin.Text) ? null : tOnMin.Text.Trim();
            prop.OnMaxBlock  = string.IsNullOrWhiteSpace(tOnMax.Text) ? null : tOnMax.Text.Trim();
            _ctx.MarkDirty();
            RebuildTree(); LiveValidate();
            Status($"Proprietatea '{prop.Key}' actualizată.");
        };

        _propPanel.Controls.Add(btnApply);
        _propPanel.Controls.Add(layout);
        _propPanel.Controls.Add(header);
        _propPanel.Visible = true;
        _previewBox.Image  = null;
    }

    // ── Block panel ───────────────────────────────────────────────────────
    private void ShowBlockPanel(StoryBlock block)
    {
        HideAll();
        _blockPanel.Controls.Clear();

        var header = SectionHeader($"Bloc: {block.Id}");
        var topLayout = new TableLayoutPanel
        {
            Dock      = DockStyle.Top,
            AutoSize  = true,
            ColumnCount = 2,
            Padding   = new Padding(0, 6, 0, 6)
        };
        topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var tId    = Txt(block.Id);
        var chkFin = new CheckBox
        {
            Checked   = block.IsFinal,
            Text      = "Bloc final (ending)",
            ForeColor = Color.FromArgb(200, 195, 180),
            BackColor = Color.Transparent
        };

        // Background image row
        var bgRow = new Panel { Dock = DockStyle.Fill, Height = 26 };
        var tBg   = Txt(block.BackgroundImage ?? "");
        var btnBg = SmBtn("...", Color.FromArgb(48, 48, 72));
        tBg.Dock  = DockStyle.Fill;
        btnBg.Dock  = DockStyle.Right;
        btnBg.Width = 32;
        btnBg.Click += (_, _) =>
        {
            using var dlg = new OpenFileDialog { Filter = "Imagini|*.jpg;*.jpeg;*.png;*.bmp" };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            tBg.Text = CopyImage(dlg.FileName);
        };
        tBg.TextChanged += (_, _) => UpdatePreview(tBg.Text.Trim());
        bgRow.Controls.Add(tBg);
        bgRow.Controls.Add(btnBg);

        var tText = new RichTextBox
        {
            Text        = block.Text,
            BackColor   = Color.FromArgb(30, 28, 45),
            ForeColor   = Color.FromArgb(210, 205, 190),
            BorderStyle = BorderStyle.FixedSingle,
            Font        = new Font("Palatino Linotype", 11f),
            Height      = 108,
            Dock        = DockStyle.Fill,
            WordWrap    = true,
            ScrollBars  = RichTextBoxScrollBars.Vertical
        };

        Row(topLayout, "ID bloc:",        tId);
        Row(topLayout, "",                chkFin);
        Row(topLayout, "Imagine fundal:", bgRow);
        topLayout.Controls.Add(Lbl("Text narativ:"));
        topLayout.Controls.Add(tText);
        topLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 116));

        var btnApply = Btn("Aplică bloc", Color.FromArgb(38, 62, 40));
        btnApply.Dock   = DockStyle.Top;
        btnApply.Height = 30;
        btnApply.Click += (_, _) =>
        {
            block.Id              = tId.Text.Trim();
            block.IsFinal         = chkFin.Checked;
            block.BackgroundImage = string.IsNullOrWhiteSpace(tBg.Text) ? null : tBg.Text.Trim();
            block.Text            = tText.Text;
            _ctx.MarkDirty();
            RebuildTree(); LiveValidate(); RefreshGraph();
            Status($"Blocul '{block.Id}' actualizat.");
        };

        // Decisions section
        var decHeader = new Label
        {
            Text      = "Decizii:",
            Dock      = DockStyle.Top,
            Height    = 24,
            ForeColor = Color.FromArgb(255, 215, 100),
            Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Padding   = new Padding(0, 6, 0, 0)
        };
        var decBar = new FlowLayoutPanel
        {
            Dock      = DockStyle.Top,
            Height    = 30,
            BackColor = Color.FromArgb(14, 14, 22),
            Padding   = new Padding(4, 3, 4, 3)
        };
        var bAdd  = SmBtn("+ Adaugă",   Color.FromArgb(38, 62, 40));
        var bEdit = SmBtn("✎ Editează", Color.FromArgb(38, 50, 75));
        var bDel  = SmBtn("✕ Șterge",  Color.FromArgb(72, 38, 38));
        var bUp   = SmBtn("↑",          Color.FromArgb(48, 48, 68));
        var bDown = SmBtn("↓",          Color.FromArgb(48, 48, 68));
        decBar.Controls.AddRange(new Control[] { bAdd, bEdit, bDel, bUp, bDown });

        var grid = BuildDecGrid();
        RefreshDecGrid(grid, block);

        bAdd.Click += (_, _) =>
        {
            var d = new DecisionDefinition
            {
                Text        = "Nouă decizie",
                TargetBlock = _ctx.Story.StartBlock
            };
            using var dlg = new DecisionDialog(d, _ctx);
            if (dlg.ShowDialog() != DialogResult.OK) return;
            block.Decisions.Add(dlg.Result);
            _ctx.MarkDirty(); RefreshDecGrid(grid, block);
            LiveValidate(); RefreshGraph();
        };

        bEdit.Click += (_, _) =>
        {
            if (grid.SelectedRows.Count == 0) return;
            var d = (DecisionDefinition)grid.SelectedRows[0].Tag!;
            using var dlg = new DecisionDialog(d, _ctx);
            if (dlg.ShowDialog() != DialogResult.OK) return;
            block.Decisions[block.Decisions.IndexOf(d)] = dlg.Result;
            _ctx.MarkDirty(); RefreshDecGrid(grid, block);
            LiveValidate(); RefreshGraph();
        };

        grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex < 0) return;
            var d = (DecisionDefinition)grid.Rows[e.RowIndex].Tag!;
            using var dlg = new DecisionDialog(d, _ctx);
            if (dlg.ShowDialog() != DialogResult.OK) return;
            block.Decisions[block.Decisions.IndexOf(d)] = dlg.Result;
            _ctx.MarkDirty(); RefreshDecGrid(grid, block);
            LiveValidate(); RefreshGraph();
        };

        bDel.Click += (_, _) =>
        {
            if (grid.SelectedRows.Count == 0) return;
            var d = (DecisionDefinition)grid.SelectedRows[0].Tag!;
            if (MessageBox.Show($"Ștergi decizia '{d.Text}'?", "Confirmare",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            block.Decisions.Remove(d);
            _ctx.MarkDirty(); RefreshDecGrid(grid, block);
            LiveValidate(); RefreshGraph();
        };

        bUp.Click += (_, _) =>
        {
            if (grid.SelectedRows.Count == 0) return;
            var d   = (DecisionDefinition)grid.SelectedRows[0].Tag!;
            int idx = block.Decisions.IndexOf(d);
            if (idx <= 0) return;
            block.Decisions.RemoveAt(idx);
            block.Decisions.Insert(idx - 1, d);
            _ctx.MarkDirty(); RefreshDecGrid(grid, block);
            if (idx - 1 < grid.Rows.Count) grid.Rows[idx - 1].Selected = true;
        };

        bDown.Click += (_, _) =>
        {
            if (grid.SelectedRows.Count == 0) return;
            var d   = (DecisionDefinition)grid.SelectedRows[0].Tag!;
            int idx = block.Decisions.IndexOf(d);
            if (idx >= block.Decisions.Count - 1) return;
            block.Decisions.RemoveAt(idx);
            block.Decisions.Insert(idx + 1, d);
            _ctx.MarkDirty(); RefreshDecGrid(grid, block);
            if (idx + 1 < grid.Rows.Count) grid.Rows[idx + 1].Selected = true;
        };

        _blockPanel.Controls.Add(grid);
        _blockPanel.Controls.Add(decBar);
        _blockPanel.Controls.Add(decHeader);
        _blockPanel.Controls.Add(btnApply);
        _blockPanel.Controls.Add(topLayout);
        _blockPanel.Controls.Add(header);
        _blockPanel.Visible = true;

        UpdatePreview(block.BackgroundImage);
        _graphTab.Graph.SelectNode(block.Id);
    }

    // ── Decisions grid ────────────────────────────────────────────────────
    private DataGridView BuildDecGrid()
    {
        var g = new DataGridView
        {
            Dock                = DockStyle.Fill,
            BackgroundColor     = Color.FromArgb(14, 14, 22),
            ForeColor           = Color.FromArgb(200, 195, 180),
            GridColor           = Color.FromArgb(38, 36, 52),
            BorderStyle         = BorderStyle.None,
            RowHeadersVisible   = false,
            AllowUserToAddRows  = false,
            ReadOnly            = true,
            SelectionMode       = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowTemplate         = { Height = 26 }
        };
        g.DefaultCellStyle.BackColor            = Color.FromArgb(20, 18, 32);
        g.DefaultCellStyle.ForeColor            = Color.FromArgb(200, 195, 180);
        g.DefaultCellStyle.SelectionBackColor   = Color.FromArgb(52, 50, 78);
        g.DefaultCellStyle.SelectionForeColor   = Color.White;
        g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(24, 22, 38);
        g.ColumnHeadersDefaultCellStyle.BackColor   = Color.FromArgb(12, 12, 20);
        g.ColumnHeadersDefaultCellStyle.ForeColor   = Color.FromArgb(255, 215, 100);
        g.ColumnHeadersDefaultCellStyle.Font        = new Font("Segoe UI", 9f, FontStyle.Bold);
        g.EnableHeadersVisualStyles = false;
        g.ColumnHeadersHeight       = 24;

        g.Columns.Add(new DataGridViewTextBoxColumn { Name = "Nr",   HeaderText = "#",         Width = 26, FillWeight = 4 });
        g.Columns.Add(new DataGridViewTextBoxColumn { Name = "Text", HeaderText = "Text",       FillWeight = 40 });
        g.Columns.Add(new DataGridViewTextBoxColumn { Name = "To",   HeaderText = "Destinație", FillWeight = 22 });
        g.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cond", HeaderText = "Condiție",   FillWeight = 18 });
        g.Columns.Add(new DataGridViewTextBoxColumn { Name = "Eff",  HeaderText = "Efecte",     FillWeight = 16 });
        return g;
    }

    private static void RefreshDecGrid(DataGridView g, StoryBlock block)
    {
        g.Rows.Clear();
        int i = 1;
        foreach (var d in block.Decisions)
        {
            var r = g.Rows[g.Rows.Add()];
            r.Tag = d;
            r.Cells["Nr"].Value   = i++;
            r.Cells["Text"].Value = d.Text;
            r.Cells["To"].Value   = d.TargetBlock;
            r.Cells["Cond"].Value = d.Condition is null ? "—" : CondStr(d.Condition);
            r.Cells["Eff"].Value  = d.Effects.Count == 0 ? "—"
                : string.Join(", ", d.Effects.Select(e =>
                    $"{e.Property}{(e.Type == EffectType.ADD ? (e.Value >= 0 ? "+" : "") : "=")}{e.Value}"));
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  TREE
    // ══════════════════════════════════════════════════════════════════════
    private void RebuildTree()
    {
        _treeView.BeginUpdate();

        var expanded = new HashSet<string>();
        void Collect(TreeNode n)
        {
            if (n.IsExpanded) expanded.Add(n.Text);
            foreach (TreeNode c in n.Nodes) Collect(c);
        }
        foreach (TreeNode n in _treeView.Nodes) Collect(n);

        _treeView.Nodes.Clear();

        var root = new TreeNode($"  {_ctx.Story.Title}")
        {
            Tag = "META",
            ForeColor = Color.FromArgb(255, 215, 100)
        };

        var pNode = new TreeNode("  Proprietăți")
        {
            Tag = "PROPS",
            ForeColor = Color.FromArgb(140, 175, 255)
        };
        foreach (var p in _ctx.Story.Properties)
            pNode.Nodes.Add(new TreeNode($"    {(p.VisibleInHud ? "◉" : "○")}  {p.Key}")
            {
                Tag = p, ForeColor = Color.FromArgb(175, 170, 155)
            });

        var bNode = new TreeNode($"  Blocuri  ({_ctx.Story.Blocks.Count})")
        {
            Tag = "BLOCKS",
            ForeColor = Color.FromArgb(140, 215, 140)
        };
        foreach (var b in _ctx.Story.Blocks)
        {
            var icon = b.IsFinal ? "🏁" : b.Id == _ctx.Story.StartBlock ? "▶" : "▷";
            var clr  = b.IsFinal ? Color.FromArgb(195, 95, 95)
                     : b.Id == _ctx.Story.StartBlock ? Color.FromArgb(255, 215, 100)
                     : Color.FromArgb(175, 170, 155);
            bNode.Nodes.Add(new TreeNode($"    {icon}  {b.Id}")
            {
                Tag = b, ForeColor = clr
            });
        }

        root.Nodes.Add(pNode);
        root.Nodes.Add(bNode);
        _treeView.Nodes.Add(root);

        void Restore(TreeNode n)
        {
            if (expanded.Contains(n.Text)) n.Expand();
            foreach (TreeNode c in n.Nodes) Restore(c);
        }
        foreach (TreeNode n in _treeView.Nodes) Restore(n);
        if (!expanded.Any()) { root.Expand(); pNode.Expand(); bNode.Expand(); }

        _treeView.EndUpdate();
        UpdateStats();
    }

    private void OnTreeSelect(object? s, TreeViewEventArgs e)
    {
        switch (e.Node?.Tag)
        {
            case "META":                        ShowMeta();              break;
            case StatePropertyDefinition prop:  ShowPropertyPanel(prop); break;
            case StoryBlock block:
                ShowBlockPanel(block);
                _graphTab.Graph.SelectNode(block.Id);
                break;
            default:
                HideAll();
                _welcomePanel.Visible = true;
                break;
        }
    }

    private void ShowWelcome()  { HideAll(); _welcomePanel.Visible = true; }
    private void HideAll()
    {
        _welcomePanel.Visible = false;
        _metaPanel.Visible    = false;
        _propPanel.Visible    = false;
        _blockPanel.Visible   = false;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  GRAPH refresh helper
    // ══════════════════════════════════════════════════════════════════════
    private void RefreshGraph()
    {
        if (_tabs.SelectedIndex == 1)
            _graphTab.Reload(_ctx.Story);
        else
            _graphTab.Reload(_ctx.Story); // refresh in background too
    }

    // ══════════════════════════════════════════════════════════════════════
    //  LIVE VALIDATION
    // ══════════════════════════════════════════════════════════════════════
    private void LiveValidate()
    {
        if (_ctx.Story.Blocks.Count == 0) return;
        var resources = _ctx.WorkingDirectory is not null
            ? _repo.GetAllFilePaths(_ctx.WorkingDirectory)
            : null;
        var r = _validator.Validate(_ctx.Story, resources);
        _validLog.Clear();

        if (r.IsValid && r.Warnings.Count == 0)
        {
            _validLog.SelectionColor = Color.FromArgb(75, 175, 85);
            _validLog.AppendText("Povestea este validă.\n");
            _validLabel.Text      = "✔ Valid";
            _validLabel.ForeColor = Color.FromArgb(75, 175, 85);
        }
        else
        {
            foreach (var err in r.Errors)
            {
                _validLog.SelectionColor = Color.FromArgb(215, 75, 75);
                _validLog.AppendText($"EROARE: {err}\n");
            }
            foreach (var w in r.Warnings)
            {
                _validLog.SelectionColor = Color.FromArgb(215, 160, 45);
                _validLog.AppendText($"AVERT: {w}\n");
            }
            _validLabel.Text      = r.IsValid
                ? $"⚠ {r.Warnings.Count} avert."
                : $"✕ {r.Errors.Count} erori";
            _validLabel.ForeColor = r.IsValid
                ? Color.FromArgb(215, 160, 45)
                : Color.FromArgb(215, 75, 75);
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  IMAGE PREVIEW
    // ══════════════════════════════════════════════════════════════════════
    private void UpdatePreview(string? rel)
    {
        if (string.IsNullOrEmpty(rel) || _ctx.WorkingDirectory is null)
        {
            _previewBox.Image = null;
            return;
        }
        var full = Path.Combine(_ctx.WorkingDirectory,
            rel.Replace('/', Path.DirectorySeparatorChar));
        try   { _previewBox.Image = File.Exists(full) ? Image.FromFile(full) : null; }
        catch { _previewBox.Image = null; }
    }

    private string CopyImage(string src)
    {
        var dir  = Path.Combine(_ctx.WorkingDirectory ?? "", "images");
        Directory.CreateDirectory(dir);
        var dest = Path.Combine(dir, Path.GetFileName(src));
        File.Copy(src, dest, true);
        return "images/" + Path.GetFileName(src);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  ADD / DELETE
    // ══════════════════════════════════════════════════════════════════════
    private void OnAddBlock(object? s, EventArgs e)
    {
        var id = InputBoxHelper.Show("ID bloc nou (ex: forest.clearing):", "Bloc nou", "bloc.nou");
        if (string.IsNullOrWhiteSpace(id)) return;
        if (_ctx.Story.Blocks.Any(b => b.Id == id))
        {
            MessageBox.Show($"Blocul '{id}' există deja.", "Eroare",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        _ctx.Story.Blocks.Add(new StoryBlock { Id = id, Text = "Text narativ..." });
        _ctx.MarkDirty();
        RebuildTree(); LiveValidate(); RefreshGraph();
        Status($"Bloc '{id}' adăugat.");
    }

    private void OnAddProperty(object? s, EventArgs e)
    {
        var key = InputBoxHelper.Show("Cheie proprietate (ex: player.life):",
            "Proprietate nouă", "player.noua");
        if (string.IsNullOrWhiteSpace(key)) return;
        if (_ctx.Story.Properties.Any(p => p.Key == key))
        {
            MessageBox.Show($"Proprietatea '{key}' există deja.", "Eroare",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        _ctx.Story.Properties.Add(new StatePropertyDefinition
        {
            Key = key, HudLabel = key,
            Min = 0, Max = 100, Initial = 50,
            VisibleInHud = true,
            HudOrder     = _ctx.Story.Properties.Count + 1
        });
        _ctx.MarkDirty(); RebuildTree(); LiveValidate();
        Status($"Proprietatea '{key}' adăugată.");
    }

    private void OnDeleteNode(object? s, EventArgs e)
    {
        switch (_treeView.SelectedNode?.Tag)
        {
            case StoryBlock block:
                if (MessageBox.Show($"Ștergi blocul '{block.Id}'?", "Confirmare",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                _ctx.Story.Blocks.Remove(block);
                _ctx.MarkDirty(); ShowWelcome(); RebuildTree();
                LiveValidate(); RefreshGraph();
                break;
            case StatePropertyDefinition prop:
                if (MessageBox.Show($"Ștergi proprietatea '{prop.Key}'?", "Confirmare",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                _ctx.Story.Properties.Remove(prop);
                _ctx.MarkDirty(); ShowWelcome(); RebuildTree(); LiveValidate();
                break;
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  FILE MENU
    // ══════════════════════════════════════════════════════════════════════
    private void OnNew(object? s, EventArgs e)
    {
        if (!ConfirmDiscard()) return;
        using var dlg = new NewStoryDialog();
        if (dlg.ShowDialog() != DialogResult.OK) return;
        _ctx.NewStory(dlg.StoryTitle, dlg.StartBlock);
        _ctx.Story.Author      = dlg.Author;
        _ctx.Story.Description = dlg.Description;
        _ctx.Story.Blocks.Add(new StoryBlock
        {
            Id = dlg.StartBlock, Text = "Începutul poveștii..."
        });
        RebuildTree(); UpdateTitle(); ShowMeta();
        LiveValidate(); RefreshGraph();
        Status("Poveste nouă creată.");
    }

    private void OnOpen(object? s, EventArgs e)
    {
        if (!ConfirmDiscard()) return;
        using var dlg = new OpenFileDialog
        {
            Title  = "Deschide poveste",
            Filter = "Povești (*.zip)|*.zip"
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        try
        {
            var tmp = _repo.ExtractToTemp(dlg.FileName);
            _ctx.Story            = _repo.LoadFromDirectory(tmp);
            _ctx.WorkingDirectory = tmp;
            _ctx.OriginalZipPath  = dlg.FileName;
            _ctx.MarkClean();
            RebuildTree(); UpdateTitle(); ShowMeta();
            LiveValidate(); RefreshGraph();
            Status($"Deschis: {Path.GetFileName(dlg.FileName)}");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Eroare:\n{ex.Message}", "Eroare",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnSave(object? s, EventArgs e)
    {
        if (_ctx.OriginalZipPath is null) { OnSaveAs(s, e); return; }
        DoSave(_ctx.OriginalZipPath);
    }

    private void OnSaveAs(object? s, EventArgs e)
    {
        using var dlg = new SaveFileDialog
        {
            Title = "Salvează poveste", Filter = "Povești (*.zip)|*.zip", DefaultExt = "zip"
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        _ctx.OriginalZipPath = dlg.FileName;
        DoSave(dlg.FileName);
    }

    private void DoSave(string path)
    {
        try
        {
            _repo.SaveToDirectory(_ctx.Story, _ctx.WorkingDirectory!);
            _repo.PackFromDirectory(_ctx.WorkingDirectory!, path);
            _ctx.MarkClean(); UpdateTitle();
            Status($"Salvat: {Path.GetFileName(path)}");
            MessageBox.Show("Poveste salvată cu succes!", "Salvat",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Eroare:\n{ex.Message}", "Eroare",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnValidate(object? s, EventArgs e)
    {
        LiveValidate();
        var res = _ctx.WorkingDirectory is not null
            ? _repo.GetAllFilePaths(_ctx.WorkingDirectory) : null;
        var r   = _validator.Validate(_ctx.Story, res);
        var msg = r.IsValid
            ? $"Povestea este validă!\n\nBlocuri: {_ctx.Story.Blocks.Count}" +
              $"\nProprietăți: {_ctx.Story.Properties.Count}" +
              (r.Warnings.Count > 0
                  ? "\n\nAvertismente:\n" + string.Join("\n", r.Warnings.Select(w => $"  {w}"))
                  : "")
            : r.ToString();
        MessageBox.Show(msg, "Rezultat validare", MessageBoxButtons.OK,
            r.IsValid ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  HELPERS
    // ══════════════════════════════════════════════════════════════════════
    private bool ConfirmDiscard()
    {
        if (!_ctx.IsDirty) return true;
        return MessageBox.Show("Există modificări nesalvate. Continui?", "Confirmare",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
    }

    private void UpdateTitle() =>
        Text = $"Story Editor  —  {_ctx.Story.Title}{(_ctx.IsDirty ? "  *" : "")}";

    private void Status(string msg) => _statusLabel.Text = $"  {msg}";

    private void UpdateStats()
    {
        int blocks = _ctx.Story.Blocks.Count;
        int finals = _ctx.Story.Blocks.Count(b => b.IsFinal);
        int props  = _ctx.Story.Properties.Count;
        int decs   = _ctx.Story.Blocks.Sum(b => b.Decisions.Count);
        _statsLabel.Text = $"Blocuri: {blocks}  |  Finale: {finals}  |  Prop: {props}  |  Decizii: {decs}";
    }

    private static string CondStr(ConditionDefinition c) => c switch
    {
        ComparisonCondition x => $"{x.Property} {x.Operator} {x.Value}",
        AndCondition a        => $"AND({a.Conditions.Count})",
        OrCondition o         => $"OR({o.Conditions.Count})",
        _                     => "?"
    };

    // ── UI factory ────────────────────────────────────────────────────────
    private static Label SectionHeader(string t) => new()
    {
        Text      = $"  {t}",
        Dock      = DockStyle.Top,
        Height    = 30,
        Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
        ForeColor = Color.FromArgb(255, 215, 100),
        BackColor = Color.FromArgb(16, 16, 26),
        TextAlign = ContentAlignment.MiddleLeft
    };

    private static Label SideLabel(string t) => new()
    {
        Text      = $"  {t}",
        Dock      = DockStyle.Top,
        Height    = 22,
        Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
        ForeColor = Color.FromArgb(255, 215, 100),
        BackColor = Color.FromArgb(14, 14, 22)
    };

    private static void Row(TableLayoutPanel tbl, string label, Control ctrl)
    {
        tbl.Controls.Add(Lbl(label));
        ctrl.Dock = DockStyle.Fill;
        tbl.Controls.Add(ctrl);
    }

    private static Label Lbl(string t) => new()
    {
        Text      = t,
        Dock      = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleRight,
        ForeColor = Color.FromArgb(145, 140, 125),
        Padding   = new Padding(0, 0, 8, 0)
    };

    private static TextBox Txt(string def = "") => new()
    {
        Dock        = DockStyle.Fill,
        BackColor   = Color.FromArgb(30, 28, 45),
        ForeColor   = Color.FromArgb(210, 205, 190),
        BorderStyle = BorderStyle.FixedSingle,
        Text        = def
    };

    private static Button Btn(string t, Color bg) => new()
    {
        Text      = t,
        FlatStyle = FlatStyle.Flat,
        BackColor = bg,
        ForeColor = Color.FromArgb(210, 205, 190),
        Height    = 28,
        FlatAppearance = { BorderColor = Color.FromArgb(55, 53, 72) }
    };

    private static Button SmBtn(string t, Color bg) => new()
    {
        Text      = t,
        AutoSize  = true,
        FlatStyle = FlatStyle.Flat,
        BackColor = bg,
        ForeColor = Color.FromArgb(210, 205, 190),
        Height    = 24,
        Font      = new Font("Segoe UI", 8.5f),
        FlatAppearance = { BorderColor = Color.FromArgb(55, 53, 72) }
    };
}
