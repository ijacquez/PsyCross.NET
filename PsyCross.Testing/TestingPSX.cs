using System;
using System.Numerics;
using PsyCross.Math;
using PsyCross.ResourceManagement;

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

            _renderStates[0].DrawEnv.Color = new Rgb888(0x00, 0x60, 0x00);
            _renderStates[0].DrawEnv.IsClear = true;

            _renderStates[1].DrawEnv.Color = new Rgb888(0x00, 0x60, 0x00);
            _renderStates[1].DrawEnv.IsClear = true;

            _renderStateIndex = 0;

            PsyQ.PutDispEnv(_renderStates[0].DispEnv);
            PsyQ.PutDrawEnv(_renderStates[0].DrawEnv);

            PsyQ.SetDispMask(true);

            PsyQ.DrawSync();

            var data = ResourceManager.GetTimFile("TIM16.TIM");

            if (PsyQ.TryReadTim(data, out PsyQ.Tim tim)) {
                PsyQ.LoadImage(tim.ImageHeader.Rect, tim.Header.Flags.BitDepth, tim.Image);

                _tPageId = PsyQ.GetTPage(tim.Header.Flags.BitDepth,
                                         (ushort)tim.ImageHeader.Rect.X,
                                         (ushort)tim.ImageHeader.Rect.Y);

                if (tim.Header.Flags.HasClut) {
                    _clutId = PsyQ.LoadClut(tim.Cluts[0].Clut, 0, 480);
                }
            }
        }

        private const int _screenWidth  = 320;
        private const int _screenHeight = 240;

        private const float _Deg2Rad = MathF.PI / 180.0f;
        private const float _Rad2Deg = 180.0f / MathF.PI;

        private const float _Fov      = 90.0f;
        private const float _FovAngle = _Fov * _Deg2Rad * 0.5f;
        private const float _Ratio    = _screenWidth / _screenHeight;
        private static readonly float _ViewDistance = 0.5f * ((_screenWidth - 1.0f) * MathF.Tan(_FovAngle));

        private const float _SW = 1.0f;
        private const float _SH = 1.0f;

        private static readonly Vector3[] _Tri1 = new Vector3[] {
            new Vector3(-_SW, -_SH, 1f),
            new Vector3(-_SW,  _SH, 1f),
            new Vector3( _SW, -_SH, 1f)
        };

        private static readonly Vector3[] _Tri2 = new Vector3[] {
            new Vector3(-_SW,  _SH, 1f),
            new Vector3( _SW, -_SH, 1f),
            new Vector3( _SW,  _SH, 1f)
        };

        private const byte _TW = 128;
        private const byte _TH = 64;

        private static readonly Texcoord[] _Uv1 = new Texcoord[] {
            new Texcoord(0, _TH),
            new Texcoord(0, 0),
            new Texcoord(_TW, _TH),
        };

        private static readonly Texcoord[] _Uv2 = new Texcoord[] {
            new Texcoord(0, 0),
            new Texcoord(_TW, _TH),
            new Texcoord(_TW, 0),
        };

        Vector3[] _rot = new Vector3[2];
        Vector3[] _pos = new Vector3[2];
        ushort _clutId;
        ushort _tPageId;

        private PrimitiveSort _primitiveSort = new PrimitiveSort(1024);
        private CommandBuffer _commandBuffer = new CommandBuffer(1024);

        private static void PrintCommand(Span<uint> words) {
            for (int i = 0; i < words.Length; i++) {
                Console.WriteLine($"{i:X08}: ${words[i]:X08}");
            }
        }

        public void Update() {
            _commandBuffer.Reset();
            _primitiveSort.Reset();

            Matrix4x4[] objectMat = new Matrix4x4[2];
            float a;
            _pos[0].Z = 1;
            a = ((_rot[0].Y * _Rad2Deg) + 0.5f) % 360.0f;
            // _rot[0].Y = a * _Deg2Rad;
            objectMat[0] = CreateMatrix(_pos[0], _rot[0]);

            _pos[1].Z = 1;
            a = ((_rot[1].Y * _Rad2Deg) + 0.5f) % 360.0f;
            // _rot[1].Y = a * _Deg2Rad;
            objectMat[1] = CreateMatrix(_pos[1], _rot[1]);

            Vector3[] clipPoints = new Vector3[3];
            Vector3[] ndcPoints = new Vector3[3];

            TransformToClip(clipPoints, objectMat[0], _Tri1);
            TransformToNdc(ndcPoints, clipPoints);
            var handle1 = _commandBuffer.AllocatePolyFt3();
            var poly1 = _commandBuffer.GetPolyFt3(handle1);
            poly1[0].SetCommand();
            poly1[0].Color = new Rgb888(128, 128, 128);
            poly1[0].P0 = TransformToScreen(ndcPoints[0]);
            poly1[0].P1 = TransformToScreen(ndcPoints[1]);
            poly1[0].P2 = TransformToScreen(ndcPoints[2]);
            poly1[0].T0 = _Uv1[0];
            poly1[0].T1 = _Uv1[1];
            poly1[0].T2 = _Uv1[2];
            poly1[0].TPageId = _tPageId;
            poly1[0].ClutId = _clutId;
            _primitiveSort.Add(clipPoints, PrimitiveSortPoint.Center, handle1);

            TransformToClip(clipPoints, objectMat[1], _Tri2);
            TransformToNdc(ndcPoints, clipPoints);
            var handle2 = _commandBuffer.AllocatePolyFt3();
            var poly2 = _commandBuffer.GetPolyFt3(handle2);
            poly2[0].SetCommand();
            poly2[0].Color = new Rgb888(128, 128, 128);
            poly2[0].P0 = TransformToScreen(ndcPoints[0]);
            poly2[0].P1 = TransformToScreen(ndcPoints[1]);
            poly2[0].P2 = TransformToScreen(ndcPoints[2]);
            poly2[0].T0 = _Uv2[0];
            poly2[0].T1 = _Uv2[1];
            poly2[0].T2 = _Uv2[2];
            poly2[0].TPageId = _tPageId;
            poly2[0].ClutId = _clutId;
            _primitiveSort.Add(clipPoints, PrimitiveSortPoint.Center, handle2);

            _primitiveSort.Sort();

            PsyQ.DrawPrim(_primitiveSort, _commandBuffer);
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
