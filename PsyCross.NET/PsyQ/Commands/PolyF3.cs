using System;
using System.Runtime.InteropServices;
using PsyCross.Math;

namespace PsyCross {
    public static partial class PsyQ {
        [StructLayout(LayoutKind.Explicit, Size = PolyF3._Size)]
        public struct PolyF3 : ICommand {
            private const byte _CommandValue = 0x20;
            private const int _WordSize      = 4;
            private const int _Size          = _WordSize * sizeof(UInt32);

            [FieldOffset( 0)] public Rgb888 Color;
            [FieldOffset( 3)] private byte Command;
            [FieldOffset( 4)] public Point2d P0;
            [FieldOffset( 8)] public Point2d P1;
            [FieldOffset(12)] public Point2d P2;

            public int GetWordSize() => _WordSize;

            public void SetCommand() => Command = CommandUtility.GetCommandCode(_CommandValue);
        }
    }
}
