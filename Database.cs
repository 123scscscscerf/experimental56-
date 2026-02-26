using Microsoft.Data.Sqlite;

namespace Experimental56;

public static class Database
{
    public static readonly string DbPath = Path.Combine(AppContext.BaseDirectory, "app.db");
    public static string ConnectionString => new SqliteConnectionStringBuilder { DataSource = DbPath }.ToString();

    public static SqliteConnection Open()
    {
        var c = new SqliteConnection(ConnectionString);
        c.Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "PRAGMA foreign_keys=ON;";
        cmd.ExecuteNonQuery();
        return c;
    }

    public static void Initialize()
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Users (
 Id INTEGER PRIMARY KEY AUTOINCREMENT,
 Login TEXT NOT NULL UNIQUE,
 DisplayName TEXT NOT NULL,
 Role TEXT NOT NULL,
 Salt TEXT NOT NULL,
 Hash TEXT NOT NULL,
 IsActive INTEGER NOT NULL DEFAULT 1,
 CreatedAt TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS Groups (
 Id INTEGER PRIMARY KEY AUTOINCREMENT,
 Name TEXT NOT NULL UNIQUE
);
CREATE TABLE IF NOT EXISTS GroupMembers (
 GroupId INTEGER NOT NULL,
 UserId INTEGER NOT NULL,
 PRIMARY KEY (GroupId, UserId),
 FOREIGN KEY(GroupId) REFERENCES Groups(Id) ON DELETE CASCADE,
 FOREIGN KEY(UserId) REFERENCES Users(Id) ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS Tests (
 Id INTEGER PRIMARY KEY AUTOINCREMENT,
 Title TEXT NOT NULL,
 Description TEXT NOT NULL,
 CreatedByTeacherId INTEGER NOT NULL,
 Status TEXT NOT NULL,
 PassPercent REAL NOT NULL,
 CreatedAt TEXT NOT NULL,
 FOREIGN KEY(CreatedByTeacherId) REFERENCES Users(Id)
);
CREATE TABLE IF NOT EXISTS Questions (
 Id INTEGER PRIMARY KEY AUTOINCREMENT,
 TestId INTEGER NOT NULL,
 Type TEXT NOT NULL,
 Text TEXT NOT NULL,
 Points REAL NOT NULL,
 SettingsJson TEXT NOT NULL,
 FOREIGN KEY(TestId) REFERENCES Tests(Id) ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS Options (
 Id INTEGER PRIMARY KEY AUTOINCREMENT,
 QuestionId INTEGER NOT NULL,
 Text TEXT NOT NULL,
 IsCorrect INTEGER NOT NULL,
 SortOrder INTEGER NOT NULL,
 FOREIGN KEY(QuestionId) REFERENCES Questions(Id) ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS Assignments (
 Id INTEGER PRIMARY KEY AUTOINCREMENT,
 TestId INTEGER NOT NULL,
 TargetType TEXT NOT NULL,
 TargetId INTEGER NOT NULL,
 AvailableFrom TEXT NOT NULL,
 Deadline TEXT NOT NULL,
 AttemptLimit INTEGER NOT NULL,
 TimeLimitMinutes INTEGER NOT NULL,
 ShuffleQuestions INTEGER NOT NULL,
 ShuffleOptions INTEGER NOT NULL,
 ShowScoreAfter INTEGER NOT NULL,
 ShowCorrectAfter INTEGER NOT NULL,
 IsActive INTEGER NOT NULL,
 FOREIGN KEY(TestId) REFERENCES Tests(Id)
);
CREATE TABLE IF NOT EXISTS Attempts (
 Id INTEGER PRIMARY KEY AUTOINCREMENT,
 AssignmentId INTEGER NOT NULL,
 UserId INTEGER NOT NULL,
 StartedAt TEXT NOT NULL,
 EndsAt TEXT NOT NULL,
 SubmittedAt TEXT,
 Status TEXT NOT NULL,
 SnapshotJson TEXT NOT NULL,
 FOREIGN KEY(AssignmentId) REFERENCES Assignments(Id),
 FOREIGN KEY(UserId) REFERENCES Users(Id)
);
CREATE TABLE IF NOT EXISTS AttemptAnswers (
 Id INTEGER PRIMARY KEY AUTOINCREMENT,
 AttemptId INTEGER NOT NULL,
 QuestionId INTEGER NOT NULL,
 AnswerJson TEXT NOT NULL,
 SavedAt TEXT NOT NULL,
 UNIQUE(AttemptId, QuestionId),
 FOREIGN KEY(AttemptId) REFERENCES Attempts(Id) ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS AttemptResults (
 AttemptId INTEGER PRIMARY KEY,
 Score REAL NOT NULL,
 MaxScore REAL NOT NULL,
 Percent REAL NOT NULL,
 Passed INTEGER NOT NULL,
 CheckedAt TEXT NOT NULL,
 DetailsJson TEXT NOT NULL,
 FOREIGN KEY(AttemptId) REFERENCES Attempts(Id) ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS AuditLog (
 Id INTEGER PRIMARY KEY AUTOINCREMENT,
 ActorUserId INTEGER,
 Action TEXT NOT NULL,
 EntityType TEXT NOT NULL,
 EntityId INTEGER,
 At TEXT NOT NULL,
 MetaJson TEXT NOT NULL
);
";
        cmd.ExecuteNonQuery();
    }
}
