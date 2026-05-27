namespace Story.Editor.WinForms.Dialogs;

public class NewStoryDialog : Form
{
    private TextBox _txtTitle = null!;
    private TextBox _txtStart = null!;
    private TextBox _txtAuthor = null!;
    private TextBox _txtDesc = null!;

    public string StoryTitle => _txtTitle.Text.Trim();
    public string StartBlock => _txtStart.Text.Trim();
    public string Author => _txtAuthor.Text.Trim();
    public string Description => _txtDesc.Text.Trim();

    public NewStoryDialog()
    {
        Text = "Poveste nouă";
        Size = new Size(420, 300);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(35, 35, 50);
        ForeColor = Color.WhiteSmoke;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 2,
            RowCount = 6
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _txtTitle = MakeTxt(); _txtStart = MakeTxt("intro.start"); _txtAuthor = MakeTxt(); _txtDesc = MakeTxt();

        layout.Controls.Add(Lbl("Titlu:"), 0, 0);     layout.Controls.Add(_txtTitle, 1, 0);
        layout.Controls.Add(Lbl("Autor:"), 0, 1);     layout.Controls.Add(_txtAuthor, 1, 1);
        layout.Controls.Add(Lbl("Descriere:"), 0, 2); layout.Controls.Add(_txtDesc, 1, 2);
        layout.Controls.Add(Lbl("Bloc start:"), 0, 3); layout.Controls.Add(_txtStart, 1, 3);

        var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 44, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
        var btnOk = new Button { Text = "Creează", DialogResult = DialogResult.OK, Width = 90, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(50, 80, 50), ForeColor = Color.WhiteSmoke };
        var btnCancel = new Button { Text = "Anulează", DialogResult = DialogResult.Cancel, Width = 90, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(80, 50, 50), ForeColor = Color.WhiteSmoke };
        btnOk.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(StoryTitle)) { MessageBox.Show("Titlul este obligatoriu."); DialogResult = DialogResult.None; return; }
            if (string.IsNullOrWhiteSpace(StartBlock)) { MessageBox.Show("Blocul de start este obligatoriu."); DialogResult = DialogResult.None; return; }
        };
        btnPanel.Controls.Add(btnCancel);
        btnPanel.Controls.Add(btnOk);

        Controls.Add(layout);
        Controls.Add(btnPanel);
        AcceptButton = btnOk;
        CancelButton = btnCancel;
    }

    private static Label Lbl(string t) => new() { Text = t, TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill, ForeColor = Color.WhiteSmoke };
    private static TextBox MakeTxt(string def = "") => new() { Dock = DockStyle.Fill, BackColor = Color.FromArgb(50, 50, 68), ForeColor = Color.WhiteSmoke, Text = def };
}
