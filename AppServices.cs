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
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
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
                return;
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 5 && attempt < 2)
            {
                Thread.Sleep(60 * (attempt + 1));
            }
        }
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
        long? id = null;
        string loginDb = "";
        string displayName = "";
        string role = "";
        string salt = "";
        string hash = "";
        long isActive = 0;

        using (var r = cmd.ExecuteReader())
        {
            if (!r.Read())
            {
                Audit.Log(null, "LoginFail", "User", null, new { login });
                return null;
            }

            id = r.GetInt64(0);
            loginDb = r.GetString(1);
            displayName = r.GetString(2);
            role = r.GetString(3);
            salt = r.GetString(4);
            hash = r.GetString(5);
            isActive = r.GetInt64(6);
        }

        if (id is null || isActive == 0 || !PasswordHasher.Verify(password, salt, hash))
        {
            Audit.Log(id, "LoginFail", "User", id, new { login });
            return null;
        }

        var user = new SessionUser { Id = id.Value, Login = loginDb, DisplayName = displayName, Role = Enum.Parse<UserRole>(role) };
        Audit.Log(user.Id, "LoginSuccess", "User", user.Id, new { login = user.Login });
        return user;
    }


    public static bool VerifyUserPassword(string login, string password)
    {
        using var c = Database.Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT Salt,Hash,IsActive FROM Users WHERE Login=@l";
        cmd.Parameters.AddWithValue("@l", login);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return false;
        if (r.GetInt64(2) == 0) return false;
        return PasswordHasher.Verify(password, r.GetString(0), r.GetString(1));
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
            var testId = DataAccess.ExecuteScalarLong(c, null,
                @"INSERT INTO Tests(Title,Description,CreatedByTeacherId,Status,PassPercent,CreatedAt)
                  VALUES(@t,@d,@u,@s,70,@at);
                  SELECT last_insert_rowid();",
                ("@t", t.Item1),
                ("@d", t.Item2),
                ("@u", teacher),
                ("@s", t.Item3),
                ("@at", DateTime.UtcNow.ToString("O")));
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
        return DataAccess.ExecuteScalarLong(c, null,
            @"INSERT INTO Users(Login,DisplayName,Role,Salt,Hash,IsActive,CreatedAt)
              VALUES(@l,@d,@r,@s,@h,1,@at);
              SELECT last_insert_rowid();",
            ("@l", login),
            ("@d", display),
            ("@r", role.ToString()),
            ("@s", salt),
            ("@h", hash),
            ("@at", DateTime.UtcNow.ToString("O")));
    }

    private static void InsertSingle(SqliteConnection c, long testId, string text, double points, string[] opts, int correctIdx)
    {
        var qid = DataAccess.ExecuteScalarLong(c, null,
            @"INSERT INTO Questions(TestId,Type,Text,Points,SettingsJson)
              VALUES(@t,'SingleChoice',@tx,@p,'{}');
              SELECT last_insert_rowid();",
            ("@t", testId),
            ("@tx", text),
            ("@p", points));
        for (var i = 0; i < 4; i++)
        {
            DataAccess.ExecuteNonQuery(c, null,
                "INSERT INTO Options(QuestionId,Text,IsCorrect,SortOrder) VALUES(@q,@t,@c,@s)",
                ("@q", qid),
                ("@t", opts[i]),
                ("@c", i == correctIdx ? 1 : 0),
                ("@s", i));
        }
    }

    private static void InsertNumeric(SqliteConnection c, long testId, string text, double points, double correct, double tolerance)
    {
        DataAccess.ExecuteNonQuery(c, null,
            "INSERT INTO Questions(TestId,Type,Text,Points,SettingsJson) VALUES(@t,'Numeric',@tx,@p,@s)",
            ("@t", testId),
            ("@tx", text),
            ("@p", points),
            ("@s", JsonSerializer.Serialize(new { correct, tolerance })));
    }

    private static void Exec(SqliteConnection c, string sql, Dictionary<string, object>? p = null)
    {
        DataAccess.ExecuteNonQuery(c, null, sql, p?.Select(kv => (kv.Key, (object?)kv.Value)).ToArray() ?? Array.Empty<(string, object?)>());
    }
}
