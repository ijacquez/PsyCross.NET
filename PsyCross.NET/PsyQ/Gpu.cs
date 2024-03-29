using PsyCross.Math;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PsyCross {
    public static partial class PsyQ {
        static PsyQ() {
        }

        public static DispEnv ActiveDispEnv { get; private set; }

        public static DrawEnv ActiveDrawEnv { get; private set; }

        private static PrimitiveSort _PrimitiveSort;
        private static CommandBuffer _CommandBuffer;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetDispMask(bool active) {
            Psx.Gpu.WriteGP1(0x03_0000_01 - (uint)((active) ? 1 : 0));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PutDispEnv(DispEnv dispEnv) {
            ActiveDispEnv = dispEnv;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PutDrawEnv(DrawEnv drawEnv) {
            ActiveDrawEnv = drawEnv;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawPrim(PrimitiveSort primitiveSort, CommandBuffer commandBuffer) {
            _PrimitiveSort = primitiveSort;
            _CommandBuffer = commandBuffer;
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

            uint horizontalRes1 = 4;

            if (PsyQ.ActiveDispEnv.Rect.Width <= 640) {
                horizontalRes1--;
            }
            if (PsyQ.ActiveDispEnv.Rect.Width <= 512) {
                horizontalRes1--;
            }
            if (PsyQ.ActiveDispEnv.Rect.Width <= 320) {
                horizontalRes1--;
            }
            if (PsyQ.ActiveDispEnv.Rect.Width <= 256) {
                horizontalRes1--;
            }

            Psx.Gpu.WriteGP0(0xE1_000000 | ditherBit | drawBit);
            Psx.Gpu.WriteGP0(0xE3_000000 | ((y1 & 0x1FF) << 10) | (x1 & 0x3FF)); // Set Drawing Area top left (X1,Y1)
            Psx.Gpu.WriteGP0(0xE4_000000 | ((y2 & 0x1FF) << 10) | (x2 & 0x3FF)); // Set Drawing Area bottom right (X2,Y2)
            Psx.Gpu.WriteGP0(0xE5_000000 | (uint)(((offsetY & 0x7FF) << 11) | (offsetX & 0x7FF))); // Must allow -1024...+1023

            uint isRgb24Bit = (PsyQ.ActiveDispEnv.IsRgb24) ? 1U << 4 : 0;

            Psx.Gpu.WriteGP1(0x08_000000 | horizontalRes1 | isRgb24Bit);

            uint dx1 = (uint)PsyQ.ActiveDispEnv.Rect.X;
            uint dy1 = (uint)PsyQ.ActiveDispEnv.Rect.Y;

            Psx.Gpu.WriteGP1(0x05_000000 | ((dy1 & 0x1FF) << 10) | (dx1 & 0x3FF)); // Start of Display area (in VRAM)

            if (PsyQ.ActiveDrawEnv.IsClear) {
                PsyQ.ClearImage(PsyQ.ActiveDrawEnv.ClipRect, PsyQ.ActiveDrawEnv.Color);
            }

            if (_CommandBuffer != null) {
                for (int i = _PrimitiveSort.Primitives.Length - 1; i >= 0; i--) {
                    var primitive = _PrimitiveSort.Primitives[i];

                    var commandSpan = CommandBuffer.GetCommandAsWords(primitive.CommandHandle);

                    Psx.Gpu.Process(commandSpan);
                }
            }

            _CommandBuffer = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort GetTPage(BitDepth bitDepth, ushort x, ushort y) {
            uint abr = 0; // Semi Transparency (0=B/2+F/2, 1=B+F, 2=B-F, 3=B+F/4)

            return (ushort)(((uint)bitDepth << 7) |
                            ((abr & 0x3) << 5) |
                            ((y & 0x100) >> 4) |
                            ((x & 0x3FF) >> 6) |
                            ((y & 0x200) << 2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort GetClut(uint x, uint y) =>
            (ushort)((y << 6) | ((x >> 4) & 0x3F));

        public static ushort LoadClut(Rgb1555[] clut, uint x, uint y) {
            // Clamp number of colors to [1..256]
            ushort width = (ushort)System.Math.Min(256, System.Math.Max(clut.Length, 1));

            LoadImage(new RectShort((short)x, (short)y, width, 1), BitDepth.Bpp15, AsWords(clut));

            return GetClut(x, y);
        }

        public static void ClearImage(RectInt rect, Rgb888 color) {
            Span<FillRectVram> commandSpan = stackalloc FillRectVram[1];

            commandSpan[0].SetCommand();
            commandSpan[0].Color = color;
            commandSpan[0].Point = new Vector2Short((short)rect.X, (short)rect.Y);
            commandSpan[0].Width = (ushort)System.Math.Min(rect.Width, 1023);
            commandSpan[0].Height = (ushort)System.Math.Min(rect.Height, 511);

            foreach (var value in AsWords(commandSpan)) {
                Psx.Gpu.WriteGP0(value);
            }
        }

        public static void MoveImage(Vector2Short srcPoint, RectInt dstRect) {
            Span<CopyVramToVram> commandSpan = stackalloc CopyVramToVram[1];

            commandSpan[0].SetCommand();
            commandSpan[0].SrcPoint = srcPoint;
            commandSpan[0].DstPoint = new Vector2Short((short)dstRect.X, (short)dstRect.Y);
            commandSpan[0].Width = (ushort)dstRect.Width;
            commandSpan[0].Height = (ushort)dstRect.Height;

            foreach (var value in AsWords(commandSpan)) {
                Psx.Gpu.WriteGP0(value);
            }
        }

        public static void StoreImage(RectShort rect, BitDepth bitDepth, Memory<byte> data) =>
            StoreImage(rect, bitDepth, data.Span);

        public static void StoreImage(RectShort rect, BitDepth bitDepth, Span<byte> data) {
            Span<CopyVramToCpu> commandSpan = stackalloc CopyVramToCpu[1];

            commandSpan[0].SetCommand();
            commandSpan[0].Point = new Vector2Short((short)rect.X, (short)rect.Y);
            commandSpan[0].ShortWordWidth = (ushort)rect.Width;
            commandSpan[0].ShortWordHeight = (ushort)rect.Height;

            foreach (var value in AsWords(commandSpan)) {
                Psx.Gpu.WriteGP0(value);
            }

            int shortWordCount = commandSpan[0].ShortWordWidth * commandSpan[0].ShortWordHeight;
            int wordCount = System.Math.Max(1, shortWordCount / sizeof(UInt32));

            Span<uint> dataWords = AsWords(data);

            for (int i = 0; i < System.Math.Min(dataWords.Length, wordCount); i++) {
                dataWords[i] = Psx.Gpu.LoadGpuRead();
            }
        }

        public static void LoadImage(RectInt rect, BitDepth bitDepth, ReadOnlyMemory<byte> data) =>
            LoadImage(rect, bitDepth, MemoryMarshal.Cast<byte, uint>(data.Span));

        public static void LoadImage(RectInt rect, BitDepth bitDepth, ReadOnlySpan<uint> data) {
            Span<CopyCpuToVram> commandSpan = stackalloc CopyCpuToVram[1];

            commandSpan[0].SetCommand();
            commandSpan[0].Point = new Vector2Short((short)rect.X, (short)rect.Y);
            commandSpan[0].ShortWordWidth = (ushort)rect.Width;
            commandSpan[0].ShortWordHeight = (ushort)rect.Height;

            foreach (var value in AsWords(commandSpan)) {
                Psx.Gpu.WriteGP0(value);
            }

            Psx.Gpu.Process(data);
        }

        public static ushort LoadTPage(RectInt rect, BitDepth bitDepth, ReadOnlyMemory<byte> data) =>
            LoadTPage(rect, bitDepth, MemoryMarshal.Cast<byte, uint>(data.Span));

        public static ushort LoadTPage(RectInt rect, BitDepth bitDepth, ReadOnlySpan<uint> data) {
            SetShortWordDimensions(bitDepth, ref rect);

            LoadImage(rect, bitDepth, data);

            return GetTPage(bitDepth, (ushort)rect.X, (ushort)rect.Y);
        }

        private static void SetShortWordDimensions(BitDepth bitDepth, ref RectInt inRect) {
            switch (bitDepth) {
                case BitDepth.Bpp4:
                    inRect.Width = (ushort)(((inRect.Width >= 0) ? inRect.Width : (inRect.Width + 3)) >> 2);
                    break;
                case BitDepth.Bpp8:
                    inRect.Width = (ushort)((inRect.Width + (inRect.Width >> 31)) >> 1);
                    break;
                case BitDepth.Bpp15:
                default:
                    inRect.Width = (ushort)inRect.Width;
                    break;
            }

            inRect.Height = (ushort)inRect.Height;
        }

        private static Span<byte> AsBytes<T>(T[] clut) where T : struct =>
            MemoryMarshal.Cast<T, byte>(clut);

        private static Span<uint> AsWords<T>(Span<T> data) where T : struct =>
            MemoryMarshal.Cast<T, uint>(data);

        private static Span<uint> AsWords<T>(T[] data) where T : struct =>
            MemoryMarshal.Cast<T, uint>(data);
    }
}
