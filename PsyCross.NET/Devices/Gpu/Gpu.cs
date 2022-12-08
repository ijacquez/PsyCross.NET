using PsyCross.Devices.GPU;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System;
using PsyCross.Math;

namespace ProjectPSX.Devices {
    public class Gpu {
        private uint _gpuRead; // 1F801810h-Read GPUREAD Receive responses to GP0(C0h) and GP1(10h) commands

        private uint _command;
        private int _commandSize;
        private readonly uint[] _commandFifo = new uint[16];
        private int _commandPointer;

        private static readonly int[] _Resolutions = { 256, 320, 512, 640, 368 }; // GPUSTAT res index
        private static readonly int[] _DotClockDiv = { 10, 8, 5, 4, 7 };

        private static readonly int[,] _DitherTable = new int[,] {
            { -4, +0, -3, +1 }, // ;\ Dither offsets for first two scanlines
            { +2, -2, +3, -1 }, // ;/
            { -3, +1, -4, +0 }, // ;\ Dither offsets for next two scanlines
            { +3, -1, +2, -2 }  // ;/  (same as above, but shifted two pixels horizontally)
        };

        private static byte[,,] _DitherTableWithSaturation = new byte[4, 4, 512];

        private enum Mode {
            Command,
            Vram
        }

        private Mode _mode;

        private struct Primitive {
            public bool IsShaded { get; set; }
            public bool IsTextured { get; set; }
            public bool IsSemiTransparent { get; set; }
            public bool IsRawTextured { get; set; } // If not: blended
            public int Depth { get; set; }
            public int SemiTransparencyMode { get; set; }
            public Point2D Clut { get; set; }
            public Point2D TextureBase { get; set; }
        }

        private struct VramTransfer {
            public int X;
            public int Y;
            public ushort W;
            public ushort H;
            public int originX;
            public int originY;
            public int halfWords;
        }

        private VramTransfer _vramTransfer;

        [StructLayout(LayoutKind.Explicit)]
        private struct Point2D {
            [FieldOffset(0)] public short X;
            [FieldOffset(2)] public short Y;

            public Point2D(short x, short y) {
                X = x;
                Y = y;
            }
        }

        private Point2D _min = new Point2D();
        private Point2D _max = new Point2D();

        [StructLayout(LayoutKind.Explicit)]
        private struct TextureData {
            [FieldOffset(0)] public ushort Value;
            [FieldOffset(0)] public byte X;
            [FieldOffset(1)] public byte Y;
        }

        private TextureData _textureData = new TextureData();

        // XXX: Change this to Rgb888?
        [StructLayout(LayoutKind.Explicit)]
        private struct Color {
            [FieldOffset(0)] public uint Value;
            [FieldOffset(0)] public byte R;
            [FieldOffset(1)] public byte G;
            [FieldOffset(2)] public byte B;
            [FieldOffset(3)] public byte Msb;
        }

        private static readonly uint[] _Color1555to8888Lut = new uint[ushort.MaxValue + 1];

        private Color _color0;
        private Color _color1;
        private Color _color2;

        private bool _isTextureDisabledAllowed;

        // GP0
        private byte _textureXBase;
        private byte _textureYBase;
        private byte _transparencyMode;
        private byte _textureDepth;
        private bool _isDithered;
        private bool _isDrawingToDisplayAllowed;
        private uint _maskWhileDrawing;
        private bool _checkMaskBeforeDraw;
        private bool _isInterlaceField;
        private bool _isReverseFlag;
        private bool _isTextureDisabled;
        private byte _horizontalResolution2;
        private byte _horizontalResolution1;
        private bool _isVerticalResolution480;
        private bool _isPal;
        private bool _is24BitDepth;
        private bool _isVerticalInterlace;
        private bool _isDisplayDisabled;
        private bool _isInterruptRequested;
        private bool _isDmaRequest;

        private bool _isReadyToReceiveCommand = true;
        private bool _isReadyToSendVramToCpu;
        private bool _isReadyToReceiveDmaBlock = true;

        private byte _dmaDirection;
        private bool _isOddLine;

        private bool _isTexturedRectangleXFlipped;
        private bool _isTexturedRectangleYFlipped;

        private uint _textureWindowBits = 0xFFFF_FFFF;
        private int _preMaskX;
        private int _preMaskY;
        private int _postMaskX;
        private int _postMaskY;

        private ushort _drawingAreaLeft;
        private ushort _drawingAreaRight;
        private ushort _drawingAreaTop;
        private ushort _drawingAreaBottom;
        private short _drawingXOffset;
        private short _drawingYOffset;

        private ushort _displayVramXStart;
        private ushort _displayVramYStart;
        private ushort _displayX1;
        private ushort _displayX2;
        private ushort _displayY1;
        private ushort _displayY2;
        private int _displayHorizontalRes;
        private int _displayVerticalRes;

        private int _horizontalTiming = 3413;
        private int _verticalTiming   = 263;

        static Gpu() {
            InitColorTable();
            InitDitheringTable();
        }

        public Gpu() {
            _mode = Mode.Command;
            GP1_00_ResetGpu();
        }

        // VRAM is 8888 and we transform everything to it
        public Vram Vram { get; } = new Vram(1024, 512);

        // An un-transformed 1555 to 8888 VRAM so we can fetch CLUT indexes
        // without reverting to 1555
        //
        // We need another copy of VRAM as CLUTs technically could be stored
        // anywhere in VRAM
        public Vram1555 Vram1555 { get; } = new Vram1555(1024, 512);

        public bool Is24BitDepth => _is24BitDepth;

        public Vector2Int DisplayVramStart =>
            new Vector2Int(_displayVramXStart, _displayVramYStart);

        public int DisplayHorizontalRes => _displayHorizontalRes;

        public int DisplayVerticalRes => _displayVerticalRes;

        public bool Debug { get; set; }

