using System;
using System.Numerics;

namespace PsyCross.Testing {
    public class TestingPSX : PSX {
        public TestingPSX() {
        }

        private const float _screenWidth = 320.0f;
        private const float _screenHeight = 240.0f;

        private const float _Deg2Rad = MathF.PI / 180.0f;
        private const float _Rad2Deg = 180.0f / MathF.PI;

        private const float _fov = 90.0f;
        private const float _fovAngle = _fov * _Deg2Rad * 0.5f;
        private const float _ratio = _screenWidth / _screenHeight;
        private static readonly float _ViewDistance = 0.5f * ((_screenWidth - 1.0f) * MathF.Tan(_fovAngle));

        private static readonly Vector3[] _Tri1 = new Vector3[] {
            new Vector3(-.32f, -.32f, 1f),
            new Vector3(-.32f,  .32f, 1f),
            new Vector3( .32f, -.32f, 1f)
        };

        private static readonly Vector3[] _Tri2 = new Vector3[] {
            new Vector3(-.32f,  .32f, 1f),
            new Vector3( .32f, -.32f, 1f),
            new Vector3( .32f,  .32f, 1f)
        };

        private static readonly Vector2[] _Uv1 = new Vector2[] {
            new Vector2(0, 8),
            new Vector2(0, 0),
            new Vector2(8, 8),
        };

        private static readonly Vector2[] _Uv2 = new Vector2[] {
            new Vector2(0, 0),
            new Vector2(8, 8),
            new Vector2(8, 0),
        };

        Vector3[] _rot = new Vector3[2];
        Vector3[] _pos = new Vector3[2];

        PrimitiveSort _primitiveSort = new PrimitiveSort();
        CommandBuffer _commandBuffer = new CommandBuffer(1024);

        public override void UpdateFrame() {
            SetupFakeEnv();
            SetDispMask(1);

            _commandBuffer.Reset();

            FillRectVram(0x555555, 0, 0, 319, 239);
            // FillRectVram(0xFFFFFF, 0, 0, 64, 64);
            // CopyVramToVram(0, 0, 100, 100, 64, 64);

            // uint[] data = new uint[] {
            //     0xFFFF_FFFF, 0xFFFF_FFFF,
            //     0xFFFF_FFFF, 0xFFFF_FFFF,
            //     0xFFFF_FFFF, 0xFFFF_FFFF,
            //     0xFFFF_FFFF, 0x801F_83C1,
            // };

            uint[] data1 = new uint[] {
                0x5555_5555, 0x5555_5555, 0x5555_5555, 0x5555_5555,
                0x5555_4444, 0x3333_2222, 0x1111_2222, 0x3333_5555,
                0x5555_4444, 0x3333_2222, 0x1111_2222, 0x3333_5555,
                0x5555_4444, 0x3333_2222, 0x1111_2222, 0x3333_5555,
                0x5555_4444, 0x3333_2222, 0x1111_2222, 0x3333_5555,
                0x5555_4444, 0x3333_2222, 0x1111_2222, 0x3333_5555,
                0x5555_4444, 0x3333_2222, 0x1111_2222, 0x3333_5555,
                0x5555_5555, 0x5555_5555, 0x5555_5555, 0x5555_5555,
            };

            uint[] data2 = new uint[] {
                0x0001_0203, 0x0405_0607,
                0x0809_0A0B, 0x0C0D_0E0F,
                0x1011_1213, 0x1415_1617,
                0x1819_1A1B, 0x1C1D_1E1F,
                0x2021_2223, 0x2425_2627,
                0x2829_2A2B, 0x2C2D_2E2F,
                0x3031_3233, 0x3435_3637,
                0x3839_3A3B, 0x3C3D_3E3F,
            };

            uint[] data3 = new uint[] {
                0x0134_5678,
                0x0134_5678,
                0x0134_5678,
                0x0134_5678,

                0x0134_5678,
                0x0134_5678,
                0x0134_5678,
                0x0134_5678,
            };

            ushort tPageId = LoadTPage(320, 0, 8, 8, BitDepth.Bpp15, data1);

            _primitiveSort.Clear();

            Vector3[] clipPoints = new Vector3[3];

            Matrix4x4[] objectMat = new Matrix4x4[2];
            float a;

            _pos[0].Z = 1;
            // a = ((_rot[0].Y * _Rad2Deg) + 0.5f) % 360.0f; _rot[0].Y = a * _Deg2Rad;
            objectMat[0] = CreateMatrix(_pos[0], _rot[0]);
            TransformToClip(clipPoints, objectMat[0], _Tri1);
            _primitiveSort.Add(clipPoints, PrimitiveSortPoint.Center, _Uv1);

            _pos[0].Z = 1;
            // a = ((_rot[1].Y * _Rad2Deg) + 0.5f) % 360.0f; _rot[1].Y = a * _Deg2Rad;
            // objectMat[1] = CreateMatrix(_pos[1], _rot[1]);
            TransformToClip(clipPoints, objectMat[0], _Tri2);
            _primitiveSort.Add(clipPoints, PrimitiveSortPoint.Center, _Uv2);

            _primitiveSort.Sort();

            Vector3[] ndcPoints = new Vector3[3];

            for (int i = _primitiveSort.Primitives.Count - 1; i >= 0; i--) {
                var primitive = _primitiveSort.Primitives[i];

                TransformToNdc(ndcPoints, primitive.Points);

                (int x1, int y1) = TransformToScreen(ndcPoints[0]);
                (int x2, int y2) = TransformToScreen(ndcPoints[1]);
                (int x3, int y3) = TransformToScreen(ndcPoints[2]);

                Console.WriteLine(_ViewDistance);
                Console.WriteLine($"  Clip: {primitive.Points[0]} - {primitive.Points[1]} - {primitive.Points[2]}");
                Console.WriteLine($"   NDC: {ndcPoints[0]} - {ndcPoints[1]} - {ndcPoints[2]}");
                Console.WriteLine($"Screen:({x1},{y1}) ({x2},{y2}) ({x3},{y3})");
                Console.WriteLine();

                var tex = (Vector2[])primitive.Attributes;

                uint u1 = (uint)tex[0].X;
                uint v1 = (uint)tex[0].Y;

                uint u2 = (uint)tex[1].X;
                uint v2 = (uint)tex[1].Y;

                uint u3 = (uint)tex[2].X;
                uint v3 = (uint)tex[2].Y;

                TexTriRaw((uint)x1, (uint)y1,
                          (uint)x2, (uint)y2,
                          (uint)x3, (uint)y3,
                          u1, v1,
                          u2, v2,
                          u3, v3,
                          tPageId,
                          0);

                // FillTri((uint)primitive.Attributes, (uint)x1, (uint)y1, (uint)x2, (uint)y2, (uint)x3, (uint)y3);
            }

            Gpu.Process(_commandBuffer.Bits);
        }

