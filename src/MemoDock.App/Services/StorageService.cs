using System;
using System.IO;
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
        }

        public static string? TryLoadText(string path)
        {
            try { return File.Exists(path) ? File.ReadAllText(path) : null; }
            catch { return null; }
        }
    }
}
