using System;

namespace PsyCross {
    [Flags]
    public enum LightFlags {
        None        = 0,
        Directional = 1 << 0,
        Point       = 1 << 1,
    }
}
