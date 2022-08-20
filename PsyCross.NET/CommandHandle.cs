using System;

namespace PsyCross {
    public struct CommandHandle {
        internal Type Type { get; }

        private readonly ArraySegment<uint> _wordsArraySegment;

        public CommandHandle(Type type, uint[] words, int offset, int size) {
            Type = type;
            _wordsArraySegment = new ArraySegment<uint>(words, offset, size);
        }

        internal Span<uint> Command => _wordsArraySegment.AsSpan();
    }
}
