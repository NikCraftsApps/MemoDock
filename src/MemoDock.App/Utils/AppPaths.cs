using System;
using System.IO;

namespace MemoDock.Utils
{
    public static class AppPaths
    {
        public static string DataFolder => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MemoDock");

        public static string DbPath => Path.Combine(DataFolder, "clips.db");
        public static string StoreFolder => Path.Combine(DataFolder, "store");
        public static string ConfigPath => Path.Combine(DataFolder, "config.json");
        public static string SyncStatePath => Path.Combine(DataFolder, "syncstate.json");
    }
}
