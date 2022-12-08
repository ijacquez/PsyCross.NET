using System;

namespace PsyCross.Testing.Rendering {
    [Flags]
    public enum GenPrimitiveTags {
        None = 0,
        ClipTriCase1  = 1 << 1,
        ClipTriCase2  = 1 << 2,
        ClipQuadCase1 = 1 << 3,
        ClipQuadCase2 = 1 << 4,
        ClipQuadCase3 = 1 << 5,
        ClipMask      = ClipTriCase1 | ClipTriCase2 | ClipQuadCase1 | ClipQuadCase2 | ClipQuadCase3
    }
}
