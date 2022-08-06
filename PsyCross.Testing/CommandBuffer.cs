using System;

namespace PsyCross.Testing {
    public sealed class CommandBuffer {
        private readonly uint[] _commandBuffer;
        private int _pointer;

        public uint[] Bits => _commandBuffer;

        private CommandBuffer() {
        }

        public CommandBuffer(int wordCount) {
            _commandBuffer = new uint[wordCount];
        }

        public void Reset() {
            _pointer = 0;
        }

        public ArraySegment<uint> AllocateCommand(uint wordCount) => AllocateCommand((int)wordCount);

        public ArraySegment<uint> AllocateCommand(int wordCount) {
            var arraySegment = new ArraySegment<uint>(_commandBuffer, _pointer, wordCount);

            _pointer += wordCount;

            return arraySegment;
        }
    }
}
