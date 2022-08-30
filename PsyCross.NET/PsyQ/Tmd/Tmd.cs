using System;
using System.Numerics;
using System.Runtime.InteropServices;
using PsyCross.Math;

namespace PsyCross {
    public static partial class PsyQ {
        public class Tmd {
            public TmdObject[] Objects { get; internal set; }
        }

        public class TmdObject {
            public TmdPacket[] Packets { get; internal set; }
            public Vector3[] Vertices { get; internal set; }
            public Vector3[] Normals { get; internal set; }
        }

        [StructLayout(LayoutKind.Explicit, Size = 12)]
        private struct TmdHeader {
            [FieldOffset( 0)] public uint Magic;
            [FieldOffset( 4)] public TmdFlags Flags;
            [FieldOffset( 8)] public uint ObjectCount;
        }

        public class TmdPacket {
            public TmdPrimitiveHeader PrimitiveHeader { get; internal set; }
            public ITmdPrimitive Primitive { get; internal set; }
        }

        public enum TmdPrimitiveType {
            // Triangle, lighting
            F3,
            G3,
            Fg3,
            Gg3,
            Ft3,
            Gt3,
            // Triangle, no lighting
            Fn3,
            Gn3,
            // Skip Fng3
            // Skip Gng3
            Fnt3 = Gn3 + 2,
            Gnt3,

            // Quad, lighting
            F4,
            G4,
            Fg4,
            Gg4,
            Ft4,
            Gt4,
            // Quad, no lighting
            Fn4,
            Gn4,
            // Skip Fng4
            // Skip Gng4
            Fnt4 = Gn4 + 2,
            Gnt4
        }

        [StructLayout(LayoutKind.Explicit, Size = 4)]
        private struct TmdFlags {
            [FieldOffset( 0)] internal uint Value;

            // The FIXP bit indicates whether the pointer value of the OBJECT
            // structure described later is a real address. A value of one means
            // a real address. A value of zero indicates the offset from the
            // start
            public bool IsFixP => ((Value & 0x1) == 0x1);
        }

        [StructLayout(LayoutKind.Explicit, Size = 28)]
        private struct TmdObjectDesc {
            [FieldOffset( 0)] public uint VerticesOffset;
            [FieldOffset( 4)] public uint VerticesCount;
            [FieldOffset( 8)] public uint NormalsOffset;
            [FieldOffset(12)] public uint NormalsCount;
            [FieldOffset(16)] public uint PrimitivesOffset;
            [FieldOffset(20)] public uint PrimitivesCount;
            [FieldOffset(24)] public int Scale;
        }

        [StructLayout(LayoutKind.Explicit, Size = 8)]
        private struct TmdVertex {
            [FieldOffset( 0)] public short X;
            [FieldOffset( 2)] public short Y;
            [FieldOffset( 4)] public short Z;
        }

        [StructLayout(LayoutKind.Explicit, Size = 4)]
        public struct TmdPrimitiveHeader {
            [FieldOffset( 0)] internal byte DrawingWordCount;
            [FieldOffset( 1)] internal byte PacketWordCount;
            [FieldOffset( 2)] public TmdPrimitiveFlags Flags;
            [FieldOffset( 3)] public TmdPrimitiveMode Mode;
        }

        [Flags]
        public enum TmdPrimitiveFlags : byte {
            None = 0,
            Lgt  = 1 << 0, // Set whether light source calculation is carried out
            Fce  = 1 << 1, // Set whether polygon is single or double faced
            Grd  = 1 << 2, // Set whether non-textured polygon is subjected to
                           // light source calculation
            Unk1 = 1 << 3,
            Unk2 = 1 << 4,
            Unk3 = 1 << 5,
            Unk4 = 1 << 6,
            Unk5 = 1 << 7
        }

        [Flags]
        public enum TmdPrimitiveMode : byte {
            None             = 0,
            Tge              = 1 << 0, // Brightness calculation at time of calculation (?)
            Abe              = 1 << 1, // Activates translucency when rendered
            Tme              = 1 << 2, // Sets whether a texture is used or not
            Quad             = 1 << 3, // Displays whether a 3 or 4 sided polygon
            Iip              = 1 << 4, // Sets flat or gouraud shading
            CodePolygon      = 1 << 5,
            CodeStraightLine = 2 << 5,
            CodeSprite       = 3 << 5,
            CodeMask         = CodePolygon | CodeStraightLine | CodeSprite
        }

        [StructLayout(LayoutKind.Explicit, Size = 2)]
        public struct TmdCba {
            [FieldOffset( 0)] public ushort Value;
            [FieldOffset( 0)] public Vector2Short Point;
        }

        [StructLayout(LayoutKind.Explicit, Size = 2)]
        public struct TmdTsb {
            [FieldOffset( 0)] public ushort Value;
        }

        #region Triangles with lighting

