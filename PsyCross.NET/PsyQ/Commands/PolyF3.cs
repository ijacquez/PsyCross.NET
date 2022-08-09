using System.Runtime.InteropServices;
using PsyCross.Math;

namespace PsyCross {
    public static partial class PsyQ {
        [StructLayout(LayoutKind.Explicit)]
        public struct PolyF3 : ICommand {
            private const byte _CommandValue = 0x20;

            [FieldOffset( 0)] public Rgb888 Color;
            [FieldOffset( 3)] internal byte Command;
            [FieldOffset( 4)] public Vector2Short P0;
            [FieldOffset( 8)] public Vector2Short P1;
            [FieldOffset(12)] public Vector2Short P2;

            public void SetCommand() =>
                PsyQ.Command.SetCommand(ref Command, _CommandValue);

            public void ToggleSemiTransparency(bool active) =>
                PsyQ.Command.ToggleSemiTransparency(ref Command, active);

            public void ToggleTexBlending(bool active) =>
                PsyQ.Command.ToggleTexBlending(ref Command, active);
        }
    }
}
