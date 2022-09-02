using System;

namespace PsyCross.Testing.Rendering {
    [Flags]
    public enum GenPrimitiveFlags {
        None      = 0,
        Discarded = 1 << 0,
    }
}
