using System.Windows.Forms;

namespace Experimental56;

public sealed class SettingsForm : Form
{
    private readonly SessionUser _user;
    private readonly ComboBox _language = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
    private readonly ComboBox _theme = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
    private readonly TextBox _dbPath = new() { Width = 420, ReadOnly = true };
    private readonly CheckBox _teacherConstructor = new() { AutoSize = true };

    public SettingsForm(SessionUser user)
    {
        _user = user;
        Text = Loc.T("Settings");
        Size = new Size(700, 520);
        StartPosition = FormStartPosition.CenterParent;
        Font = AppTheme.Font;

        _language.Items.AddRange(new object[] { "ru", "en", "kz" });
        _theme.Items.AddRange(new object[] { "light", "dark" });
        _language.SelectedItem = AppSettingsService.Get("ui.language", "ru");
        _theme.SelectedItem = AppSettingsService.Get("ui.theme", "light");
        _dbPath.Text = AppSettingsService.Get("db.path", Database.DbPath);
        _teacherConstructor.Checked = AppSettingsService.TeacherConstructorEnabled();
        _teacherConstructor.Text = Loc.T("TeacherConstructorEnabled");

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), RowCount = 6, ColumnCount = 1 };
        root.RowStyles.Clear();
        for (var i = 0; i < 6; i++) root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var langBox = BuildGroup(Loc.T("Language"), new Control[] { _language });
        root.Controls.Add(langBox);

        if (_user.Role != UserRole.Student)
        {
            var themeBox = BuildGroup(Loc.T("Theme"), new Control[] { _theme });
            root.Controls.Add(themeBox);
        }

        if (_user.Role == UserRole.Admin)
        {
            var chooseDb = AppTheme.FlatButton("..."); chooseDb.Width = 44;
            chooseDb.Click += (_, _) => { using var d = new OpenFileDialog { Filter = "DB (*.db)|*.db" }; if (d.ShowDialog() == DialogResult.OK) _dbPath.Text = d.FileName; };
            var backup = AppTheme.FlatButton(Loc.T("Backup"));
            backup.Click += (_, _) => BackupDb();
            var restore = AppTheme.FlatButton(Loc.T("Restore"));
            restore.Click += (_, _) => RestoreDb();
            var row = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
            row.Controls.Add(_dbPath); row.Controls.Add(chooseDb); row.Controls.Add(backup); row.Controls.Add(restore);
            var dbBox = BuildGroup(Loc.T("Database"), new Control[] { row, _teacherConstructor });
            root.Controls.Add(dbBox);

            var ch = AppTheme.FlatButton(Loc.T("ChangePassword"));
            ch.Click += (_, _) => { using var f = new ChangePasswordDialog(_user); f.ShowDialog(); };
            root.Controls.Add(BuildGroup(Loc.T("ChangePassword"), new Control[] { ch }));
        }
        else if (_user.Role == UserRole.Teacher)
        {
            var ch = AppTheme.FlatButton(Loc.T("ChangePassword"));
            ch.Click += (_, _) => { using var f = new ChangePasswordDialog(_user); f.ShowDialog(); };
            root.Controls.Add(BuildGroup(Loc.T("ChangePassword"), new Control[] { ch }));
        }

        var save = AppTheme.FlatButton(Loc.T("Save"));
        save.Click += (_, _) => SaveSettings();
        root.Controls.Add(save);
        Controls.Add(root);
        ThemeManager.ApplyTheme(this);
    }

    private GroupBox BuildGroup(string title, IEnumerable<Control> controls)
    {
        var g = new GroupBox { Text = title, Dock = DockStyle.Top, Height = 100, Padding = new Padding(10), AutoSize = true };
        var f = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        foreach (var c in controls) f.Controls.Add(c);
        g.Controls.Add(f);
        return g;
    }

    private void SaveSettings()
    {
        AppSettingsService.Set("ui.language", _language.SelectedItem?.ToString() ?? "ru");
        if (_user.Role != UserRole.Student) AppSettingsService.Set("ui.theme", _theme.SelectedItem?.ToString() ?? "light");
        if (_user.Role == UserRole.Admin)
        {
            AppSettingsService.Set("db.path", _dbPath.Text);
            AppSettingsService.Set("features.teacherConstructor", _teacherConstructor.Checked ? "1" : "0");
        }
        AppSettingsService.ApplyUiFromSettings();
        DialogResult = DialogResult.OK;
        Close();
    }

    private void BackupDb()
    {
        using var f = new FolderBrowserDialog();
        if (f.ShowDialog() != DialogResult.OK) return;
        var src = AppSettingsService.Get("db.path", Database.DbPath);
        var dst = Path.Combine(f.SelectedPath, $"app-backup-{DateTime.Now:yyyyMMdd-HHmmss}.db");
        File.Copy(src, dst, true);
        MessageBox.Show(dst);
    }

    private void RestoreDb()
    {
        using var d = new OpenFileDialog { Filter = "DB (*.db)|*.db" };
        if (d.ShowDialog() != DialogResult.OK) return;
        if (MessageBox.Show("Replace current DB?", "Confirm", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
        var target = AppSettingsService.Get("db.path", Database.DbPath);
        File.Copy(d.FileName, target, true);
        MessageBox.Show("Done. Restart app.");
    }
}

public sealed class ChangePasswordDialog : Form
{
    public ChangePasswordDialog(SessionUser user)
    {
        Text = Loc.T("ChangePassword");
        Size = new Size(380, 260);
        StartPosition = FormStartPosition.CenterParent;
        var c = new TextBox { Top = 20, Left = 20, Width = 320, PlaceholderText = Loc.T("CurrentPassword"), UseSystemPasswordChar = true };
        var n = new TextBox { Top = 60, Left = 20, Width = 320, PlaceholderText = Loc.T("NewPassword"), UseSystemPasswordChar = true };
        var cf = new TextBox { Top = 100, Left = 20, Width = 320, PlaceholderText = Loc.T("ConfirmPassword"), UseSystemPasswordChar = true };
        var b = AppTheme.FlatButton(Loc.T("Save")); b.Top = 145; b.Left = 20;
        b.Click += (_, _) =>
        {
            if (n.Text.Length < 6 || n.Text != cf.Text) return;
            if (!AuthService.VerifyUserPassword(user.Login, c.Text)) { MessageBox.Show("Wrong current password"); return; }
            AuthService.ChangePassword(user.Id, n.Text);
            Close();
        };
        Controls.AddRange(new Control[] { c, n, cf, b });
        ThemeManager.ApplyTheme(this);
    }
}
