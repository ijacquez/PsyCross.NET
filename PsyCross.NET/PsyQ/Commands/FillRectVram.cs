using System;
using System.Runtime.InteropServices;
using PsyCross.Math;

namespace PsyCross {
    public static partial class PsyQ {
        [StructLayout(LayoutKind.Explicit, Size = FillRectVram._Size)]
        public struct FillRectVram : ICommand {
            private const byte _CommandValue = 0x02;
            private const int _WordSize      = 3;
            private const int _Size          = _WordSize * sizeof(UInt32);

            [FieldOffset( 0)] public Rgb888 Color;
            [FieldOffset( 3)] private byte Command;
            [FieldOffset( 4)] public Point2d P;
            [FieldOffset( 8)] public ushort Width;
            [FieldOffset(10)] public ushort Height;

            public int GetWordSize() => _WordSize;

            public void SetCommand() => Command = _CommandValue;
        }
    }
}
