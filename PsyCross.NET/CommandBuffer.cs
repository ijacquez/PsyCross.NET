using System;
using System.Runtime.InteropServices;

namespace PsyCross {
    public sealed class CommandBuffer {
        private readonly byte[] _commandBuffer;
        private int _pointer;

        public ReadOnlySpan<uint> Bits => MemoryMarshal.Cast<byte, uint>(_commandBuffer);

        private CommandBuffer() {
        }

        public CommandBuffer(int wordCount) {
            _commandBuffer = new byte[wordCount * sizeof(UInt32)];
        }

        public void Reset() {
            _pointer = 0;
        }

        public Span<PsyQ.PolyFt3> AllocatePolyFt3() {
            var polySpan = AllocateAs<PsyQ.PolyFt3>(PsyQ.PolyFt3.Size);

            polySpan[0].Command = PsyQ.PolyFt3.CommandValue;

            return polySpan;
        }

        public Span<PsyQ.PolyG3> AllocatePolyG3() {
            var span = AllocateAs<PsyQ.PolyG3>(PsyQ.PolyG3.Size);

            span[0].Command = PsyQ.PolyG3.CommandValue;

            return span;
        }

        public Span<PsyQ.PolyGt3> AllocatePolyGt3() {
            var span = AllocateAs<PsyQ.PolyGt3>(PsyQ.PolyGt3.Size);

            span[0].Command = PsyQ.PolyGt3.CommandValue;

            return span;
        }

        public Span<uint> AllocateCommand(int wordCount) => AllocateAs<uint>(wordCount);

        private Span<T> AllocateAs<T>(int wordCount) where T : struct {
            int commandSize = wordCount * sizeof(UInt32);
            var span = MemoryMarshal.Cast<byte, T>(_commandBuffer.AsSpan<byte>(_pointer, commandSize));

            _pointer += commandSize;

            return span;
        }

        // private static unsafe T ByteArrayToStructure<T>(byte[] bytes, int offset) where T : struct {
        //     fixed (byte* ptr = &bytes[offset]) {
        //         return (T)Marshal.PtrToStructure((IntPtr)ptr, typeof(T));
        //     }
        // }
    }
}
