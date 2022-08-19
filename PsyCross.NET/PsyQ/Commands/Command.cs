using System;
using System.Runtime.InteropServices;

namespace PsyCross {
    public static partial class PsyQ {
        internal static class Command {
            internal static int GetWordCount<T>() where T : ICommand =>
                ((Marshal.SizeOf<T>() + (sizeof(UInt32) - 1)) / sizeof(UInt32));

            internal static void SetCommand(ref byte command, byte commandCode) =>
                command = commandCode;

            internal static void ToggleSemiTransparency(ref byte command, bool active) =>
                command = (byte)((active) ? (command | 0x02) : (command & ~0x02));

            internal static void ToggleTexBlending(ref byte command, bool active) =>
                command = (byte)((active) ? (command | 0x01) : (command & ~0x01));
        }
    }
}
