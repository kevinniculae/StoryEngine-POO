using Story.Model;

namespace Story.Editor.WinForms.Forms;

/// <summary>
/// Panou care găzduiește GraphPanel + toolbar (Fit All, Zoom +/-).
/// Se adaugă ca tab în MainEditorForm.
/// </summary>
public class GraphTabPanel : Panel
{
    public readonly GraphPanel Graph;

    public GraphTabPanel()
    {
        Dock      = DockStyle.Fill;
        BackColor = Color.FromArgb(12, 12, 20);

        // ── toolbar ────────────────────────────────────────────────
        var toolbar = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 34,
            BackColor = Color.FromArgb(14, 14, 24),
            Padding   = new Padding(6, 5, 6, 5)
        };
        var flow = new FlowLayoutPanel
        {
            Dock         = DockStyle.Fill,
            WrapContents = false
        };

        Button SmBtn(string t, Color bg)
        {
            return new Button
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

        var btnFit  = SmBtn("⊞ Fit All",  Color.FromArgb(38, 50, 75));
        var btnZoomP = SmBtn("＋",          Color.FromArgb(38, 50, 38));
        var btnZoomM = SmBtn("－",          Color.FromArgb(55, 38, 38));
        var lblHelp = new Label
        {
            Text      = "  Click stânga = selectare   |   Click dreapta/mijloc + drag = pan   |   Scroll = zoom",
            AutoSize  = true,
            ForeColor = Color.FromArgb(70, 65, 55),
            Font      = new Font("Segoe UI", 8f, FontStyle.Italic),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(8, 4, 0, 0)
        };

        btnFit.Click   += (_, _) => Graph.FitAll();
        btnZoomP.Click += (_, _) =>
        {
            // simulate wheel zoom in
            var ev = new MouseEventArgs(MouseButtons.None, 0,
                Graph.Width/2, Graph.Height/2, 120);
            Graph.GetType().GetMethod("OnMouseWheel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(Graph, new object[] { ev });
        };
        btnZoomM.Click += (_, _) =>
        {
            var ev = new MouseEventArgs(MouseButtons.None, 0,
                Graph.Width/2, Graph.Height/2, -120);
            Graph.GetType().GetMethod("OnMouseWheel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(Graph, new object[] { ev });
        };

        flow.Controls.Add(btnFit);
        flow.Controls.Add(btnZoomP);
        flow.Controls.Add(btnZoomM);
        flow.Controls.Add(lblHelp);
        toolbar.Controls.Add(flow);

        // ── graph ──────────────────────────────────────────────────
        Graph = new GraphPanel { Dock = DockStyle.Fill };

        Controls.Add(Graph);
        Controls.Add(toolbar);
    }

    public void Load(StoryDefinition story)
    {
        Graph.LoadStory(story);
        Graph.FitAll();
    }

    public void Reload(StoryDefinition story)
    {
        Graph.Refresh(story);
    }
}
