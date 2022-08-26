using System;

namespace PsyCross.Testing {
    [Flags]
    public enum ClipFlags {
        None   = 0,
        Near   = 1 << 0,
        Far    = 1 << 1,
        Left   = 1 << 2,
        Right  = 1 << 3,
        Top    = 1 << 4,
        Bottom = 1 << 5,
        Side   = 1 << 6
    }
}
