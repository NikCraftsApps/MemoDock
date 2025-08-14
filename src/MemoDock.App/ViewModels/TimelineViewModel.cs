using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemoDock.Services;

namespace MemoDock.ViewModels
{
    public class VersionItem
    {
        public int VersionNo { get; set; }
        public string? Text { get; set; }
        public string? ContentPath { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ContentHash { get; set; } = "";
    }

    public partial class TimelineViewModel : ObservableObject
    {
        public long EntryId { get; }
        public ObservableCollection<VersionItem> Versions { get; } = new();

        public TimelineViewModel(long entryId)
        {
            EntryId = entryId;
            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            var list = await Task.Run(() =>
            {
                var res = new System.Collections.Generic.List<VersionItem>();
                using var conn = DatabaseService.Instance.OpenConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT version_no,text,content_path,created_at,content_hash
                                    FROM versions WHERE entry_id=@e
                                    ORDER BY version_no DESC";
                cmd.Parameters.AddWithValue("@e", EntryId);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    res.Add(new VersionItem
                    {
                        VersionNo = r.GetInt32(0),
                        Text = r.IsDBNull(1) ? null : r.GetString(1),
                        ContentPath = r.IsDBNull(2) ? null : r.GetString(2),
                        CreatedAt = DateTime.Parse(r.GetString(3)),
                        ContentHash = r.GetString(4)
                    });
                }
                return res;
            });

            Versions.Clear();
            foreach (var v in list) Versions.Add(v);
        }

        [RelayCommand]
        private void Copy(VersionItem? v)
        {
            if (v == null) return;
            if (!string.IsNullOrEmpty(v.Text))
            {
                System.Windows.Clipboard.SetText(v.Text);
            }
            else if (!string.IsNullOrEmpty(v.ContentPath))
            {
                try
                {
                    var bi = new System.Windows.Media.Imaging.BitmapImage(new Uri(v.ContentPath));
                    System.Windows.Clipboard.SetImage(bi);
                }
                catch { }
            }
        }

        [RelayCommand]
        private void Restore(VersionItem? v)
        {
            if (v == null) return;
            var now = DateTime.UtcNow;
            using var conn = DatabaseService.Instance.OpenConnection();

            using (var c = conn.CreateCommand())
            {
                c.CommandText = @"
INSERT INTO versions(entry_id,version_no,text,content_path,created_at,content_hash)
VALUES(@e,(SELECT IFNULL(MAX(version_no),0)+1 FROM versions WHERE entry_id=@e),@t,@p,@c,@h);
UPDATE entries SET updated_at=@c WHERE id=@e;";
                c.Parameters.AddWithValue("@e", EntryId);
                c.Parameters.AddWithValue("@t", v.Text ?? (object)DBNull.Value);
                c.Parameters.AddWithValue("@p", v.ContentPath ?? (object)DBNull.Value);
                c.Parameters.AddWithValue("@c", now.ToString("o"));
                c.Parameters.AddWithValue("@h", v.ContentHash);
                c.ExecuteNonQuery();
            }
            _ = LoadAsync();
        }
    }
}
