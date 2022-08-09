using System.Runtime.InteropServices;
using PsyCross.Math;

namespace PsyCross {
    public static partial class PsyQ {
        [StructLayout(LayoutKind.Explicit)]
        public struct CopyVramToCpu : ICommand {
            private const byte _CommandValue = 0xC0;

            [FieldOffset( 3)] internal byte Command;
            [FieldOffset( 4)] public Vector2Short Point;
            [FieldOffset( 8)] public ushort ShortWordWidth;
            [FieldOffset(10)] public ushort ShortWordHeight;

            public void SetCommand() =>
                PsyQ.Command.SetCommand(ref Command, _CommandValue);

            public void SetShortWordDim(int width, int height, BitDepth bitDepth) =>
                PsyQ.Command.SetShortWordDim(bitDepth, width, height, ref ShortWordWidth, ref ShortWordHeight);
        }
    }
}
