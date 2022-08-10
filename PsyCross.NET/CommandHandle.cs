using System;

namespace PsyCross {
    public struct CommandHandle {
        private readonly ArraySegment<uint> _arraySegment;

        public CommandHandle(uint[] bits, int offset, int size) {
            _arraySegment = new ArraySegment<uint>(bits, offset, size);
        }

        internal Span<uint> Command => _arraySegment.AsSpan();
    }
}