        [StructLayout(LayoutKind.Explicit, Size = 12)]
        public struct TmdPrimitiveF3 : ITmdPrimitive {
            [FieldOffset( 0)] private Rgb888 _Color;
            [FieldOffset( 3)] public TmdPrimitiveMode Mode;
            [FieldOffset( 4)] private ushort _IndexNormal;
            [FieldOffset( 6)] private ushort _IndexV0;
            [FieldOffset( 8)] private ushort _IndexV1;
            [FieldOffset(10)] private ushort _IndexV2;

            public TmdPrimitiveType Type => TmdPrimitiveType.F3;

            public int IndexV0 => _IndexV0;
            public int IndexV1 => _IndexV1;
            public int IndexV2 => _IndexV2;
            public int IndexV3 => -1;

            public int IndexN0 => _IndexNormal;
            public int IndexN1 => -1;
            public int IndexN2 => -1;
            public int IndexN3 => -1;

            public Rgb888 C0 => _Color;
            public Rgb888 C1 => _Color;
            public Rgb888 C2 => _Color;
            public Rgb888 C3 => Rgb888.Black;

            public Texcoord T0 => default(Texcoord);
            public Texcoord T1 => default(Texcoord);
            public Texcoord T2 => default(Texcoord);
            public Texcoord T3 => default(Texcoord);

            public TmdTsb Tsb => default(TmdTsb);

            public TmdCba Cba => default(TmdCba);

            public int VertexCount => 3;

            public int NormalCount => 1;

            public int ColorCount => 1;
        }

        [StructLayout(LayoutKind.Explicit, Size = 16)]
        public struct TmdPrimitiveG3 : ITmdPrimitive {
            [FieldOffset( 0)] private Rgb888 _Color;
            [FieldOffset( 3)] public TmdPrimitiveMode Mode;
            [FieldOffset( 4)] private ushort _IndexN0;
            [FieldOffset( 6)] private ushort _IndexV0;
            [FieldOffset( 8)] private ushort _IndexN1;
            [FieldOffset(10)] private ushort _IndexV1;
            [FieldOffset(12)] private ushort _IndexN2;
            [FieldOffset(14)] private ushort _IndexV2;

            public TmdPrimitiveType Type => TmdPrimitiveType.G3;

            public int IndexV0 => _IndexV0;
            public int IndexV1 => _IndexV1;
            public int IndexV2 => _IndexV2;
            public int IndexV3 => -1;

            public int IndexN0 => _IndexN0;
            public int IndexN1 => _IndexN1;
            public int IndexN2 => _IndexN2;
            public int IndexN3 => -1;

            public Rgb888 C0 => _Color;
            public Rgb888 C1 => _Color;
            public Rgb888 C2 => _Color;
            public Rgb888 C3 => Rgb888.Black;

            public Texcoord T0 => default(Texcoord);
            public Texcoord T1 => default(Texcoord);
            public Texcoord T2 => default(Texcoord);
            public Texcoord T3 => default(Texcoord);

            public TmdTsb Tsb => default(TmdTsb);

            public TmdCba Cba => default(TmdCba);

            public int VertexCount => 3;

            public int NormalCount => 3;

            public int ColorCount => 1;
        }

        [StructLayout(LayoutKind.Explicit, Size = 16)]
        public struct TmdPrimitiveFg3 : ITmdPrimitive {
            [FieldOffset( 0)] private Rgb888 _C0;
            [FieldOffset( 3)] public TmdPrimitiveMode Mode;
            [FieldOffset( 4)] private Rgb888 _C1;
            [FieldOffset( 8)] private Rgb888 _C2;
            [FieldOffset(12)] private ushort _IndexNormal;
            [FieldOffset(14)] private ushort _IndexV0;
            [FieldOffset(16)] private ushort _IndexV1;
            [FieldOffset(18)] private ushort _IndexV2;

            public TmdPrimitiveType Type => TmdPrimitiveType.Fg3;

            public int IndexV0 => _IndexV0;
            public int IndexV1 => _IndexV1;
            public int IndexV2 => _IndexV2;
            public int IndexV3 => -1;

            public int IndexN0 => _IndexNormal;
            public int IndexN1 => -1;
            public int IndexN2 => -1;
            public int IndexN3 => -1;

            public Rgb888 C0 => _C0;
            public Rgb888 C1 => _C1;
            public Rgb888 C2 => _C2;
            public Rgb888 C3 => Rgb888.Black;

            public Texcoord T0 => default(Texcoord);
            public Texcoord T1 => default(Texcoord);
            public Texcoord T2 => default(Texcoord);
            public Texcoord T3 => default(Texcoord);

            public TmdTsb Tsb => default(TmdTsb);

            public TmdCba Cba => default(TmdCba);

            public int VertexCount => 3;

            public int NormalCount => 1;

            public int ColorCount => 3;
        }

