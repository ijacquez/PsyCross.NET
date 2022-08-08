using System;
using System.Runtime.InteropServices;
using PsyCross.Math;

namespace PsyCross {
    public static partial class PsyQ {
        [StructLayout(LayoutKind.Explicit, Size = PolyFt3._Size)]
        public struct PolyFt3 : ICommand {
            private const byte _CommandValue = 0x24;
            private const int _WordSize      = 7;
            private const int _Size          = _WordSize * sizeof(UInt32);

            [FieldOffset( 0)] public Rgb888 Color;
            [FieldOffset( 3)] private byte Command;
            [FieldOffset( 4)] public Point2d P0;
            [FieldOffset( 8)] public Texcoord T0;
            [FieldOffset(10)] public ushort ClutId;
            [FieldOffset(12)] public Point2d P1;
            [FieldOffset(16)] public Texcoord T1;
            [FieldOffset(18)] public ushort TPageId;
            [FieldOffset(20)] public Point2d P2;
            [FieldOffset(24)] public Texcoord T2; 

            public int GetWordSize() => _WordSize;

            public void SetCommand() => Command = CommandUtility.GetCommandCode(_CommandValue);
        }
    }
}
