using PsyCross.Math;
using System.Runtime.InteropServices;

namespace PsyCross {
    public static partial class PsyQ {
        [StructLayout(LayoutKind.Explicit)]
        public struct PolyGt3 : ICommand {
            private const byte _CommandValue = 0x34;

            [FieldOffset( 0)] public Rgb888 C0;
            [FieldOffset( 3)] internal byte Command;
            [FieldOffset( 4)] public Vector2Short P0;
            [FieldOffset( 8)] public Texcoord T0;
            [FieldOffset(10)] public ushort ClutId;
            [FieldOffset(12)] public Rgb888 C1;
            [FieldOffset(16)] public Vector2Short P1;
            [FieldOffset(20)] public Texcoord T1;
            [FieldOffset(22)] public ushort TPageId;
            [FieldOffset(24)] public Rgb888 C2;
            [FieldOffset(28)] public Vector2Short P2;
            [FieldOffset(32)] public Texcoord T2;

            public void SetCommand() =>
                PsyQ.Command.SetCommand(ref Command, _CommandValue);

            public void ToggleSemiTransparency(bool active) =>
                PsyQ.Command.ToggleSemiTransparency(ref Command, active);

            public void ToggleTexBlending(bool active) =>
                PsyQ.Command.ToggleTexBlending(ref Command, active);
        }
    }
}