        [StructLayout(LayoutKind.Explicit, Size = 24)]
        public struct TmdPrimitiveGg3 : ITmdPrimitive {
            [FieldOffset( 0)] private Rgb888 _C0;
            [FieldOffset( 3)] public TmdPrimitiveMode Mode;
            [FieldOffset( 4)] private Rgb888 _C1;
            [FieldOffset( 8)] private Rgb888 _C2;
            [FieldOffset(12)] private ushort _IndexN0;
            [FieldOffset(14)] private ushort _IndexV0;
            [FieldOffset(16)] private ushort _IndexN1;
            [FieldOffset(18)] private ushort _IndexV1;
            [FieldOffset(20)] private ushort _IndexN2;
            [FieldOffset(22)] private ushort _IndexV2;

            public TmdPrimitiveType Type => TmdPrimitiveType.Gg3;

            public int IndexV0 => _IndexV0;
            public int IndexV1 => _IndexV1;
            public int IndexV2 => _IndexV2;
            public int IndexV3 => -1;

            public int IndexN0 => _IndexN0;
            public int IndexN1 => _IndexN1;
            public int IndexN2 => _IndexN2;
            public int IndexN3 => -1;

            public Rgb888 C0 => _C0;
            public Rgb888 C1 => _C1;
            public Rgb888 C2 => _C2;
            public Rgb888 C3 => Rgb888.Black;

            public Texcoord T0 => default(Texcoord);
            public Texcoord T1 => default(Texcoord);
            public Texcoord T2 => default(Texcoord);
            public Texcoord T3 => default(Texcoord);

            public TmdTsb Tsb => default(TmdTsb);

            public TmdCba Cba => default(TmdCba);

            public int VertexCount => 3;

            public int NormalCount => 3;

            public int ColorCount => 3;
        }

        [StructLayout(LayoutKind.Explicit, Size = 20)]
        public struct TmdPrimitiveFt3 : ITmdPrimitive {
            [FieldOffset( 0)] private Texcoord _T0;
            [FieldOffset( 2)] private TmdCba _Cba;
            [FieldOffset( 4)] private Texcoord _T1;
            [FieldOffset( 6)] private TmdTsb _Tsb;
            [FieldOffset( 8)] private Texcoord _T2;
            [FieldOffset(12)] private ushort _IndexNormal;
            [FieldOffset(14)] private ushort _IndexV0;
            [FieldOffset(16)] private ushort _IndexV1;
            [FieldOffset(18)] private ushort _IndexV2;

            public TmdPrimitiveType Type => TmdPrimitiveType.Ft3;

            public int IndexV0 => _IndexV0;
            public int IndexV1 => _IndexV1;
            public int IndexV2 => _IndexV2;
            public int IndexV3 => -1;

            public int IndexN0 => _IndexNormal;
            public int IndexN1 => -1;
            public int IndexN2 => -1;
            public int IndexN3 => -1;

            public Rgb888 C0 => Rgb888.Black;
            public Rgb888 C1 => Rgb888.Black;
            public Rgb888 C2 => Rgb888.Black;
            public Rgb888 C3 => Rgb888.Black;

            public Texcoord T0 => _T0;
            public Texcoord T1 => _T1;
            public Texcoord T2 => _T2;
            public Texcoord T3 => default(Texcoord);

            public TmdTsb Tsb => _Tsb;

            public TmdCba Cba => _Cba;

            public int VertexCount => 3;

            public int NormalCount => 1;

            public int ColorCount => 0;
        }

        [StructLayout(LayoutKind.Explicit, Size = 24)]
        public struct TmdPrimitiveGt3 : ITmdPrimitive {
            [FieldOffset( 0)] private Texcoord _T0;
            [FieldOffset( 2)] private TmdCba _Cba;
            [FieldOffset( 4)] private Texcoord _T1;
            [FieldOffset( 6)] private TmdTsb _Tsb;
            [FieldOffset( 8)] private Texcoord _T2;
            [FieldOffset(12)] private ushort _IndexN0;
            [FieldOffset(14)] private ushort _IndexV0;
            [FieldOffset(16)] private ushort _IndexN1;
            [FieldOffset(18)] private ushort _IndexV1;
            [FieldOffset(20)] private ushort _IndexN2;
            [FieldOffset(22)] private ushort _IndexV2;

            public TmdPrimitiveType Type => TmdPrimitiveType.Gt3;

            public int IndexV0 => _IndexV0;
            public int IndexV1 => _IndexV1;
            public int IndexV2 => _IndexV2;
            public int IndexV3 => -1;

            public int IndexN0 => _IndexN0;
            public int IndexN1 => _IndexN1;
            public int IndexN2 => _IndexN2;
            public int IndexN3 => -1;

            public Rgb888 C0 => Rgb888.Black;
            public Rgb888 C1 => Rgb888.Black;
            public Rgb888 C2 => Rgb888.Black;
            public Rgb888 C3 => Rgb888.Black;

            public Texcoord T0 => _T0;
            public Texcoord T1 => _T1;
            public Texcoord T2 => _T2;
            public Texcoord T3 => default(Texcoord);

