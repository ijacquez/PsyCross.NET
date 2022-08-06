using PsyCross.Math;

namespace PsyCross {
    public static partial class PsyQ {
        static PsyQ() {
        }

        public static DispEnv ActiveDispEnv { get; private set; }

        public static DrawEnv ActiveDrawEnv { get; private set; }

        private static CommandBuffer _drawCommandBuffer;

        public static void SetDispMask(uint v) {
            PSX.Gpu.WriteGP1(0x03000001 - (v & 0x1));
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

        // public static void ClearImage() {
        // }

        public static void LoadImage(CommandBuffer commandBuffer, RectInt rect, BitDepth bitDepth, uint[] data) {
            uint shortWordWidth = 0;
            uint shortWordHeight = 0;

            switch (bitDepth) {
                case BitDepth.Bpp4:
                    shortWordWidth = (uint)(((rect.Width >= 0) ? rect.Width : (rect.Width + 3)) >> 2);
                    shortWordHeight = (uint)rect.Height;
                    break;
                case BitDepth.Bpp8:
                    shortWordWidth = (uint)((rect.Width + (rect.Width >> 31)) >> 1);
                    break;
                case BitDepth.Bpp15:
                    shortWordWidth = (uint)rect.Width;
                    shortWordHeight = (uint)rect.Height;
                    break;
            }

            CopyCpuToVram(commandBuffer, (uint)rect.X, (uint)rect.Y, shortWordWidth, shortWordHeight, data);
        }

        public static ushort LoadTPage(CommandBuffer commandBuffer, RectInt rect, BitDepth bitDepth, uint[] data) {
            LoadImage(commandBuffer, rect, bitDepth, data);

            return GetTPage(bitDepth, (uint)rect.X, (uint)rect.Y);
        }

        // GPU Memory Transfer Commands (GP0): $02 - Fill Rectangle In VRAM
        public static void FillRectVram(CommandBuffer commandBuffer, uint color, RectInt rect) {
            var command = commandBuffer.AllocateCommand(3);

            command[0] = 0x02_000000 | (color & 0xFF_FF_FF);
            command[1] = (uint)((rect.Y << 16) + (rect.X & 0xFFFF));
            command[2] = (uint)((rect.Height << 16) + (rect.Width & 0xFFFF));
        }

        // GPU Memory Transfer Commands (GP0): $80 - Copy Rectangle (VRAM To VRAM)
        private static void CopyVramToVram(CommandBuffer commandBuffer, RectInt srcRect, Vector2Int dstPoint) {
            var command = commandBuffer.AllocateCommand(4);

            command[0] = 0x80_000000;
            command[1] = (uint)((srcRect.Y << 16) + (srcRect.X & 0xFFFF));
            command[2] = (uint)((dstPoint.Y << 16) + (dstPoint.X & 0xFFFF));
            command[3] = (uint)((srcRect.Height << 16) + (srcRect.Width & 0xFFFF));
        }

        // GPU Memory Transfer Commands (GP0): $A0 - Copy Rectangle (CPU To VRAM)
        private static void CopyCpuToVram(CommandBuffer commandBuffer, uint x, uint y, uint shortWordWidth, uint shortWordHeight, uint[] data) {
            int dataWordCount =  data.Length;
            int dataRoundedWordCount = dataWordCount + (dataWordCount & 1);
            var command = commandBuffer.AllocateCommand(3 + dataRoundedWordCount);

            command[0] = 0xA0_000000;
            command[1] = ((y << 16) + (x & 0xFFFF));
            command[2] = ((shortWordHeight << 16) + (shortWordWidth & 0xFFFF));

            for (int i = 0; i < data.Length; i++) {
                command[3 + i] = data[i];
            }
        }

        // private void FillTri(uint color, uint x1, uint y1, uint x2, uint y2, uint x3, uint y3) {
        //     var command = _commandBuffer.AllocateCommand(4);
        //
        //     command[0] = (0x20 << 24) | (color & 0xFF_FF_FF);
        //     command[1] = (y1 << 16) + (x1 & 0xFFFF);
        //     command[2] = (y2 << 16) + (x2 & 0xFFFF);
        //     command[3] = (y3 << 16) + (x3 & 0xFFFF);
        // }

        // private void FillTriAlpha(uint color, uint x1, uint y1, uint x2, uint y2, uint x3, uint y3) {
        //     var command = _commandBuffer.AllocateCommand(4);
        //
        //     command[0] = ((0x22 << 24) | (color & 0xFF_FF_FF));
        //     command[1] = ((y1 << 16) + (x1 & 0xFFFF));
        //     command[2] = ((y2 << 16) + (x2 & 0xFFFF));
        //     command[3] = ((y3 << 16) + (x3 & 0xFFFF));
        // }
    }
}
