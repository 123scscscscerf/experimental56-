using System.Text.Json;

namespace Experimental56;

public static class AppSettingsService
{
    private static readonly string JsonPath = Path.Combine(AppContext.BaseDirectory, "settings.json");

    public static void Initialize()
    {
        using var c = Database.Open();
        DataAccess.ExecuteNonQuery(c, null, "CREATE TABLE IF NOT EXISTS AppSettings(Key TEXT PRIMARY KEY, Value TEXT NOT NULL)");
        EnsureDefault(c, "ui.language", "ru");
        EnsureDefault(c, "ui.theme", "light");
        EnsureDefault(c, "db.path", Database.DbPath);
        EnsureDefault(c, "features.teacherConstructor", "1");
        SaveJsonBackup();
    }

    private static void EnsureDefault(Microsoft.Data.Sqlite.SqliteConnection c, string key, string value)
    {
        var exists = DataAccess.ExecuteScalarLong(c, null, "SELECT COUNT(*) FROM AppSettings WHERE Key=@k", ("@k", key));
        if (exists == 0) DataAccess.ExecuteNonQuery(c, null, "INSERT INTO AppSettings(Key,Value) VALUES(@k,@v)", ("@k", key), ("@v", value));
    }

    public static string Get(string key, string defaultValue = "")
    {
        using var c = Database.Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT Value FROM AppSettings WHERE Key=@k";
        cmd.Parameters.AddWithValue("@k", key);
        var v = cmd.ExecuteScalar()?.ToString();
        return string.IsNullOrWhiteSpace(v) ? defaultValue : v;
    }

    public static void Set(string key, string value)
    {
        using var c = Database.Open();
        DataAccess.ExecuteNonQuery(c, null, "INSERT INTO AppSettings(Key,Value) VALUES(@k,@v) ON CONFLICT(Key) DO UPDATE SET Value=excluded.Value", ("@k", key), ("@v", value));
        SaveJsonBackup();
    }

    public static bool TeacherConstructorEnabled() => Get("features.teacherConstructor", "1") == "1";

    public static void ApplyUiFromSettings()
    {
        Database.SetDbPath(Get("db.path", Database.DbPath));
        Loc.SetLanguage(Get("ui.language", "ru"));
        ThemeManager.SetTheme(Get("ui.theme", "light"));
    }

    public static void SaveJsonBackup()
    {
        using var c = Database.Open();
        var dt = DataAccess.Table("SELECT Key,Value FROM AppSettings");
        var dict = dt.Rows.Cast<System.Data.DataRow>().ToDictionary(r => r[0]?.ToString() ?? "", r => r[1]?.ToString() ?? "");
        File.WriteAllText(JsonPath, JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true }));
    }
}
