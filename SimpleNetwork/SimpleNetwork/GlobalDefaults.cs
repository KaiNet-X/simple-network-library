using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleNetwork
{
    public static class GlobalDefaults
    {
        public static ForcibleDisconnectBehavior ForcibleDisconnectMode = ForcibleDisconnectBehavior.REMOVE;
        public enum ForcibleDisconnectBehavior
        {
            REMOVE,
            KEEP
        }
    }
}
