using System;
using System.Runtime.InteropServices;

namespace PsyCross {
    public static partial class PsyQ {
        internal static class Command {
            internal static int GetWordCount<T>() where T : ICommand =>
                ((Marshal.SizeOf<T>() + (sizeof(UInt32) - 1)) / sizeof(UInt32));

            internal static void SetCommand(ref byte code, byte command) =>
                code = (byte)((code & ~0xFC) | command);

            internal static void ToggleSemiTransparency(ref byte code, bool active) =>
                code = (byte)((active) ? (code | 0x02) : (code & ~0x02));

            internal static void ToggleTexBlending(ref byte code, bool active) =>
                code = (byte)((active) ? (code | 0x01) : (code & ~0x01));

            internal static void SetShortWordDim(BitDepth bitDepth,
                                                 int width,
                                                 int height,
                                                 ref ushort shortWordWidth,
                                                 ref ushort shortWordHeight) {
                switch (bitDepth) {
                    case BitDepth.Bpp4:
                        shortWordWidth = (ushort)(((width >= 0) ? width : (width + 3)) >> 2);
                        break;
                    case BitDepth.Bpp8:
                        shortWordWidth = (ushort)((width + (width >> 31)) >> 1);
                        break;
                    case BitDepth.Bpp15:
                    default:
                        shortWordWidth = (ushort)width;
                        break;
                }

                shortWordHeight = (ushort)height;
            }
        }
    }
}
