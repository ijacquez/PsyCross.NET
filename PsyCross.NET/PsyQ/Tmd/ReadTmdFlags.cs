using System;

namespace PsyCross {
    public static partial class PsyQ {
        [Flags]
        public enum ReadTmdFlags {
            None                    = 0,
            IgnoreMagicValue        = 1 << 0,
            NegateYAxis             = 1 << 1,
            ConvertVerticesToFixed  = 1 << 2,

            ApplyKingsField2JpFixes = (1 << 3) | IgnoreMagicValue | ConvertVerticesToFixed | ConvertVerticesToFixed,
        }
    }
}