            public TmdTsb Tsb => _Tsb;

            public TmdCba Cba => _Cba;

            public int VertexCount => 3;

            public int NormalCount => 3;

            public int ColorCount => 0;
        }

        #endregion

        #region Quadrangles with lighting

        [StructLayout(LayoutKind.Explicit, Size = 16)]
        public struct TmdPrimitiveF4 : ITmdPrimitive {
            [FieldOffset( 0)] private Rgb888 _Color;
            [FieldOffset( 3)] public TmdPrimitiveMode Mode;
            [FieldOffset( 4)] private ushort _IndexNormal;
            [FieldOffset( 6)] private ushort _IndexV0;
            [FieldOffset( 8)] private ushort _IndexV1;
            [FieldOffset(10)] private ushort _IndexV2;
            [FieldOffset(12)] private ushort _IndexV3;

            public TmdPrimitiveType Type => TmdPrimitiveType.F4;

            public int IndexV0 => _IndexV0;
            public int IndexV1 => _IndexV1;
            public int IndexV2 => _IndexV2;
            public int IndexV3 => _IndexV3;

            public int IndexN0 => _IndexNormal;
            public int IndexN1 => -1;
            public int IndexN2 => -1;
            public int IndexN3 => -1;

            public Rgb888 C0 => _Color;
            public Rgb888 C1 => _Color;
            public Rgb888 C2 => _Color;
            public Rgb888 C3 => Rgb888.Black;

            public Texcoord T0 => default(Texcoord);
            public Texcoord T1 => default(Texcoord);
            public Texcoord T2 => default(Texcoord);
            public Texcoord T3 => default(Texcoord);

            public TmdTsb Tsb => default(TmdTsb);

            public TmdCba Cba => default(TmdCba);

            public int VertexCount => 4;

            public int NormalCount => 1;

            public int ColorCount => 1;
        }

        [StructLayout(LayoutKind.Explicit, Size = 20)]
        public struct TmdPrimitiveG4 : ITmdPrimitive {
            [FieldOffset( 0)] private Rgb888 _Color;
            [FieldOffset( 3)] public TmdPrimitiveMode Mode;
            [FieldOffset( 4)] private ushort _IndexN0;
            [FieldOffset( 6)] private ushort _IndexV0;
            [FieldOffset( 8)] private ushort _IndexN1;
            [FieldOffset(10)] private ushort _IndexV1;
            [FieldOffset(12)] private ushort _IndexN2;
            [FieldOffset(14)] private ushort _IndexV2;
            [FieldOffset(16)] private ushort _IndexN3;
            [FieldOffset(18)] private ushort _IndexV3;

            public TmdPrimitiveType Type => TmdPrimitiveType.G4;

            public int IndexV0 => _IndexV0;
            public int IndexV1 => _IndexV1;
            public int IndexV2 => _IndexV2;
            public int IndexV3 => _IndexV3;

            public int IndexN0 => _IndexN0;
            public int IndexN1 => _IndexN1;
            public int IndexN2 => _IndexN2;
            public int IndexN3 => _IndexN3;

            public Rgb888 C0 => _Color;
            public Rgb888 C1 => _Color;
            public Rgb888 C2 => _Color;
            public Rgb888 C3 => _Color;

            public Texcoord T0 => default(Texcoord);
            public Texcoord T1 => default(Texcoord);
            public Texcoord T2 => default(Texcoord);
            public Texcoord T3 => default(Texcoord);

            public TmdTsb Tsb => default(TmdTsb);

            public TmdCba Cba => default(TmdCba);

            public int VertexCount => 4;

            public int NormalCount => 4;

            public int ColorCount => 1;
        }

        [StructLayout(LayoutKind.Explicit, Size = 28)]
        public struct TmdPrimitiveFg4 : ITmdPrimitive {
            [FieldOffset( 0)] private Rgb888 _C0;
            [FieldOffset( 3)] public TmdPrimitiveMode Mode;
            [FieldOffset( 4)] private Rgb888 _C1;
            [FieldOffset( 8)] private Rgb888 _C2;
            [FieldOffset(12)] private Rgb888 _C3;
            [FieldOffset(16)] private ushort _IndexNormal;
            [FieldOffset(18)] private ushort _IndexV0;
            [FieldOffset(20)] private ushort _IndexV1;
            [FieldOffset(22)] private ushort _IndexV2;
            [FieldOffset(24)] private ushort _IndexV3;

            public TmdPrimitiveType Type => TmdPrimitiveType.Fg4;

            public int IndexV0 => _IndexV0;
            public int IndexV1 => _IndexV1;
            public int IndexV2 => _IndexV2;
            public int IndexV3 => _IndexV3;

            public int IndexN0 => _IndexNormal;
            public int IndexN1 => -1;
            public int IndexN2 => -1;
            public int IndexN3 => -1;

            public Rgb888 C0 => _C0;
            public Rgb888 C1 => _C1;
            public Rgb888 C2 => _C2;
            public Rgb888 C3 => _C3;

