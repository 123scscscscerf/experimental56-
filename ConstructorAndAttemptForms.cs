using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;

namespace Experimental56;

public sealed class ConstructorForm : Form
{
    private readonly SessionUser _user;
    private long? _testId;
    private readonly TextBox _title = new() { Width = 300 };
    private readonly TextBox _desc = new() { Width = 300 };
    private readonly NumericUpDown _pass = new() { Minimum = 1, Maximum = 100, Value = 70 };
    private readonly ListBox _questions = new() { Width = 260, Dock = DockStyle.Left };
    private readonly ComboBox _type = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160 };
    private readonly TextBox _qText = new() { Width = 420 };
    private readonly NumericUpDown _points = new() { Minimum = 0.1M, Maximum = 100, DecimalPlaces = 1, Value = 1 };
    private readonly TextBox[] _optText = { new(), new(), new(), new() };
    private readonly RadioButton[] _optRadio = { new(), new(), new(), new() };
    private readonly NumericUpDown _numCorrect = new() { DecimalPlaces = 2, Maximum = 100000, Minimum = -100000 };
    private readonly NumericUpDown _numTol = new() { DecimalPlaces = 3, Maximum = 1000, Minimum = 0, Value = 0.01M };
    private readonly Panel _singlePanel = new() { Height = 160, Width = 500 };
    private readonly Panel _numPanel = new() { Height = 70, Width = 500 };

    public ConstructorForm(SessionUser user, long? testId)
    {
        _user = user; _testId = testId;
        Text = "Constructor"; Size = new Size(1040, 700); StartPosition = FormStartPosition.CenterParent; Font = AppTheme.Font;
        _type.Items.AddRange(new[] { "SingleChoice", "Numeric" }); _type.SelectedIndex = 0;
        _type.SelectedIndexChanged += (_, _) => RefreshTypePanels();

        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44 };
        var addQ = AppTheme.FlatButton("Add Question"); var del = AppTheme.FlatButton("Delete Question"); var up = AppTheme.FlatButton("Move Up"); var down = AppTheme.FlatButton("Move Down"); var save = AppTheme.FlatButton("Save Draft");
        top.Controls.AddRange(new Control[] { new Label { Text = "Title" }, _title, new Label { Text = "Description" }, _desc, new Label { Text = "Pass%" }, _pass, addQ, del, up, down, save });
        addQ.Click += (_, _) => AddQuestion(); del.Click += (_, _) => DeleteQuestion(); up.Click += (_, _) => Move(-1); down.Click += (_, _) => Move(1); save.Click += (_, _) => SaveDraft();

        var right = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10), BackColor = AppTheme.Panel };
        var editFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
        editFlow.Controls.AddRange(new Control[] { new Label { Text = "Type" }, _type, new Label { Text = "Text" }, _qText, new Label { Text = "Points" }, _points });
        BuildSinglePanel(); BuildNumericPanel(); editFlow.Controls.Add(_singlePanel); editFlow.Controls.Add(_numPanel);
        right.Controls.Add(editFlow);

        _questions.SelectedIndexChanged += (_, _) => LoadQuestionIntoEditor();
        Controls.Add(right); Controls.Add(_questions); Controls.Add(top);
        LoadOrCreate();
    }

    private void BuildSinglePanel()
    {
        _singlePanel.Controls.Clear();
        for (var i = 0; i < 4; i++)
        {
            _optText[i].Width = 360; _optText[i].Top = i * 34; _optText[i].Left = 30;
            _optRadio[i].Top = i * 34 + 6; _optRadio[i].Left = 5;
            _singlePanel.Controls.Add(_optText[i]); _singlePanel.Controls.Add(_optRadio[i]);
        }
    }

    private void BuildNumericPanel()
    {
        _numPanel.Controls.Add(new Label { Text = "Correct", Left = 5, Top = 8, Width = 60 });
        _numCorrect.Left = 70; _numCorrect.Top = 5; _numPanel.Controls.Add(_numCorrect);
        _numPanel.Controls.Add(new Label { Text = "Tolerance", Left = 230, Top = 8, Width = 80 });
        _numTol.Left = 315; _numTol.Top = 5; _numPanel.Controls.Add(_numTol);
    }

    private void RefreshTypePanels() { _singlePanel.Visible = _type.Text == "SingleChoice"; _numPanel.Visible = _type.Text == "Numeric"; }

    private void LoadOrCreate()
    {
        if (_testId is null)
        {
            _testId = DataAccess.ExecuteInsert("INSERT INTO Tests(Title,Description,CreatedByTeacherId,Status,PassPercent,CreatedAt) VALUES('New Test','',@u,'Draft',70,@at)", ("@u", _user.Id), ("@at", DateTime.UtcNow.ToString("O")));
            Audit.Log(_user.Id, "CreateTest", "Test", _testId);
        }
        using var c = Database.Open();
        using var t = c.CreateCommand(); t.CommandText = "SELECT Title,Description,PassPercent,Status FROM Tests WHERE Id=@id"; t.Parameters.AddWithValue("@id", _testId);
        using var tr = t.ExecuteReader(); if (tr.Read()) { _title.Text = tr.GetString(0); _desc.Text = tr.GetString(1); _pass.Value = (decimal)tr.GetDouble(2); if (tr.GetString(3) == "Published") MessageBox.Show("Published тест не редактируется. Используйте Clone."); }
        LoadQuestions();
    }

    private void LoadQuestions()
    {
        _questions.DisplayMember = "Text"; _questions.ValueMember = "Id";
        _questions.DataSource = DataAccess.Table("SELECT Id, Text FROM Questions WHERE TestId=@t ORDER BY Id", ("@t", _testId!.Value));
    }

    private long CurrentQId() => _questions.SelectedItem is null ? 0 : Convert.ToInt64(((System.Data.DataRowView)_questions.SelectedItem)["Id"]);

    private void LoadQuestionIntoEditor()
    {
        var qid = CurrentQId(); if (qid == 0) return;
        using var c = Database.Open();
        using var q = c.CreateCommand(); q.CommandText = "SELECT Type,Text,Points,SettingsJson FROM Questions WHERE Id=@id"; q.Parameters.AddWithValue("@id", qid);
        using var qr = q.ExecuteReader(); if (!qr.Read()) return;
        _type.Text = qr.GetString(0); _qText.Text = qr.GetString(1); _points.Value = (decimal)qr.GetDouble(2);
        if (_type.Text == "SingleChoice")
        {
            using var o = c.CreateCommand(); o.CommandText = "SELECT Text,IsCorrect FROM Options WHERE QuestionId=@q ORDER BY SortOrder"; o.Parameters.AddWithValue("@q", qid);
            using var or = o.ExecuteReader(); var i = 0; while (or.Read() && i < 4) { _optText[i].Text = or.GetString(0); _optRadio[i].Checked = or.GetInt64(1) == 1; i++; }
        }
        else
        {
            var doc = JsonDocument.Parse(qr.GetString(3)); _numCorrect.Value = (decimal)doc.RootElement.GetProperty("correct").GetDouble(); _numTol.Value = (decimal)doc.RootElement.GetProperty("tolerance").GetDouble();
        }
        RefreshTypePanels();
    }

    private void AddQuestion()
    {
        if (!ValidateEditor()) return;
        if (_type.Text == "SingleChoice")
        {
            var qid = DataAccess.ExecuteInsert("INSERT INTO Questions(TestId,Type,Text,Points,SettingsJson) VALUES(@t,'SingleChoice',@tx,@p,'{}')", ("@t", _testId!.Value), ("@tx", _qText.Text), ("@p", _points.Value));
            for (var i = 0; i < 4; i++) DataAccess.Execute("INSERT INTO Options(QuestionId,Text,IsCorrect,SortOrder) VALUES(@q,@t,@c,@s)", ("@q", qid), ("@t", _optText[i].Text), ("@c", _optRadio[i].Checked ? 1 : 0), ("@s", i));
        }
        else
        {
            DataAccess.Execute("INSERT INTO Questions(TestId,Type,Text,Points,SettingsJson) VALUES(@t,'Numeric',@tx,@p,@s)", ("@t", _testId!.Value), ("@tx", _qText.Text), ("@p", _points.Value), ("@s", JsonSerializer.Serialize(new { correct = (double)_numCorrect.Value, tolerance = (double)_numTol.Value })));
        }
        LoadQuestions();
    }

    private void DeleteQuestion() { var qid = CurrentQId(); if (qid == 0) return; DataAccess.Execute("DELETE FROM Questions WHERE Id=@id", ("@id", qid)); LoadQuestions(); }
    private void Move(int dir) { }

    private void SaveDraft()
    {
        if (string.IsNullOrWhiteSpace(_title.Text)) { MessageBox.Show("Title required"); return; }
        DataAccess.Execute("UPDATE Tests SET Title=@t,Description=@d,PassPercent=@p WHERE Id=@id AND Status='Draft'", ("@t", _title.Text), ("@d", _desc.Text), ("@p", _pass.Value), ("@id", _testId!.Value));
        Audit.Log(_user.Id, "SaveDraft", "Test", _testId);
        MessageBox.Show("Saved");
    }

    private bool ValidateEditor()
    {
        if (string.IsNullOrWhiteSpace(_qText.Text)) return false;
        if (_type.Text == "SingleChoice")
        {
            if (_optText.Any(t => string.IsNullOrWhiteSpace(t.Text))) return false;
            if (!_optRadio.Any(r => r.Checked)) return false;
        }
        return true;
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
    private readonly Panel _answer = new() { Dock = DockStyle.Fill };
    private readonly System.Windows.Forms.Timer _tick = new() { Interval = 1000 };
    private DateTime _endsAt;

    public AttemptPlayerForm(SessionUser user, long attemptId, bool reviewMode = false)
    {
        _user = user; _attemptId = attemptId; _reviewMode = reviewMode;
        Size = new Size(980, 650); StartPosition = FormStartPosition.CenterParent; Font = AppTheme.Font;
        var top = new Panel { Dock = DockStyle.Top, Height = 60 }; top.Controls.Add(_timer); top.Controls.Add(_title);
        var nav = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 46 };
        var back = AppTheme.FlatButton("Back"); var next = AppTheme.FlatButton("Next"); var save = AppTheme.FlatButton("Save"); var submit = AppTheme.FlatButton("Submit");
        nav.Controls.AddRange(new Control[] { back, next, save, submit });
        back.Click += (_, _) => { if (_qList.SelectedIndex > 0) _qList.SelectedIndex--; };
        next.Click += (_, _) => { if (_qList.SelectedIndex < _qList.Items.Count - 1) _qList.SelectedIndex++; };
        save.Click += (_, _) => SaveCurrent();
        submit.Click += (_, _) => SubmitAttempt(true);
        _qList.SelectedIndexChanged += (_, _) => RenderQuestion();
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
        var lbl = new Label { Text = q.GetProperty("text").GetString(), AutoSize = false, Width = 650, Height = 70, Top = 10, Left = 10 };
        _answer.Controls.Add(lbl);
        if (q.GetProperty("type").GetString() == "SingleChoice")
        {
            var top = 90;
            foreach (var op in q.GetProperty("options").EnumerateArray())
            {
                var rb = new RadioButton { Left = 20, Top = top, Width = 620, Text = op.GetProperty("text").GetString(), Tag = op.GetProperty("id").GetInt64() };
                _answer.Controls.Add(rb); top += 32;
            }
        }
        else
        {
            var tb = new TextBox { Left = 20, Top = 90, Width = 200, Name = "num" };
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
