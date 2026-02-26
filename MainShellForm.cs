using System.Data;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;

namespace Experimental56;

public sealed class MainShellForm : Form
{
    private readonly SessionUser _user;
    private readonly Panel _content = new() { Dock = DockStyle.Fill, Padding = new Padding(12), BackColor = ThemeManager.Back };
    public bool ShouldRelogin { get; private set; }

    public MainShellForm(SessionUser user)
    {
        _user = user;
        Text = Loc.T("AppTitle");
        WindowState = FormWindowState.Maximized;
        Font = AppTheme.Font;
        BackColor = ThemeManager.Back;

        var header = BuildHeader();
        var menu = BuildMenu();
        Controls.Add(_content);
        Controls.Add(menu);
        Controls.Add(header);
        LoadDefault();
        ThemeManager.ApplyTheme(this);
    }

    private Control BuildHeader()
    {
        var p = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = ThemeManager.Surface, Padding = new Padding(12) };
        var lbl = new Label { Text = $"{_user.DisplayName} ({_user.Role})", AutoSize = true, Location = new Point(12, 17) };
        var exit = AppTheme.FlatButton(Loc.T("Logout")); exit.Width = 90; exit.Location = new Point(Width - 180, 10); exit.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        exit.Click += (_, _) => { ShouldRelogin = true; Close(); };
        p.Controls.Add(lbl); p.Controls.Add(exit);
        return p;
    }

    private Control BuildMenu()
    {
        var menu = new Panel { Dock = DockStyle.Left, Width = 220, BackColor = Color.FromArgb(28, 36, 54), Padding = new Padding(10, 70, 10, 10) };
        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
        void Add(string txt, Action click)
        {
            var b = new Button { Text = txt, Width = 185, Height = 38, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(45, 58, 86), ForeColor = Color.White };
            b.FlatAppearance.BorderSize = 0;
            b.Click += (_, _) => click();
            flow.Controls.Add(b);
        }
        if (_user.Role == UserRole.Admin)
        {
            Add(Loc.T("Users"), () => Open(new AdminUsersPanel(_user)));
            Add(Loc.T("Groups"), () => Open(new AdminGroupsPanel(_user)));
            Add(Loc.T("Audit"), () => Open(new AdminAuditPanel()));
        }
        if (_user.Role == UserRole.Teacher)
        {
            Add(Loc.T("Tests"), () => Open(new TeacherTestsPanel(_user)));
            Add(Loc.T("Assignments"), () => Open(new TeacherAssignmentsPanel(_user)));
            Add(Loc.T("Results"), () => Open(new TeacherResultsPanel()));
            if (AppSettingsService.TeacherConstructorEnabled()) Add(Loc.T("Constructor"), () => Open(new ConstructorPanel(_user)));
        }
        if (_user.Role == UserRole.Student)
        {
            Add(Loc.T("AvailableTests"), () => Open(new StudentAvailablePanel(_user)));
            Add(Loc.T("MyAttempts"), () => Open(new StudentAttemptsPanel(_user)));
        }
        Add(Loc.T("Settings"), () => { using var f = new SettingsForm(_user); if (f.ShowDialog() == DialogResult.OK) { ThemeManager.ApplyTheme(this); Text = Loc.T("AppTitle"); } });
        menu.Controls.Add(flow);
        return menu;
    }

    private void LoadDefault()
    {
        if (_user.Role == UserRole.Admin) Open(new AdminUsersPanel(_user));
        if (_user.Role == UserRole.Teacher) Open(new TeacherTestsPanel(_user));
        if (_user.Role == UserRole.Student) Open(new StudentAvailablePanel(_user));
    }

    private void Open(Control c)
    {
        _content.Controls.Clear();
        c.Dock = DockStyle.Fill;
        _content.Controls.Add(c);
    }
}

