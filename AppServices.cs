using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Experimental56;

public static class PasswordHasher
{
    public static (string Salt, string Hash) HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return (Convert.ToBase64String(salt), Convert.ToBase64String(hash));
    }

    public static bool Verify(string password, string saltB64, string hashB64)
    {
        var salt = Convert.FromBase64String(saltB64);
        var expected = Convert.FromBase64String(hashB64);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}

public static class Audit
{
    public static void Log(long? actorUserId, string action, string entityType, long? entityId, object? meta = null)
    {
        using var c = Database.Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "INSERT INTO AuditLog(ActorUserId,Action,EntityType,EntityId,At,MetaJson) VALUES(@a,@ac,@et,@eid,@at,@m)";
        cmd.Parameters.AddWithValue("@a", actorUserId is null ? DBNull.Value : actorUserId.Value);
        cmd.Parameters.AddWithValue("@ac", action);
        cmd.Parameters.AddWithValue("@et", entityType);
        cmd.Parameters.AddWithValue("@eid", entityId is null ? DBNull.Value : entityId.Value);
        cmd.Parameters.AddWithValue("@at", DateTime.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("@m", meta is null ? "{}" : JsonSerializer.Serialize(meta));
        cmd.ExecuteNonQuery();
    }
}

public static class AuthService
{
    public static SessionUser? Login(string login, string password)
    {
        using var c = Database.Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT Id,Login,DisplayName,Role,Salt,Hash,IsActive FROM Users WHERE Login=@l";
        cmd.Parameters.AddWithValue("@l", login);
        using var r = cmd.ExecuteReader();
        if (!r.Read())
        {
            Audit.Log(null, "LoginFail", "User", null, new { login });
            return null;
        }
        if (r.GetInt64(6) == 0 || !PasswordHasher.Verify(password, r.GetString(4), r.GetString(5)))
        {
            Audit.Log(r.GetInt64(0), "LoginFail", "User", r.GetInt64(0), new { login });
            return null;
        }
        var user = new SessionUser { Id = r.GetInt64(0), Login = r.GetString(1), DisplayName = r.GetString(2), Role = Enum.Parse<UserRole>(r.GetString(3)) };
        Audit.Log(user.Id, "LoginSuccess", "User", user.Id, new { login = user.Login });
        return user;
    }

    public static void ChangePassword(long userId, string password)
    {
        var (salt, hash) = PasswordHasher.HashPassword(password);
        using var c = Database.Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "UPDATE Users SET Salt=@s, Hash=@h WHERE Id=@id";
        cmd.Parameters.AddWithValue("@s", salt);
        cmd.Parameters.AddWithValue("@h", hash);
        cmd.Parameters.AddWithValue("@id", userId);
        cmd.ExecuteNonQuery();
        Audit.Log(userId, "ChangePassword", "User", userId);
    }
}

public static class SeedData
{
    public static void EnsureSeeded()
    {
        using var c = Database.Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Users";
        if (Convert.ToInt32(cmd.ExecuteScalar()) > 0) return;

        CreateUser(c, "admin", "Administrator", UserRole.Admin, "admin123");
        var teacher = CreateUser(c, "teacher", "Teacher", UserRole.Teacher, "teacher123");
        for (var i = 1; i <= 6; i++) CreateUser(c, $"student{i}", $"Student {i}", UserRole.Student, "student123");

        Exec(c, "INSERT INTO Groups(Name) VALUES ('Group A'),('Group B')");
        Exec(c, "INSERT INTO GroupMembers(GroupId,UserId) SELECT 1, Id FROM Users WHERE Login IN ('student1','student2','student3')");
        Exec(c, "INSERT INTO GroupMembers(GroupId,UserId) SELECT 2, Id FROM Users WHERE Login IN ('student4','student5','student6')");

        var tests = new[] {
            ("Линейные уравнения","Базовые линейные", "Published"),
            ("Дроби и проценты","Смешанный", "Published"),
            ("Квадратные уравнения","Дискриминант", "Draft"),
            ("Геометрия: площади","Площади фигур", "Draft"),
            ("Вероятность","Простые события", "Published"),
            ("Логарифмы и степени","Степенные выражения", "Draft")
        };
        foreach (var t in tests)
        {
            using var tc = c.CreateCommand();
            tc.CommandText = "INSERT INTO Tests(Title,Description,CreatedByTeacherId,Status,PassPercent,CreatedAt) VALUES(@t,@d,@u,@s,70,@at)";
            tc.Parameters.AddWithValue("@t", t.Item1);
            tc.Parameters.AddWithValue("@d", t.Item2);
            tc.Parameters.AddWithValue("@u", teacher);
            tc.Parameters.AddWithValue("@s", t.Item3);
            tc.Parameters.AddWithValue("@at", DateTime.UtcNow.ToString("O"));
            tc.ExecuteNonQuery();
            var testId = c.LastInsertRowId;
            SeedQuestionsForTest(c, testId, t.Item1);
        }
        Exec(c, "INSERT INTO Assignments(TestId,TargetType,TargetId,AvailableFrom,Deadline,AttemptLimit,TimeLimitMinutes,ShuffleQuestions,ShuffleOptions,ShowScoreAfter,ShowCorrectAfter,IsActive) VALUES (1,'Group',1,@af,@dl,2,30,1,1,1,1,1),(2,'Group',1,@af,@dl,2,30,1,1,1,0,1),(5,'Group',2,@af,@dl,2,30,0,0,1,1,1)",
            new Dictionary<string, object> { ["@af"] = DateTime.UtcNow.AddDays(-1).ToString("O"), ["@dl"] = DateTime.UtcNow.AddMonths(3).ToString("O") });
    }

