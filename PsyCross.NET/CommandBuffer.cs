using System;
using System.Runtime.InteropServices;

namespace PsyCross {
    public sealed class CommandBuffer {
        private const int _WordSize = sizeof(UInt32);

        private readonly uint[] _commandBuffer;
        private readonly GCHandle _commandBufferHandle;

        private int _commandPointer;
        private int _commandCount;

        private CommandBuffer() {
        }

        public CommandBuffer(int wordCount) {
            _commandBuffer = new uint[wordCount];
            _commandBufferHandle = GCHandle.Alloc(_commandBuffer, GCHandleType.Pinned);
        }

        public ReadOnlySpan<uint> Bits => _commandBuffer.AsSpan();

        public int AllocatedCount => _commandCount;

        public void Reset() {
            _commandPointer = 0;
            _commandCount = 0;
        }

        public CommandHandle AllocatePolyF3() => AllocateCommand<PsyQ.PolyF3>();

        public CommandHandle AllocatePolyF4() => AllocateCommand<PsyQ.PolyF4>();

        public CommandHandle AllocatePolyFt3() => AllocateCommand<PsyQ.PolyFt3>();

        public CommandHandle AllocatePolyFt4() => AllocateCommand<PsyQ.PolyFt4>();

        public CommandHandle AllocatePolyG3() => AllocateCommand<PsyQ.PolyG3>();

        public CommandHandle AllocatePolyG4() => AllocateCommand<PsyQ.PolyG4>();

        public CommandHandle AllocatePolyGt3() => AllocateCommand<PsyQ.PolyGt3>();

        public CommandHandle AllocatePolyGt4() => AllocateCommand<PsyQ.PolyGt4>();

        public Span<PsyQ.PolyF3> GetPolyF3(CommandHandle handle) => GetCommand<PsyQ.PolyF3>(handle);

        public Span<PsyQ.PolyF4> GetPolyF4(CommandHandle handle) => GetCommand<PsyQ.PolyF4>(handle);

        public Span<PsyQ.PolyFt3> GetPolyFt3(CommandHandle handle) => GetCommand<PsyQ.PolyFt3>(handle);

        public Span<PsyQ.PolyFt4> GetPolyFt4(CommandHandle handle) => GetCommand<PsyQ.PolyFt4>(handle);

        public Span<PsyQ.PolyG3> GetPolyG3(CommandHandle handle) => GetCommand<PsyQ.PolyG3>(handle);

        public Span<PsyQ.PolyG4> GetPolyG4(CommandHandle handle) => GetCommand<PsyQ.PolyG4>(handle);

        public Span<PsyQ.PolyGt3> GetPolyGt3(CommandHandle handle) => GetCommand<PsyQ.PolyGt3>(handle);

        public Span<PsyQ.PolyGt4> GetPolyGt4(CommandHandle handle) => GetCommand<PsyQ.PolyGt4>(handle);

        public static Span<T> GetCommand<T>(CommandHandle handle) where T : struct, PsyQ.ICommand {
            // XXX: Wrap this around debug
            if (handle.Type != typeof(T)) {
                throw new InvalidCastException($"Type mismatch. Expected {handle.Type}, got {typeof(T)}.");
            }

            return MemoryMarshal.Cast<uint, T>(handle.Command);
        }

        public static Span<uint> GetCommandAsWords(CommandHandle handle) => handle.Command;

        public static Span<uint> GetCommandAsWords<T>(Span<T> spanCommand) where T : struct, PsyQ.ICommand =>
            MemoryMarshal.Cast<T, uint>(spanCommand);

        private CommandHandle AllocateCommand<T>() where T : struct, PsyQ.ICommand {
            int wordCount = PsyQ.Command.GetWordCount<T>();
            int prevPointer = _commandPointer;

            _commandPointer += wordCount;
            _commandCount++;

            // XXX: Wrap this around debug
            if (_commandPointer >= _commandBuffer.Length) {
                throw new OutOfMemoryException("CommandBuffer: Out of memory.");
            }

            return new CommandHandle(typeof(T), _commandBuffer, prevPointer, wordCount);
        }
    }
}