        private static Matrix4x4 CreateMatrix(Vector3 pos, Vector3 rot) {
            var rotZ = Matrix4x4.CreateRotationZ(rot.Z);
            var rotY = Matrix4x4.CreateRotationZ(rot.Y);
            var rotX = Matrix4x4.CreateRotationZ(rot.X);
            var translation = Matrix4x4.CreateTranslation(pos);

            return (rotZ * rotY * rotX) * translation;
        }

        private static void TransformToClip(Vector3[] clipPoints, Matrix4x4 matrix, Vector3[] vertices) {
            for (int t = 0; t < 3; t++) {
                clipPoints[t] = Vector3.Transform(vertices[t], matrix) + matrix.Translation;
            }
        }

        private static void TransformToNdc(Vector3[] ndcPoints, Vector3[] clipPoints) {
            for (int t = 0; t < 3; t++) {
                Vector3 clipPoint = clipPoints[t];
                float invZ = _ViewDistance / clipPoint.Z;

                ndcPoints[t] = new Vector3(clipPoint.X * invZ,
                                           clipPoint.Y * invZ,
                                           clipPoint.Z * invZ);
            }
        }

        private static (int x, int y) TransformToScreen(Vector3 ndcPoint) {
            return ((int)( _screenWidth * 0.5f) + (int)ndcPoint.X,
                    (int)(_screenHeight * 0.5f) - (int)(_ratio * ndcPoint.Y));
        }

        // GPU Memory Transfer Commands (GP0): $02 - Fill Rectangle In VRAM
        private void FillRectVram(uint color, uint x, uint y, uint width, uint height) {
            var command = _commandBuffer.AllocateCommand(3);

            command[0] = ((0x02 << 24) | (color & 0xFF_FF_FF));
            command[1] = ((y << 16) + (x & 0xFFFF));
            command[2] = ((height << 16) + (width & 0xFFFF));
        }

        // GPU Memory Transfer Commands (GP0): $80 - Copy Rectangle (VRAM To VRAM)
        private void CopyVramToVram(uint x1, uint y1, uint x2, uint y2, uint width, uint height) {
            var command = _commandBuffer.AllocateCommand(4);

            command[0] = ((uint)0x80 << 24);
            command[1] = ((y1 << 16) + (x1 & 0xFFFF));
            command[2] = ((y2 << 16) + (x2 & 0xFFFF));
            command[3] = ((height << 16) + (width & 0xFFFF));
        }

        public enum BitDepth {
            Bpp4,
            Bpp8,
            Bpp15
        }

