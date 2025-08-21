using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoDock.App.Services
{
    public static class PinnedEvents
    {
        public static event Action? Changed;
        public static void Raise() => Changed?.Invoke();
    }
}
