namespace Story.Editor.WinForms.Forms;

/// <summary>
/// Dialog simplu de input text – înlocuiește Microsoft.VisualBasic.Interaction.InputBox.
/// </summary>
public static class InputBoxHelper
{
    public static string Show(string prompt, string title = "", string defaultValue = "")
    {
        var form = new Form
        {
            Text = title,
            Size = new Size(380, 140),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            StartPosition = FormStartPosition.CenterParent,
            BackColor = Color.FromArgb(35, 35, 50),
            ForeColor = Color.WhiteSmoke
        };

        var lbl = new Label
        {
            Text = prompt,
            Location = new Point(12, 12),
            Size = new Size(350, 20),
            ForeColor = Color.WhiteSmoke
        };

        var txt = new TextBox
        {
            Text = defaultValue,
            Location = new Point(12, 36),
            Size = new Size(350, 24),
            BackColor = Color.FromArgb(50, 50, 68),
            ForeColor = Color.WhiteSmoke,
            BorderStyle = BorderStyle.FixedSingle
        };

        var btnOk = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Location = new Point(200, 72),
            Size = new Size(80, 28),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(50, 80, 50),
            ForeColor = Color.WhiteSmoke
        };

        var btnCancel = new Button
        {
            Text = "Anulează",
            DialogResult = DialogResult.Cancel,
            Location = new Point(290, 72),
            Size = new Size(80, 28),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(80, 50, 50),
            ForeColor = Color.WhiteSmoke
        };

        form.Controls.AddRange(new Control[] { lbl, txt, btnOk, btnCancel });
        form.AcceptButton = btnOk;
        form.CancelButton = btnCancel;

        return form.ShowDialog() == DialogResult.OK ? txt.Text : string.Empty;
    }
}
