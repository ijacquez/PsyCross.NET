using System.Runtime.InteropServices;
using PsyCross.Math;

namespace PsyCross {
    public static partial class PsyQ {
        [StructLayout(LayoutKind.Explicit)]
        public struct PolyG4 : ICommand {
            private const byte _CommandValue = 0x38;

            [FieldOffset( 0)] public Rgb888 C0;
            [FieldOffset( 3)] internal byte Command;
            [FieldOffset( 4)] public Point2d P0;
            [FieldOffset( 8)] public Rgb888 C1;
            [FieldOffset(12)] public Point2d P1;
            [FieldOffset(16)] public Rgb888 C2;
            [FieldOffset(20)] public Point2d P2;
            [FieldOffset(16)] public Rgb888 C3;
            [FieldOffset(20)] public Point2d P3;

            public void SetCommand() =>
                PsyQ.Command.SetCommand(ref Command, _CommandValue);

            public void ToggleSemiTransparency(bool active) =>
                PsyQ.Command.ToggleSemiTransparency(ref Command, active);

            public void ToggleTexBlending(bool active) =>
                PsyQ.Command.ToggleTexBlending(ref Command, active);
        }
    }
}
