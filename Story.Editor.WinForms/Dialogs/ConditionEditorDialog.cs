using Story.Model;

namespace Story.Editor.WinForms.Dialogs;

/// <summary>
/// Dialog pentru editarea vizuală a condiţiilor AST pe bază de TreeView.
/// </summary>
public class ConditionEditorDialog : Form
{
    private readonly EditorContext _ctx;
    private ConditionDefinition? _root;

    private TreeView _tree = null!;
    private Panel _editPanel = null!;
    private Button _btnAddChild = null!;
    private Button _btnDelete = null!;
    private Button _btnOk = null!;
    private Button _btnClear = null!;

    public ConditionDefinition? Result => _root;

    public ConditionEditorDialog(ConditionDefinition? existing, EditorContext ctx)
    {
        _ctx = ctx;
        _root = existing;
        InitializeComponent();
        RebuildTree();
    }

    private void InitializeComponent()
    {
        Text = "Editor condiție";
        Size = new Size(620, 500);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(35, 35, 50);
        ForeColor = Color.WhiteSmoke;

        var splitContainer = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 240,
            BackColor = Color.FromArgb(35, 35, 50)
        };

        // Left: tree
        _tree = new TreeView
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(28, 28, 42),
            ForeColor = Color.WhiteSmoke,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Consolas", 10f)
        };
        _tree.AfterSelect += OnTreeSelect;

        var treeToolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 38,
            BackColor = Color.FromArgb(25, 25, 38),
            Padding = new Padding(4)
        };
        _btnAddChild = MakeButton("+ Adaugă nod");
        _btnDelete = MakeButton("✕ Șterge");
        _btnClear = MakeButton("Golește tot");
        _btnAddChild.Click += OnAddChild;
        _btnDelete.Click += OnDelete;
        _btnClear.Click += (_, _) => { _root = null; RebuildTree(); };
        treeToolbar.Controls.Add(_btnAddChild);
        treeToolbar.Controls.Add(_btnDelete);
        treeToolbar.Controls.Add(_btnClear);

        var leftPanel = new Panel { Dock = DockStyle.Fill };
        leftPanel.Controls.Add(_tree);
        leftPanel.Controls.Add(treeToolbar);
        splitContainer.Panel1.Controls.Add(leftPanel);

        // Right: edit panel
        _editPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(32, 32, 48),
            Padding = new Padding(8)
        };
        splitContainer.Panel2.Controls.Add(_editPanel);

        // Bottom buttons
        var btnRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 44,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(8),
            BackColor = Color.FromArgb(25, 25, 38)
        };
        _btnOk = MakeButton("OK");
        _btnOk.DialogResult = DialogResult.OK;
        var btnCancel = MakeButton("Anulează");
        btnCancel.DialogResult = DialogResult.Cancel;
        btnRow.Controls.Add(btnCancel);
        btnRow.Controls.Add(_btnOk);

        Controls.Add(splitContainer);
        Controls.Add(btnRow);
        AcceptButton = _btnOk;
        CancelButton = btnCancel;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  TREE BUILDING
    // ══════════════════════════════════════════════════════════════════════
    private void RebuildTree()
    {
        _tree.Nodes.Clear();
        if (_root is not null)
        {
            var rootNode = BuildTreeNode(_root);
            _tree.Nodes.Add(rootNode);
            rootNode.ExpandAll();
        }
    }

    private TreeNode BuildTreeNode(ConditionDefinition cond)
    {
        var node = new TreeNode
        {
            Text = ConditionLabel(cond),
            Tag = cond
        };

        switch (cond)
        {
            case AndCondition a:
                foreach (var child in a.Conditions)
                    node.Nodes.Add(BuildTreeNode(child));
                break;
            case OrCondition o:
                foreach (var child in o.Conditions)
                    node.Nodes.Add(BuildTreeNode(child));
                break;
        }

        return node;
    }

    private static string ConditionLabel(ConditionDefinition c) => c switch
    {
        ComparisonCondition comp => $"{comp.Property} {comp.Operator} {comp.Value}",
        AndCondition => "AND",
        OrCondition => "OR",
        _ => "?"
    };

    // ══════════════════════════════════════════════════════════════════════
    //  TREE SELECTION → EDIT PANEL
    // ══════════════════════════════════════════════════════════════════════
    private void OnTreeSelect(object? s, TreeViewEventArgs e)
    {
        if (e.Node?.Tag is not ConditionDefinition cond) return;
        ShowEditPanel(cond, e.Node);
    }

    private void ShowEditPanel(ConditionDefinition cond, TreeNode node)
    {
        _editPanel.Controls.Clear();

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 10,
            Padding = new Padding(4)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        int row = 0;

        // Type selector
        var cboType = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Color.FromArgb(50, 50, 68),
            ForeColor = Color.WhiteSmoke
        };
        cboType.Items.AddRange(new object[] { "COMPARISON", "AND", "OR" });
        cboType.SelectedItem = cond.Type;

        layout.Controls.Add(MakeLabel("Tip nod:"), 0, row);
        layout.Controls.Add(cboType, 1, row++);

        // Comparison-specific fields
        TextBox? txtProp = null;
        ComboBox? cboOp = null;
        TextBox? txtVal = null;

        if (cond is ComparisonCondition comp)
        {
            txtProp = new TextBox { Dock = DockStyle.Fill, BackColor = Color.FromArgb(50, 50, 68), ForeColor = Color.WhiteSmoke, Text = comp.Property };
            var cboPropSuggest = new ComboBox { Dock = DockStyle.Fill, BackColor = Color.FromArgb(50, 50, 68), ForeColor = Color.WhiteSmoke, Text = comp.Property };
            foreach (var p in _ctx.Story.Properties) cboPropSuggest.Items.Add(p.Key);

            cboOp = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(50, 50, 68), ForeColor = Color.WhiteSmoke };
            cboOp.Items.AddRange(new object[] { "<", "<=", ">", ">=", "==", "!=" });
            cboOp.SelectedItem = comp.Operator;

            txtVal = new TextBox { Dock = DockStyle.Fill, BackColor = Color.FromArgb(50, 50, 68), ForeColor = Color.WhiteSmoke, Text = comp.Value.ToString() };

            layout.Controls.Add(MakeLabel("Proprietate:"), 0, row);
            layout.Controls.Add(cboPropSuggest, 1, row++);
            layout.Controls.Add(MakeLabel("Operator:"), 0, row);
            layout.Controls.Add(cboOp, 1, row++);
            layout.Controls.Add(MakeLabel("Valoare:"), 0, row);
            layout.Controls.Add(txtVal, 1, row++);

            // Save on change
            void Save()
            {
                comp.Property = cboPropSuggest.Text.Trim();
                comp.Operator = cboOp.SelectedItem?.ToString() ?? "==";
                if (double.TryParse(txtVal.Text, out var v)) comp.Value = v;
                node.Text = ConditionLabel(comp);
            }
            cboPropSuggest.TextChanged += (_, _) => Save();
            cboOp.SelectedIndexChanged += (_, _) => Save();
            txtVal.TextChanged += (_, _) => Save();
        }

        // Apply type change
        cboType.SelectedIndexChanged += (_, _) =>
        {
            var newType = cboType.SelectedItem?.ToString() ?? "COMPARISON";
            ChangeNodeType(node, newType);
        };

        _editPanel.Controls.Add(layout);
    }

    private void ChangeNodeType(TreeNode node, string newType)
    {
        ConditionDefinition newCond = newType switch
        {
            "AND" => new AndCondition(),
            "OR" => new OrCondition(),
            _ => new ComparisonCondition { Operator = "==" }
        };

        // Replace in parent
        if (node.Parent is null)
        {
            _root = newCond;
        }
        else if (node.Parent.Tag is AndCondition parentAnd)
        {
            var idx = parentAnd.Conditions.IndexOf((ConditionDefinition)node.Tag!);
            if (idx >= 0) parentAnd.Conditions[idx] = newCond;
        }
        else if (node.Parent.Tag is OrCondition parentOr)
        {
            var idx = parentOr.Conditions.IndexOf((ConditionDefinition)node.Tag!);
            if (idx >= 0) parentOr.Conditions[idx] = newCond;
        }

        RebuildTree();
    }

    // ══════════════════════════════════════════════════════════════════════
    //  ADD / DELETE
    // ══════════════════════════════════════════════════════════════════════
    private void OnAddChild(object? s, EventArgs e)
    {
        if (_root is null)
        {
            // Adaugă nodul rădăcină
            using var typeDialog = new NodeTypePickerDialog();
            if (typeDialog.ShowDialog() != DialogResult.OK) return;
            _root = CreateNode(typeDialog.SelectedType);
            RebuildTree();
            return;
        }

        var selected = _tree.SelectedNode;
        if (selected?.Tag is AndCondition a)
        {
            using var td = new NodeTypePickerDialog();
            if (td.ShowDialog() != DialogResult.OK) return;
            a.Conditions.Add(CreateNode(td.SelectedType));
            RebuildTree();
        }
        else if (selected?.Tag is OrCondition o)
        {
            using var td = new NodeTypePickerDialog();
            if (td.ShowDialog() != DialogResult.OK) return;
            o.Conditions.Add(CreateNode(td.SelectedType));
            RebuildTree();
        }
        else
        {
            MessageBox.Show("Selectează un nod AND sau OR pentru a adăuga condiții copil.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void OnDelete(object? s, EventArgs e)
    {
        var selected = _tree.SelectedNode;
        if (selected is null) return;

        if (selected.Tag is ConditionDefinition cond)
        {
            if (selected.Parent is null)
            {
                _root = null;
            }
            else if (selected.Parent.Tag is AndCondition pa)
            {
                pa.Conditions.Remove(cond);
            }
            else if (selected.Parent.Tag is OrCondition po)
            {
                po.Conditions.Remove(cond);
            }
            RebuildTree();
        }
    }

    private static ConditionDefinition CreateNode(string type) => type switch
    {
        "AND" => new AndCondition(),
        "OR" => new OrCondition(),
        _ => new ComparisonCondition { Operator = "==" }
    };

    private static Label MakeLabel(string text) =>
        new() { Text = text, TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill, ForeColor = Color.WhiteSmoke };

    private static Button MakeButton(string text) =>
        new() { Text = text, AutoSize = true, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(55, 55, 78), ForeColor = Color.WhiteSmoke };
}

/// <summary>Mini-dialog pentru alegerea tipului de nod nou.</summary>
public class NodeTypePickerDialog : Form
{
    public string SelectedType { get; private set; } = "COMPARISON";

    public NodeTypePickerDialog()
    {
        Text = "Tip nod";
        Size = new Size(260, 160);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(35, 35, 50);
        ForeColor = Color.WhiteSmoke;

        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(20) };

        foreach (var t in new[] { "COMPARISON", "AND", "OR" })
        {
            var type = t;
            var btn = new Button
            {
                Text = type,
                Width = 200,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(55, 55, 78),
                ForeColor = Color.WhiteSmoke
            };
            btn.Click += (_, _) =>
            {
                SelectedType = type;
                DialogResult = DialogResult.OK;
                Close();
            };
            flow.Controls.Add(btn);
        }
        Controls.Add(flow);
    }
}