            public Texcoord T0 => default(Texcoord);
            public Texcoord T1 => default(Texcoord);
            public Texcoord T2 => default(Texcoord);
            public Texcoord T3 => default(Texcoord);

            public TmdTsb Tsb => default(TmdTsb);

            public TmdCba Cba => default(TmdCba);

            public int VertexCount => 4;

            public int NormalCount => 1;

            public int ColorCount => 4;
        }

        [StructLayout(LayoutKind.Explicit, Size = 32)]
        public struct TmdPrimitiveGg4 : ITmdPrimitive {
            [FieldOffset( 0)] private Rgb888 _C0;
            [FieldOffset( 3)] public TmdPrimitiveMode Mode;
            [FieldOffset( 4)] private Rgb888 _C1;
            [FieldOffset( 8)] private Rgb888 _C2;
            [FieldOffset(12)] private Rgb888 _C3;
            [FieldOffset(16)] private ushort _IndexN0;
            [FieldOffset(18)] private ushort _IndexV0;
            [FieldOffset(20)] private ushort _IndexN1;
            [FieldOffset(22)] private ushort _IndexV1;
            [FieldOffset(24)] private ushort _IndexN2;
            [FieldOffset(26)] private ushort _IndexV2;
            [FieldOffset(28)] private ushort _IndexN3;
            [FieldOffset(30)] private ushort _IndexV3;

            public TmdPrimitiveType Type => TmdPrimitiveType.Gg4;

            public int IndexV0 => _IndexV0;
            public int IndexV1 => _IndexV1;
            public int IndexV2 => _IndexV2;
            public int IndexV3 => _IndexV3;

            public int IndexN0 => _IndexN0;
            public int IndexN1 => _IndexN1;
            public int IndexN2 => _IndexN2;
            public int IndexN3 => _IndexN3;

            public Rgb888 C0 => _C0;
            public Rgb888 C1 => _C1;
            public Rgb888 C2 => _C2;
            public Rgb888 C3 => _C3;

            public Texcoord T0 => default(Texcoord);
            public Texcoord T1 => default(Texcoord);
            public Texcoord T2 => default(Texcoord);
            public Texcoord T3 => default(Texcoord);

            public TmdTsb Tsb => default(TmdTsb);

            public TmdCba Cba => default(TmdCba);

            public int VertexCount => 4;

            public int NormalCount => 4;

            public int ColorCount => 4;
        }

        [StructLayout(LayoutKind.Explicit, Size = 28)]
        public struct TmdPrimitiveFt4 : ITmdPrimitive {
            [FieldOffset( 0)] private Texcoord _T0;
            [FieldOffset( 2)] private TmdCba _Cba;
            [FieldOffset( 4)] private Texcoord _T1;
            [FieldOffset( 6)] private TmdTsb _Tsb;
            [FieldOffset( 8)] private Texcoord _T2;
            [FieldOffset(12)] private Texcoord _T3;
            [FieldOffset(16)] private ushort _IndexNormal;
            [FieldOffset(18)] private ushort _IndexV0;
            [FieldOffset(20)] private ushort _IndexV1;
            [FieldOffset(22)] private ushort _IndexV2;
            [FieldOffset(24)] private ushort _IndexV3;

            public TmdPrimitiveType Type => TmdPrimitiveType.Ft4;

            public int IndexV0 => _IndexV0;
            public int IndexV1 => _IndexV1;
            public int IndexV2 => _IndexV2;
            public int IndexV3 => _IndexV3;

            public int IndexN0 => _IndexNormal;
            public int IndexN1 => -1;
            public int IndexN2 => -1;
            public int IndexN3 => -1;

            public Rgb888 C0 => Rgb888.Black;
            public Rgb888 C1 => Rgb888.Black;
            public Rgb888 C2 => Rgb888.Black;
            public Rgb888 C3 => Rgb888.Black;

            public Texcoord T0 => _T0;
            public Texcoord T1 => _T1;
            public Texcoord T2 => _T2;
            public Texcoord T3 => _T3;

            public TmdTsb Tsb => _Tsb;

            public TmdCba Cba => _Cba;

            public int VertexCount => 4;

            public int NormalCount => 1;

            public int ColorCount => 0;
        }

        [StructLayout(LayoutKind.Explicit, Size = 32)]
        public struct TmdPrimitiveGt4 : ITmdPrimitive {
            [FieldOffset( 0)] private Texcoord _T0;
            [FieldOffset( 2)] private TmdCba _Cba;
            [FieldOffset( 4)] private Texcoord _T1;
            [FieldOffset( 6)] private TmdTsb _Tsb;
            [FieldOffset( 8)] private Texcoord _T2;
            [FieldOffset(12)] private Texcoord _T3;
            [FieldOffset(16)] private ushort _IndexN0;
            [FieldOffset(18)] private ushort _IndexV0;
            [FieldOffset(20)] private ushort _IndexN1;
            [FieldOffset(22)] private ushort _IndexV1;
            [FieldOffset(24)] private ushort _IndexN2;
            [FieldOffset(26)] private ushort _IndexV2;
            [FieldOffset(28)] private ushort _IndexN3;
            [FieldOffset(30)] private ushort _IndexV3;