public sealed class ChangePasswordForm : Form
{
    public ChangePasswordForm(SessionUser user)
    {
        Text = "Change password"; Size = new Size(320, 180); StartPosition = FormStartPosition.CenterParent;
        var tb = new TextBox { UseSystemPasswordChar = true, Width = 240, Location = new Point(20, 30) };
        var b = AppTheme.FlatButton("Save"); b.Location = new Point(20, 70);
        b.Click += (_, _) => { if (tb.Text.Length < 6) return; AuthService.ChangePassword(user.Id, tb.Text); Close(); };
        Controls.Add(tb); Controls.Add(b);
    }
}

public sealed class AdminUsersPanel : UserControl
{
    private readonly SessionUser _user;
    private readonly DataGridView _grid = AppTheme.CreateGrid();
    public AdminUsersPanel(SessionUser user)
    {
        _user = user;
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 50 };
        var add = AppTheme.FlatButton("Add"); var pass = AppTheme.FlatButton("Reset pass"); var toggle = AppTheme.FlatButton("Toggle active");
        top.Controls.AddRange(new Control[] { add, pass, toggle });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Id", HeaderText = "Id" });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Login", HeaderText = "Login" });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DisplayName", HeaderText = "Name", Width = 160 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Role", HeaderText = "Role" });
        _grid.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = "IsActive", HeaderText = "Active" });
        add.Click += (_, _) => AddUser(); pass.Click += (_, _) => ResetPass(); toggle.Click += (_, _) => Toggle();
        Controls.Add(_grid); Controls.Add(top);
        LoadData();
    }
    private void LoadData() => _grid.DataSource = DataAccess.Table("SELECT Id,Login,DisplayName,Role,IsActive FROM Users ORDER BY Id");
    private long SelectedId() => _grid.CurrentRow is null ? 0 : Convert.ToInt64(_grid.CurrentRow.Cells[0].Value);
    private void AddUser()
    {
        var f = new Form { Size = new Size(300, 280), StartPosition = FormStartPosition.CenterParent };
        var login = new TextBox { PlaceholderText = "login", Top = 20, Left = 20, Width = 240 };
        var name = new TextBox { PlaceholderText = "name", Top = 55, Left = 20, Width = 240 };
        var role = new ComboBox { Top = 90, Left = 20, Width = 240, DropDownStyle = ComboBoxStyle.DropDownList };
        role.Items.AddRange(new[] { "Admin", "Teacher", "Student" }); role.SelectedIndex = 2;
        var pwd = new TextBox { PlaceholderText = "password", Top = 125, Left = 20, Width = 240 };
        var b = AppTheme.FlatButton("Save"); b.Top = 165; b.Left = 20;
        b.Click += (_, _) =>
        {
            var ph = PasswordHasher.HashPassword(pwd.Text);
            DataAccess.Execute("INSERT INTO Users(Login,DisplayName,Role,Salt,Hash,IsActive,CreatedAt) VALUES(@l,@d,@r,@s,@h,1,@at)",
                ("@l", login.Text), ("@d", name.Text), ("@r", role.Text), ("@s", ph.Salt), ("@h", ph.Hash), ("@at", DateTime.UtcNow.ToString("O")));
            Audit.Log(_user.Id, "CreateUser", "User", null, new { login = login.Text });
            f.Close(); LoadData();
        };
        f.Controls.AddRange(new Control[] { login, name, role, pwd, b }); f.ShowDialog();
    }
    private void ResetPass() { var id = SelectedId(); if (id == 0) return; AuthService.ChangePassword(id, "newpass123"); LoadData(); }
    private void Toggle() { var id = SelectedId(); if (id == 0) return; DataAccess.Execute("UPDATE Users SET IsActive=CASE IsActive WHEN 1 THEN 0 ELSE 1 END WHERE Id=@id", ("@id", id)); LoadData(); }
}

