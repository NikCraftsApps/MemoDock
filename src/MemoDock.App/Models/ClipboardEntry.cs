using System;
using System.Globalization;
using Microsoft.Data.Sqlite;

namespace MemoDock.Models
{

    public class ClipboardEntry
    {
        public long Id { get; set; }

        public string Type { get; set; } = "text";
        public string? Text { get; set; }
        public string? ContentPath { get; set; }
        public bool IsPinned { get; set; }
        public DateTime CreatedAt { get; set; }  
        public DateTime UpdatedAt { get; set; }   
        public string ContentHash { get; set; } = "";

        public ClipboardEntry() { }

        public ClipboardEntry(long id, string type, string? text, string? contentPath,
                              bool isPinned, DateTime createdAt, DateTime updatedAt, string contentHash)
        {
            Id = id;
            Type = type;
            Text = text;
            ContentPath = contentPath;
            IsPinned = isPinned;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
            ContentHash = contentHash;
        }

        public static ClipboardEntry FromReader(SqliteDataReader r)
        {
            DateTime ParseIso(string s) => DateTime.Parse(s, null, DateTimeStyles.RoundtripKind);

            return new ClipboardEntry(
                id: r.GetInt64(r.GetOrdinal("id")),
                type: r.GetString(r.GetOrdinal("type")),
                text: r.IsDBNull(r.GetOrdinal("text")) ? null : r.GetString(r.GetOrdinal("text")),
                contentPath: r.IsDBNull(r.GetOrdinal("content_path")) ? null : r.GetString(r.GetOrdinal("content_path")),
                isPinned: !r.IsDBNull(r.GetOrdinal("is_pinned")) && r.GetInt32(r.GetOrdinal("is_pinned")) == 1,
                createdAt: ParseIso(r.GetString(r.GetOrdinal("created_at"))),
                updatedAt: ParseIso(r.GetString(r.GetOrdinal("updated_at"))),
                contentHash: r.GetString(r.GetOrdinal("content_hash"))
            );
        }
    }
}