            public TmdPrimitiveType Type => TmdPrimitiveType.Gt4;

            public int IndexV0 => _IndexV0;
            public int IndexV1 => _IndexV1;
            public int IndexV2 => _IndexV2;
            public int IndexV3 => _IndexV3;

            public int IndexN0 => _IndexN0;
            public int IndexN1 => _IndexN1;
            public int IndexN2 => _IndexN2;
            public int IndexN3 => _IndexN3;

            public Rgb888 C0 => Rgb888.Black;
            public Rgb888 C1 => Rgb888.Black;
            public Rgb888 C2 => Rgb888.Black;
            public Rgb888 C3 => Rgb888.Black;

            public Texcoord T0 => _T0;
            public Texcoord T1 => _T1;
            public Texcoord T2 => _T2;
            public Texcoord T3 => _T3;

            public TmdTsb Tsb => _Tsb;

            public TmdCba Cba => _Cba;

            public int VertexCount => 4;

            public int NormalCount => 4;

            public int ColorCount => 0;
        }

        #endregion

        #region Triangles without lighting

        [StructLayout(LayoutKind.Explicit, Size = 12)]
        public struct TmdPrimitiveFn3 : ITmdPrimitive {
            [FieldOffset( 0)] private Rgb888 _Color;
            [FieldOffset( 3)] public TmdPrimitiveMode Mode;
            [FieldOffset( 4)] private ushort _IndexV0;
            [FieldOffset( 6)] private ushort _IndexV1;
            [FieldOffset( 8)] private ushort _IndexV2;

            public TmdPrimitiveType Type => TmdPrimitiveType.Fn3;

            public int IndexV0 => _IndexV0;
            public int IndexV1 => _IndexV1;
            public int IndexV2 => _IndexV2;
            public int IndexV3 => -1;

            public int IndexN0 => -1;
            public int IndexN1 => -1;
            public int IndexN2 => -1;
            public int IndexN3 => -1;

            public Rgb888 C0 => _Color;
            public Rgb888 C1 => _Color;
            public Rgb888 C2 => _Color;
            public Rgb888 C3 => Rgb888.Black;

            public Texcoord T0 => default(Texcoord);
            public Texcoord T1 => default(Texcoord);
            public Texcoord T2 => default(Texcoord);
            public Texcoord T3 => default(Texcoord);

            public TmdTsb Tsb => default(TmdTsb);

            public TmdCba Cba => default(TmdCba);

            public int VertexCount => 3;

            public int NormalCount => 0;

            public int ColorCount => 1;
        }

        [StructLayout(LayoutKind.Explicit, Size = 20)]
        public struct TmdPrimitiveGn3 : ITmdPrimitive {
            [FieldOffset( 0)] private Rgb888 _C0;
            [FieldOffset( 3)] public TmdPrimitiveMode Mode;
            [FieldOffset( 4)] private Rgb888 _C1;
            [FieldOffset( 8)] private Rgb888 _C2;
            [FieldOffset(12)] private ushort _IndexV0;
            [FieldOffset(14)] private ushort _IndexV1;
            [FieldOffset(16)] private ushort _IndexV2;

            public TmdPrimitiveType Type => TmdPrimitiveType.Gn3;

            public int IndexV0 => _IndexV0;
            public int IndexV1 => _IndexV1;
            public int IndexV2 => _IndexV2;
            public int IndexV3 => -1;

            public int IndexN0 => -1;
            public int IndexN1 => -1;
            public int IndexN2 => -1;
            public int IndexN3 => -1;

            public Rgb888 C0 => _C0;
            public Rgb888 C1 => _C1;
            public Rgb888 C2 => _C2;
            public Rgb888 C3 => Rgb888.Black;

            public Texcoord T0 => default(Texcoord);
            public Texcoord T1 => default(Texcoord);
            public Texcoord T2 => default(Texcoord);
            public Texcoord T3 => default(Texcoord);

            public TmdTsb Tsb => default(TmdTsb);

            public TmdCba Cba => default(TmdCba);

            public int VertexCount => 3;

            public int NormalCount => 0;

            public int ColorCount => 3;
        }

        [StructLayout(LayoutKind.Explicit, Size = 24)]
        public struct TmdPrimitiveFnt3 : ITmdPrimitive {
            [FieldOffset( 0)] private Texcoord _T0;
            [FieldOffset( 2)] private TmdCba _Cba;
            [FieldOffset( 4)] private Texcoord _T1;
            [FieldOffset( 6)] private TmdTsb _Tsb;
            [FieldOffset( 8)] private Texcoord _T2;
            [FieldOffset(12)] private Rgb888 _Color;
            [FieldOffset(16)] private ushort _IndexV0;
            [FieldOffset(18)] private ushort _IndexV1;
            [FieldOffset(20)] private ushort _IndexV2;

