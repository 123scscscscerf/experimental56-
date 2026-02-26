using System.Data;

namespace Experimental56;

public static class DataAccess
{
    public static DataTable Table(string sql, params (string, object)[] p)
    {
        using var c = Database.Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (k, v) in p) cmd.Parameters.AddWithValue(k, v);
        using var reader = cmd.ExecuteReader();
        var dt = new DataTable();
        dt.Load(reader);
        return dt;
    }

    public static long ExecuteInsert(string sql, params (string, object)[] p)
    {
        using var c = Database.Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (k, v) in p) cmd.Parameters.AddWithValue(k, v);
        cmd.ExecuteNonQuery();
        return c.LastInsertRowId;
    }

    public static int Execute(string sql, params (string, object)[] p)
    {
        using var c = Database.Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (k, v) in p) cmd.Parameters.AddWithValue(k, v);
        return cmd.ExecuteNonQuery();
    }
}