        public uint LoadGpuStat() {
            uint GpuStat = 0;

            GpuStat |= _textureXBase;
            GpuStat |= (uint)_textureYBase << 4;
            GpuStat |= (uint)_transparencyMode << 5;
            GpuStat |= (uint)_textureDepth << 7;
            GpuStat |= (uint)(_isDithered ? 1 : 0) << 9;
            GpuStat |= (uint)(_isDrawingToDisplayAllowed ? 1 : 0) << 10;
            GpuStat |= (uint)_maskWhileDrawing << 11;
            GpuStat |= (uint)(_checkMaskBeforeDraw ? 1 : 0) << 12;
            GpuStat |= (uint)(_isInterlaceField ? 1 : 0) << 13;
            GpuStat |= (uint)(_isReverseFlag ? 1 : 0) << 14;
            GpuStat |= (uint)(_isTextureDisabled ? 1 : 0) << 15;
            GpuStat |= (uint)_horizontalResolution2 << 16;
            GpuStat |= (uint)_horizontalResolution1 << 17;
            GpuStat |= (uint)(_isVerticalResolution480 ? 1 : 0) << 19;
            GpuStat |= (uint)(_isPal ? 1 : 0) << 20;
            GpuStat |= (uint)(_is24BitDepth ? 1 : 0) << 21;
            GpuStat |= (uint)(_isVerticalInterlace ? 1 : 0) << 22;
            GpuStat |= (uint)(_isDisplayDisabled ? 1 : 0) << 23;
            GpuStat |= (uint)(_isInterruptRequested ? 1 : 0) << 24;
            GpuStat |= (uint)(_isDmaRequest ? 1 : 0) << 25;

            GpuStat |= (uint)(_isReadyToReceiveCommand ? 1 : 0) << 26;
            GpuStat |= (uint)(_isReadyToSendVramToCpu ? 1 : 0) << 27;
            GpuStat |= (uint)(_isReadyToReceiveDmaBlock ? 1 : 0) << 28;

            GpuStat |= (uint)_dmaDirection << 29;
            GpuStat |= (uint)(_isOddLine ? 1 : 0) << 31;

            // Console.WriteLine("[GPU] LOAD GPUSTAT: {0}", GPUSTAT.ToString("x8"));
            return GpuStat;
        }

        public uint LoadGpuRead() {
            // TODO: check if correct and refactor
            uint value;
            if (_vramTransfer.halfWords > 0) {
                value = ReadFromVram();
            } else {
                value = _gpuRead;
            }
            // Console.WriteLine("[GPU] LOAD GPUREAD: {0}", value.ToString("x8"));
            return value;
        }

