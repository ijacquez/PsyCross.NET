using System;
using System.Numerics;

namespace PsyCross.Testing {
    public class TestingPSX : PSX {
        public TestingPSX() {
            Console.WriteLine("Initialize here");
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
            new Vector3(-.16f, -.16f, 0f),
            new Vector3(-.16f,  .16f, 0f),
            new Vector3( .16f, -.16f, 0f)
        };

        float dir = 1.0f;
        Vector3[] _rot = new Vector3[2];
        Vector3[] _pos = new Vector3[2];

        PrimitiveSort _primitiveSort = new PrimitiveSort();

        private Matrix4x4 CreateMatrix(Vector3 pos, Vector3 rot) {
            return (Matrix4x4.CreateRotationZ(rot.Z) *
                    Matrix4x4.CreateRotationY(rot.Y) *
                    Matrix4x4.CreateRotationX(rot.X)) *
                   Matrix4x4.CreateTranslation(pos);
        }

        public override void UpdateFrame() {
            SetupFakeEnv();
            SetDispMask(1);
            FillRectVram(0x555555, 0, 0, 319, 239);
            FillRectVram(0xFFFFFF, 0, 0, 64, 64);
            CopyRectVram(0, 0, 100, 100, 64, 64);

            _primitiveSort.Clear();

            Vector3[] clipPoints = new Vector3[3];

            Matrix4x4[] objectMat = new Matrix4x4[2];
            float a;

            _pos[0].Z = 1;
            a = ((_rot[0].Y * _Rad2Deg) + 0.5f) % 360.0f; _rot[0].Y = a * _Deg2Rad;
            objectMat[0] = CreateMatrix(_pos[0], _rot[0]);
            TransformToClip(clipPoints, objectMat[0], _Tri1);
            _primitiveSort.Add(clipPoints, PrimitiveSortPoint.Center, (uint)0xFF_00_00);

            _pos[1].Z = 1;
            a = ((_rot[1].Y * _Rad2Deg) + 0.5f) % 360.0f; _rot[1].Y = a * _Deg2Rad;
            objectMat[1] = CreateMatrix(_pos[1], _rot[1]);
            TransformToClip(clipPoints, objectMat[1], _Tri2);
            _primitiveSort.Add(clipPoints, PrimitiveSortPoint.Center, (uint)0xFF_00_FF);

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

                FillTri((uint)primitive.Attributes, (uint)x1, (uint)y1, (uint)x2, (uint)y2, (uint)x3, (uint)y3);
            }
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
            return ((int)(_screenWidth * 0.5f) + (int)ndcPoint.X,
                    (int)(_screenHeight * 0.5f) - (int)(_ratio * ndcPoint.Y));
        }

        // GPU Memory Transfer Commands (GP0): $02 - Fill Rectangle In VRAM
        private void FillRectVram(uint color, uint x, uint y, uint width, uint height) {
            Gpu.WriteGP0((0x02 << 24) | (color & 0xFF_FF_FF));
            Gpu.WriteGP0((y << 16) + (x & 0xFFFF));
            Gpu.WriteGP0((height << 16) + (width & 0xFFFF));
        }

        // GPU Memory Transfer Commands (GP0): $80 - Copy Rectangle (VRAM To VRAM)
        private void CopyRectVram(uint x1, uint y1, uint x2, uint y2, uint width, uint height) {
            Gpu.WriteGP0((uint)0x80 << 24);
            Gpu.WriteGP0((y1 << 16) + (x1 & 0xFFFF));
            Gpu.WriteGP0((y2 << 16) + (x2 & 0xFFFF));
            Gpu.WriteGP0((height << 16) + (width & 0xFFFF));
        }

        // GPU Memory Transfer Commands (GP0): $A0 - Copy Rectangle (CPU To VRAM)
        private void CopyRectCpu(uint x, uint y, uint width, uint height) {
            Gpu.WriteGP0((uint)0xA0 << 24);
            Gpu.WriteGP0((y << 16) + (x & 0xFFFF));
            Gpu.WriteGP0((height << 16) + (width & 0xFFFF));
        }

        private void FillTri(uint color, uint x1, uint y1, uint x2, uint y2, uint x3, uint y3) {
            Gpu.WriteGP0((0x20 << 24) | (color & 0xFF_FF_FF));
            Gpu.WriteGP0((y1 << 16) + (x1 & 0xFFFF));
            Gpu.WriteGP0((y2 << 16) + (x2 & 0xFFFF));
            Gpu.WriteGP0((y3 << 16) + (x3 & 0xFFFF));
        }

        private void FillTriAlpha(uint color, uint x1, uint y1, uint x2, uint y2, uint x3, uint y3) {
            Gpu.WriteGP0((0x22 << 24) | (color & 0xFF_FF_FF));
            Gpu.WriteGP0((y1 << 16) + (x1 & 0xFFFF));
            Gpu.WriteGP0((y2 << 16) + (x2 & 0xFFFF));
            Gpu.WriteGP0((y3 << 16) + (x3 & 0xFFFF));
        }

        private void SetupFakeEnv() {
            Gpu.WriteGP0(0xE1_000600);
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
