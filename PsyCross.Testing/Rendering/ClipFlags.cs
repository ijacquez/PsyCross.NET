using System;

namespace PsyCross.Testing.Rendering {
    [Flags]
    public enum ClipFlags {
        None   = 0,
        Left   = 1 << 0,
        Right  = 1 << 1,
        Top    = 1 << 2,
        Bottom = 1 << 3,
        Near   = 1 << 4,
        Far    = 1 << 5
    }
}
