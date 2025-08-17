using System;
using System.IO;
using System.Threading;
using MemoDock.Utils;
using Microsoft.Data.Sqlite;

namespace MemoDock.Services
{
    public class DatabaseService : IDisposable
    {
        public static DatabaseService Instance { get; } = new DatabaseService();

        private string? _connectionString;
        private bool _initialized;

        public bool IsInitialized => _initialized;   

        private DatabaseService() { }

        public void Initialize()
        {
            Directory.CreateDirectory(AppPaths.DataFolder);

            var csb = new SqliteConnectionStringBuilder
            {
                DataSource = AppPaths.DbPath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared
            };
            _connectionString = csb.ConnectionString;

            const int maxAttempts = 5;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                try
                {
                    using var conn = new SqliteConnection(_connectionString);
                    conn.Open();

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS entries (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    type TEXT NOT NULL,
    text TEXT,
    content_path TEXT,
    is_pinned INTEGER NOT NULL DEFAULT 0,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL,
    content_hash TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS versions (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    entry_id INTEGER NOT NULL,
    version_no INTEGER NOT NULL,
    text TEXT,
    content_path TEXT,
    created_at TEXT NOT NULL,
    content_hash TEXT NOT NULL,
    FOREIGN KEY(entry_id) REFERENCES entries(id)
);
CREATE INDEX IF NOT EXISTS idx_entries_hash ON entries(content_hash);";
                        cmd.ExecuteNonQuery();
                    }

                    bool hasPinOrder = false;
                    using (var c = conn.CreateCommand())
                    {
                        c.CommandText = "PRAGMA table_info(entries);";
                        using var r = c.ExecuteReader();
                        while (r.Read())
                        {
                            var colName = r.GetString(1);
                            if (string.Equals(colName, "pin_order", StringComparison.OrdinalIgnoreCase))
                            {
                                hasPinOrder = true; break;
                            }
                        }
                    }
                    if (!hasPinOrder)
                    {
                        using var tx = conn.BeginTransaction();
                        using (var c1 = conn.CreateCommand())
                        {
                            c1.Transaction = tx;
                            c1.CommandText = "ALTER TABLE entries ADD COLUMN pin_order INTEGER NULL;";
                            c1.ExecuteNonQuery();
                        }
                        using (var c2 = conn.CreateCommand())
                        {
                            c2.Transaction = tx;
                            c2.CommandText = "CREATE INDEX IF NOT EXISTS idx_entries_pinned_order ON entries(is_pinned, pin_order);";
                            c2.ExecuteNonQuery();
                        }
                        tx.Commit();
                    }

                    _initialized = true;  
                    return;
                }
                catch (SqliteException ex) when (ex.SqliteErrorCode == 14 && attempt < maxAttempts - 1)
                {
                    Thread.Sleep(200 * (attempt + 1));
                }
            }

            throw new InvalidOperationException("Failed to initialize SQLite database.");
        }

        public SqliteConnection OpenConnection()
        {
            if (!_initialized || string.IsNullOrEmpty(_connectionString))
                throw new InvalidOperationException("DB not initialized");
            var c = new SqliteConnection(_connectionString);
            c.Open();
            return c;
        }

        public void Dispose() { }
    }
}