        private void LoadImage(uint x, uint y, uint width, uint height, BitDepth bitDepth, uint[] data) {
            uint shortWordWidth = 0;
            uint shortWordHeight = 0;

            switch (bitDepth) {
                case BitDepth.Bpp4:
                    shortWordWidth = ((width >= 0) ? width : (width + 3)) >> 2;
                    shortWordHeight = height;
                    break;
                case BitDepth.Bpp8:
                    shortWordWidth = (width + (width >> 31)) >> 1;
                    break;
                case BitDepth.Bpp15:
                    shortWordWidth = width;
                    shortWordHeight = height;
                    break;
            }

            CopyCpuToVram(x, y, shortWordWidth, shortWordHeight, data);
        }

        private ushort LoadTPage(uint x, uint y, uint width, uint height, BitDepth bitDepth, uint[] data) {
            LoadImage(x, y, width, height, bitDepth, data);

            return GetTPage(bitDepth, x, y);
        }

        // Calculates the TPage attributes
        private static ushort GetTPage(BitDepth bitDepth, uint x, uint y) {
            uint abr = 0; // Semi Transparency (0=B/2+F/2, 1=B+F, 2=B-F, 3=B+F/4)

            return (ushort)(((uint)bitDepth << 7) |
                            ((abr & 0x3) << 5) |
                            ((y & 0x100) >> 4) |
                            ((x & 0x3FF) >> 6) |
                            ((y & 0x200) << 2));
        }

        // GPU Memory Transfer Commands (GP0): $A0 - Copy Rectangle (CPU To VRAM)
        private void CopyCpuToVram(uint x, uint y, uint shortWordWidth, uint shortWordHeight, uint[] data) {
            Console.WriteLine($"{shortWordWidth}+{shortWordHeight}");

            uint dataWordCount =  (uint)data.Length;
            uint dataRoundedWordCount = dataWordCount + (dataWordCount & 1);
            var command = _commandBuffer.AllocateCommand(3 + dataRoundedWordCount);

            Console.WriteLine($"{command.Count}");

            command[0] = ((uint)0xA0 << 24);
            command[1] = ((y << 16) + (x & 0xFFFF));
            command[2] = ((shortWordHeight << 16) + (shortWordWidth & 0xFFFF));

            Console.WriteLine($"-> {data.Length}");
            for (int i = 0; i < data.Length; i++) {
                command[3 + i] = data[i];
            }

            for (int i = 0; i < command.Count; i++) {
                Console.WriteLine($"{i:D02} {command[i]:X08}");
            }
        }

        private void FillTri(uint color, uint x1, uint y1, uint x2, uint y2, uint x3, uint y3) {
            var command = _commandBuffer.AllocateCommand(4);

            command[0] = (0x20 << 24) | (color & 0xFF_FF_FF);
            command[1] = (y1 << 16) + (x1 & 0xFFFF);
            command[2] = (y2 << 16) + (x2 & 0xFFFF);
            command[3] = (y3 << 16) + (x3 & 0xFFFF);
        }

        private void FillTriAlpha(uint color, uint x1, uint y1, uint x2, uint y2, uint x3, uint y3) {
            var command = _commandBuffer.AllocateCommand(4);

            command[0] = ((0x22 << 24) | (color & 0xFF_FF_FF));
            command[1] = ((y1 << 16) + (x1 & 0xFFFF));
            command[2] = ((y2 << 16) + (x2 & 0xFFFF));
            command[3] = ((y3 << 16) + (x3 & 0xFFFF));
        }

        private void TexTriRaw(uint x1, uint y1,
                               uint x2, uint y2,
                               uint x3, uint y3,
                               uint u1, uint v1,
                               uint u2, uint v2,
                               uint u3, uint v3,
                               uint tPageId,
                               uint clutId) {
            var command = _commandBuffer.AllocateCommand(7);

            command[0] = (0x25 << 24);
            command[1] = (y1 << 16) + (x1 & 0xFFFF);
            command[2] = (clutId << 16) + ((v1 & 0xFF) << 8) + (u1 & 0xFF);
            command[3] = (y2 << 16) + (x2 & 0xFFFF);
            command[4] = (tPageId << 16) + ((v2 & 0xFF) << 8) + (u2 & 0xFF);
            command[5] = (y3 << 16) + (x3 & 0xFFFF);
            command[6] = ((v3 & 0xFF) << 8) + (u3 & 0xFF);
        }

        private void SetupFakeEnv() {
            Gpu.WriteGP0(0xE1_000600); // Set Draw Mode (Global TPage Attributes)
            Gpu.WriteGP0(0xE3_000000); // Set Drawing Area Top Left X1=0, Y1=0
            Gpu.WriteGP0(0xE4_03BD3F); // Set Drawing Area Bottom Right X2=319, Y2=239
            Gpu.WriteGP0(0xE5_000000);
        }

        private void ResetGpu() { // What is the name of the PsyQ function?
            Gpu.WriteGP1(0x00000000);
        }

        private void SetDispMask(uint v) {
            Gpu.WriteGP1(0x03000001 - (v & 0x1));
        }
    }
}