public sealed class AdminGroupsPanel : UserControl
{
    private readonly ListBox _groups = new() { Dock = DockStyle.Left, Width = 220 };
    private readonly DataGridView _members = AppTheme.CreateGrid();
    public AdminGroupsPanel(SessionUser user)
    {
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 45 };
        var addG = AppTheme.FlatButton("Add group"); var addM = AppTheme.FlatButton("Add member"); var rem = AppTheme.FlatButton("Remove member");
        top.Controls.AddRange(new Control[] { addG, addM, rem });
        _members.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Id", HeaderText = "Id" });
        _members.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Login", HeaderText = "Login" });
        _members.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DisplayName", HeaderText = "Name", Width = 150 });
        _groups.SelectedIndexChanged += (_, _) => LoadMembers();
        addG.Click += (_, _) =>
        {
            var n = Prompt("Group name");
            if (string.IsNullOrWhiteSpace(n)) return;
            var affected = DataAccess.Execute("INSERT OR IGNORE INTO Groups(Name) VALUES(@n)", ("@n", n.Trim()));
            if (affected == 0)
            {
                MessageBox.Show("Group with this name already exists.");
                return;
            }
            LoadGroups();
        };
        addM.Click += (_, _) => AddMember(); rem.Click += (_, _) => RemoveMember();
        Controls.Add(_members); Controls.Add(_groups); Controls.Add(top);
        LoadGroups();
    }
    private static string Prompt(string caption) { using var f = new Form { Size = new Size(280, 130), StartPosition = FormStartPosition.CenterParent }; var t = new TextBox { Left = 20, Top = 20, Width = 220 }; var b = AppTheme.FlatButton("OK"); b.Left = 20; b.Top = 50; string v = ""; b.Click += (_, _) => { v = t.Text; f.Close(); }; f.Controls.AddRange(new Control[] { t, b }); f.ShowDialog(); return v; }
    private long GroupId()
    {
        var row = _groups.SelectedItem as DataRowView;
        var value = row?["Id"];
        return value is null || value == DBNull.Value ? 0 : Convert.ToInt64(value);
    }
    private void LoadGroups() { _groups.DisplayMember = "Name"; _groups.ValueMember = "Id"; _groups.DataSource = DataAccess.Table("SELECT Id,Name FROM Groups"); }
    private void LoadMembers() { var id = GroupId(); if (id == 0) return; _members.DataSource = DataAccess.Table("SELECT u.Id,u.Login,u.DisplayName FROM GroupMembers gm JOIN Users u ON u.Id=gm.UserId WHERE gm.GroupId=@g", ("@g", id)); }
    private void AddMember() { var gid = GroupId(); if (gid == 0) return; var uid = Convert.ToInt64(Prompt("UserId")); DataAccess.Execute("INSERT OR IGNORE INTO GroupMembers(GroupId,UserId) VALUES(@g,@u)", ("@g", gid), ("@u", uid)); LoadMembers(); }
    private void RemoveMember() { var gid = GroupId(); if (gid == 0 || _members.CurrentRow is null) return; var uid = Convert.ToInt64(_members.CurrentRow.Cells[0].Value); DataAccess.Execute("DELETE FROM GroupMembers WHERE GroupId=@g AND UserId=@u", ("@g", gid), ("@u", uid)); LoadMembers(); }
}

public sealed class AdminAuditPanel : UserControl
{
    private readonly DataGridView _grid = AppTheme.CreateGrid();
    private readonly TextBox _actor = new() { PlaceholderText = "Actor login" };
    private readonly TextBox _action = new() { PlaceholderText = "Action" };
    public AdminAuditPanel()
    {
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 42 };
        var refresh = AppTheme.FlatButton("Refresh");
        top.Controls.AddRange(new Control[] { _actor, _action, refresh });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "At", HeaderText = "At", Width = 180 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Actor", HeaderText = "Actor" });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Action", HeaderText = "Action", Width = 130 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "EntityType", HeaderText = "Entity" });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "MetaJson", HeaderText = "MetaJson", Width = 350 });
        refresh.Click += (_, _) => LoadData();
        Controls.Add(_grid); Controls.Add(top);
        LoadData();
    }
    private void LoadData()
    {
        _grid.DataSource = DataAccess.Table("SELECT a.At, IFNULL(u.Login,'-') Actor, a.Action, a.EntityType, a.MetaJson FROM AuditLog a LEFT JOIN Users u ON u.Id=a.ActorUserId WHERE (@actor='' OR u.Login LIKE '%'||@actor||'%') AND (@action='' OR a.Action LIKE '%'||@action||'%') ORDER BY a.Id DESC LIMIT 500", ("@actor", _actor.Text), ("@action", _action.Text));
    }
}

