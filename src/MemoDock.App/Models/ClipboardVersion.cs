using System;
using System.Globalization;
using Microsoft.Data.Sqlite;

namespace MemoDock.Models
{
    public class ClipboardVersion
    {
        public long Id { get; set; }
        public long EntryId { get; set; }
        public int VersionNo { get; set; }
        public string? Text { get; set; }
        public string? ContentPath { get; set; }
        public DateTime CreatedAt { get; set; }   
        public string ContentHash { get; set; } = "";

        public ClipboardVersion() { }

        public ClipboardVersion(long id, long entryId, int versionNo, string? text,
                                string? contentPath, DateTime createdAt, string contentHash)
        {
            Id = id;
            EntryId = entryId;
            VersionNo = versionNo;
            Text = text;
            ContentPath = contentPath;
            CreatedAt = createdAt;
            ContentHash = contentHash;
        }
        public static ClipboardVersion FromReader(SqliteDataReader r)
        {
            DateTime ParseIso(string s) => DateTime.Parse(s, null, DateTimeStyles.RoundtripKind);
          
            int Ord(string name)
            {
                try { return r.GetOrdinal(name); }
                catch { return -1; }
            }

            var idOrd = Ord("id");                
            var entryOrd = Ord("entry_id");
            var verOrd = Ord("version_no");
            var textOrd = Ord("text");
            var pathOrd = Ord("content_path");
            var createdOrd = Ord("created_at");
            var hashOrd = Ord("content_hash");

            return new ClipboardVersion(
                id: idOrd >= 0 && !r.IsDBNull(idOrd) ? r.GetInt64(idOrd) : 0L,
                entryId: entryOrd >= 0 ? r.GetInt64(entryOrd) : 0L,
                versionNo: verOrd >= 0 ? r.GetInt32(verOrd) : 0,
                text: (textOrd >= 0 && !r.IsDBNull(textOrd)) ? r.GetString(textOrd) : null,
                contentPath: (pathOrd >= 0 && !r.IsDBNull(pathOrd)) ? r.GetString(pathOrd) : null,
                createdAt: createdOrd >= 0 ? ParseIso(r.GetString(createdOrd)) : DateTime.MinValue,
                contentHash: hashOrd >= 0 ? r.GetString(hashOrd) : ""
            );
        }
    }
}
