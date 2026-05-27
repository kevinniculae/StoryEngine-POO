using Story.Model;

namespace Story.Editor.WinForms.Dialogs;

/// <summary>
/// Dialog modal pentru editarea completă a unei decizii.
/// Include autocomplete pentru ID-uri de bloc la câmpul "Bloc destinație".
/// </summary>
public class DecisionDialog : Form
{
    private readonly DecisionDefinition _decision;
    private readonly EditorContext _ctx;

    // Controls
    private TextBox  _txtText   = null!;
    private ComboBox _cboTarget = null!;
    private TextBox  _txtIcon   = null!;
    private Button   _btnBrowseIcon = null!;
    private DataGridView _gridEffects = null!;
    private Label    _conditionSummary = null!;
    private Button   _btnEditCondition = null!;
    private Button   _btnOk     = null!;
    private Button   _btnCancel = null!;

    public DecisionDefinition Result => _decision;

    public DecisionDialog(DecisionDefinition decision, EditorContext ctx)
    {
        _decision = CloneDecision(decision);
        _ctx      = ctx;
        InitializeComponent();
        LoadData();
    }

    private void InitializeComponent()
    {
        Text            = "Editare decizie";
        Size            = new Size(580, 560);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        StartPosition   = FormStartPosition.CenterParent;
        BackColor       = Color.FromArgb(32, 30, 48);
        ForeColor       = Color.FromArgb(210, 205, 190);
        Font            = new Font("Segoe UI", 9.5f);

        // ── Main layout ────────────────────────────────────────────────
        var layout = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            Padding     = new Padding(14),
            ColumnCount = 2,
            RowCount    = 8
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 135));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        int row = 0;

        // Text decizie
        _txtText = Txt();
        layout.Controls.Add(Lbl("Text decizie:"), 0, row);
        layout.Controls.Add(_txtText, 1, row++);

        // Bloc destinație cu AUTOCOMPLETE
        _cboTarget = new ComboBox
        {
            Dock              = DockStyle.Fill,
            BackColor         = Color.FromArgb(44, 42, 65),
            ForeColor         = Color.FromArgb(210, 205, 190),
            DropDownStyle     = ComboBoxStyle.DropDown,
            AutoCompleteMode  = AutoCompleteMode.SuggestAppend,
            AutoCompleteSource = AutoCompleteSource.ListItems,
            Font              = new Font("Segoe UI", 9.5f)
        };
        // Populează cu toate ID-urile de bloc
        foreach (var b in _ctx.Story.Blocks)
            _cboTarget.Items.Add(b.Id);

        // Colorează itemii în funcție de tip
        _cboTarget.DrawMode = DrawMode.OwnerDrawFixed;
        _cboTarget.ItemHeight = 20;
        _cboTarget.DrawItem += (s, e) =>
        {
            if (e.Index < 0) return;
            e.DrawBackground();
            var id    = _cboTarget.Items[e.Index].ToString() ?? "";
            var block = _ctx.Story.Blocks.FirstOrDefault(b => b.Id == id);
            var color = block?.IsFinal == true  ? Color.FromArgb(210, 100, 100)
                      : block?.Id == _ctx.Story.StartBlock ? Color.FromArgb(255, 215, 100)
                      : Color.FromArgb(210, 205, 190);
            var prefix = block?.IsFinal == true ? "🏁 "
                       : block?.Id == _ctx.Story.StartBlock ? "▶ "
                       : "▷ ";
            using var brush = new SolidBrush(color);
            e.Graphics.DrawString(prefix + id,
                new Font("Segoe UI", 9f), brush, e.Bounds.X + 4, e.Bounds.Y + 2);
        };

        layout.Controls.Add(Lbl("Bloc destinație:"), 0, row);
        layout.Controls.Add(_cboTarget, 1, row++);

        // Iconiță
        var iconRow = new Panel { Dock = DockStyle.Fill, Height = 28 };
        _txtIcon       = Txt();
        _btnBrowseIcon = SmBtn("...", Color.FromArgb(48, 48, 72));
        _txtIcon.Dock       = DockStyle.Fill;
        _btnBrowseIcon.Dock = DockStyle.Right;
        _btnBrowseIcon.Width = 32;
        _btnBrowseIcon.Click += OnBrowseIcon;
        iconRow.Controls.Add(_txtIcon);
        iconRow.Controls.Add(_btnBrowseIcon);
        layout.Controls.Add(Lbl("Iconiță:"), 0, row);
        layout.Controls.Add(iconRow, 1, row++);

        // Condiție
        var condRow = new Panel { Dock = DockStyle.Fill, Height = 32 };
        _conditionSummary = new Label
        {
            Dock      = DockStyle.Fill,
            Text      = "(nicio condiție)",
            ForeColor = Color.FromArgb(90, 85, 75),
            TextAlign = ContentAlignment.MiddleLeft,
            Font      = new Font("Consolas", 8.5f)
        };
        _btnEditCondition = SmBtn("Editare...", Color.FromArgb(38, 50, 75));
        _btnEditCondition.Dock  = DockStyle.Right;
        _btnEditCondition.Width = 80;
        _btnEditCondition.Click += OnEditCondition;
        condRow.Controls.Add(_conditionSummary);
        condRow.Controls.Add(_btnEditCondition);
        layout.Controls.Add(Lbl("Condiție:"), 0, row);
        layout.Controls.Add(condRow, 1, row++);

        // Efecte header
        var effBar = new Panel { Dock = DockStyle.Fill };
        var btnAdd = SmBtn("+ Adaugă efect", Color.FromArgb(38, 62, 40));
        var btnDel = SmBtn("✕ Șterge",       Color.FromArgb(72, 38, 38));
        btnAdd.Location = new Point(0, 0);
        btnDel.Location = new Point(130, 0);
        btnAdd.AutoSize = true;
        btnDel.AutoSize = true;
        btnAdd.Click += OnAddEffect;
        btnDel.Click += OnDeleteEffect;
        effBar.Controls.Add(btnAdd);
        effBar.Controls.Add(btnDel);
        layout.Controls.Add(Lbl("Efecte:"), 0, row);
        layout.Controls.Add(effBar, 1, row++);

        // Effects grid
        _gridEffects = BuildEffectsGrid();
        layout.SetColumnSpan(_gridEffects, 2);
        layout.Controls.Add(_gridEffects, 0, row++);
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // OK / Cancel
        var btnRow = new FlowLayoutPanel
        {
            Dock          = DockStyle.Bottom,
            Height        = 44,
            FlowDirection = FlowDirection.RightToLeft,
            Padding       = new Padding(8)
        };
        _btnCancel = new Button
        {
            Text          = "Anulează",
            DialogResult  = DialogResult.Cancel,
            Width         = 90,
            FlatStyle     = FlatStyle.Flat,
            BackColor     = Color.FromArgb(72, 40, 40),
            ForeColor     = Color.FromArgb(210, 205, 190)
        };
        _btnOk = new Button
        {
            Text          = "OK",
            DialogResult  = DialogResult.OK,
            Width         = 90,
            FlatStyle     = FlatStyle.Flat,
            BackColor     = Color.FromArgb(38, 72, 40),
            ForeColor     = Color.FromArgb(210, 205, 190)
        };
        _btnOk.Click += OnOk;
        btnRow.Controls.Add(_btnCancel);
        btnRow.Controls.Add(_btnOk);

        Controls.Add(layout);
        Controls.Add(btnRow);
        AcceptButton = _btnOk;
        CancelButton = _btnCancel;
    }

    private DataGridView BuildEffectsGrid()
    {
        var g = new DataGridView
        {
            Dock                = DockStyle.Fill,
            BackgroundColor     = Color.FromArgb(20, 18, 32),
            ForeColor           = Color.FromArgb(200, 195, 180),
            GridColor           = Color.FromArgb(45, 43, 62),
            BorderStyle         = BorderStyle.None,
            RowHeadersVisible   = false,
            AllowUserToAddRows  = false,
            SelectionMode       = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowTemplate         = { Height = 26 }
        };
        g.DefaultCellStyle.BackColor          = Color.FromArgb(26, 24, 40);
        g.DefaultCellStyle.ForeColor          = Color.FromArgb(200, 195, 180);
        g.DefaultCellStyle.SelectionBackColor = Color.FromArgb(55, 52, 82);
        g.DefaultCellStyle.SelectionForeColor = Color.White;
        g.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(14, 12, 24);
        g.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(255, 215, 100);
        g.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 9f, FontStyle.Bold);
        g.EnableHeadersVisualStyles = false;
        g.ColumnHeadersHeight       = 24;
        g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(30, 28, 46);

        // Tip (ADD / SET)
        var colType = new DataGridViewComboBoxColumn
        {
            Name        = "Type",
            HeaderText  = "Tip",
            FillWeight  = 15,
            FlatStyle   = FlatStyle.Flat
        };
        colType.Items.AddRange("ADD", "SET");

        // Proprietate cu autocomplete via ComboBox
        var colProp = new DataGridViewComboBoxColumn
        {
            Name       = "Property",
            HeaderText = "Proprietate",
            FillWeight = 50,
            FlatStyle  = FlatStyle.Flat
        };
        foreach (var p in _ctx.Story.Properties)
            colProp.Items.Add(p.Key);

        var colVal = new DataGridViewTextBoxColumn
        {
            Name       = "Value",
            HeaderText = "Valoare (delta / absolut)",
            FillWeight = 35
        };

        g.Columns.Add(colType);
        g.Columns.Add(colProp);
        g.Columns.Add(colVal);

        return g;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  DATA
    // ══════════════════════════════════════════════════════════════════════
    private void LoadData()
    {
        _txtText.Text   = _decision.Text;
        _cboTarget.Text = _decision.TargetBlock;
        _txtIcon.Text   = _decision.Icon ?? "";
        UpdateCondSummary();

        _gridEffects.Rows.Clear();
        foreach (var ef in _decision.Effects)
        {
            var r = _gridEffects.Rows[_gridEffects.Rows.Add()];
            r.Cells["Type"].Value     = ef.Type.ToString();
            r.Cells["Property"].Value = ef.Property;
            r.Cells["Value"].Value    = ef.Value.ToString();
        }
    }

    private void UpdateCondSummary()
    {
        if (_decision.Condition is null)
        {
            _conditionSummary.Text      = "(nicio condiție)";
            _conditionSummary.ForeColor = Color.FromArgb(90, 85, 75);
        }
        else
        {
            _conditionSummary.Text      = CondStr(_decision.Condition);
            _conditionSummary.ForeColor = Color.FromArgb(100, 200, 110);
        }
    }

    private static string CondStr(ConditionDefinition c) => c switch
    {
        ComparisonCondition x => $"{x.Property} {x.Operator} {x.Value}",
        AndCondition a        => $"AND ({a.Conditions.Count} condiții)",
        OrCondition o         => $"OR ({o.Conditions.Count} condiții)",
        _                     => "?"
    };

    // ══════════════════════════════════════════════════════════════════════
    //  EVENT HANDLERS
    // ══════════════════════════════════════════════════════════════════════
    private void OnBrowseIcon(object? s, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title  = "Selectează iconiță",
            Filter = "Imagini (*.png;*.jpg;*.bmp)|*.png;*.jpg;*.bmp"
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;

        var imagesDir = Path.Combine(_ctx.WorkingDirectory ?? "", "images");
        Directory.CreateDirectory(imagesDir);
        var dest = Path.Combine(imagesDir, Path.GetFileName(dlg.FileName));
        File.Copy(dlg.FileName, dest, true);
        _txtIcon.Text = "images/" + Path.GetFileName(dlg.FileName);
    }

    private void OnEditCondition(object? s, EventArgs e)
    {
        using var dlg = new ConditionEditorDialog(_decision.Condition, _ctx);
        if (dlg.ShowDialog() != DialogResult.OK) return;
        _decision.Condition = dlg.Result;
        UpdateCondSummary();
    }

    private void OnAddEffect(object? s, EventArgs e)
    {
        var r = _gridEffects.Rows[_gridEffects.Rows.Add()];
        r.Cells["Type"].Value     = "ADD";
        r.Cells["Property"].Value = _ctx.Story.Properties.Count > 0
            ? _ctx.Story.Properties[0].Key : "";
        r.Cells["Value"].Value    = "0";
    }

    private void OnDeleteEffect(object? s, EventArgs e)
    {
        foreach (DataGridViewRow r in _gridEffects.SelectedRows)
            _gridEffects.Rows.Remove(r);
    }

    private void OnOk(object? s, EventArgs e)
    {
        // Validare minimă
        if (string.IsNullOrWhiteSpace(_txtText.Text))
        {
            MessageBox.Show("Textul deciziei este obligatoriu.", "Validare",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }
        if (string.IsNullOrWhiteSpace(_cboTarget.Text))
        {
            MessageBox.Show("Blocul destinație este obligatoriu.", "Validare",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        _decision.Text        = _txtText.Text.Trim();
        _decision.TargetBlock = _cboTarget.Text.Trim();
        _decision.Icon        = string.IsNullOrWhiteSpace(_txtIcon.Text)
            ? null : _txtIcon.Text.Trim();

        _decision.Effects.Clear();
        foreach (DataGridViewRow row in _gridEffects.Rows)
        {
            var typeStr = row.Cells["Type"].Value?.ToString() ?? "ADD";
            var prop    = row.Cells["Property"].Value?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(prop)) continue;
            if (!double.TryParse(row.Cells["Value"].Value?.ToString(), out var val))
                val = 0;

            _decision.Effects.Add(new EffectDefinition
            {
                Type     = typeStr == "SET" ? EffectType.SET : EffectType.ADD,
                Property = prop,
                Value    = val
            });
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  HELPERS
    // ══════════════════════════════════════════════════════════════════════
    private static DecisionDefinition CloneDecision(DecisionDefinition d) =>
        new()
        {
            Text        = d.Text,
            TargetBlock = d.TargetBlock,
            Icon        = d.Icon,
            Condition   = d.Condition,
            Effects     = d.Effects.Select(e => new EffectDefinition
            {
                Type     = e.Type,
                Property = e.Property,
                Value    = e.Value
            }).ToList()
        };

    private static Label Lbl(string t) => new()
    {
        Text      = t,
        TextAlign = ContentAlignment.MiddleRight,
        Dock      = DockStyle.Fill,
        ForeColor = Color.FromArgb(150, 145, 130),
        Padding   = new Padding(0, 0, 8, 0)
    };

    private static TextBox Txt(string def = "") => new()
    {
        Dock        = DockStyle.Fill,
        BackColor   = Color.FromArgb(44, 42, 65),
        ForeColor   = Color.FromArgb(210, 205, 190),
        BorderStyle = BorderStyle.FixedSingle,
        Text        = def
    };

    private static Button SmBtn(string t, Color bg) => new()
    {
        Text      = t,
        AutoSize  = true,
        FlatStyle = FlatStyle.Flat,
        BackColor = bg,
        ForeColor = Color.FromArgb(210, 205, 190),
        Height    = 26,
        Font      = new Font("Segoe UI", 8.5f),
        FlatAppearance = { BorderColor = Color.FromArgb(60, 58, 80) }
    };
}
