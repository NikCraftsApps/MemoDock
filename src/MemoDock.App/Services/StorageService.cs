using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using MemoDock.App.Utils;
using MemoDock.Utils;
using Microsoft.Data.Sqlite;

namespace MemoDock.Services
{
    public static class StorageService
    {
        public static void SaveText(string text)
        {
            var nowIso = DateTime.UtcNow.ToString("o");
            var hash = Crypto.Sha256Hex(text ?? string.Empty);

            using var conn = DatabaseService.Instance.OpenConnection();

            long id;
            using (var c1 = conn.CreateCommand())
            {
                c1.CommandText = @"
INSERT INTO entries(type,text,content_path,is_pinned,created_at,updated_at,content_hash)
VALUES('text',@t,NULL,0,@c,@u,@h);";
                c1.Parameters.AddWithValue("@t", (object?)text ?? DBNull.Value);
                c1.Parameters.AddWithValue("@c", nowIso);
                c1.Parameters.AddWithValue("@u", nowIso);
                c1.Parameters.AddWithValue("@h", hash);
                c1.ExecuteNonQuery();
            }
            using (var cId = conn.CreateCommand())
            {
                cId.CommandText = "SELECT last_insert_rowid()";
                id = Convert.ToInt64(cId.ExecuteScalar());
            }

            using (var c2 = conn.CreateCommand())
            {
                c2.CommandText = @"
INSERT INTO versions(entry_id,version_no,text,content_path,created_at,content_hash)
VALUES(@e,(SELECT IFNULL(MAX(version_no),0)+1 FROM versions WHERE entry_id=@e),@t,NULL,@c,@h);";
                c2.Parameters.AddWithValue("@e", id);
                c2.Parameters.AddWithValue("@t", (object?)text ?? DBNull.Value);
                c2.Parameters.AddWithValue("@c", nowIso);
                c2.Parameters.AddWithValue("@h", hash);
                c2.ExecuteNonQuery();
            }

            RetentionService.TrimToLimit(conn, SettingsService.Instance.Settings.RetentionItems);
            StorageEvents.RaiseEntriesMutated();
        }

        public static void SaveImage(byte[] pngBytes)
        {
            var nowIso = DateTime.UtcNow.ToString("o");
            var hash = Crypto.Sha256Hex(pngBytes);

            Directory.CreateDirectory(AppPaths.StoreFolder);
            var file = Path.Combine(AppPaths.StoreFolder, $"{Guid.NewGuid():N}.png");
            File.WriteAllBytes(file, pngBytes);

            using var conn = DatabaseService.Instance.OpenConnection();

            long id;
            using (var c1 = conn.CreateCommand())
            {
                c1.CommandText = @"
INSERT INTO entries(type,text,content_path,is_pinned,created_at,updated_at,content_hash)
VALUES('image',NULL,@p,0,@c,@u,@h);";
                c1.Parameters.AddWithValue("@p", file);
                c1.Parameters.AddWithValue("@c", nowIso);
                c1.Parameters.AddWithValue("@u", nowIso);
                c1.Parameters.AddWithValue("@h", hash);
                c1.ExecuteNonQuery();
            }
            using (var cId = conn.CreateCommand())
            {
                cId.CommandText = "SELECT last_insert_rowid()";
                id = Convert.ToInt64(cId.ExecuteScalar());
            }

            using (var c2 = conn.CreateCommand())
            {
                c2.CommandText = @"
INSERT INTO versions(entry_id,version_no,text,content_path,created_at,content_hash)
VALUES(@e,(SELECT IFNULL(MAX(version_no),0)+1 FROM versions WHERE entry_id=@e),NULL,@p,@c,@h);";
                c2.Parameters.AddWithValue("@e", id);
                c2.Parameters.AddWithValue("@p", file);
                c2.Parameters.AddWithValue("@c", nowIso);
                c2.Parameters.AddWithValue("@h", hash);
                c2.ExecuteNonQuery();
            }

            RetentionService.TrimToLimit(conn, SettingsService.Instance.Settings.RetentionItems);
            StorageEvents.RaiseEntriesMutated(); // NEW
        }