            public TmdPrimitiveType Type => TmdPrimitiveType.Fnt3;

            public int IndexV0 => _IndexV0;
            public int IndexV1 => _IndexV1;
            public int IndexV2 => _IndexV2;
            public int IndexV3 => -1;

            public int IndexN0 => -1;
            public int IndexN1 => -1;
            public int IndexN2 => -1;
            public int IndexN3 => -1;

            public Rgb888 C0 => _Color;
            public Rgb888 C1 => _Color;
            public Rgb888 C2 => _Color;
            public Rgb888 C3 => Rgb888.Black;

            public Texcoord T0 => _T0;
            public Texcoord T1 => _T1;
            public Texcoord T2 => _T2;
            public Texcoord T3 => default(Texcoord);

            public TmdTsb Tsb => _Tsb;

            public TmdCba Cba => _Cba;

            public int VertexCount => 3;

            public int NormalCount => 0;

            public int ColorCount => 3;
        }

        [StructLayout(LayoutKind.Explicit, Size = 32)]
        public struct TmdPrimitiveGnt3 : ITmdPrimitive {
            [FieldOffset( 0)] private Texcoord _T0;
            [FieldOffset( 2)] private TmdCba _Cba;
            [FieldOffset( 4)] private Texcoord _T1;
            [FieldOffset( 6)] private TmdTsb _Tsb;
            [FieldOffset( 8)] private Texcoord _T2;
            [FieldOffset(12)] private Rgb888 _C0;
            [FieldOffset(16)] private Rgb888 _C1;
            [FieldOffset(20)] private Rgb888 _C2;
            [FieldOffset(24)] private ushort _IndexV0;
            [FieldOffset(26)] private ushort _IndexV1;
            [FieldOffset(28)] private ushort _IndexV2;

            public TmdPrimitiveType Type => TmdPrimitiveType.Gnt3;

            public int IndexV0 => _IndexV0;
            public int IndexV1 => _IndexV1;
            public int IndexV2 => _IndexV2;
            public int IndexV3 => -1;

            public int IndexN0 => -1;
            public int IndexN1 => -1;
            public int IndexN2 => -1;
            public int IndexN3 => -1;

            public Rgb888 C0 => _C0;
            public Rgb888 C1 => _C1;
            public Rgb888 C2 => _C2;
            public Rgb888 C3 => _C2;

            public Texcoord T0 => _T0;
            public Texcoord T1 => _T1;
            public Texcoord T2 => _T2;
            public Texcoord T3 => default(Texcoord);

            public TmdTsb Tsb => _Tsb;

            public TmdCba Cba => _Cba;

            public int VertexCount => 3;

            public int NormalCount => 0;

            public int ColorCount => 3;
        }

        #endregion

        #region Quadrangles with lighting

        [StructLayout(LayoutKind.Explicit, Size = 12)]
        public struct TmdPrimitiveFn4 : ITmdPrimitive {
            [FieldOffset( 0)] private Rgb888 _Color;
            [FieldOffset( 3)] public TmdPrimitiveMode Mode;
            [FieldOffset( 4)] private ushort _IndexV0;
            [FieldOffset( 6)] private ushort _IndexV1;
            [FieldOffset( 8)] private ushort _IndexV2;
            [FieldOffset(10)] private ushort _IndexV3;

            public TmdPrimitiveType Type => TmdPrimitiveType.Fn4;

            public int IndexV0 => _IndexV0;
            public int IndexV1 => _IndexV1;
            public int IndexV2 => _IndexV2;
            public int IndexV3 => _IndexV3;

            public int IndexN0 => -1;
            public int IndexN1 => -1;
            public int IndexN2 => -1;
            public int IndexN3 => -1;

            public Rgb888 C0 => _Color;
            public Rgb888 C1 => _Color;
            public Rgb888 C2 => _Color;
            public Rgb888 C3 => _Color;

            public Texcoord T0 => default(Texcoord);
            public Texcoord T1 => default(Texcoord);
            public Texcoord T2 => default(Texcoord);
            public Texcoord T3 => default(Texcoord);

            public TmdTsb Tsb => default(TmdTsb);

            public TmdCba Cba => default(TmdCba);

            public int VertexCount => 4;

            public int NormalCount => 0;

            public int ColorCount => 1;
        }

        [StructLayout(LayoutKind.Explicit, Size = 24)]
        public struct TmdPrimitiveGn4 : ITmdPrimitive {
            [FieldOffset( 0)] private Rgb888 _C0;
            [FieldOffset( 3)] public TmdPrimitiveMode Mode;
            [FieldOffset( 4)] private Rgb888 _C1;
            [FieldOffset( 8)] private Rgb888 _C2;
            [FieldOffset(12)] private Rgb888 _C3;
            [FieldOffset(16)] private ushort _IndexV0;
            [FieldOffset(18)] private ushort _IndexV1;
            [FieldOffset(20)] private ushort _IndexV2;
            [FieldOffset(22)] private ushort _IndexV3;

