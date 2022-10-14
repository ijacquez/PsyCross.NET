using System;

namespace PsyCross {
    public static partial class PsyQ {
        [Flags]
        public enum ReadTmdFlags {
            None                    = 0,
            IgnoreMagicValue        = 1 << 0,

            ApplyKingsField2JpFixes = (1 << 3) | IgnoreMagicValue,
        }
    }
}
