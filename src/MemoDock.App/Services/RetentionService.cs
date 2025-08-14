using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;

namespace MemoDock.Services
{
    public static class RetentionService
    {
        public static void TrimToLimit(SqliteConnection conn, int limit)
        {
            if (limit <= 0) return;

            var toDelete = new List<long>();
            using (var c = conn.CreateCommand())
            {
                c.CommandText = @"
SELECT id FROM entries
WHERE is_pinned=0
ORDER BY updated_at DESC
LIMIT -1 OFFSET @limit;";
                c.Parameters.AddWithValue("@limit", limit);
                using var r = c.ExecuteReader();
                while (r.Read()) toDelete.Add(r.GetInt64(0));
            }
            if (toDelete.Count == 0) return;

            var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var c = conn.CreateCommand())
            {
                c.CommandText = $"SELECT content_path FROM entries WHERE id IN ({string.Join(",", toDelete)}) AND content_path IS NOT NULL";
                using var r = c.ExecuteReader();
                while (r.Read()) if (!r.IsDBNull(0)) files.Add(r.GetString(0));
            }
            using (var c = conn.CreateCommand())
            {
                c.CommandText = $"SELECT content_path FROM versions WHERE entry_id IN ({string.Join(",", toDelete)}) AND content_path IS NOT NULL";
                using var r = c.ExecuteReader();
                while (r.Read()) if (!r.IsDBNull(0)) files.Add(r.GetString(0));
            }

            using (var c = conn.CreateCommand())
            {
                c.CommandText = $"DELETE FROM versions WHERE entry_id IN ({string.Join(",", toDelete)});";
                c.ExecuteNonQuery();
            }
            using (var c = conn.CreateCommand())
            {
                c.CommandText = $"DELETE FROM entries WHERE id IN ({string.Join(",", toDelete)});";
                c.ExecuteNonQuery();
            }

            foreach (var f in files)
                try { if (File.Exists(f)) File.Delete(f); } catch { }
        }
    }
}
