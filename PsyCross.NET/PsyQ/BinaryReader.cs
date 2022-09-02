using System;
using System.IO;
using System.Runtime.InteropServices;

namespace PsyCross {
    public static partial class PsyQ {
        internal class BinaryReader : IDisposable {
            private byte[] _stream;

            private BinaryReader() {
            }

            public BinaryReader(byte[] stream) {
                _stream = stream;
                Position = 0;
            }

            public int Position { get; private set; }

            public void Seek(int offset, SeekOrigin origin) {
                switch (origin) {
                    case SeekOrigin.Begin:
                        Position = offset;
                        break;
                    case SeekOrigin.Current:
                        Position = Position + offset;
                        break;
                    case SeekOrigin.End:
                        Position = System.Math.Min(_stream.Length - 1, _stream.Length + offset);
                        break;
                }
            }

            public Span<T> ReadStruct<T>(int size = 1) where T : struct {
                Span<byte> byteSpan = _stream.AsSpan(Position, System.Math.Max(size, 1) * Marshal.SizeOf<T>());

                Position += byteSpan.Length;

                return MemoryMarshal.Cast<byte, T>(byteSpan);
            }

            public Span<byte> ReadBytes(int size) {
                Span<byte> byteSpan = _stream.AsSpan(Position, size);

                Position += byteSpan.Length;

                return byteSpan;
            }

            public Memory<byte> ReadBytesAsMemory(int size) {
                Memory<byte> byteMemory = _stream.AsMemory(Position, size);

                Position += byteMemory.Length;

                return byteMemory;
            }

            public void Dispose() {
            }
        }
    }
}
