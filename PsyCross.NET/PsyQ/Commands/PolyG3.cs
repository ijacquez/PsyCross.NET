using System;
using System.Runtime.InteropServices;
using PsyCross.Math;

namespace PsyCross {
    public static partial class PsyQ {
        [StructLayout(LayoutKind.Explicit, Size = PolyG3._Size)]
        public struct PolyG3 : ICommand {
            private const byte _CommandValue = 0x30;
            private const int _WordSize      = 6;
            private const int _Size          = _WordSize * sizeof(UInt32);

            [FieldOffset( 0)] public Rgb888 C0;
            [FieldOffset( 3)] private byte Command;
            [FieldOffset( 4)] public Point2d P0;
            [FieldOffset( 8)] public Rgb888 C1;
            [FieldOffset(12)] public Point2d P1;
            [FieldOffset(16)] public Rgb888 C2;
            [FieldOffset(20)] public Point2d P2;

            public int GetWordSize() => _WordSize;

            public void SetCommand() => Command = CommandUtility.GetCommandCode(_CommandValue);
        }
    }
}
