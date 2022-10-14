using System;

namespace PsyCross {
    public static partial class PsyQ {
        public class ReadTmdException : Exception {
            public ReadTmdException() : base() {
            }

            public ReadTmdException(string message) : base(message) {
            }
        }
    }
}
