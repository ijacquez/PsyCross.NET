using System;
using System.Runtime.InteropServices;
using PsyCross.Math;

namespace PsyCross {
    public static partial class PsyQ {
        private const uint _Magic         = 0x00000010;
        private const uint _PModeCount    = 4;
        private const int _ClutColorCount = 16;

        private class Reader : IDisposable {
            private byte[] _stream;
            private int _offset;

            private Reader() {
            }

            public Reader(byte[] stream) {
                _stream = stream;
                _offset = 0;
            }

            public ReadOnlySpan<T> ReadStruct<T>(int size = 1) where T : struct {
                ReadOnlySpan<byte> byteSpan = _stream.AsSpan(_offset, System.Math.Max(size, 1) * Marshal.SizeOf<T>());

                _offset += byteSpan.Length;

                return MemoryMarshal.Cast<byte, T>(byteSpan);
            }

            public ReadOnlySpan<byte> ReadBytes(int size) {
                ReadOnlySpan<byte> byteSpan = _stream.AsSpan(_offset, size);

                _offset += byteSpan.Length;

                return byteSpan;
            }

            public ReadOnlyMemory<byte> ReadBytesAsMemory(int size) {
                ReadOnlyMemory<byte> byteMemory = _stream.AsMemory(_offset, size);

                _offset += byteMemory.Length;

                return byteMemory;
            }

            public void Dispose() {
            }
        }

        public static bool TryReadTim(byte[] data, out Tim tim) {
            tim = default(Tim);

            if (data.Length < Marshal.SizeOf<TimHeader>()) {
                return false;
            }

            tim = new Tim();

            using (var reader = new Reader(data)) {
                tim.Header = reader.ReadStruct<TimHeader>()[0];

                if (tim.Header.Magic != _Magic) {
                    return false;
                }

                if (!Enum.IsDefined<BitDepth>(tim.Header.Flags.BitDepth)) {
                    return false;
                }

                if (tim.Header.Flags.HasClut) {
                    if (tim.Header.Flags.BitDepth >= BitDepth.Bpp15) {
                        return false;
                    }

                    tim.ClutHeader = reader.ReadStruct<TimClutHeader>()[0];
                    tim.Cluts = new TimClut[tim.ClutHeader.ClutCount];

                    for (int i = 0; i < tim.ClutHeader.ClutCount; i++) {
                        tim.Cluts[i].Clut = reader.ReadStruct<Rgb1555>(tim.ClutHeader.Count).ToArray();
                    }
                }

                tim.ImageHeader = reader.ReadStruct<TimImageHeader>()[0];
                tim.Image = reader.ReadBytesAsMemory((int)tim.ImageHeader.ByteSize - Marshal.SizeOf<TimImageHeader>());

                return true;
            }
        }
    }
}
