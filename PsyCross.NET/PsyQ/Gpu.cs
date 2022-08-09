using System;
using System.Runtime.InteropServices;
using PsyCross.Math;

namespace PsyCross {
    public static partial class PsyQ {
        static PsyQ() {
        }

        public static DispEnv ActiveDispEnv { get; private set; }

        public static DrawEnv ActiveDrawEnv { get; private set; }

        private static CommandBuffer _drawCommandBuffer;

        public static void SetDispMask(uint v) {
            PSX.Gpu.WriteGP1(0x03_0000_01 - (v & 0x1));
        }

        public static void PutDispEnv(DispEnv dispEnv) {
            ActiveDispEnv = dispEnv;
        }

        public static void PutDrawEnv(DrawEnv dispEnv) {
            ActiveDrawEnv = dispEnv;
        }

        public static void DrawPrim(CommandBuffer commandBuffer) {
            _drawCommandBuffer = commandBuffer;
        }

        public static void DrawSync() {
            uint x1 = (uint)PsyQ.ActiveDrawEnv.ClipRect.X;
            uint y1 = (uint)PsyQ.ActiveDrawEnv.ClipRect.Y;
            uint x2 = x1 + (uint)PsyQ.ActiveDrawEnv.ClipRect.Width - 1;
            uint y2 = y1 + (uint)PsyQ.ActiveDrawEnv.ClipRect.Height - 1;

            int offsetX = PsyQ.ActiveDrawEnv.Offset.X;
            int offsetY = PsyQ.ActiveDrawEnv.Offset.Y;

            uint ditherBit = (PsyQ.ActiveDrawEnv.IsDithered) ? 1U << 9 : 0U;
            uint drawBit = (PsyQ.ActiveDrawEnv.IsDraw) ? 1U << 10 : 0;

            PSX.Gpu.WriteGP0(0xE1_000000 | ditherBit | drawBit);
            PSX.Gpu.WriteGP0(0xE3_000000 | ((y1 & 0x1FF) << 10) | (x1 & 0x3FF)); // Set Drawing Area top left (X1,Y1)
            PSX.Gpu.WriteGP0(0xE4_000000 | ((y2 & 0x1FF) << 10) | (x2 & 0x3FF)); // Set Drawing Area bottom right (X2,Y2)
            PSX.Gpu.WriteGP0(0xE5_000000 | (uint)(((offsetY & 0x7FF) << 11) | (offsetX & 0x7FF))); // Must allow -1024...+1023

            uint dx1 = (uint)PsyQ.ActiveDispEnv.Rect.X;
            uint dy1 = (uint)PsyQ.ActiveDispEnv.Rect.Y;

            PSX.Gpu.WriteGP1(0x05_000000 | ((dy1 & 0x1FF) << 10) | (dx1 & 0x3FF)); // Start of Display area (in VRAM)

            if (PsyQ.ActiveDrawEnv.IsClear) {
                PsyQ.ClearImage(PsyQ.ActiveDrawEnv.ClipRect, new Rgb888(0x55, 0x55, 0x55));
            }

            if (_drawCommandBuffer != null) {
                PSX.Gpu.Process(_drawCommandBuffer.Bits);
            }

            _drawCommandBuffer = null;
        }

        // Calculates the TPage attributes
        public static ushort GetTPage(BitDepth bitDepth, uint x, uint y) {
            uint abr = 0; // Semi Transparency (0=B/2+F/2, 1=B+F, 2=B-F, 3=B+F/4)

            return (ushort)(((uint)bitDepth << 7) |
                            ((abr & 0x3) << 5) |
                            ((y & 0x100) >> 4) |
                            ((x & 0x3FF) >> 6) |
                            ((y & 0x200) << 2));
        }

        public static void ClearImage(RectInt rect, Rgb888 color) {
            Span<FillRectVram> commandSpan = stackalloc FillRectVram[1];

            commandSpan[0].SetCommand();
            commandSpan[0].Color = color;
            commandSpan[0].P = new Point2d((short)rect.X, (short)rect.Y);
            commandSpan[0].Width = (ushort)rect.Width;
            commandSpan[0].Height = (ushort)rect.Height;

            foreach (var value in AsWords(commandSpan)) {
                PSX.Gpu.WriteGP0(value);
            }
        }

        public static void LoadImage(RectInt rect, BitDepth bitDepth, uint[] data) {
            Span<CopyCpuToVram> commandSpan = stackalloc CopyCpuToVram[1];

            commandSpan[0].SetCommand();
            commandSpan[0].P = new Point2d((short)rect.X, (short)rect.Y);
            commandSpan[0].SetShortWordDim(rect.Width, rect.Height, bitDepth);

            foreach (var value in AsWords(commandSpan)) {
                PSX.Gpu.WriteGP0(value);
            }

            PSX.Gpu.Process(data.AsSpan<uint>());
        }

        public static ushort LoadTPage(RectInt rect, BitDepth bitDepth, uint[] data) {
            LoadImage(rect, bitDepth, data);

            return GetTPage(bitDepth, (uint)rect.X, (uint)rect.Y);
        }

        // // GPU Memory Transfer Commands (GP0): $80 - Copy Rectangle (VRAM To VRAM)
        // private static void CopyVramToVram(CommandBuffer commandBuffer, RectInt srcRect, Vector2Int dstPoint) {
        //     var command = commandBuffer.AllocateCommand(4);
        //
        //     command[0] = 0x80_000000;
        //     command[1] = (uint)((srcRect.Y << 16) + (srcRect.X & 0xFFFF));
        //     command[2] = (uint)((dstPoint.Y << 16) + (dstPoint.X & 0xFFFF));
        //     command[3] = (uint)((srcRect.Height << 16) + (srcRect.Width & 0xFFFF));
        // }

        private static Span<uint> AsWords<T>(Span<T> commandSpan) where T : struct, ICommand =>
            MemoryMarshal.Cast<T, uint>(commandSpan);
    }
}
