using System;
using System.Runtime.InteropServices;
using PsyCross.Math;

namespace PsyCross {
    public static partial class PsyQ {
        [StructLayout(LayoutKind.Explicit, Size = CopyCpuToVram._Size)]
        public struct CopyCpuToVram : ICommand {
            private const byte _CommandValue = 0xA0;
            private const int _WordSize      = 3;
            private const int _Size          = _WordSize * sizeof(UInt32);

            [FieldOffset( 3)] private byte Command;
            [FieldOffset( 4)] public Point2d P;
            [FieldOffset( 8)] public ushort ShortWordWidth;
            [FieldOffset(10)] public ushort ShortWordHeight;

            public int GetWordSize() => _WordSize;

            public void SetCommand() => Command = CommandUtility.GetCommandCode(_CommandValue);

            public void CalculateShortWordDim(int width, int height, BitDepth bitDepth) {
                switch (bitDepth) {
                    case BitDepth.Bpp4:
                        ShortWordWidth = (ushort)(((width >= 0) ? width : (width + 3)) >> 2);
                        break;
                    case BitDepth.Bpp8:
                        ShortWordWidth = (ushort)((width + (width >> 31)) >> 1);
                        break;
                    case BitDepth.Bpp15:
                    default:
                        ShortWordWidth = (ushort)width;
                        break;
                }

                ShortWordHeight = (ushort)height;
            }
        }
    }
}