public sealed class TeacherTestsPanel : UserControl
{
    private readonly SessionUser _user;
    private readonly DataGridView _grid = AppTheme.CreateGrid();
    public TeacherTestsPanel(SessionUser user)
    {
        _user = user;
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 46 };
        var create = AppTheme.FlatButton("Create"); var open = AppTheme.FlatButton("Open Constructor"); var pub = AppTheme.FlatButton("Publish"); var arc = AppTheme.FlatButton("Archive"); var clone = AppTheme.FlatButton("Clone");
        top.Controls.AddRange(new Control[] { create, open, pub, arc, clone });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Id", HeaderText = "Id" });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Title", HeaderText = "Title", Width = 240 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Status", HeaderText = "Status" });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PassPercent", HeaderText = "Pass%" });
        var canUseConstructor = AppSettingsService.TeacherConstructorEnabled();
        create.Visible = canUseConstructor;
        open.Visible = canUseConstructor;
        create.Click += (_, _) => { if (!canUseConstructor) return; using var f = new ConstructorForm(_user, null); f.ShowDialog(); LoadData(); };
        open.Click += (_, _) => { if (!canUseConstructor) return; var id = Id(); if (id == 0) return; using var f = new ConstructorForm(_user, id); f.ShowDialog(); LoadData(); };
        pub.Click += (_, _) => { if (Id() == 0) return; DataAccess.Execute("UPDATE Tests SET Status='Published' WHERE Id=@id", ("@id", Id())); Audit.Log(_user.Id, "PublishTest", "Test", Id()); LoadData(); };
        arc.Click += (_, _) => { if (Id() == 0) return; DataAccess.Execute("UPDATE Tests SET Status='Archived' WHERE Id=@id", ("@id", Id())); LoadData(); };
        clone.Click += (_, _) => Clone();
        Controls.Add(_grid); Controls.Add(top);
        LoadData();
    }
    private long Id() => _grid.CurrentRow is null ? 0 : Convert.ToInt64(_grid.CurrentRow.Cells[0].Value);
    private void LoadData() => _grid.DataSource = DataAccess.Table("SELECT Id,Title,Status,PassPercent FROM Tests WHERE CreatedByTeacherId=@u ORDER BY Id DESC", ("@u", _user.Id));
    private void Clone() { var id = Id(); if (id == 0) return; DataAccess.Execute("INSERT INTO Tests(Title,Description,CreatedByTeacherId,Status,PassPercent,CreatedAt) SELECT Title||' (Clone)',Description,CreatedByTeacherId,'Draft',PassPercent,@at FROM Tests WHERE Id=@id", ("@id", id), ("@at", DateTime.UtcNow.ToString("O"))); LoadData(); }
}

public sealed class ConstructorPanel : UserControl
{
    public ConstructorPanel(SessionUser user)
    {
        var b = AppTheme.FlatButton("Open Constructor Form"); b.Click += (_, _) => { using var f = new ConstructorForm(user, null); f.ShowDialog(); };
        Controls.Add(b);
    }
}

