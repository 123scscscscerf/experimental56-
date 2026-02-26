using System.Data;
using Microsoft.Data.Sqlite;

namespace Experimental56;

public static class DataAccess
{
    public static long ExecuteScalarLong(SqliteConnection conn, SqliteTransaction? tx, string sql, params (string name, object? value)[] p)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = sql;
        foreach (var (name, value) in p)
        {
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }

        var scalar = cmd.ExecuteScalar();
        if (scalar is null || scalar == DBNull.Value)
        {
            return 0;
        }

        return Convert.ToInt64(scalar);
    }

    public static int ExecuteNonQuery(SqliteConnection conn, SqliteTransaction? tx, string sql, params (string name, object? value)[] p)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = sql;
        foreach (var (name, value) in p)
        {
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }

        return cmd.ExecuteNonQuery();
    }

    public static DataTable Table(string sql, params (string, object?)[] p)
    {
        using var c = Database.Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (k, v) in p) cmd.Parameters.AddWithValue(k, v ?? DBNull.Value);
        using var reader = cmd.ExecuteReader();
        var dt = new DataTable();
        dt.Load(reader);
        return dt;
    }

    public static long ExecuteInsert(string sql, params (string, object?)[] p)
    {
        using var c = Database.Open();
        return ExecuteScalarLong(c, null, $@"{sql};
SELECT last_insert_rowid();", p);
    }

    public static int Execute(string sql, params (string, object?)[] p)
    {
        using var c = Database.Open();
        return ExecuteNonQuery(c, null, sql, p);
    }
}
