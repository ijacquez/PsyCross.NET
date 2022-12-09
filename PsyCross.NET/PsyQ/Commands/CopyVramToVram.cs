using PsyCross.Math;
using System.Runtime.InteropServices;

namespace PsyCross {
    public static partial class PsyQ {
        [StructLayout(LayoutKind.Explicit)]
        public struct CopyVramToVram : ICommand {
            private const byte _CommandValue = 0x80;

            [FieldOffset( 3)] internal byte Command;
            [FieldOffset( 4)] public Vector2Short SrcPoint;
            [FieldOffset( 8)] public Vector2Short DstPoint;
            [FieldOffset(12)] public ushort Width;
            [FieldOffset(14)] public ushort Height;

            public void SetCommand() =>
                PsyQ.Command.SetCommand(ref Command, _CommandValue);
        }
    }
}