public sealed class TeacherAssignmentsPanel : UserControl
{
    private readonly SessionUser _user;
    private readonly DataGridView _grid = AppTheme.CreateGrid();
    public TeacherAssignmentsPanel(SessionUser user)
    {
        _user = user;
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48 };
        var add = AppTheme.FlatButton("Assign");
        top.Controls.Add(add);
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Id", HeaderText = "Id" });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Title", HeaderText = "Test", Width = 220 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TargetType", HeaderText = "Type" });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TargetId", HeaderText = "Target" });
        add.Click += (_, _) => Add();
        Controls.Add(_grid); Controls.Add(top); LoadData();
    }
    private void Add()
    {
        var f = new Form { Size = new Size(360, 360), StartPosition = FormStartPosition.CenterParent };
        var test = new ComboBox { Left = 20, Top = 20, Width = 300, DropDownStyle = ComboBoxStyle.DropDownList, DataSource = DataAccess.Table("SELECT Id,Title FROM Tests WHERE CreatedByTeacherId=@u AND Status='Published'", ("@u", _user.Id)), DisplayMember = "Title", ValueMember = "Id" };
        var type = new ComboBox { Left = 20, Top = 55, Width = 300, DropDownStyle = ComboBoxStyle.DropDownList }; type.Items.AddRange(new[] { "Group", "User" }); type.SelectedIndex = 0;
        var target = new TextBox { Left = 20, Top = 90, Width = 300, PlaceholderText = "Target Id" };
        var lim = new NumericUpDown { Left = 20, Top = 125, Width = 120, Minimum = 1, Maximum = 10, Value = 2 };
        var tl = new NumericUpDown { Left = 160, Top = 125, Width = 120, Minimum = 5, Maximum = 180, Value = 30 };
        var s1 = new CheckBox { Left = 20, Top = 160, Text = "Shuffle Q", Checked = true };
        var s2 = new CheckBox { Left = 140, Top = 160, Text = "Shuffle O", Checked = true };
        var sc = new CheckBox { Left = 20, Top = 190, Text = "Show score", Checked = true };
        var cc = new CheckBox { Left = 140, Top = 190, Text = "Show correct", Checked = false };
        var b = AppTheme.FlatButton("Save"); b.Left = 20; b.Top = 230;
        b.Click += (_, _) =>
        {
            var selected = test.SelectedItem as DataRowView;
            var testIdValue = selected?["Id"];
            if (testIdValue is null || testIdValue == DBNull.Value) return;
            if (!long.TryParse(target.Text, out var targetId)) return;
            DataAccess.Execute("INSERT INTO Assignments(TestId,TargetType,TargetId,AvailableFrom,Deadline,AttemptLimit,TimeLimitMinutes,ShuffleQuestions,ShuffleOptions,ShowScoreAfter,ShowCorrectAfter,IsActive) VALUES(@t,@tt,@ti,@af,@dl,@al,@tl,@sq,@so,@ss,@sc,1)",
                ("@t", Convert.ToInt64(testIdValue)), ("@tt", type.Text), ("@ti", targetId), ("@af", DateTime.UtcNow.ToString("O")), ("@dl", DateTime.UtcNow.AddMonths(1).ToString("O")), ("@al", lim.Value), ("@tl", tl.Value), ("@sq", s1.Checked ? 1 : 0), ("@so", s2.Checked ? 1 : 0), ("@ss", sc.Checked ? 1 : 0), ("@sc", cc.Checked ? 1 : 0));
            f.Close(); LoadData();
        };
        f.Controls.AddRange(new Control[] { test, type, target, lim, tl, s1, s2, sc, cc, b });
        f.ShowDialog();
    }
    private void LoadData() => _grid.DataSource = DataAccess.Table("SELECT a.Id,t.Title,a.TargetType,a.TargetId FROM Assignments a JOIN Tests t ON t.Id=a.TestId WHERE t.CreatedByTeacherId=@u ORDER BY a.Id DESC", ("@u", _user.Id));
}

public sealed class TeacherResultsPanel : UserControl
{
    public TeacherResultsPanel()
    {
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 46 };
        var export = AppTheme.FlatButton("Export CSV");
        var grid = AppTheme.CreateGrid();
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "AttemptId", HeaderText = "Attempt" });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Student", HeaderText = "Student", Width = 150 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Title", HeaderText = "Test", Width = 220 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Percent", HeaderText = "%" });
        grid.DataSource = DataAccess.Table("SELECT ar.AttemptId,u.Login Student,t.Title,ar.Percent FROM AttemptResults ar JOIN Attempts a ON a.Id=ar.AttemptId JOIN Users u ON u.Id=a.UserId JOIN Assignments ass ON ass.Id=a.AssignmentId JOIN Tests t ON t.Id=ass.TestId ORDER BY ar.AttemptId DESC");
        export.Click += (_, _) =>
        {
            var dt = (DataTable)grid.DataSource;
            var path = Path.Combine(AppContext.BaseDirectory, "results.csv");
            var lines = new List<string> { "AttemptId,Student,Title,Percent" };
            lines.AddRange(dt.Rows.Cast<DataRow>().Select(r => $"{r[0]},{r[1]},\"{r[2]}\",{r[3]}"));
            File.WriteAllLines(path, lines);
            MessageBox.Show(path);
        };
        top.Controls.Add(export); Controls.Add(grid); Controls.Add(top);
    }
}

