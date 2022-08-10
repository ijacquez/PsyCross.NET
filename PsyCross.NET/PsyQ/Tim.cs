using System;
using System.Runtime.InteropServices;
using PsyCross.Math;

namespace PsyCross {
    public static partial class PsyQ {
        public class Tim {
            public TimHeader Header { get; internal set; }
            public TimClutHeader ClutHeader { get; internal set; }
            public TimClut[] Cluts { get; internal set; }
            public TimImageHeader ImageHeader { get; internal set; }

            public ReadOnlyMemory<byte> Image { get; internal set; }
        }

        [StructLayout(LayoutKind.Explicit, Size = 8)]
        public struct TimHeader {
            [FieldOffset( 0)] public uint Magic;
            [FieldOffset( 4)] public TimFlags Flags;
        }

        [StructLayout(LayoutKind.Explicit, Size = 8)]
        public struct TimImageHeader {
            [FieldOffset( 0)] public uint ByteSize;
            [FieldOffset( 4)] public RectShort Rect;
        }

        [StructLayout(LayoutKind.Explicit, Size = 2)]
        public struct TimFlags {
            [FieldOffset( 0)] internal byte Value;

            public BitDepth BitDepth => (BitDepth)(Value & 0x7);

            public bool HasClut => ((Value >> 3) & 0x1) == 0x1;
        }

        [StructLayout(LayoutKind.Explicit, Size = 12)]
        public struct TimClutHeader {
            [FieldOffset( 0)] public uint Size;
            [FieldOffset( 4)] public Vector2Short P;
            [FieldOffset( 8)] public ushort Count;
            [FieldOffset(10)] public ushort ClutCount;
        }

        public struct TimClut {
            public Rgb1555[] Clut { get; internal set; }
        }
    }
}
