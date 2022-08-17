using System;
using System.Runtime.InteropServices;
using PsyCross.Math;

namespace PsyCross {
    public static partial class PsyQ {
        private const uint _TimMagic         = 0x00000010;
        private const uint _TimPModeCount    = 4;
        private const int _TimClutColorCount = 16;

        public static bool TryReadTim(byte[] data, out Tim tim) {
            tim = default(Tim);

            if (data.Length < Marshal.SizeOf<TimHeader>()) {
                return false;
            }

            tim = new Tim();

            using (var binaryReader = new BinaryReader(data)) {
                tim.Header = binaryReader.ReadStruct<TimHeader>()[0];

                if (tim.Header.Magic != _TimMagic) {
                    return false;
                }

                if (!Enum.IsDefined<BitDepth>(tim.Header.Flags.BitDepth)) {
                    return false;
                }

                if (tim.Header.Flags.HasClut) {
                    if (tim.Header.Flags.BitDepth >= BitDepth.Bpp15) {
                        return false;
                    }

                    tim.ClutHeader = binaryReader.ReadStruct<TimClutHeader>()[0];
                    tim.Cluts = new TimClut[tim.ClutHeader.ClutCount];

                    for (int i = 0; i < tim.ClutHeader.ClutCount; i++) {
                        tim.Cluts[i].Clut = binaryReader.ReadStruct<Rgb1555>(tim.ClutHeader.Count).ToArray();
                    }
                }

                tim.ImageHeader = binaryReader.ReadStruct<TimImageHeader>()[0];
                tim.Image = binaryReader.ReadBytesAsMemory((int)tim.ImageHeader.ByteSize - Marshal.SizeOf<TimImageHeader>());

                return true;
            }
        }
    }
}
