using System.Drawing;
using System.Windows.Forms;

namespace Experimental56;

public static class AppTheme
{
    public static readonly Font Font = new("Segoe UI", 10f);
    public static readonly Color Bg = Color.FromArgb(245, 247, 252);
    public static readonly Color Panel = Color.White;
    public static readonly Color Accent = Color.FromArgb(46, 117, 255);

    public static void ApplyGlobal()
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
    }

    public static Button FlatButton(string text)
    {
        return new Button
        {
            Text = text,
            AutoSize = false,
            Height = 34,
            FlatStyle = FlatStyle.Flat,
            BackColor = Accent,
            ForeColor = Color.White,
            Font = Font
        };
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
            BackgroundColor = Panel,
            BorderStyle = BorderStyle.None,
            Font = Font
        };
        g.DataError += (s, e) => e.ThrowException = false;
        return g;
    }
}
