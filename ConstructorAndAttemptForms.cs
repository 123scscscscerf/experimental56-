using System.Data;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;

namespace Experimental56;

public sealed class ConstructorForm : Form
{
    private readonly SessionUser _user;
    private readonly TextBox _searchTests = new() { Width = 220, PlaceholderText = "Search" };
    private readonly ListBox _tests = new() { Dock = DockStyle.Fill };
    private readonly ListBox _questions = new() { Dock = DockStyle.Fill };
    private readonly TextBox _title = new() { Width = 260 };
    private readonly TextBox _desc = new() { Width = 260 };
    private readonly NumericUpDown _pass = new() { Minimum = 1, Maximum = 100, Value = 70 };
    private readonly ComboBox _type = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
    private readonly TextBox _qText = new() { Multiline = true, Width = 420, Height = 110, ScrollBars = ScrollBars.Vertical };
    private readonly NumericUpDown _points = new() { Minimum = 0.1M, Maximum = 50, DecimalPlaces = 1, Value = 1 };
    private readonly TextBox[] _optText = { new() { Width = 320 }, new() { Width = 320 }, new() { Width = 320 }, new() { Width = 320 } };
    private readonly RadioButton[] _singleRadios = { new(), new(), new(), new() };
    private readonly CheckBox[] _multiChecks = { new(), new(), new(), new() };
    private readonly NumericUpDown _numCorrect = new() { DecimalPlaces = 2, Maximum = 100000, Minimum = -100000 };
    private readonly NumericUpDown _numTol = new() { DecimalPlaces = 3, Maximum = 100, Minimum = 0, Value = 0.01M };
    private readonly TextBox _textSample = new() { Width = 300 };
    private readonly Panel _singlePanel = new() { Width = 500, Height = 160 };
    private readonly Panel _multiPanel = new() { Width = 500, Height = 160 };
    private readonly Panel _numPanel = new() { Width = 500, Height = 70 };
    private readonly Panel _textPanel = new() { Width = 500, Height = 70 };
    private long _currentTestId;

