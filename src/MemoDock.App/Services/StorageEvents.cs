using System;

namespace MemoDock.Services
{
    public static class StorageEvents
    {
        public static event Action? EntriesMutated;
        public static void RaiseEntriesMutated() => EntriesMutated?.Invoke();
    }
}
