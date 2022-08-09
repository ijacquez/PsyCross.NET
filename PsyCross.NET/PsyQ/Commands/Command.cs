namespace PsyCross {
    public static partial class PsyQ {
        internal static class Command {
            internal static void SetCommand(ref byte code, byte command) =>
                code = (byte)((code & ~0xFC) | command);

            internal static void ToggleSemiTransparency(ref byte code, bool active) =>
                code = (byte)((active) ? (code | 0x02) : (code & ~0x02));

            internal static void ToggleTexBlending(ref byte code, bool active) =>
                code = (byte)((active) ? (code | 0x01) : (code & ~0x01));
        }
    }
}