public sealed class StudentAvailablePanel : UserControl
{
    private readonly SessionUser _user;
    private readonly DataGridView _grid = AppTheme.CreateGrid();
    public StudentAvailablePanel(SessionUser user)
    {
        _user = user;
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44 };
        var start = AppTheme.FlatButton("Start"); top.Controls.Add(start);
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "AssignmentId", HeaderText = "Assignment" });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Title", HeaderText = "Title", Width = 220 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Deadline", HeaderText = "Deadline", Width = 180 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "AttemptLimit", HeaderText = "Limit" });
        start.Click += (_, _) => StartAttempt();
        Controls.Add(_grid); Controls.Add(top);
        LoadData();
    }
    private long AssignmentId() => _grid.CurrentRow is null ? 0 : Convert.ToInt64(_grid.CurrentRow.Cells[0].Value);
    private void LoadData()
    {
        _grid.DataSource = DataAccess.Table(@"SELECT DISTINCT a.Id AssignmentId,t.Title,a.Deadline,a.AttemptLimit FROM Assignments a JOIN Tests t ON t.Id=a.TestId
LEFT JOIN GroupMembers gm ON gm.GroupId=a.TargetId AND a.TargetType='Group'
WHERE a.IsActive=1 AND t.Status='Published' AND ((a.TargetType='User' AND a.TargetId=@u) OR (a.TargetType='Group' AND gm.UserId=@u))", ("@u", _user.Id));
    }
    private void StartAttempt()
    {
        var assignmentId = AssignmentId(); if (assignmentId == 0) return;
        using var c = Database.Open(); using var tx = c.BeginTransaction();
        try
        {
            using var chk = c.CreateCommand(); chk.Transaction = tx;
            chk.CommandText = "SELECT a.TestId,a.AttemptLimit,a.TimeLimitMinutes,a.ShuffleQuestions,a.ShuffleOptions,t.Title,t.PassPercent,a.Deadline,a.AvailableFrom,a.ShowScoreAfter,a.ShowCorrectAfter FROM Assignments a JOIN Tests t ON t.Id=a.TestId WHERE a.Id=@id AND a.IsActive=1 AND t.Status='Published'";
            chk.Parameters.AddWithValue("@id", assignmentId);
            using var rd = chk.ExecuteReader(); if (!rd.Read()) throw new Exception("Недоступно");
            var testId = rd.GetInt64(0); var limit = rd.GetInt64(1); var tl = rd.GetInt64(2); var shuffleQ = rd.GetInt64(3) == 1; var shuffleO = rd.GetInt64(4) == 1;
            var title = rd.GetString(5); var pass = rd.GetDouble(6); var deadline = DateTime.Parse(rd.GetString(7)); var from = DateTime.Parse(rd.GetString(8));
            var showScore = rd.GetInt64(9) == 1; var showCorrect = rd.GetInt64(10) == 1;
            if (DateTime.UtcNow < from || DateTime.UtcNow > deadline) throw new Exception("Вне окна доступности");

            long used;
            using (var c2 = c.CreateCommand()) { c2.Transaction = tx; c2.CommandText = "SELECT COUNT(*) FROM Attempts WHERE AssignmentId=@a AND UserId=@u"; c2.Parameters.AddWithValue("@a", assignmentId); c2.Parameters.AddWithValue("@u", _user.Id); used = Convert.ToInt64(c2.ExecuteScalar() ?? 0L); }
            if (used >= limit) throw new Exception("Лимит попыток");
            using (var c3 = c.CreateCommand()) { c3.Transaction = tx; c3.CommandText = "SELECT COUNT(*) FROM Attempts WHERE AssignmentId=@a AND UserId=@u AND Status='Active'"; c3.Parameters.AddWithValue("@a", assignmentId); c3.Parameters.AddWithValue("@u", _user.Id); if (Convert.ToInt64(c3.ExecuteScalar() ?? 0L) > 0) throw new Exception("Есть активная попытка"); }

            var questions = new List<Dictionary<string, object>>();
            using (var q = c.CreateCommand())
            {
                q.Transaction = tx;
                q.CommandText = "SELECT Id,Type,Text,Points,SettingsJson FROM Questions WHERE TestId=@t";
                q.Parameters.AddWithValue("@t", testId);
                using var qr = q.ExecuteReader();
                while (qr.Read())
                {
                    var item = new Dictionary<string, object> { ["id"] = qr.GetInt64(0), ["type"] = qr.GetString(1), ["text"] = qr.GetString(2), ["points"] = qr.GetDouble(3), ["settings"] = qr.GetString(4) };
                    if (qr.GetString(1) == "SingleChoice")
                    {
                        using var o = c.CreateCommand(); o.Transaction = tx;
                        o.CommandText = "SELECT Id,Text,IsCorrect FROM Options WHERE QuestionId=@q ORDER BY SortOrder"; o.Parameters.AddWithValue("@q", qr.GetInt64(0));
                        using var or = o.ExecuteReader(); var opts = new List<Dictionary<string, object>>();
                        while (or.Read()) opts.Add(new Dictionary<string, object> { ["id"] = or.GetInt64(0), ["text"] = or.GetString(1), ["isCorrect"] = or.GetInt64(2) == 1 });
                        if (shuffleO) opts = opts.OrderBy(_ => Guid.NewGuid()).ToList();
                        item["options"] = opts;
                    }
                    questions.Add(item);
                }
            }
            if (shuffleQ) questions = questions.OrderBy(_ => Guid.NewGuid()).ToList();
            var snap = JsonSerializer.Serialize(new { title, passPercent = pass, showScoreAfter = showScore, showCorrectAfter = showCorrect, questions });
            var attemptId = DataAccess.ExecuteScalarLong(c, tx,
                @"INSERT INTO Attempts(AssignmentId,UserId,StartedAt,EndsAt,Status,SnapshotJson)
                  VALUES(@a,@u,@st,@en,'Active',@s);
                  SELECT last_insert_rowid();",
                ("@a", assignmentId),
                ("@u", _user.Id),
                ("@st", DateTime.UtcNow.ToString("O")),
                ("@en", DateTime.UtcNow.AddMinutes(tl).ToString("O")),
                ("@s", snap));
            tx.Commit();
            Audit.Log(_user.Id, "StartAttempt", "Attempt", attemptId, new { assignmentId });
            using var f = new AttemptPlayerForm(_user, attemptId); f.ShowDialog();
            LoadData();
        }
        catch (Exception ex) { tx.Rollback(); MessageBox.Show(ex.Message); }
    }
}

