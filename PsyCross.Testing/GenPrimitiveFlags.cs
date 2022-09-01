using System;

namespace PsyCross.Testing {
    [Flags]
    public enum GenPrimitiveFlags {
        None      = 0,
        Discarded = 1 << 0,
    }
}