        public static string? TryLoadText(string path)
        {
            try { return File.Exists(path) ? File.ReadAllText(path) : null; }
            catch { return null; }
        }

        public static void SetPinned(long[] ids, bool pinned)
        {
            if (ids == null || ids.Length == 0) return;
            using var conn = DatabaseService.Instance.OpenConnection();
            using var tx = conn.BeginTransaction();
            if (pinned)
            {
                foreach (var id in ids)
                {
                    using var c = conn.CreateCommand();
                    c.Transaction = tx;
                    c.CommandText = @"
UPDATE entries
SET is_pinned = 1,
    pin_order = (SELECT IFNULL(MAX(pin_order), -1) + 1 FROM entries WHERE is_pinned=1),
    updated_at = CURRENT_TIMESTAMP
WHERE id = @id;";
                    c.Parameters.AddWithValue("@id", id);
                    c.ExecuteNonQuery();
                }
            }
            else
            {
                using var c = conn.CreateCommand();
                c.Transaction = tx;
                var pars = string.Join(",", ids.Select((_, i) => "@p" + i));
                c.CommandText = $@"
UPDATE entries
SET is_pinned = 0,
    pin_order = NULL,
    updated_at = CURRENT_TIMESTAMP
WHERE id IN ({pars});";
                for (int i = 0; i < ids.Length; i++) c.Parameters.AddWithValue("@p" + i, ids[i]);
                c.ExecuteNonQuery();
            }

            tx.Commit();
            StorageEvents.RaiseEntriesMutated(); 
        }

        public static void UpdatePinOrder(IReadOnlyList<(long id, int order)> map)
        {
            if (map == null || map.Count == 0) return;
            using var conn = DatabaseService.Instance.OpenConnection();
            using var tx = conn.BeginTransaction();
            foreach (var (id, ord) in map)
            {
                using var c = conn.CreateCommand();
                c.Transaction = tx;
                c.CommandText = @"UPDATE entries SET pin_order=@o, updated_at=CURRENT_TIMESTAMP WHERE id=@id;";
                c.Parameters.AddWithValue("@o", ord);
                c.Parameters.AddWithValue("@id", id);
                c.ExecuteNonQuery();
            }
            tx.Commit();
            StorageEvents.RaiseEntriesMutated(); 
        }

        public static void DeleteEntries(long[] ids)
        {
            if (ids == null || ids.Length == 0) return;

            using var conn = DatabaseService.Instance.OpenConnection();
            using var tx = conn.BeginTransaction();

            var paths = new List<string>();

            using (var c = conn.CreateCommand())
            {
                c.Transaction = tx;
                var epars = string.Join(",", ids.Select((_, i) => "@e" + i));
                var vpars = string.Join(",", ids.Select((_, i) => "@v" + i));
                c.CommandText = $@"
SELECT content_path FROM entries  WHERE id       IN ({epars}) AND content_path IS NOT NULL
UNION
SELECT content_path FROM versions WHERE entry_id IN ({vpars}) AND content_path IS NOT NULL";
                for (int i = 0; i < ids.Length; i++)
                {
                    c.Parameters.AddWithValue("@e" + i, ids[i]);
                    c.Parameters.AddWithValue("@v" + i, ids[i]);
                }
                using var r = c.ExecuteReader();
                while (r.Read()) if (!r.IsDBNull(0)) paths.Add(r.GetString(0));
            }

            using (var c = conn.CreateCommand())
            {
                c.Transaction = tx;
                var a = string.Join(",", ids.Select((_, i) => "@a" + i));
                var b = string.Join(",", ids.Select((_, i) => "@b" + i));
                c.CommandText = $@"DELETE FROM versions WHERE entry_id IN ({a});
                                   DELETE FROM entries  WHERE id       IN ({b});";
                for (int i = 0; i < ids.Length; i++)
                {
                    c.Parameters.AddWithValue("@a" + i, ids[i]);
                    c.Parameters.AddWithValue("@b" + i, ids[i]);
                }
                c.ExecuteNonQuery();
            }

            tx.Commit();

            foreach (var p in new HashSet<string>(paths))
                try { if (File.Exists(p)) File.Delete(p); } catch { /* ignore */ }

            StorageEvents.RaiseEntriesMutated(); 
        }
    }
}