    private static void SeedQuestionsForTest(SqliteConnection c, long testId, string title)
    {
        for (var i = 1; i <= 6; i++)
        {
            if (title.Contains("Геометрия") || (title.Contains("Дроби") && i % 2 == 0))
            {
                var correct = i * 2.5;
                InsertNumeric(c, testId, $"Вычислите значение #{i}", 1, correct, 0.01);
            }
            else
            {
                InsertSingle(c, testId, $"Вопрос #{i} по теме {title}", 1, new[] { "A", "B", "C", "D" }, i % 4);
            }
        }
    }

    private static long CreateUser(SqliteConnection c, string login, string display, UserRole role, string password)
    {
        var (salt, hash) = PasswordHasher.HashPassword(password);
        using var cmd = c.CreateCommand();
        cmd.CommandText = "INSERT INTO Users(Login,DisplayName,Role,Salt,Hash,IsActive,CreatedAt) VALUES(@l,@d,@r,@s,@h,1,@at)";
        cmd.Parameters.AddWithValue("@l", login);
        cmd.Parameters.AddWithValue("@d", display);
        cmd.Parameters.AddWithValue("@r", role.ToString());
        cmd.Parameters.AddWithValue("@s", salt);
        cmd.Parameters.AddWithValue("@h", hash);
        cmd.Parameters.AddWithValue("@at", DateTime.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();
        return c.LastInsertRowId;
    }

    private static void InsertSingle(SqliteConnection c, long testId, string text, double points, string[] opts, int correctIdx)
    {
        using var q = c.CreateCommand();
        q.CommandText = "INSERT INTO Questions(TestId,Type,Text,Points,SettingsJson) VALUES(@t,'SingleChoice',@tx,@p,'{}')";
        q.Parameters.AddWithValue("@t", testId);
        q.Parameters.AddWithValue("@tx", text);
        q.Parameters.AddWithValue("@p", points);
        q.ExecuteNonQuery();
        var qid = c.LastInsertRowId;
        for (var i = 0; i < 4; i++)
        {
            using var o = c.CreateCommand();
            o.CommandText = "INSERT INTO Options(QuestionId,Text,IsCorrect,SortOrder) VALUES(@q,@t,@c,@s)";
            o.Parameters.AddWithValue("@q", qid);
            o.Parameters.AddWithValue("@t", opts[i]);
            o.Parameters.AddWithValue("@c", i == correctIdx ? 1 : 0);
            o.Parameters.AddWithValue("@s", i);
            o.ExecuteNonQuery();
        }
    }

    private static void InsertNumeric(SqliteConnection c, long testId, string text, double points, double correct, double tolerance)
    {
        using var q = c.CreateCommand();
        q.CommandText = "INSERT INTO Questions(TestId,Type,Text,Points,SettingsJson) VALUES(@t,'Numeric',@tx,@p,@s)";
        q.Parameters.AddWithValue("@t", testId);
        q.Parameters.AddWithValue("@tx", text);
        q.Parameters.AddWithValue("@p", points);
        q.Parameters.AddWithValue("@s", JsonSerializer.Serialize(new { correct, tolerance }));
        q.ExecuteNonQuery();
    }

    private static void Exec(SqliteConnection c, string sql, Dictionary<string, object>? p = null)
    {
        using var cmd = c.CreateCommand();
        cmd.CommandText = sql;
        if (p != null) foreach (var kv in p) cmd.Parameters.AddWithValue(kv.Key, kv.Value);
        cmd.ExecuteNonQuery();
    }
}
