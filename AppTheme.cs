using System.Drawing;
using System.Windows.Forms;

namespace Experimental56;

public static class AppTheme
{
    public static readonly Font Font = new("Segoe UI", 10f);

    public static void ApplyGlobal()
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
    }

    public static Button FlatButton(string text)
    {
        var b = new Button
        {
            Text = text,
            AutoSize = false,
            Height = 34,
            Width = 140,
            Font = Font,
            FlatStyle = FlatStyle.Flat,
            BackColor = ThemeManager.Accent,
            ForeColor = Color.White
        };
        b.FlatAppearance.BorderSize = 0;
        return b;
    }

    public static DataGridView CreateGrid()
    {
        var g = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            BorderStyle = BorderStyle.None,
            Font = Font
        };
        g.DataError += (s, e) => e.ThrowException = false;
        ThemeManager.ApplyTheme(g);
        return g;
    }
}
