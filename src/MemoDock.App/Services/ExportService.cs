using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using MemoDock.Utils;
using Microsoft.Data.Sqlite;

namespace MemoDock.Services
{
    public static class ExportService
    {
        private class ExportEntry
        {
            public long Id { get; set; }
            public string Type { get; set; } = "text";
            public string? Text { get; set; }
            public string? ContentPath { get; set; }
            public bool IsPinned { get; set; }
            public string CreatedAt { get; set; } = "";
            public string UpdatedAt { get; set; } = "";
            public string ContentHash { get; set; } = "";
            public List<ExportVersion> Versions { get; set; } = new();
        }

        private class ExportVersion
        {
            public int VersionNo { get; set; }
            public string? Text { get; set; }
            public string? ContentPath { get; set; } 
            public string CreatedAt { get; set; } = "";
            public string ContentHash { get; set; } = "";
        }

        private static bool IsFolder(string path) => Directory.Exists(path);
        private static string EnsureJsonPath(string path)
            => IsFolder(path) ? Path.Combine(path, "clips.json") : path;

        public static void ExportAll(string folder)
        {
            Directory.CreateDirectory(folder);
            var filesFolder = Path.Combine(folder, "files");
            Directory.CreateDirectory(filesFolder);

            var list = new List<ExportEntry>();
            using var conn = DatabaseService.Instance.OpenConnection();

            using (var c = conn.CreateCommand())
            {
                c.CommandText = @"SELECT id,type,text,content_path,is_pinned,created_at,updated_at,content_hash
                                  FROM entries ORDER BY updated_at DESC";
                using var r = c.ExecuteReader();
                while (r.Read())
                {
                    var e = new ExportEntry
                    {
                        Id = r.GetInt64(0),
                        Type = r.GetString(1),
                        Text = r.IsDBNull(2) ? null : r.GetString(2),
                        ContentPath = r.IsDBNull(3) ? null : r.GetString(3),
                        IsPinned = r.GetInt32(4) == 1,
                        CreatedAt = r.GetString(5),
                        UpdatedAt = r.GetString(6),
                        ContentHash = r.GetString(7)
                    };

                    if (!string.IsNullOrEmpty(e.ContentPath) && File.Exists(e.ContentPath))
                    {
                        var name = Path.GetFileName(e.ContentPath);
                        var dst = Path.Combine(filesFolder, name);
                        try { File.Copy(e.ContentPath, dst, overwrite: true); } catch { }
                        e.ContentPath = Path.Combine("files", name);
                    }

                    using var c2 = conn.CreateCommand();
                    c2.CommandText = @"SELECT version_no,text,content_path,created_at,content_hash
                                       FROM versions WHERE entry_id=@e ORDER BY version_no ASC";
                    c2.Parameters.AddWithValue("@e", e.Id);
                    using var r2 = c2.ExecuteReader();
                    while (r2.Read())
                    {
                        var v = new ExportVersion
                        {
                            VersionNo = r2.GetInt32(0),
                            Text = r2.IsDBNull(1) ? null : r2.GetString(1),
                            ContentPath = r2.IsDBNull(2) ? null : r2.GetString(2),
                            CreatedAt = r2.GetString(3),
                            ContentHash = r2.GetString(4)
                        };
                        if (!string.IsNullOrEmpty(v.ContentPath) && File.Exists(v.ContentPath))
                        {
                            var name = Path.GetFileName(v.ContentPath);
                            var dst = Path.Combine(filesFolder, name);
                            try { File.Copy(v.ContentPath, dst, overwrite: true); } catch { }
                            v.ContentPath = Path.Combine("files", name);
                        }
                        e.Versions.Add(v);
                    }

                    list.Add(e);
                }
            }

            var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(folder, "clips.json"), json);
        }

        public static void ImportAll(string folderOrFile)
        {
            var jsonFile = EnsureJsonPath(folderOrFile);
            if (!File.Exists(jsonFile))
                throw new FileNotFoundException("Nie znaleziono clips.json", jsonFile);

            var baseFolder = IsFolder(folderOrFile) ? folderOrFile : Path.GetDirectoryName(jsonFile)!;
            var text = File.ReadAllText(jsonFile);
            var entries = JsonSerializer.Deserialize<List<ExportEntry>>(text) ?? new();

            Directory.CreateDirectory(AppPaths.StoreFolder);

            using var conn = DatabaseService.Instance.OpenConnection();

            foreach (var e in entries)
            {
                string? newEntryPath = null;
                if (!string.IsNullOrEmpty(e.ContentPath))
                {
                    var src = Path.Combine(baseFolder, e.ContentPath); 
                    {
                        newEntryPath = Path.Combine(AppPaths.StoreFolder, $"{Guid.NewGuid():N}{Path.GetExtension(src)}");
                        try { File.Copy(src, newEntryPath, overwrite: true); } catch { newEntryPath = null; }
                    }
                }

                long id;
                using (var c1 = conn.CreateCommand())
                {
                    c1.CommandText = @"
INSERT INTO entries(type,text,content_path,is_pinned,created_at,updated_at,content_hash)
VALUES(@t,@txt,@p,@pin,@c,@u,@h);";
                    c1.Parameters.AddWithValue("@t", e.Type);
                    c1.Parameters.AddWithValue("@txt", (object?)e.Text ?? DBNull.Value);
                    c1.Parameters.AddWithValue("@p", (object?)newEntryPath ?? DBNull.Value);
                    c1.Parameters.AddWithValue("@pin", e.IsPinned ? 1 : 0);
                    c1.Parameters.AddWithValue("@c", e.CreatedAt);
                    c1.Parameters.AddWithValue("@u", e.UpdatedAt);
                    c1.Parameters.AddWithValue("@h", e.ContentHash ?? "");
                    c1.ExecuteNonQuery();
                }
                using (var cid = conn.CreateCommand())
                {
                    cid.CommandText = "SELECT last_insert_rowid()";
                    id = Convert.ToInt64(cid.ExecuteScalar());
                }

                foreach (var v in e.Versions)
                {
                    string? newVerPath = null;
                    if (!string.IsNullOrEmpty(v.ContentPath))
                    {
                        var src = Path.Combine(baseFolder, v.ContentPath);
                        if (File.Exists(src))
                        {
                            newVerPath = Path.Combine(AppPaths.StoreFolder, $"{Guid.NewGuid():N}{Path.GetExtension(src)}");
                            try { File.Copy(src, newVerPath, overwrite: true); } catch { newVerPath = null; }
                        }
                    }

                    using var c2 = conn.CreateCommand();
                    c2.CommandText = @"
INSERT INTO versions(entry_id,version_no,text,content_path,created_at,content_hash)
VALUES(@e,@no,@txt,@p,@c,@h);";
                    c2.Parameters.AddWithValue("@e", id);
                    c2.Parameters.AddWithValue("@no", v.VersionNo);
                    c2.Parameters.AddWithValue("@txt", (object?)v.Text ?? DBNull.Value);
                    c2.Parameters.AddWithValue("@p", (object?)newVerPath ?? DBNull.Value);
                    c2.Parameters.AddWithValue("@c", v.CreatedAt);
                    c2.Parameters.AddWithValue("@h", v.ContentHash ?? "");
                    c2.ExecuteNonQuery();
                }
            }
        }
    }
}
