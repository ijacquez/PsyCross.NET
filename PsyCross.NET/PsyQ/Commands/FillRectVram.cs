using System.Runtime.InteropServices;
using PsyCross.Math;

namespace PsyCross {
    public static partial class PsyQ {
        [StructLayout(LayoutKind.Explicit)]
        public struct FillRectVram : ICommand {
            private const byte _CommandValue = 0x02;

            [FieldOffset( 0)] public Rgb888 Color;
            [FieldOffset( 3)] internal byte Command;
            [FieldOffset( 4)] public Point2d P;
            [FieldOffset( 8)] public ushort Width;
            [FieldOffset(10)] public ushort Height;

            public void SetCommand() =>
                PsyQ.Command.SetCommand(ref Command, _CommandValue);
        }
    }
}
