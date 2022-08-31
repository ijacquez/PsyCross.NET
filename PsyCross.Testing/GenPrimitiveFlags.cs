using System;

namespace PsyCross.Testing {
    [Flags]
    public enum GenPrimitiveFlags {
        None        = 0,
        DoNotRender = 1 << 0,
    }
}