        public void Write(uint addr, uint value) {
            uint register = addr & 0xF;
            if (register == 0) {
                WriteGP0(value);
            } else if (register == 4) {
                WriteGP1(value);
            } else {
                Console.WriteLine($"[GPU] Unhandled GPU write access to register {register} : {value}");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteGP0(uint value) {
            // Console.WriteLine("Direct " + value.ToString("x8"));
            // Console.WriteLine(mode);
            if (_mode == Mode.Command) {
                // Console.WriteLine($"[GPU] GP0 Write Value: {value:X8}");
                DecodeGP0CommandFromFifo(value);
            } else {
                WriteToVram(value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Process(ReadOnlySpan<uint> buffer) {
            if (_mode == Mode.Command) {
                DecodeGP0Command(buffer);
            } else {
                for (int i = 0; i < buffer.Length; i++) {
                    WriteToVram(buffer[i]);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteToVram(uint value) {
            ushort pixel1 = (ushort)(value >> 16);
            ushort pixel0 = (ushort)(value & 0xFFFF);

            pixel0 |= (ushort)(_maskWhileDrawing << 15);
            pixel1 |= (ushort)(_maskWhileDrawing << 15);

            DrawVramPixel(pixel0);

            // Force exit if we arrived to the end pixel (fixes weird artifacts
            // on textures on Metal Gear Solid)
            if (--_vramTransfer.halfWords == 0) {
                _mode = Mode.Command;
                return;
            }

            DrawVramPixel(pixel1);

            if (--_vramTransfer.halfWords == 0) {
                _mode = Mode.Command;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint ReadFromVram() {
            ushort pixel0 = Vram.GetPixelBgr555(_vramTransfer.X++ & 0x3FF, _vramTransfer.Y & 0x1FF);
            ushort pixel1 = Vram.GetPixelBgr555(_vramTransfer.X++ & 0x3FF, _vramTransfer.Y & 0x1FF);

            if (_vramTransfer.X == _vramTransfer.originX + _vramTransfer.W) {
                _vramTransfer.X -= _vramTransfer.W;
                _vramTransfer.Y++;
            }

            _vramTransfer.halfWords -= 2;

            if (_vramTransfer.halfWords == 0) {
                _isReadyToSendVramToCpu = false;
                _isReadyToReceiveDmaBlock = true;
            }

            return (uint)((pixel1 << 16) | pixel0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DrawVramPixel(ushort color1555) {
            if (!_checkMaskBeforeDraw || ((Vram.GetPixelRgb888(_vramTransfer.X, _vramTransfer.Y) >> 24) == 0)) {
                Vram.SetPixel(_vramTransfer.X & 0x3FF, _vramTransfer.Y & 0x1FF, _Color1555to8888Lut[color1555]);
                Vram1555.SetPixel(_vramTransfer.X & 0x3FF, _vramTransfer.Y & 0x1FF, color1555);
            }

            _vramTransfer.X++;

            if (_vramTransfer.X == _vramTransfer.originX + _vramTransfer.W) {
                _vramTransfer.X -= _vramTransfer.W;
                _vramTransfer.Y++;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DecodeGP0CommandFromFifo(uint value) {
            if (_commandPointer == 0) {
                _command = value >> 24;
                _commandSize = _CommandSizeTable[(int)_command];
                // Console.WriteLine("[GPU] Direct GP0 COMMAND: {0} size: {1}", value.ToString("x8"), _commandSize);
            }

            _commandFifo[_commandPointer++] = value;
            // Console.WriteLine("[GPU] Direct GP0: {0} buffer: {1}", value.ToString("x8"), _commandPointer);

            if ((_commandPointer == _commandSize) || (_commandSize == 16) && ((value & 0xF000_F000) == 0x5000_5000)) {
                _commandPointer = 0;
                // Console.WriteLine("EXECUTING");
                ExecuteGP0(_command, _commandFifo.AsSpan());
                _commandPointer = 0;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DecodeGP0Command(ReadOnlySpan<uint> buffer) {
            while (_commandPointer < buffer.Length) {
                if (_mode == Mode.Command) {
                    _command = buffer[_commandPointer] >> 24;
                    // Console.WriteLine("Buffer Executing " + _command.ToString("x2") + " pointer " + _commandPointer);
                    ExecuteGP0(_command, buffer);
                } else {
                    WriteToVram(buffer[_commandPointer++]);
                }
            }

            _commandPointer = 0;
        }

        private void ExecuteGP0(uint opcode, ReadOnlySpan<uint> buffer) {
            // Console.WriteLine("GP0 Command: " + opcode.ToString("x2"));
            switch (opcode) {
                case 0x00: GP0_00_NOP(); break;
                case 0x01: GP0_01_MemClearCache(); break;
                case 0x02: GP0_02_FillRectVram(buffer); break;
                case 0x1F: GP0_1F_InterruptRequest(); break;

                case 0xE1: GP0_E1_SetDrawMode(buffer[_commandPointer++]); break;
                case 0xE2: GP0_E2_SetTextureWindow(buffer[_commandPointer++]); break;
                case 0xE3: GP0_E3_SetDrawingAreaTopLeft(buffer[_commandPointer++]); break;
                case 0xE4: GP0_E4_SetDrawingAreaBottomRight(buffer[_commandPointer++]); break;
                case 0xE5: GP0_E5_SetDrawingOffset(buffer[_commandPointer++]); break;
                case 0xE6: GP0_E6_SetMaskBit(buffer[_commandPointer++]); break;

                case uint _ when opcode >= 0x20 && opcode <= 0x3F:
                    GP0_RenderPolygon(buffer); break;
                case uint _ when opcode >= 0x40 && opcode <= 0x5F:
                    GP0_RenderLine(buffer); break;
                case uint _ when opcode >= 0x60 && opcode <= 0x7F:
                    GP0_RenderRectangle(buffer); break;
                case uint _ when opcode >= 0x80 && opcode <= 0x9F:
                    GP0_MemCopyRectVramtoVram(buffer); break;
                case uint _ when opcode >= 0xA0 && opcode <= 0xBF:
                    GP0_MemCopyRectCputoVram(buffer); break;
                case uint _ when opcode >= 0xC0 && opcode <= 0xDF:
                    GP0_MemCopyRectVramtoCpu(buffer); break;
                case uint _ when (opcode >= 0x3 && opcode <= 0x1E) || opcode == 0xE0 || opcode >= 0xE7 && opcode <= 0xEF:
                    GP0_00_NOP(); break;

                default:
                    Console.WriteLine("[GPU] Unsupported GP0 Command " + opcode.ToString("x8"));
                    // Console.ReadLine();
                    GP0_00_NOP();
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GP0_00_NOP() => _commandPointer++;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GP0_01_MemClearCache() => _commandPointer++;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GP0_02_FillRectVram(ReadOnlySpan<uint> buffer) {
            _color0.Value = buffer[_commandPointer++];
            uint yx = buffer[_commandPointer++];
            uint hw = buffer[_commandPointer++];

            ushort x = (ushort)(yx & 0x3F0);
            ushort y = (ushort)((yx >> 16) & 0x1FF);

            ushort w = (ushort)(((hw & 0x3FF) + 0xF) & ~0xF);
            ushort h = (ushort)((((hw >> 16) & 0x1FF) + 0xF) & ~0xF);

            uint color = (uint)(_color0.R << 16 | _color0.G << 8 | _color0.B);

            if (((x + w) <= 0x400) && ((y + h) <= 0x200)) {
                var vramSpan = new Span<uint>(Vram.Bits);
                for (int yPos = y; yPos < (h + y); yPos++) {
                    vramSpan.Slice(x + (yPos * 1024), w).Fill(color);
                }
            } else {
                for (int yPos = y; yPos < h + y; yPos++) {
                    for (int xPos = x; xPos < (w + x); xPos++) {
                        Vram.SetPixel(xPos & 0x3FF, yPos & 0x1FF, color);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GP0_1F_InterruptRequest() {
            _commandPointer++;
            _isInterruptRequested = true;
        }

        public void GP0_RenderPolygon(ReadOnlySpan<uint> buffer) {
            uint command = buffer[_commandPointer];
            // Console.WriteLine(command.ToString("x8") +  " "  + _commandBuffer.Length + " " + _pointer);

            bool isQuad = (command & (1 << 27)) != 0;

            bool isShaded = (command & (1 << 28)) != 0;
            bool isTextured = (command & (1 << 26)) != 0;
            bool isSemiTransparent = (command & (1 << 25)) != 0;
            bool isRawTextured = (command & (1 << 24)) != 0;

            Primitive primitive = new Primitive();
            primitive.IsShaded = isShaded;
            primitive.IsTextured = isTextured;
            primitive.IsSemiTransparent = isSemiTransparent;
            primitive.IsRawTextured = isRawTextured;

            int vertexN = isQuad ? 4 : 3;
            Span<uint> c = stackalloc uint[vertexN];
            Span<Point2D> v = stackalloc Point2D[vertexN];
            Span<TextureData> t = stackalloc TextureData[vertexN];

            if (!isShaded) {
                uint color = buffer[_commandPointer++];
                c[0] = color; // Triangle 1 opaque color
                c[1] = color; // Triangle 2 opaque color
            }

            primitive.SemiTransparencyMode = _transparencyMode;

            for (int i = 0; i < vertexN; i++) {
                if (isShaded) {
                    c[i] = buffer[_commandPointer++];
                }

                uint xy = buffer[_commandPointer++];
                v[i].X = (short)(Signed11bit(xy & 0xFFFF) + _drawingXOffset);
                v[i].Y = (short)(Signed11bit(xy >> 16) + _drawingYOffset);

                if (isTextured) {
                    uint textureData = buffer[_commandPointer++];
                    t[i].Value = (ushort)(textureData & 0xFFFF);
                    if (i == 0) {
                        uint palette = textureData >> 16;

                        primitive.Clut = new Point2D((short)((palette & 0x3F) << 4),
                                                     (short)((palette >> 6) & 0x1FF));
                    } else if (i == 1) {
                        uint texpage = textureData >> 16;

                        // Set Global GPU E1
                        _textureXBase = (byte)(texpage & 0xF);
                        _textureYBase = (byte)((texpage >> 4) & 0x1);
                        _transparencyMode = (byte)((texpage >> 5) & 0x3);
                        _textureDepth = (byte)((texpage >> 7) & 0x3);
                        _isTextureDisabled = _isTextureDisabledAllowed && ((texpage >> 11) & 0x1) != 0;

                        primitive.Depth = _textureDepth;
                        primitive.TextureBase = new Point2D((short)(_textureXBase << 6),
                                                            (short)(_textureYBase << 8));

                        primitive.SemiTransparencyMode = _transparencyMode;
                    }
                }
            }

            RasterizeTri(v[0], v[1], v[2], t[0], t[1], t[2], c[0], c[1], c[2], primitive);

            if (isQuad) {
                RasterizeTri(v[1], v[2], v[3], t[1], t[2], t[3], c[1], c[2], c[3], primitive);
            }
        }

        private void RasterizeTri(Point2D v0, Point2D v1, Point2D v2, TextureData t0, TextureData t1, TextureData t2, uint c0, uint c1, uint c2, Primitive primitive) {
            int area = Orient2d(v0, v1, v2);

            if (area == 0) {
                return;
            }

            if (area < 0) {
                (v1, v2) = (v2, v1);
                (t1, t2) = (t2, t1);
                (c1, c2) = (c2, c1);
                area = -area;
            }

            // Bounding box
            int minX = Math.Min(v0.X, Math.Min(v1.X, v2.X));
            int minY = Math.Min(v0.Y, Math.Min(v1.Y, v2.Y));
            int maxX = Math.Max(v0.X, Math.Max(v1.X, v2.X));
            int maxY = Math.Max(v0.Y, Math.Max(v1.Y, v2.Y));

            if (((maxX - minX) > 1023) || ((maxY - minY) > 511)) {
                return;
            }

            // Clip
            _min.X = (short)Math.Max(minX, _drawingAreaLeft);
            _min.Y = (short)Math.Max(minY, _drawingAreaTop);
            _max.X = (short)Math.Min(maxX, _drawingAreaRight);
            _max.Y = (short)Math.Min(maxY, _drawingAreaBottom);

            int A01 = v0.Y - v1.Y, B01 = v1.X - v0.X;
            int A12 = v1.Y - v2.Y, B12 = v2.X - v1.X;
            int A20 = v2.Y - v0.Y, B20 = v0.X - v2.X;

            int bias0 = IsTopLeft(v1, v2) ? 0 : -1;
            int bias1 = IsTopLeft(v2, v0) ? 0 : -1;
            int bias2 = IsTopLeft(v0, v1) ? 0 : -1;

            int w0Row = Orient2d(v1, v2, _min) + bias0;
            int w1Row = Orient2d(v2, v0, _min) + bias1;
            int w2Row = Orient2d(v0, v1, _min) + bias2;

            uint baseColor = GetRgbColor(c0);

            // Rasterize
            for (int y = _min.Y; y < _max.Y; y++) {
                // Barycentric coordinates at start of row
                int w0 = w0Row;
                int w1 = w1Row;
                int w2 = w2Row;

                for (int x = _min.X; x < _max.X; x++) {
                    // If p is on or inside all edges, render pixel
                    if ((w0 | w1 | w2) >= 0) {
                        // Adjustements per triangle instead of per pixel can be
                        // done at area level but it still does some little by 1
                        // error apreciable on some textured quads I assume it
                        // could be handled recalculating AXX and BXX offsets
                        // but those maths are beyond my scope

                        // Check background mask
                        if (_checkMaskBeforeDraw) {
                            _color0.Value = (uint)Vram.GetPixelRgb888(x, y); // Back
                            if (_color0.Msb != 0) {
                                w0 += A12;
                                w1 += A20;
                                w2 += A01;
                                continue;
                            }
                        }

                        // Reset default color of the triangle calculated outside the for as it gets overwriten as follows...
                        uint color = baseColor;

                        if (primitive.IsShaded) {
                            _color0.Value = c0;
                            _color1.Value = c1;
                            _color2.Value = c2;

                            int r = Lerp(w0 - bias0, w1 - bias1, w2 - bias2, _color0.R, _color1.R, _color2.R, area);
                            int g = Lerp(w0 - bias0, w1 - bias1, w2 - bias2, _color0.G, _color1.G, _color2.G, area);
                            int b = Lerp(w0 - bias0, w1 - bias1, w2 - bias2, _color0.B, _color1.B, _color2.B, area);
                            color = (uint)(r << 16 | g << 8 | b);
                        }

                        if (primitive.IsTextured) {
                            int texelX = Lerp(w0 - bias0, w1 - bias1, w2 - bias2, t0.X, t1.X, t2.X, area);
                            int texelY = Lerp(w0 - bias0, w1 - bias1, w2 - bias2, t0.Y, t1.Y, t2.Y, area);
                            uint texel = GetTexel(MaskTexelAxis(texelX, _preMaskX, _postMaskX), MaskTexelAxis(texelY, _preMaskY, _postMaskY), primitive.Clut, primitive.TextureBase, primitive.Depth);
                            if (texel == 0) {
                                w0 += A12;
                                w1 += A20;
                                w2 += A01;
                                continue;
                            }

                            if (!primitive.IsRawTextured) {
                                _color0.Value = (uint)color;
                                _color1.Value = (uint)texel;
                                _color1.R = ClampTo255(_color0.R * _color1.R >> 7);
                                _color1.G = ClampTo255(_color0.G * _color1.G >> 7);
                                _color1.B = ClampTo255(_color0.B * _color1.B >> 7);

                                texel = _color1.Value;
                            }

                            color = texel;
                        }

                        if (primitive.IsSemiTransparent && (!primitive.IsTextured || ((color & 0xFF00_0000) != 0))) {
                            color = HandleSemiTransp(x, y, color, primitive.SemiTransparencyMode);
                        }

                        if (_isDithered) {
                            color = HandleDithering(x, y, color);
                        }

                        color |= _maskWhileDrawing << 24;

                        Vram.SetPixel(x, y, color);
                    }
                    // One step to the right
                    w0 += A12;
                    w1 += A20;
                    w2 += A01;
                }
                // One row step
                w0Row += B12;
                w1Row += B20;
                w2Row += B01;
            }
        }

        private void GP0_RenderLine(ReadOnlySpan<uint> buffer) {
            // Console.WriteLine("size " + commandBuffer.Count);
            // int arguments = 0;
            uint command = buffer[_commandPointer++];
            // arguments++;

            uint color1 = command & 0xFFFFFF;
            uint color2 = color1;

            bool isPoly = (command & (1 << 27)) != 0;
            bool isShaded = (command & (1 << 28)) != 0;
            bool isTransparent = (command & (1 << 25)) != 0;

            //if (isTextureMapped /*isRaw*/) return;

            uint v1 = buffer[_commandPointer++];
            //arguments++;

            if (isShaded) {
                color2 = buffer[_commandPointer++];
                //arguments++;
            }
            uint v2 = buffer[_commandPointer++];
            //arguments++;

            RasterizeLine(v1, v2, color1, color2, isTransparent);

            if (!isPoly) return;
            //renderline = 0;
            while (/* arguments < 0xF && */ (buffer[_commandPointer] & 0xF000_F000) != 0x5000_5000) {
                // Console.WriteLine("DOING ANOTHER LINE " + ++renderline);
                // arguments++;
                color1 = color2;
                if (isShaded) {
                    color2 = buffer[_commandPointer++];
                    //arguments++;
                }
                v1 = v2;
                v2 = buffer[_commandPointer++];
                RasterizeLine(v1, v2, color1, color2, isTransparent);
                // Console.WriteLine("RASTERIZE " + ++rasterizeline);
                // window.update(Vram.Bits);
                // Console.ReadLine();
            }

            /*if (arguments != 0xF) */
            _commandPointer++; // discard 5555_5555 termination (need to rewrite all this from the GP0...)
        }

        private void RasterizeLine(uint v1, uint v2, uint color1, uint color2, bool isTransparent) {
            short x = Signed11bit(v1 & 0xFFFF);
            short y = Signed11bit(v1 >> 16);

            short x2 = Signed11bit(v2 & 0xFFFF);
            short y2 = Signed11bit(v2 >> 16);

            if ((Math.Abs(x - x2) > 0x3FF) || (Math.Abs(y - y2) > 0x1FF)) {
                return;
            }

            x += _drawingXOffset;
            y += _drawingYOffset;

            x2 += _drawingXOffset;
            y2 += _drawingYOffset;

            int w = x2 - x;
            int h = y2 - y;
            int dx1 = 0, dy1 = 0, dx2 = 0, dy2 = 0;

            if (w < 0) dx1 = -1; else if (w > 0) dx1 = 1;
            if (h < 0) dy1 = -1; else if (h > 0) dy1 = 1;
            if (w < 0) dx2 = -1; else if (w > 0) dx2 = 1;

            int longest = Math.Abs(w);
            int shortest = Math.Abs(h);

            if (!(longest > shortest)) {
                longest = Math.Abs(h);
                shortest = Math.Abs(w);
                if (h < 0) dy2 = -1; else if (h > 0) dy2 = 1;
                dx2 = 0;
            }

            int numerator = longest >> 1;

            for (int i = 0; i <= longest; i++) {
                float ratio = (float)i / longest;
                uint color = Lerp(color1, color2, ratio);

                // x = (short)Math.Min(Math.Max(x, drawingAreaLeft), drawingAreaRight); //this generates glitches on RR4
                // y = (short)Math.Min(Math.Max(y, drawingAreaTop), drawingAreaBottom);

                if (x >= _drawingAreaLeft && x < _drawingAreaRight && y >= _drawingAreaTop && y < _drawingAreaBottom) {
                    // if (primitive.isSemiTransparent && (!primitive.isTextured || (color & 0xFF00_0000) != 0)) {
                    if (isTransparent) {
                        color = HandleSemiTransp(x, y, color, _transparencyMode);
                    }

                    if (_isDithered) {
                        color = HandleDithering(x, y, color);
                    }

                    color |= _maskWhileDrawing << 24;

                    Vram.SetPixel(x, y, color);
                }

                numerator += shortest;
                if (!(numerator < longest)) {
                    numerator -= longest;
                    x += (short)dx1;
                    y += (short)dy1;
                } else {
                    x += (short)dx2;
                    y += (short)dy2;
                }
            }
        }

        private void GP0_RenderRectangle(ReadOnlySpan<uint> buffer) {
            // 1st Color+Command(CcBbGgRrh)
            // 2nd Vertex(YyyyXxxxh)
            // 3rd Texcoord+Palette(ClutYyXxh)(for 4bpp Textures Xxh must be even!) //Only textured
            // 4rd (3rd non textured) Width + Height(YsizXsizh)(variable opcode only)(max 1023x511)
            uint command = buffer[_commandPointer++];
            uint color = command & 0xFFFFFF;
            uint opcode = command >> 24;

            bool isTextured = (command & (1 << 26)) != 0;
            bool isSemiTransparent = (command & (1 << 25)) != 0;
            bool isRawTextured = (command & (1 << 24)) != 0;

            Primitive primitive = new Primitive();
            primitive.IsTextured = isTextured;
            primitive.IsSemiTransparent = isSemiTransparent;
            primitive.IsRawTextured = isRawTextured;

            uint vertex = buffer[_commandPointer++];
            short xo = (short)(vertex & 0xFFFF);
            short yo = (short)(vertex >> 16);

            if (isTextured) {
                uint texture = buffer[_commandPointer++];
                _textureData.X = (byte)(texture & 0xFF);
                _textureData.Y = (byte)((texture >> 8) & 0xFF);

                ushort palette = (ushort)((texture >> 16) & 0xFFFF);
                primitive.Clut = new Point2D((short)((palette & 0x3F) << 4),
                                             (short)((palette >> 6) & 0x1FF));
            }

            primitive.Depth = _textureDepth;
            primitive.TextureBase = new Point2D((short)(_textureXBase << 6),
                                                (short)(_textureYBase << 8));
            primitive.SemiTransparencyMode = _transparencyMode;

            short width = 0;
            short heigth = 0;

            switch ((opcode & 0x18) >> 3) {
                case 0x0:
                    uint hw = buffer[_commandPointer++];
                    width = (short)(hw & 0xFFFF);
                    heigth = (short)(hw >> 16);
                    break;
                case 0x1:
                    width = 1;
                    heigth = 1;
                    break;
                case 0x2:
                    width = 8;
                    heigth = 8;
                    break;
                case 0x3:
                    width = 16;
                    heigth = 16;
                    break;
            }

            short y = Signed11bit((uint)(yo + _drawingYOffset));
            short x = Signed11bit((uint)(xo + _drawingXOffset));

            Point2D origin;
            origin.X = x;
            origin.Y = y;

            Point2D size;
            size.X = (short)(x + width);
            size.Y = (short)(y + heigth);

            RasterizeRect(origin, size, _textureData, color, primitive);
        }

        private void RasterizeRect(Point2D origin, Point2D size, TextureData texture, uint bgrColor, Primitive primitive) {
            int xOrigin = Math.Max(origin.X, _drawingAreaLeft);
            int yOrigin = Math.Max(origin.Y, _drawingAreaTop);
            int width = Math.Min(size.X, _drawingAreaRight);
            int height = Math.Min(size.Y, _drawingAreaBottom);

            int uOrigin = texture.X + (xOrigin - origin.X);
            int vOrigin = texture.Y + (yOrigin - origin.Y);

            uint baseColor = GetRgbColor(bgrColor);

            for (int y = yOrigin, v = vOrigin; y < height; y++, v++) {
                for (int x = xOrigin, u = uOrigin; x < width; x++, u++) {
                    //Check background mask
                    if (_checkMaskBeforeDraw) {
                        _color0.Value = (uint)Vram.GetPixelRgb888(x & 0x3FF, y & 0x1FF); //back
                        if (_color0.Msb != 0) {
                            continue;
                        }
                    }

                    uint color = baseColor;

                    if (primitive.IsTextured) {
                        //int texel = getTexel(u, v, clut, textureBase, depth);
                        uint texel = GetTexel(MaskTexelAxis(u, _preMaskX, _postMaskX),MaskTexelAxis(v, _preMaskY, _postMaskY),primitive.Clut, primitive.TextureBase, primitive.Depth);
                        if (texel == 0) {
                            continue;
                        }

                        if (!primitive.IsRawTextured) {
                            _color0.Value = (uint)color;
                            _color1.Value = (uint)texel;
                            _color1.R = ClampTo255(_color0.R * _color1.R >> 7);
                            _color1.G = ClampTo255(_color0.G * _color1.G >> 7);
                            _color1.B = ClampTo255(_color0.B * _color1.B >> 7);

                            texel = _color1.Value;
                        }

                        color = texel;
                    }

                    if (primitive.IsSemiTransparent && (!primitive.IsTextured || (color & 0xFF00_0000) != 0)) {
                        color = HandleSemiTransp(x, y, color, primitive.SemiTransparencyMode);
                    }

                    if (_isDithered) {
                        color = HandleDithering(x, y, color);
                    }

                    color |= _maskWhileDrawing << 24;

                    Vram.SetPixel(x, y, color);
                }
            }
        }

        private void GP0_MemCopyRectVramtoVram(ReadOnlySpan<uint> buffer) {
            _commandPointer++; //Command/Color parameter unused
            uint sourceXY = buffer[_commandPointer++];
            uint destinationXY = buffer[_commandPointer++];
            uint wh = buffer[_commandPointer++];

            ushort sx = (ushort)(sourceXY & 0x3FF);
            ushort sy = (ushort)((sourceXY >> 16) & 0x1FF);

            ushort dx = (ushort)(destinationXY & 0x3FF);
            ushort dy = (ushort)((destinationXY >> 16) & 0x1FF);

            ushort w = (ushort)((((wh & 0xFFFF) - 1) & 0x3FF) + 1);
            ushort h = (ushort)((((wh >> 16) - 1) & 0x1FF) + 1);

            for (int yPos = 0; yPos < h; yPos++) {
                for (int xPos = 0; xPos < w; xPos++) {
                    uint color = Vram.GetPixelRgb888((sx + xPos) & 0x3FF, (sy + yPos) & 0x1FF);

                    if (_checkMaskBeforeDraw) {
                        _color0.Value = (uint)Vram.GetPixelRgb888((dx + xPos) & 0x3FF, (dy + yPos) & 0x1FF);
                        if (_color0.Msb != 0) continue;
                    }

                    color |= _maskWhileDrawing << 24;

                    Vram.SetPixel((dx + xPos) & 0x3FF, (dy + yPos) & 0x1FF, color);
                }
            }
        }

        private void GP0_MemCopyRectCputoVram(ReadOnlySpan<uint> buffer) { //todo rewrite VRAM coord struct mess
            _commandPointer++; //Command/Color parameter unused
            uint yx = buffer[_commandPointer++];
            uint wh = buffer[_commandPointer++];

            ushort x = (ushort)(yx & 0x3FF);
            ushort y = (ushort)((yx >> 16) & 0x1FF);

            ushort w = (ushort)((((wh & 0xFFFF) - 1) & 0x3FF) + 1);
            ushort h = (ushort)((((wh >> 16) - 1) & 0x1FF) + 1);

            _vramTransfer.X = x;
            _vramTransfer.Y = y;
            _vramTransfer.W = w;
            _vramTransfer.H = h;
            _vramTransfer.originX = x;
            _vramTransfer.originY = y;
            _vramTransfer.halfWords = w * h;

            _mode = Mode.Vram;
        }

        private void GP0_MemCopyRectVramtoCpu(ReadOnlySpan<uint> buffer) {
            _commandPointer++; //Command/Color parameter unused
            uint yx = buffer[_commandPointer++];
            uint wh = buffer[_commandPointer++];

            ushort x = (ushort)(yx & 0x3FF);
            ushort y = (ushort)((yx >> 16) & 0x1FF);

            ushort w = (ushort)((((wh & 0xFFFF) - 1) & 0x3FF) + 1);
            ushort h = (ushort)((((wh >> 16) - 1) & 0x1FF) + 1);

            _vramTransfer.X = x;
            _vramTransfer.Y = y;
            _vramTransfer.W = w;
            _vramTransfer.H = h;
            _vramTransfer.originX = x;
            _vramTransfer.originY = y;
            _vramTransfer.halfWords = w * h;

            _isReadyToSendVramToCpu = true;
            _isReadyToReceiveDmaBlock = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int MaskTexelAxis(int axis, int preMaskAxis, int postMaskAxis) {
            return axis & 0xFF & preMaskAxis | postMaskAxis;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint GetTexel(int x, int y, Point2D clut, Point2D textureBase, int depth) {
            if (depth == 0) {
                return Get4bppTexel(x, y, clut, textureBase);
            } else if (depth == 1) {
                return Get8bppTexel(x, y, clut, textureBase);
            } else {
                return Get16bppTexel(x, y, textureBase);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint Get4bppTexel(int x, int y, Point2D clut, Point2D textureBase) {
            ushort index = Vram1555.GetPixel(x / 4 + textureBase.X, y + textureBase.Y);
            int p = (index >> (x & 3) * 4) & 0xF;
            return Vram.GetPixelRgb888(clut.X + p, clut.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint Get8bppTexel(int x, int y, Point2D clut, Point2D textureBase) {
            ushort index = Vram1555.GetPixel(x / 2 + textureBase.X, y + textureBase.Y);
            int p = (index >> ((x & 1) * 8)) & 0xFF;
            return Vram.GetPixelRgb888(clut.X + p, clut.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint Get16bppTexel(int x, int y, Point2D textureBase) {
            return Vram.GetPixelRgb888(x + textureBase.X, y + textureBase.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Orient2d(Point2D a, Point2D b, Point2D c) {
            return (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
        }

        private void GP0_E1_SetDrawMode(uint val) {
            _textureXBase = (byte)(val & 0xF);
            _textureYBase = (byte)((val >> 4) & 0x1);
            _transparencyMode = (byte)((val >> 5) & 0x3);
            _textureDepth = (byte)((val >> 7) & 0x3);
            _isDithered = ((val >> 9) & 0x1) != 0;
            _isDrawingToDisplayAllowed = ((val >> 10) & 0x1) != 0;
            _isTextureDisabled = _isTextureDisabledAllowed && ((val >> 11) & 0x1) != 0;
            _isTexturedRectangleXFlipped = ((val >> 12) & 0x1) != 0;
            _isTexturedRectangleYFlipped = ((val >> 13) & 0x1) != 0;

            // Console.WriteLine("[GPU] [GP0] DrawMode");
        }

        private void GP0_E2_SetTextureWindow(uint val) {
            uint bits = val & 0xFF_FFFF;

            if (bits == _textureWindowBits) return;

            _textureWindowBits = bits;

            byte textureWindowMaskX = (byte)(val & 0x1F);
            byte textureWindowMaskY = (byte)((val >> 5) & 0x1F);
            byte textureWindowOffsetX = (byte)((val >> 10) & 0x1F);
            byte textureWindowOffsetY = (byte)((val >> 15) & 0x1F);

            _preMaskX = ~(textureWindowMaskX * 8);
            _preMaskY = ~(textureWindowMaskY * 8);
            _postMaskX = (textureWindowOffsetX & textureWindowMaskX) * 8;
            _postMaskY = (textureWindowOffsetY & textureWindowMaskY) * 8;
        }

        private void GP0_E3_SetDrawingAreaTopLeft(uint val) {
            _drawingAreaTop = (ushort)((val >> 10) & 0x1FF);
            _drawingAreaLeft = (ushort)(val & 0x3FF);
        }

        private void GP0_E4_SetDrawingAreaBottomRight(uint val) {
            _drawingAreaBottom = (ushort)((val >> 10) & 0x1FF);
            _drawingAreaRight = (ushort)(val & 0x3FF);
        }

        private void GP0_E5_SetDrawingOffset(uint val) {
            _drawingXOffset = Signed11bit(val & 0x7FF);
            _drawingYOffset = Signed11bit((val >> 11) & 0x7FF);
        }

        private void GP0_E6_SetMaskBit(uint val) {
            _maskWhileDrawing = val & 0x1;
            _checkMaskBeforeDraw = (val & 0x2) != 0;
        }

        public void WriteGP1(uint value) {
            // Console.WriteLine($"[GPU] GP1 Write Value: {value:X8}");
            uint opcode = value >> 24;
            switch (opcode) {
                case 0x00: GP1_00_ResetGpu(); break;
                case 0x01: GP1_01_ResetCommandBuffer(); break;
                case 0x02: GP1_02_AckGpuInterrupt(); break;
                case 0x03: GP1_03_DisplayEnable(value); break;
                case 0x04: GP1_04_DmaDirection(value); break;
                case 0x05: GP1_05_DisplayVramStart(value); break;
                case 0x06: GP1_06_DisplayHorizontalRange(value); break;
                case 0x07: GP1_07_DisplayVerticalRange(value); break;
                case 0x08: GP1_08_DisplayMode(value); break;
                case 0x09: GP1_09_TextureDisable(value); break;
                case uint _ when opcode >= 0x10 && opcode <= 0x1F:
                    GP1_GpuInfo(value); break;
                default:
                    Console.WriteLine("[GPU] Unsupported GP1 Command " + opcode.ToString("x8"));
                    Console.ReadLine();
                    break;
            }
        }

        private void GP1_00_ResetGpu() {
            GP1_01_ResetCommandBuffer();
            GP1_02_AckGpuInterrupt();
            GP1_03_DisplayEnable(1);
            GP1_04_DmaDirection(0);
            GP1_05_DisplayVramStart(0);
            GP1_06_DisplayHorizontalRange(0x00C0_0200);
            GP1_07_DisplayVerticalRange(0x0010_0010);
            GP1_08_DisplayMode(0);

            GP0_E1_SetDrawMode(0);
            GP0_E2_SetTextureWindow(0);
            GP0_E3_SetDrawingAreaTopLeft(0);
            GP0_E4_SetDrawingAreaBottomRight(0);
            GP0_E5_SetDrawingOffset(0);
            GP0_E6_SetMaskBit(0);
        }

        private void GP1_01_ResetCommandBuffer() => _commandPointer = 0;

        private void GP1_02_AckGpuInterrupt() => _isInterruptRequested = false;

        private void GP1_03_DisplayEnable(uint value) => _isDisplayDisabled = (value & 1) != 0;

        private void GP1_04_DmaDirection(uint value) {
            _dmaDirection = (byte)(value & 0x3);

            _isDmaRequest = _dmaDirection switch {
                0 => false,
                1 => _isReadyToReceiveDmaBlock,
                2 => _isReadyToReceiveDmaBlock,
                3 => _isReadyToSendVramToCpu,
                _ => false,
            };
        }

        private void GP1_05_DisplayVramStart(uint value) {
            _displayVramXStart = (ushort)(value & 0x3FE);
            _displayVramYStart = (ushort)((value >> 10) & 0x1FE);
        }

        private void GP1_06_DisplayHorizontalRange(uint value) {
            _displayX1 = (ushort)(value & 0xFFF);
            _displayX2 = (ushort)((value >> 12) & 0xFFF);
        }

        private void GP1_07_DisplayVerticalRange(uint value) {
            _displayY1 = (ushort)(value & 0x3FF);
            _displayY2 = (ushort)((value >> 10) & 0x3FF);
        }

        private void GP1_08_DisplayMode(uint value) {
            _horizontalResolution1 = (byte)(value & 0x3);
            _isVerticalResolution480 = (value & 0x4) != 0;
            _isPal = (value & 0x8) != 0;
            _is24BitDepth = (value & 0x10) != 0;
            _isVerticalInterlace = (value & 0x20) != 0;
            _horizontalResolution2 = (byte)((value & 0x40) >> 6);
            _isReverseFlag = (value & 0x80) != 0;

            _isInterlaceField = _isVerticalInterlace;

            _horizontalTiming = _isPal ? 3406 : 3413;
            _verticalTiming = _isPal ? 314 : 263;

            _displayHorizontalRes = _Resolutions[_horizontalResolution2 << 2 | _horizontalResolution1];
            _displayVerticalRes = _isVerticalResolution480 ? 480 : 240;
        }

        private void GP1_09_TextureDisable(uint value) => _isTextureDisabledAllowed = (value & 0x1) != 0;

        private void GP1_GpuInfo(uint value) {
            uint info = value & 0xF;
            switch (info) {
                case 0x2: _gpuRead = _textureWindowBits; break;
                case 0x3: _gpuRead = (uint)(_drawingAreaTop << 10 | _drawingAreaLeft); break;
                case 0x4: _gpuRead = (uint)(_drawingAreaBottom << 10 | _drawingAreaRight); break;
                case 0x5: _gpuRead = (uint)(_drawingYOffset << 11 | (ushort)_drawingXOffset); break;
                case 0x7: _gpuRead = 2; break;
                case 0x8: _gpuRead = 0; break;
                default:
                    Console.WriteLine("[GPU] GP1 Unhandled GetInfo: " + info.ToString("x8"));
                    break;
            }
        }

        private uint GetTexpageFromGpu() {
            uint texpage = 0;

            texpage |= (_isTexturedRectangleYFlipped ? 1u : 0) << 13;
            texpage |= (_isTexturedRectangleXFlipped ? 1u : 0) << 12;
            texpage |= (_isTextureDisabled ? 1u : 0) << 11;
            texpage |= (_isDrawingToDisplayAllowed ? 1u : 0) << 10;
            texpage |= (_isDithered ? 1u : 0) << 9;
            texpage |= (uint)(_textureDepth << 7);
            texpage |= (uint)(_transparencyMode << 5);
            texpage |= (uint)(_textureYBase << 4);
            texpage |= _textureXBase;

            return texpage;
        }

        public static void InitColorTable() {
            for (uint m = 0; m < 2; m++) {
                for (uint r = 0; r < 32; r++) {
                    for (uint g = 0; g < 32; g++) {
                        for (uint b = 0; b < 32; b++) {
                            _Color1555to8888Lut[(m << 15) | (b << 10) | (g << 5) | r] = (m << 24) | (r << (16 + 3)) | (g << (8 + 3)) | (b << 3);
                        }
                    }
                }
            }
        }

        private static void InitDitheringTable() {
            for (int y = 0; y < 4; y++) {
                for (int x = 0; x < 4; x++) {
                    for (int v = 0; v < 512; v++) {
                        _DitherTableWithSaturation[y, x, v] = (byte)System.Math.Clamp((v + _DitherTable[y, x]), 0, 255);
                    }
                }
            }
        }

        private static uint HandleDithering(int x, int y, uint color) {
            int px = x & 3;
            int py = y & 3;

            Color color0 = default(Color);
            color0.Value = (uint)color;

            color0.R = _DitherTableWithSaturation[py, px, color0.R];
            color0.G = _DitherTableWithSaturation[py, px, color0.G];
            color0.B = _DitherTableWithSaturation[py, px, color0.B];

            return color0.Value;
        }

        private uint HandleSemiTransp(int x, int y, uint color, int semiTranspMode) {
            _color0.Value = (uint)Vram.GetPixelRgb888(x, y); // Back
            _color1.Value = (uint)color; // Front
            switch (semiTranspMode) {
                case 0: //0.5 x B + 0.5 x F   ; AKA B/2+F/2
                    _color1.R = (byte)((_color0.R + _color1.R) >> 1);
                    _color1.G = (byte)((_color0.G + _color1.G) >> 1);
                    _color1.B = (byte)((_color0.B + _color1.B) >> 1);
                    break;
                case 1://1.0 x B + 1.0 x F    ; AKA B+F
                    _color1.R = ClampTo255(_color0.R + _color1.R);
                    _color1.G = ClampTo255(_color0.G + _color1.G);
                    _color1.B = ClampTo255(_color0.B + _color1.B);
                    break;
                case 2: //1.0 x B - 1.0 x F   ; AKA B-F
                    _color1.R = ClampToZero(_color0.R - _color1.R);
                    _color1.G = ClampToZero(_color0.G - _color1.G);
                    _color1.B = ClampToZero(_color0.B - _color1.B);
                    break;
                case 3: //1.0 x B +0.25 x F   ; AKA B+F/4
                    _color1.R = ClampTo255(_color0.R + (_color1.R >> 2));
                    _color1.G = ClampTo255(_color0.G + (_color1.G >> 2));
                    _color1.B = ClampTo255(_color0.B + (_color1.B >> 2));
                    break;
            } // Actually doing RGB calcs on BGR struct...
            return _color1.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte ClampToZero(int v) {
            if (v < 0) {
                return 0;
            }

            return (byte)v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte ClampTo255(int v) {
            if (v > 0xFF) {
                return 0xFF;
            }

            return (byte)v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint GetRgbColor(uint value) {
            _color0.Value = value;
            return (uint)(((uint)_color0.Msb << 24) | ((uint)_color0.R << 16) | ((uint)_color0.G << 8) | (uint)_color0.B);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsTopLeft(Point2D a, Point2D b) => (a.Y == b.Y) && (b.X > a.X) || (b.Y < a.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint Lerp(uint c1, uint c2, float ratio) {
            _color1.Value = c1;
            _color2.Value = c2;

            byte r = (byte)(_color2.R * ratio + _color1.R * (1 - ratio));
            byte g = (byte)(_color2.G * ratio + _color1.G * (1 - ratio));
            byte b = (byte)(_color2.B * ratio + _color1.B * (1 - ratio));

            return (uint)((uint)r << 16 | (uint)g << 8 | b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Lerp(int w0, int w1, int w2, int t0, int t1, int t2, int area) {
            // https://codeplea.com/triangular-interpolation
            return (t0 * w0 + t1 * w1 + t2 * w2) / area;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static short Signed11bit(uint n) {
            return (short)(((int)n << 21) >> 21);
        }

        // This is only needed for the Direct GP0 commands as the command number
        // needs to be known ahead of the first command on queue.
        private static ReadOnlySpan<byte> _CommandSizeTable => new byte[] {
            // 0   1   2   3   4   5   6   7   8   9   A   B   C   D   E   F
               1,  1,  3,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1, // 0
               1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1, // 1
               4,  4,  4,  4,  7,  7,  7,  7,  5,  5,  5,  5,  9,  9,  9,  9, // 2
               6,  6,  6,  6,  9,  9,  9,  9,  8,  8,  8,  8, 12, 12, 12, 12, // 3
               3,  3,  3,  3,  3,  3,  3,  3, 16, 16, 16, 16, 16, 16, 16, 16, // 4
               4,  4,  4,  4,  4,  4,  4,  4, 16, 16, 16, 16, 16, 16, 16, 16, // 5
               3,  3,  3,  1,  4,  4,  4,  4,  2,  1,  2,  1,  3,  3,  3,  3, // 6
               2,  1,  2,  1,  3,  3,  3,  3,  2,  1,  2,  2,  3,  3,  3,  3, // 7
               4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4, // 8
               4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4, // 9
               3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3, // A
               3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3, // B
               3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3, // C
               3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3, // D
               1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1, // E
               1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1  // F
        };
    }
}