public sealed class StudentAttemptsPanel : UserControl
{
    private readonly SessionUser _user;
    private readonly DataGridView _grid = AppTheme.CreateGrid();
    public StudentAttemptsPanel(SessionUser user)
    {
        _user = user;
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 45 };
        var rev = AppTheme.FlatButton("Review"); top.Controls.Add(rev);
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Id", HeaderText = "Attempt" });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Title", HeaderText = "Test", Width = 240 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Status", HeaderText = "Status" });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Percent", HeaderText = "%" });
        rev.Click += (_, _) => { if (_grid.CurrentRow is null) return; using var f = new AttemptPlayerForm(_user, Convert.ToInt64(_grid.CurrentRow.Cells[0].Value), true); f.ShowDialog(); };
        Controls.Add(_grid); Controls.Add(top); LoadData();
    }
    private void LoadData() => _grid.DataSource = DataAccess.Table("SELECT a.Id,t.Title,a.Status,IFNULL(ar.Percent,'') Percent FROM Attempts a JOIN Assignments ass ON ass.Id=a.AssignmentId JOIN Tests t ON t.Id=ass.TestId LEFT JOIN AttemptResults ar ON ar.AttemptId=a.Id WHERE a.UserId=@u ORDER BY a.Id DESC", ("@u", _user.Id));
}
