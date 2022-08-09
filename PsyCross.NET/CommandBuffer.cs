using System;
using System.Runtime.InteropServices;
using PsyCross.Math;
using static PsyCross.PsyQ;

namespace PsyCross {
    public sealed class CommandBuffer {
        private const int _WordSize = sizeof(UInt32);

        private readonly byte[] _commandBuffer;
        private int _pointer;

        public ReadOnlySpan<uint> Bits => MemoryMarshal.Cast<byte, uint>(_commandBuffer);

        private CommandBuffer() {
        }

        public CommandBuffer(int wordCount) {
            _commandBuffer = new byte[wordCount * _WordSize];
        }

        public void Reset() {
            _pointer = 0;
        }

        public void AllocateLoadImage(RectInt rect, PsyQ.BitDepth bitDepth, Span<byte> data) {
            var commandSpan = AllocateCommandAs<PsyQ.CopyCpuToVram>();

            commandSpan[0].Point = new Vector2Short((short)rect.X, (short)rect.Y);
            commandSpan[0].SetShortWordDim(rect.Width, rect.Height, bitDepth);

            var dataSpan = AllocateAs<byte>(_WordSize * RoundToNearestEvenWordCount(data.Length));

            data.CopyTo(dataSpan);
        }

        public Span<PsyQ.PolyFt3> AllocatePolyFt3() => AllocateCommandAs<PsyQ.PolyFt3>();

        public Span<PsyQ.PolyG3> AllocatePolyG3() => AllocateCommandAs<PsyQ.PolyG3>();

        public Span<PsyQ.PolyGt3> AllocatePolyGt3() => AllocateCommandAs<PsyQ.PolyGt3>();

        public Span<uint> AllocateCommand(int wordCount) => AllocateAs<uint>(wordCount);

        private Span<T> AllocateCommandAs<T>() where T : struct, ICommand {
            int wordCount = RoundToNearestEvenWordCount(Marshal.SizeOf<T>());
            var commandSpan = AllocateAs<T>(wordCount);

            commandSpan[0].SetCommand();

            return commandSpan;
        }

        private Span<T> AllocateAs<T>(int wordCount) where T : struct {
            int roundedCommandSize = wordCount * _WordSize;
            var span = MemoryMarshal.Cast<byte, T>(_commandBuffer.AsSpan<byte>(_pointer, roundedCommandSize));

            _pointer += roundedCommandSize;

            return span;
        }

        private static int RoundToNearestEvenWordCount(int byteSize) =>
            ((byteSize + (_WordSize - 1)) / _WordSize);
    }
}
