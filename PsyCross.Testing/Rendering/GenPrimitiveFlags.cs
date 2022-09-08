using System;

namespace PsyCross.Testing.Rendering {
    [Flags]
    public enum GenPrimitiveFlags : uint {
        None            = 0,
        Shaded          = 1U << 0,
        Textured        = 1U << 1,
        DoubleSided     = 1U << 2,
        Lit             = 1U << 3,
        SemiTransparent = 1U << 4,
        Discarded       = 1U << 31,
    }
}
