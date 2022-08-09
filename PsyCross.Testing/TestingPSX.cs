using System;
using System.Numerics;
using PsyCross.Math;

namespace PsyCross.Testing {
    public class Testing {
        private RenderState[] _renderStates = new RenderState[2] {
            new RenderState(),
            new RenderState()
        };

        private int _renderStateIndex;

        public Testing() {
            _renderStates[0].DispEnv = new PsyQ.DispEnv(new RectInt(0,             0, _screenWidth, _screenHeight));
            _renderStates[0].DrawEnv = new PsyQ.DrawEnv(new RectInt(0, _screenHeight, _screenWidth, _screenHeight), new Vector2Int(_screenWidth / 2, _screenHeight + (_screenHeight / 2)));

            _renderStates[1].DispEnv = new PsyQ.DispEnv(new RectInt(0, _screenHeight, _screenWidth, _screenHeight));
            _renderStates[1].DrawEnv = new PsyQ.DrawEnv(new RectInt(0,             0, _screenWidth, _screenHeight), new Vector2Int(_screenWidth / 2, _screenHeight / 2));

            _renderStates[0].DrawEnv.IsClear = true;
            _renderStates[1].DrawEnv.IsClear = true;

            _renderStateIndex = 0;

            PsyQ.PutDispEnv(_renderStates[0].DispEnv);
            PsyQ.PutDrawEnv(_renderStates[0].DrawEnv);

            PsyQ.SetDispMask(1);

            PsyQ.DrawSync();
        }

        private const int _screenWidth  = 320;
        private const int _screenHeight = 240;

        private const float _Deg2Rad = MathF.PI / 180.0f;
        private const float _Rad2Deg = 180.0f / MathF.PI;

        private const float _Fov      = 90.0f;
        private const float _FovAngle = _Fov * _Deg2Rad * 0.5f;
        private const float _Ratio    = _screenWidth / _screenHeight;
        private static readonly float _ViewDistance = 0.5f * ((_screenWidth - 1.0f) * MathF.Tan(_FovAngle));

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

        private static readonly Texcoord[] _Uv1 = new Texcoord[] {
            new Texcoord(0, 8),
            new Texcoord(0, 0),
            new Texcoord(8, 8),
        };

        private static readonly Texcoord[] _Uv2 = new Texcoord[] {
            new Texcoord(0, 0),
            new Texcoord(8, 8),
            new Texcoord(8, 0),
        };

        Vector3[] _rot = new Vector3[2];
        Vector3[] _pos = new Vector3[2];

        private PrimitiveSort _primitiveSort = new PrimitiveSort();
        private CommandBuffer _commandBuffer = new CommandBuffer(1024);

        public void Update() {
            _commandBuffer.Reset();

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

            ushort tPageId = PsyQ.LoadTPage(new RectInt(320, 0, 8, 8), PsyQ.BitDepth.Bpp15, data1);

            _primitiveSort.Clear();

            Vector3[] clipPoints = new Vector3[3];

            Matrix4x4[] objectMat = new Matrix4x4[2];
            float a;

            _pos[0].Z = 1;
            a = ((_rot[0].Y * _Rad2Deg) + 0.5f) % 360.0f; _rot[0].Y = a * _Deg2Rad;
            objectMat[0] = CreateMatrix(_pos[0], _rot[0]);
            TransformToClip(clipPoints, objectMat[0], _Tri1);
            _primitiveSort.Add(clipPoints, PrimitiveSortPoint.Center, _Uv1);

            _pos[1].Z = 1;
            a = ((_rot[1].Y * _Rad2Deg) + 0.5f) % 360.0f; _rot[1].Y = a * _Deg2Rad;
            objectMat[1] = CreateMatrix(_pos[1], _rot[1]);
            TransformToClip(clipPoints, objectMat[0], _Tri2);
            _primitiveSort.Add(clipPoints, PrimitiveSortPoint.Center, _Uv2);

            _primitiveSort.Sort();

            Vector3[] ndcPoints = new Vector3[3];

            for (int i = _primitiveSort.Primitives.Count - 1; i >= 0; i--) {
                var primitive = _primitiveSort.Primitives[i];

                TransformToNdc(ndcPoints, primitive.Points);

                Vector2Int p1 = TransformToScreen(ndcPoints[0]);
                Vector2Int p2 = TransformToScreen(ndcPoints[1]);
                Vector2Int p3 = TransformToScreen(ndcPoints[2]);

                var tex = (Texcoord[])primitive.Attributes;

                var poly = _commandBuffer.AllocatePolyGt3();

                poly[0].C0.R = 0xFF;
                poly[0].C0.G = 0xFF;
                poly[0].C0.B = 0xFF;
                poly[0].C1.R = 0x66;
                poly[0].C1.G = 0x00;
                poly[0].C1.B = 0x55;

                poly[0].C2.R = 0x22;
                poly[0].C2.G = 0x33;
                poly[0].C2.B = 0x00;

                poly[0].P0 = p1;
                poly[0].P1 = p2;
                poly[0].P2 = p3;
                poly[0].T0 = tex[0];
                poly[0].T1 = tex[1];
                poly[0].T2 = tex[2];
                poly[0].TPageId = tPageId;

                // var v = System.Runtime.InteropServices.MemoryMarshal.Cast<PsyQ.PolyGt3, uint>(poly);
                //
                // for (int j = 0; j < v.Length; j++) {
                //     Console.WriteLine($"{j:X08}: ${v[j]:X08}");
                // }
                // Console.WriteLine("End");
            }

            PsyQ.DrawPrim(_commandBuffer);
            PsyQ.DrawSync();

            // Swap buffer
            _renderStateIndex ^= 1;

            PsyQ.PutDispEnv(_renderStates[_renderStateIndex].DispEnv);
            PsyQ.PutDrawEnv(_renderStates[_renderStateIndex].DrawEnv);
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

        private static Vector2Int TransformToScreen(Vector3 ndcPoint) {
            return new Vector2(ndcPoint.X, _Ratio * -ndcPoint.Y);
        }
    }
}
