using System.Drawing;
using System.Windows.Forms;

namespace Experimental56;

public enum UiTheme { Light, Dark }

public static class ThemeManager
{
    public static UiTheme Current { get; private set; } = UiTheme.Light;
    public static Color Back => Current == UiTheme.Light ? Color.FromArgb(245, 247, 252) : Color.FromArgb(32, 35, 42);
    public static Color Surface => Current == UiTheme.Light ? Color.White : Color.FromArgb(45, 50, 61);
    public static Color Text => Current == UiTheme.Light ? Color.FromArgb(35, 35, 35) : Color.FromArgb(230, 230, 230);
    public static Color Accent => Color.FromArgb(46, 117, 255);

    public static void SetTheme(string theme) => Current = theme == "dark" ? UiTheme.Dark : UiTheme.Light;

    public static void ApplyTheme(Control c)
    {
        if (c is DataGridView g)
        {
            g.BackgroundColor = Surface;
            g.DefaultCellStyle.BackColor = Surface;
            g.DefaultCellStyle.ForeColor = Text;
            g.ColumnHeadersDefaultCellStyle.BackColor = Current == UiTheme.Light ? Color.FromArgb(236, 240, 249) : Color.FromArgb(55, 60, 72);
            g.ColumnHeadersDefaultCellStyle.ForeColor = Text;
            g.EnableHeadersVisualStyles = false;
        }
        else if (c is Button b)
        {
            b.BackColor = Accent;
            b.ForeColor = Color.White;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
        }
        else if (c is Panel or UserControl or Form or FlowLayoutPanel or TableLayoutPanel or GroupBox)
        {
            c.BackColor = Back;
            c.ForeColor = Text;
        }
        else
        {
            c.BackColor = Surface;
            c.ForeColor = Text;
        }

        foreach (Control child in c.Controls) ApplyTheme(child);
    }
}
