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

        public void AllocateLoadImage(RectInt rect, PsyQ.BitDepth bitDepth, byte[] data) {
            var commandSpan = AllocateCommandAs<PsyQ.CopyCpuToVram>();

            commandSpan[0].P = new Point2d((short)rect.X, (short)rect.Y);
            commandSpan[0].CalculateShortWordDim(rect.Width, rect.Height, bitDepth);

            var dataSpan = AllocateAs<byte>(RoundToNearestWordSize(data.Length));
            data.AsSpan().CopyTo(dataSpan);
        }

        public Span<PsyQ.PolyFt3> AllocatePolyFt3() => AllocateCommandAs<PsyQ.PolyFt3>();

        public Span<PsyQ.PolyG3> AllocatePolyG3() => AllocateCommandAs<PsyQ.PolyG3>();

        public Span<PsyQ.PolyGt3> AllocatePolyGt3() => AllocateCommandAs<PsyQ.PolyGt3>();

        public Span<uint> AllocateCommand(int wordCount) => AllocateAs<uint>(wordCount);

        private Span<T> AllocateCommandAs<T>() where T : struct, ICommand {
            var commandSpan = AllocateAs<T>((default(T)).GetWordSize());

            commandSpan[0].SetCommand();

            return commandSpan;
        }

        private Span<T> AllocateAs<T>(int wordCount) where T : struct {
            int roundedCommandSize = wordCount * _WordSize;
            var span = MemoryMarshal.Cast<byte, T>(_commandBuffer.AsSpan<byte>(_pointer, roundedCommandSize));

            _pointer += roundedCommandSize;

            return span;
        }

        private static int RoundToNearestWordSize(int byteSize) =>
            (_WordSize * ((byteSize / _WordSize) + (_WordSize - 1)) / _WordSize);

        // private static unsafe T ByteArrayToStructure<T>(byte[] bytes, int offset) where T : struct {
        //     fixed (byte* ptr = &bytes[offset]) {
        //         return (T)Marshal.PtrToStructure((IntPtr)ptr, typeof(T));
        //     }
        // }
    }
}
