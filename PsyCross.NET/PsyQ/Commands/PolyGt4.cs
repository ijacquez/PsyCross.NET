using System.Runtime.InteropServices;
using PsyCross.Math;

namespace PsyCross {
    public static partial class PsyQ {
        [StructLayout(LayoutKind.Explicit)]
        public struct PolyGt4 : ICommand {
            private const byte _CommandValue = 0x3C;

            [FieldOffset( 0)] public Rgb888 C0;
            [FieldOffset( 3)] internal byte Command;
            [FieldOffset( 4)] public Point2d P0;
            [FieldOffset( 8)] public Texcoord T0;
            [FieldOffset(10)] public ushort ClutId;
            [FieldOffset(12)] public Rgb888 C1;
            [FieldOffset(16)] public Point2d P1;
            [FieldOffset(20)] public Texcoord T1;
            [FieldOffset(22)] public ushort TPageId;
            [FieldOffset(24)] public Rgb888 C2;
            [FieldOffset(28)] public Point2d P2;
            [FieldOffset(32)] public Texcoord T2;
            [FieldOffset(36)] public Rgb888 C3;
            [FieldOffset(40)] public Point2d P3;
            [FieldOffset(44)] public Texcoord T3;

            public void SetCommand() =>
                PsyQ.Command.SetCommand(ref Command, _CommandValue);

            public void ToggleSemiTransparency(bool active) =>
                PsyQ.Command.ToggleSemiTransparency(ref Command, active);

            public void ToggleTexBlending(bool active) =>
                PsyQ.Command.ToggleTexBlending(ref Command, active);
        }
    }
}