    public ConstructorForm(SessionUser user, long? testId)
    {
        _user = user;
        Text = Loc.T("Constructor");
        Width = 1480; Height = 860;
        MinimumSize = new Size(1200, 760);
        StartPosition = FormStartPosition.CenterParent;
        WindowState = FormWindowState.Maximized;
        Font = AppTheme.Font;

        _type.Items.AddRange(new object[] { "SingleChoice", "Numeric", "MultipleChoice", "Text" });
        _type.SelectedIndex = 0;
        _type.SelectedIndexChanged += (_, _) => RefreshTypeBlocks();

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3 };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 340));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 360));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        root.Controls.Add(BuildTestsPane(), 0, 0);
        root.Controls.Add(BuildQuestionsPane(), 1, 0);
        root.Controls.Add(BuildEditorPane(), 2, 0);

        var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, Padding = new Padding(8) };
        var save = AppTheme.FlatButton(Loc.T("Save"));
        var preview = AppTheme.FlatButton(Loc.T("Preview"));
        var exit = AppTheme.FlatButton(Loc.T("Exit"));
        save.Click += (_, _) => SaveDraft();
        preview.Click += (_, _) => Preview();
        exit.Click += (_, _) => Close();
        bottom.Controls.AddRange(new Control[] { save, preview, exit });

        Controls.Add(root);
        Controls.Add(bottom);

        _searchTests.TextChanged += (_, _) => LoadTests();
        _tests.SelectedIndexChanged += (_, _) => OnTestSelected();
        _questions.SelectedIndexChanged += (_, _) => LoadQuestion();

        BuildTypePanels();
        LoadTests();
        if (testId.HasValue) SelectTest(testId.Value);
        ThemeManager.ApplyTheme(this);
    }

    private Control BuildTestsPane()
    {
        var p = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 90 };
        var newDraft = AppTheme.FlatButton(Loc.T("NewDraft"));
        var clone = AppTheme.FlatButton(Loc.T("CloneToDraft"));
        var publish = AppTheme.FlatButton(Loc.T("Publish"));
        var archive = AppTheme.FlatButton(Loc.T("Archive"));
        newDraft.Click += (_, _) => NewDraft();
        clone.Click += (_, _) => CloneToDraft();
        publish.Click += (_, _) => Publish();
        archive.Click += (_, _) => Archive();
        top.Controls.AddRange(new Control[] { _searchTests, newDraft, clone, publish, archive });
        p.Controls.Add(_tests);
        p.Controls.Add(top);
        return p;
    }

    private Control BuildQuestionsPane()
    {
        var p = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 90 };
        var add = AppTheme.FlatButton(Loc.T("AddQuestion"));
        var del = AppTheme.FlatButton(Loc.T("Delete"));
        var up = AppTheme.FlatButton(Loc.T("MoveUp"));
        var down = AppTheme.FlatButton(Loc.T("MoveDown"));
        var dup = AppTheme.FlatButton(Loc.T("Duplicate"));
        add.Click += (_, _) => AddQuestion();
        del.Click += (_, _) => DeleteQuestion();
        up.Click += (_, _) => MoveQuestion(-1);
        down.Click += (_, _) => MoveQuestion(1);
        dup.Click += (_, _) => DuplicateQuestion();
        top.Controls.AddRange(new Control[] { add, del, up, down, dup });
        p.Controls.Add(_questions);
        p.Controls.Add(top);
        return p;
    }

    private Control BuildEditorPane()
    {
        var p = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
        var f = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
        f.Controls.AddRange(new Control[]
        {
            new Label { Text = Loc.T("Title"), AutoSize = true }, _title,
            new Label { Text = Loc.T("Description"), AutoSize = true }, _desc,
            new Label { Text = Loc.T("PassPercent"), AutoSize = true }, _pass,
            new Label { Text = Loc.T("Type"), AutoSize = true }, _type,
            new Label { Text = Loc.T("QuestionText"), AutoSize = true }, _qText,
            new Label { Text = Loc.T("Points"), AutoSize = true }, _points,
            _singlePanel, _multiPanel, _numPanel, _textPanel
        });
        p.Controls.Add(f);
        return p;
    }

    private void BuildTypePanels()
    {
        for (var i = 0; i < 4; i++)
        {
            _singleRadios[i].Left = 8; _singleRadios[i].Top = i * 34 + 8;
            _optText[i].Left = 34; _optText[i].Top = i * 34 + 4;
            _singlePanel.Controls.Add(_singleRadios[i]); _singlePanel.Controls.Add(_optText[i]);

            _multiChecks[i].Left = 8; _multiChecks[i].Top = i * 34 + 8;
            var clone = new TextBox { Left = 34, Top = i * 34 + 4, Width = 320 };
            clone.TextChanged += (_, _) => _optText[i].Text = clone.Text;
            _multiPanel.Controls.Add(_multiChecks[i]); _multiPanel.Controls.Add(clone);
        }
        _numPanel.Controls.Add(new Label { Text = "Correct", Left = 8, Top = 8, Width = 60 });
        _numCorrect.Left = 75; _numCorrect.Top = 5; _numPanel.Controls.Add(_numCorrect);
        _numPanel.Controls.Add(new Label { Text = "Tolerance", Left = 245, Top = 8, Width = 70 });
        _numTol.Left = 320; _numTol.Top = 5; _numPanel.Controls.Add(_numTol);

        _textPanel.Controls.Add(new Label { Text = "Sample", Left = 8, Top = 8, Width = 60 });
        _textSample.Left = 75; _textSample.Top = 5; _textPanel.Controls.Add(_textSample);
        RefreshTypeBlocks();
    }

    private void RefreshTypeBlocks()
    {
        _singlePanel.Visible = _type.Text == "SingleChoice";
        _multiPanel.Visible = _type.Text == "MultipleChoice";
        _numPanel.Visible = _type.Text == "Numeric";
        _textPanel.Visible = _type.Text == "Text";
    }

    private void LoadTests()
    {
        var search = _searchTests.Text.Trim();
        _tests.DisplayMember = "Title";
        _tests.ValueMember = "Id";
        _tests.DataSource = DataAccess.Table("SELECT Id,Title||' ['||Status||']' Title FROM Tests WHERE CreatedByTeacherId=@u AND (@s='' OR Title LIKE '%'||@s||'%') ORDER BY Id DESC", ("@u", _user.Id), ("@s", search));
    }

    private void SelectTest(long testId)
    {
        for (var i = 0; i < _tests.Items.Count; i++)
        {
            var row = _tests.Items[i] as DataRowView;
            if (row is null) continue;
            if (Convert.ToInt64(row["Id"]) == testId) { _tests.SelectedIndex = i; return; }
        }
    }

    private void OnTestSelected()
    {
        var row = _tests.SelectedItem as DataRowView;
        if (row is null) return;
        _currentTestId = Convert.ToInt64(row["Id"]);
        using var c = Database.Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT Title,Description,PassPercent FROM Tests WHERE Id=@id";
        cmd.Parameters.AddWithValue("@id", _currentTestId);
        using var r = cmd.ExecuteReader();
        if (r.Read()) { _title.Text = r.GetString(0); _desc.Text = r.GetString(1); _pass.Value = (decimal)r.GetDouble(2); }
        LoadQuestions();
    }

    private void LoadQuestions()
    {
        if (_currentTestId == 0) return;
        _questions.DisplayMember = "Text";
        _questions.ValueMember = "Id";
        _questions.DataSource = DataAccess.Table("SELECT Id, Text FROM Questions WHERE TestId=@t ORDER BY Id", ("@t", _currentTestId));
    }

    private long CurrentQuestionId()
    {
        var row = _questions.SelectedItem as DataRowView;
        return row is null ? 0 : Convert.ToInt64(row["Id"]);
    }

    private void LoadQuestion()
    {
        var qid = CurrentQuestionId(); if (qid == 0) return;
        using var c = Database.Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT Type,Text,Points,SettingsJson FROM Questions WHERE Id=@id";
        cmd.Parameters.AddWithValue("@id", qid);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return;
        _type.Text = r.GetString(0);
        _qText.Text = r.GetString(1);
        _points.Value = (decimal)r.GetDouble(2);
        if (_type.Text == "SingleChoice" || _type.Text == "MultipleChoice")
        {
            using var o = c.CreateCommand();
            o.CommandText = "SELECT Text,IsCorrect FROM Options WHERE QuestionId=@q ORDER BY SortOrder";
            o.Parameters.AddWithValue("@q", qid);
            using var or = o.ExecuteReader();
            var i = 0;
            while (or.Read() && i < 4)
            {
                _optText[i].Text = or.GetString(0);
                _singleRadios[i].Checked = or.GetInt64(1) == 1;
                _multiChecks[i].Checked = or.GetInt64(1) == 1;
                i++;
            }
        }
        if (_type.Text == "Numeric")
        {
            var doc = JsonDocument.Parse(r.GetString(3));
            _numCorrect.Value = (decimal)doc.RootElement.GetProperty("correct").GetDouble();
            _numTol.Value = (decimal)doc.RootElement.GetProperty("tolerance").GetDouble();
        }
        RefreshTypeBlocks();
    }

    private void NewDraft()
    {
        var id = DataAccess.ExecuteInsert("INSERT INTO Tests(Title,Description,CreatedByTeacherId,Status,PassPercent,CreatedAt) VALUES(@t,@d,@u,'Draft',70,@at)", ("@t", "New draft"), ("@d", ""), ("@u", _user.Id), ("@at", DateTime.UtcNow.ToString("O")));
        LoadTests();
        SelectTest(id);
    }

    private void CloneToDraft()
    {
        if (_currentTestId == 0) return;
        var id = DataAccess.ExecuteInsert("INSERT INTO Tests(Title,Description,CreatedByTeacherId,Status,PassPercent,CreatedAt) SELECT Title||' (Clone)',Description,CreatedByTeacherId,'Draft',PassPercent,@at FROM Tests WHERE Id=@id", ("@id", _currentTestId), ("@at", DateTime.UtcNow.ToString("O")));
        var q = DataAccess.Table("SELECT Id,Type,Text,Points,SettingsJson FROM Questions WHERE TestId=@t", ("@t", _currentTestId));
        foreach (DataRow qr in q.Rows)
        {
            var nq = DataAccess.ExecuteInsert("INSERT INTO Questions(TestId,Type,Text,Points,SettingsJson) VALUES(@t,@ty,@tx,@p,@s)", ("@t", id), ("@ty", qr["Type"]), ("@tx", qr["Text"]), ("@p", qr["Points"]), ("@s", qr["SettingsJson"]));
            var opts = DataAccess.Table("SELECT Text,IsCorrect,SortOrder FROM Options WHERE QuestionId=@q", ("@q", Convert.ToInt64(qr["Id"])));
            foreach (DataRow op in opts.Rows)
                DataAccess.Execute("INSERT INTO Options(QuestionId,Text,IsCorrect,SortOrder) VALUES(@q,@t,@c,@s)", ("@q", nq), ("@t", op[0]), ("@c", op[1]), ("@s", op[2]));
        }
        LoadTests(); SelectTest(id);
    }

    private bool ValidateForPublish()
    {
        if (_currentTestId == 0 || string.IsNullOrWhiteSpace(_title.Text)) return false;
        var qs = DataAccess.Table("SELECT Id,Type,Text FROM Questions WHERE TestId=@t", ("@t", _currentTestId));
        if (qs.Rows.Count == 0) return false;
        foreach (DataRow q in qs.Rows)
        {
            if (string.IsNullOrWhiteSpace(q["Text"]?.ToString())) return false;
            var type = q["Type"]?.ToString() ?? "";
            if (type is "SingleChoice" or "MultipleChoice")
            {
                var opts = DataAccess.Table("SELECT Text,IsCorrect FROM Options WHERE QuestionId=@q", ("@q", Convert.ToInt64(q["Id"])));
                if (opts.Rows.Count != 4) return false;
                if (opts.Rows.Cast<DataRow>().Any(x => string.IsNullOrWhiteSpace(x["Text"]?.ToString()))) return false;
                if (!opts.Rows.Cast<DataRow>().Any(x => Convert.ToInt64(x["IsCorrect"]) == 1)) return false;
            }
        }
        return true;
    }

    private void Publish() { if (!ValidateForPublish()) { MessageBox.Show("Validation failed"); return; } DataAccess.Execute("UPDATE Tests SET Status='Published' WHERE Id=@id", ("@id", _currentTestId)); LoadTests(); }
    private void Archive() { if (_currentTestId == 0) return; DataAccess.Execute("UPDATE Tests SET Status='Archived' WHERE Id=@id", ("@id", _currentTestId)); LoadTests(); }

    private void AddQuestion()
    {
        if (_currentTestId == 0 || string.IsNullOrWhiteSpace(_qText.Text)) return;
        var type = _type.Text;
        var qid = DataAccess.ExecuteInsert("INSERT INTO Questions(TestId,Type,Text,Points,SettingsJson) VALUES(@t,@ty,@tx,@p,@s)",
            ("@t", _currentTestId), ("@ty", type), ("@tx", _qText.Text), ("@p", _points.Value),
            ("@s", type == "Numeric" ? JsonSerializer.Serialize(new { correct = (double)_numCorrect.Value, tolerance = (double)_numTol.Value }) : type == "Text" ? JsonSerializer.Serialize(new { sample = _textSample.Text }) : "{}"));
        if (type is "SingleChoice" or "MultipleChoice")
            for (var i = 0; i < 4; i++)
                DataAccess.Execute("INSERT INTO Options(QuestionId,Text,IsCorrect,SortOrder) VALUES(@q,@t,@c,@s)", ("@q", qid), ("@t", _optText[i].Text), ("@c", type == "SingleChoice" ? (_singleRadios[i].Checked ? 1 : 0) : (_multiChecks[i].Checked ? 1 : 0)), ("@s", i));
        LoadQuestions();
    }

    private void DeleteQuestion() { var qid = CurrentQuestionId(); if (qid == 0) return; DataAccess.Execute("DELETE FROM Questions WHERE Id=@id", ("@id", qid)); LoadQuestions(); }
    private void MoveQuestion(int delta) { }
    private void DuplicateQuestion()
    {
        var qid = CurrentQuestionId(); if (qid == 0) return;
        var q = DataAccess.Table("SELECT Type,Text,Points,SettingsJson FROM Questions WHERE Id=@id", ("@id", qid));
        if (q.Rows.Count == 0) return;
        var row = q.Rows[0];
        var nq = DataAccess.ExecuteInsert("INSERT INTO Questions(TestId,Type,Text,Points,SettingsJson) VALUES(@t,@ty,@tx,@p,@s)", ("@t", _currentTestId), ("@ty", row[0]), ("@tx", row[1]+" (Copy)"), ("@p", row[2]), ("@s", row[3]));
        var opts = DataAccess.Table("SELECT Text,IsCorrect,SortOrder FROM Options WHERE QuestionId=@q", ("@q", qid));
        foreach (DataRow op in opts.Rows) DataAccess.Execute("INSERT INTO Options(QuestionId,Text,IsCorrect,SortOrder) VALUES(@q,@t,@c,@s)", ("@q", nq), ("@t", op[0]), ("@c", op[1]), ("@s", op[2]));
        LoadQuestions();
    }

    private void SaveDraft()
    {
        if (_currentTestId == 0) return;
        DataAccess.Execute("UPDATE Tests SET Title=@t,Description=@d,PassPercent=@p WHERE Id=@id AND Status='Draft'", ("@t", _title.Text), ("@d", _desc.Text), ("@p", _pass.Value), ("@id", _currentTestId));
        MessageBox.Show(Loc.T("Saved"));
        LoadTests();
        SelectTest(_currentTestId);
    }

    private void Preview()
    {
        if (_currentTestId == 0) return;
        var q = DataAccess.Table("SELECT Text FROM Questions WHERE TestId=@t ORDER BY Id", ("@t", _currentTestId));
        using var f = new Form { Width = 700, Height = 500, StartPosition = FormStartPosition.CenterParent, Text = Loc.T("Preview") };
        var lb = new ListBox { Dock = DockStyle.Fill };
        foreach (DataRow r in q.Rows) lb.Items.Add(r[0]?.ToString() ?? "");
        f.Controls.Add(lb);
        ThemeManager.ApplyTheme(f);
        f.ShowDialog();
    }
}