            public TmdPrimitiveType Type => TmdPrimitiveType.Gn4;

            public int IndexV0 => _IndexV0;
            public int IndexV1 => _IndexV1;
            public int IndexV2 => _IndexV2;
            public int IndexV3 => _IndexV3;

            public int IndexN0 => -1;
            public int IndexN1 => -1;
            public int IndexN2 => -1;
            public int IndexN3 => -1;

            public Rgb888 C0 => _C0;
            public Rgb888 C1 => _C1;
            public Rgb888 C2 => _C2;
            public Rgb888 C3 => _C3;

            public Texcoord T0 => default(Texcoord);
            public Texcoord T1 => default(Texcoord);
            public Texcoord T2 => default(Texcoord);
            public Texcoord T3 => default(Texcoord);

            public TmdTsb Tsb => default(TmdTsb);

            public TmdCba Cba => default(TmdCba);

            public int VertexCount => 4;

            public int NormalCount => 0;

            public int ColorCount => 4;
        }

        [StructLayout(LayoutKind.Explicit, Size = 28)]
        public struct TmdPrimitiveFnt4 : ITmdPrimitive {
            [FieldOffset( 0)] private Texcoord _T0;
            [FieldOffset( 2)] private TmdCba _Cba;
            [FieldOffset( 4)] private Texcoord _T1;
            [FieldOffset( 6)] private TmdTsb _Tsb;
            [FieldOffset( 8)] private Texcoord _T2;
            [FieldOffset(12)] private Texcoord _T3;
            [FieldOffset(16)] private Rgb888 _Color;
            [FieldOffset(20)] private ushort _IndexV0;
            [FieldOffset(22)] private ushort _IndexV1;
            [FieldOffset(24)] private ushort _IndexV2;
            [FieldOffset(26)] private ushort _IndexV3;

            public TmdPrimitiveType Type => TmdPrimitiveType.Fnt4;

            public int IndexV0 => _IndexV0;
            public int IndexV1 => _IndexV1;
            public int IndexV2 => _IndexV2;
            public int IndexV3 => _IndexV3;

            public int IndexN0 => -1;
            public int IndexN1 => -1;
            public int IndexN2 => -1;
            public int IndexN3 => -1;

            public Rgb888 C0 => _Color;
            public Rgb888 C1 => _Color;
            public Rgb888 C2 => _Color;
            public Rgb888 C3 => _Color;

            public Texcoord T0 => _T0;
            public Texcoord T1 => _T1;
            public Texcoord T2 => _T2;
            public Texcoord T3 => _T3;

            public TmdTsb Tsb => _Tsb;

            public TmdCba Cba => _Cba;

            public int VertexCount => 4;

            public int NormalCount => 0;

            public int ColorCount => 1;
        }

        [StructLayout(LayoutKind.Explicit, Size = 40)]
        public struct TmdPrimitiveGnt4 : ITmdPrimitive {
            [FieldOffset( 0)] private Texcoord _T0;
            [FieldOffset( 2)] private TmdCba _Cba;
            [FieldOffset( 4)] private Texcoord _T1;
            [FieldOffset( 6)] private TmdTsb _Tsb;
            [FieldOffset( 8)] private Texcoord _T2;
            [FieldOffset(12)] private Texcoord _T3;
            [FieldOffset(16)] private Rgb888 _C0;
            [FieldOffset(20)] private Rgb888 _C1;
            [FieldOffset(24)] private Rgb888 _C2;
            [FieldOffset(28)] private Rgb888 _C3;
            [FieldOffset(32)] private ushort _IndexV0;
            [FieldOffset(34)] private ushort _IndexV1;
            [FieldOffset(36)] private ushort _IndexV2;
            [FieldOffset(38)] private ushort _IndexV3;

            public TmdPrimitiveType Type => TmdPrimitiveType.Gnt4;

            public int IndexV0 => _IndexV0;
            public int IndexV1 => _IndexV1;
            public int IndexV2 => _IndexV2;
            public int IndexV3 => _IndexV3;

            public int IndexN0 => -1;
            public int IndexN1 => -1;
            public int IndexN2 => -1;
            public int IndexN3 => -1;

            public Rgb888 C0 => _C0;
            public Rgb888 C1 => _C1;
            public Rgb888 C2 => _C2;
            public Rgb888 C3 => _C3;

            public Texcoord T0 => _T0;
            public Texcoord T1 => _T1;
            public Texcoord T2 => _T2;
            public Texcoord T3 => _T3;

            public TmdTsb Tsb => _Tsb;

            public TmdCba Cba => _Cba;

            public int VertexCount => 4;

            public int NormalCount => 0;

            public int ColorCount => 4;
        }

        #endregion
    }
}