public sealed class AttemptPlayerForm : Form
{
    private readonly SessionUser _user;
    private readonly long _attemptId;
    private readonly bool _reviewMode;
    private JsonDocument _snap = JsonDocument.Parse("{}");
    private readonly ListBox _qList = new() { Dock = DockStyle.Left, Width = 220 };
    private readonly Label _title = new() { Dock = DockStyle.Top, Height = 28 };
    private readonly Label _timer = new() { Dock = DockStyle.Top, Height = 28 };
    private readonly Panel _answer = new() { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(12) };
    private readonly System.Windows.Forms.Timer _tick = new() { Interval = 1000 };
    private DateTime _endsAt;

    public AttemptPlayerForm(SessionUser user, long attemptId, bool reviewMode = false)
    {
        _user = user; _attemptId = attemptId; _reviewMode = reviewMode;
        Size = new Size(1200, 760); MinimumSize = new Size(980, 640); StartPosition = FormStartPosition.CenterParent; Font = AppTheme.Font;
        var top = new Panel { Dock = DockStyle.Top, Height = 60 }; top.Controls.Add(_timer); top.Controls.Add(_title);
        var nav = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 46 };
        var back = AppTheme.FlatButton("Back"); var next = AppTheme.FlatButton("Next"); var save = AppTheme.FlatButton("Save"); var submit = AppTheme.FlatButton("Submit");
        nav.Controls.AddRange(new Control[] { back, next, save, submit });
        back.Click += (_, _) => { if (_qList.SelectedIndex > 0) _qList.SelectedIndex--; };
        next.Click += (_, _) => { if (_qList.SelectedIndex < _qList.Items.Count - 1) _qList.SelectedIndex++; };
        save.Click += (_, _) => SaveCurrent();
        submit.Click += (_, _) => SubmitAttempt(true);
        _qList.SelectedIndexChanged += (_, _) => RenderQuestion();
        _answer.Resize += (_, _) => { if (_qList.SelectedIndex >= 0) RenderQuestion(); };
        _tick.Tick += (_, _) => { var left = _endsAt - DateTime.UtcNow; _timer.Text = "Time: " + (left <= TimeSpan.Zero ? "00:00" : left.ToString("hh\\:mm\\:ss")); if (left <= TimeSpan.Zero && !_reviewMode) SubmitAttempt(false); };
        Controls.Add(_answer); Controls.Add(_qList); Controls.Add(nav); Controls.Add(top);
        LoadAttempt();
    }

    private void LoadAttempt()
    {
        using var c = Database.Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT SnapshotJson,EndsAt,Status FROM Attempts WHERE Id=@id AND UserId=@u";
        cmd.Parameters.AddWithValue("@id", _attemptId); cmd.Parameters.AddWithValue("@u", _user.Id);
        using var r = cmd.ExecuteReader(); if (!r.Read()) { Close(); return; }
        _snap = JsonDocument.Parse(r.GetString(0)); _endsAt = DateTime.Parse(r.GetString(1));
        _title.Text = _snap.RootElement.GetProperty("title").GetString() ?? "";
        var arr = _snap.RootElement.GetProperty("questions").EnumerateArray().ToArray();
        _qList.Items.Clear(); for (var i = 0; i < arr.Length; i++) _qList.Items.Add($"Q{i + 1}");
        if (_qList.Items.Count > 0) _qList.SelectedIndex = 0;
        if (!_reviewMode && r.GetString(2) == "Active") _tick.Start();
    }

    private int CurrentIndex() => _qList.SelectedIndex < 0 ? 0 : _qList.SelectedIndex;

    private void RenderQuestion()
    {
        _answer.Controls.Clear();
        var q = _snap.RootElement.GetProperty("questions")[CurrentIndex()];
        var lbl = new Label
        {
            Text = q.GetProperty("text").GetString() ?? string.Empty,
            AutoSize = true,
            MaximumSize = new Size(Math.Max(500, _answer.ClientSize.Width - 40), 0),
            Top = 10,
            Left = 10,
            Font = new Font(AppTheme.Font, FontStyle.Bold)
        };
        _answer.Controls.Add(lbl);
        var nextTop = lbl.Bottom + 16;
        if (q.GetProperty("type").GetString() == "SingleChoice")
        {
            var top = nextTop;
            foreach (var op in q.GetProperty("options").EnumerateArray())
            {
                var rb = new RadioButton { Left = 20, Top = top, Width = 620, Text = op.GetProperty("text").GetString(), Tag = op.GetProperty("id").GetInt64() };
                _answer.Controls.Add(rb); top += 32;
            }
        }
        else
        {
            var tb = new TextBox { Left = 20, Top = nextTop, Width = 240, Name = "num" };
            _answer.Controls.Add(tb);
        }
        if (_reviewMode) LoadSavedAndReview(q);
    }

    private void SaveCurrent()
    {
        if (_reviewMode) return;
        var q = _snap.RootElement.GetProperty("questions")[CurrentIndex()];
        var qid = q.GetProperty("id").GetInt64();
        string answer;
        if (q.GetProperty("type").GetString() == "SingleChoice")
        {
            var rb = _answer.Controls.OfType<RadioButton>().FirstOrDefault(x => x.Checked);
            if (rb is null) return;
            answer = JsonSerializer.Serialize(new { selectedOptionId = Convert.ToInt64(rb.Tag) });
        }
        else
        {
            var txt = _answer.Controls.Find("num", true).OfType<TextBox>().FirstOrDefault()?.Text ?? "";
            if (!double.TryParse(txt.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var val)) return;
            answer = JsonSerializer.Serialize(new { value = val });
        }
        DataAccess.Execute("INSERT INTO AttemptAnswers(AttemptId,QuestionId,AnswerJson,SavedAt) VALUES(@a,@q,@ans,@at) ON CONFLICT(AttemptId,QuestionId) DO UPDATE SET AnswerJson=excluded.AnswerJson,SavedAt=excluded.SavedAt", ("@a", _attemptId), ("@q", qid), ("@ans", answer), ("@at", DateTime.UtcNow.ToString("O")));
    }

    private void SubmitAttempt(bool manual)
    {
        if (_reviewMode) return;
        _tick.Stop();
        using var c = Database.Open(); using var tx = c.BeginTransaction();
        try
        {
            double score = 0, max = 0;
            var details = new List<object>();
            foreach (var q in _snap.RootElement.GetProperty("questions").EnumerateArray())
            {
                var qid = q.GetProperty("id").GetInt64();
                var points = q.GetProperty("points").GetDouble();
                max += points;
                using var ansCmd = c.CreateCommand(); ansCmd.Transaction = tx;
                ansCmd.CommandText = "SELECT AnswerJson FROM AttemptAnswers WHERE AttemptId=@a AND QuestionId=@q";
                ansCmd.Parameters.AddWithValue("@a", _attemptId); ansCmd.Parameters.AddWithValue("@q", qid);
                var raw = ansCmd.ExecuteScalar()?.ToString();
                var ok = false;
                if (!string.IsNullOrWhiteSpace(raw))
                {
                    using var doc = JsonDocument.Parse(raw);
                    if (q.GetProperty("type").GetString() == "SingleChoice")
                    {
                        var selected = doc.RootElement.GetProperty("selectedOptionId").GetInt64();
                        ok = q.GetProperty("options").EnumerateArray().Any(o => o.GetProperty("id").GetInt64() == selected && o.GetProperty("isCorrect").GetBoolean());
                    }
                    else
                    {
                        var settings = JsonDocument.Parse(q.GetProperty("settings").GetString()!);
                        var correct = settings.RootElement.GetProperty("correct").GetDouble();
                        var tol = settings.RootElement.GetProperty("tolerance").GetDouble();
                        var value = doc.RootElement.GetProperty("value").GetDouble();
                        ok = Math.Abs(value - correct) <= tol;
                    }
                }
                if (ok) score += points;
                details.Add(new { questionId = qid, correct = ok });
            }
            var percent = max == 0 ? 0 : score / max * 100;
            var pass = percent >= _snap.RootElement.GetProperty("passPercent").GetDouble();
            using var upd = c.CreateCommand(); upd.Transaction = tx;
            upd.CommandText = "UPDATE Attempts SET Status=@s,SubmittedAt=@at WHERE Id=@id"; upd.Parameters.AddWithValue("@s", manual ? "Submitted" : "Expired"); upd.Parameters.AddWithValue("@at", DateTime.UtcNow.ToString("O")); upd.Parameters.AddWithValue("@id", _attemptId); upd.ExecuteNonQuery();
            using var ir = c.CreateCommand(); ir.Transaction = tx;
            ir.CommandText = "INSERT INTO AttemptResults(AttemptId,Score,MaxScore,Percent,Passed,CheckedAt,DetailsJson) VALUES(@a,@s,@m,@p,@ps,@at,@d)";
            ir.Parameters.AddWithValue("@a", _attemptId); ir.Parameters.AddWithValue("@s", score); ir.Parameters.AddWithValue("@m", max); ir.Parameters.AddWithValue("@p", percent); ir.Parameters.AddWithValue("@ps", pass ? 1 : 0); ir.Parameters.AddWithValue("@at", DateTime.UtcNow.ToString("O")); ir.Parameters.AddWithValue("@d", JsonSerializer.Serialize(details)); ir.ExecuteNonQuery();
            tx.Commit();
            Audit.Log(_user.Id, "SubmitAttempt", "Attempt", _attemptId, new { manual });
            MessageBox.Show($"Submitted: {percent:F1}%");
            Close();
        }
        catch { tx.Rollback(); }
    }

    private void LoadSavedAndReview(JsonElement q)
    {
        using var c = Database.Open();
        using var chk = c.CreateCommand(); chk.CommandText = "SELECT ass.ShowCorrectAfter,ass.ShowScoreAfter,aa.AnswerJson FROM Attempts a JOIN Assignments ass ON ass.Id=a.AssignmentId LEFT JOIN AttemptAnswers aa ON aa.AttemptId=a.Id AND aa.QuestionId=@q WHERE a.Id=@a";
        chk.Parameters.AddWithValue("@a", _attemptId); chk.Parameters.AddWithValue("@q", q.GetProperty("id").GetInt64());
        using var r = chk.ExecuteReader(); if (!r.Read()) return;
        var showCorrect = r.GetInt64(0) == 1;
        if (!r.IsDBNull(2))
        {
            using var ans = JsonDocument.Parse(r.GetString(2));
            if (q.GetProperty("type").GetString() == "SingleChoice")
            {
                var sel = ans.RootElement.GetProperty("selectedOptionId").GetInt64();
                foreach (var rb in _answer.Controls.OfType<RadioButton>())
                {
                    rb.Checked = Convert.ToInt64(rb.Tag) == sel;
                    rb.Enabled = false;
                    if (showCorrect && q.GetProperty("options").EnumerateArray().Any(o => o.GetProperty("id").GetInt64() == Convert.ToInt64(rb.Tag) && o.GetProperty("isCorrect").GetBoolean())) rb.ForeColor = Color.Green;
                }
            }
            else
            {
                var tb = _answer.Controls.Find("num", true).OfType<TextBox>().FirstOrDefault();
                if (tb is not null) { tb.Text = ans.RootElement.GetProperty("value").GetDouble().ToString(); tb.ReadOnly = true; }
            }
        }
    }
}
