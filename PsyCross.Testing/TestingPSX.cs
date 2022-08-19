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
            _renderStates[0].DispEnv = new PsyQ.DispEnv(new RectInt(0,             0, _ScreenWidth, _ScreenHeight));
            _renderStates[0].DrawEnv = new PsyQ.DrawEnv(new RectInt(0, _ScreenHeight, _ScreenWidth, _ScreenHeight), new Vector2Int(_ScreenWidth / 2, _ScreenHeight + (_ScreenHeight / 2)));

            _renderStates[1].DispEnv = new PsyQ.DispEnv(new RectInt(0, _ScreenHeight, _ScreenWidth, _ScreenHeight));
            _renderStates[1].DrawEnv = new PsyQ.DrawEnv(new RectInt(0,             0, _ScreenWidth, _ScreenHeight), new Vector2Int(_ScreenWidth / 2, _ScreenHeight / 2));

            _renderStates[0].DrawEnv.Color = new Rgb888(0x60, 0x60, 0x60);
            _renderStates[0].DrawEnv.IsClear = true;

            _renderStates[1].DrawEnv.Color = new Rgb888(0x60, 0x60, 0x60);
            _renderStates[1].DrawEnv.IsClear = true;

            _renderStateIndex = 0;

            PsyQ.PutDispEnv(_renderStates[0].DispEnv);
            PsyQ.PutDrawEnv(_renderStates[0].DrawEnv);

            PsyQ.SetDispMask(true);

            PsyQ.DrawSync();

            var timData = ResourceManager.GetBinaryFile("PAT4T.TIM");

            // if (PsyQ.TryReadTim(timData, out PsyQ.Tim tim)) {
            //     PsyQ.LoadImage(tim.ImageHeader.Rect, tim.Header.Flags.BitDepth, tim.Image);
            //
            //     int _tPageId = PsyQ.GetTPage(tim.Header.Flags.BitDepth,
            //                                  (ushort)tim.ImageHeader.Rect.X,
            //                                  (ushort)tim.ImageHeader.Rect.Y);
            //
            //     if (tim.Header.Flags.HasClut) {
            //         int _clutId = PsyQ.LoadClut(tim.Cluts[0].Clut, 0, 480);
            //     }
            // }

            // var tmdData = ResourceManager.GetBinaryFile("VENUS3G.TMD");
            // var tmdData = ResourceManager.GetBinaryFile("SHUTTLE1.TMD");
            var tmdData = ResourceManager.GetBinaryFile("CUBE3.TMD");
            // var tmdData = ResourceManager.GetBinaryFile("CUBE3G.TMD");
            // var tmdData = ResourceManager.GetBinaryFile("CUBE3GT.TMD");
            if (PsyQ.TryReadTmd(tmdData, out _tmd)) {
                Console.WriteLine("Success reading TMD");
            }
            PsyQ.ClearImage(new RectInt(0, 0, 1024, 512), new Rgb888(255, 255, 255));
        }

        private const int _ScreenWidth  = 320;
        private const int _ScreenHeight = 240;

        private const float _Deg2Rad = MathF.PI / 180.0f;
        private const float _Rad2Deg = 180.0f / MathF.PI;

        private const float _Fov      = 90.0f;
        private const float _FovAngle = _Fov * _Deg2Rad * 0.5f;
        private const float _Ratio    = _ScreenWidth / (float)_ScreenHeight;
        private static readonly float _ViewDistance = 0.5f * ((_ScreenWidth - 1.0f) * MathF.Tan(_FovAngle));

        private PrimitiveSort _primitiveSort = new PrimitiveSort(65536);
        private CommandBuffer _commandBuffer = new CommandBuffer(65536);

        private PsyQ.Tmd _tmd;
        private Vector3[] _pos = new Vector3[2];
        private Vector3[] _rot = new Vector3[2];

        public void Update() {
            _commandBuffer.Reset();
            _primitiveSort.Reset();

            Matrix4x4[] objectMat = new Matrix4x4[2];

            _pos[0].Z = 0.3f;
            _rot[0].X = (((_rot[0].X * _Rad2Deg) + 0.15f) % 360.0f) * _Deg2Rad;
            _rot[0].Y = (((_rot[0].Y * _Rad2Deg) + 0.25f) % 360.0f) * _Deg2Rad;
            _rot[0].Z = (((_rot[0].Z * _Rad2Deg) + 0.35f) % 360.0f) * _Deg2Rad;
            objectMat[0] = CreateMatrix(_pos[0], _rot[0]);

            DrawTmd(_tmd.Objects[0], objectMat[0], _commandBuffer, _primitiveSort);
            // DrawTmd(_tmd.Objects[0], objectMat[0], _commandBuffer, _primitiveSort);
            // DrawTmd(_tmd.Objects[0], objectMat[0], _commandBuffer, _primitiveSort);
            // DrawTmd(_tmd.Objects[0], objectMat[0], _commandBuffer, _primitiveSort);
            // DrawTmd(_tmd.Objects[0], objectMat[0], _commandBuffer, _primitiveSort);
            // DrawTmd(_tmd.Objects[0], objectMat[0], _commandBuffer, _primitiveSort);

            _primitiveSort.Sort();

            PsyQ.DrawPrim(_primitiveSort, _commandBuffer);
            Console.WriteLine($"_commandBuffer.AllocatedCount: {_commandBuffer.AllocatedCount}");
            PsyQ.DrawSync();

            // Swap buffer
            _renderStateIndex ^= 1;

            PsyQ.PutDispEnv(_renderStates[_renderStateIndex].DispEnv);
            PsyQ.PutDrawEnv(_renderStates[_renderStateIndex].DrawEnv);
        }

        private static Matrix4x4 CreateMatrix(Vector3 pos, Vector3 rot) {
            var rotZ = Matrix4x4.CreateRotationZ(rot.Z);
            var rotY = Matrix4x4.CreateRotationY(rot.Y);
            var rotX = Matrix4x4.CreateRotationX(rot.X);
            var translation = Matrix4x4.CreateTranslation(pos);

            return (rotZ * rotY * rotX) * translation;
        }

        private static Vector3[] _clipPoints = new Vector3[4];
        private static Vector3[] _ndcPoints = new Vector3[4];
        private static Vector3[] _polygonVertices = new Vector3[4];
        private static Vector3[] _polygonNormals = new Vector3[4];

        private static void DrawTmd(PsyQ.TmdObject tmdObject, Matrix4x4 matrix, CommandBuffer commandBuffer, PrimitiveSort primitiveSort) {
            foreach (var tmdPacket in tmdObject.Packets) {
                _polygonVertices[0] = tmdObject.Vertices[tmdPacket.Primitive.IndexV0];
                _polygonVertices[1] = tmdObject.Vertices[tmdPacket.Primitive.IndexV1];

                if (tmdPacket.Primitive.VertexCount >= 3) {
                    _polygonVertices[2] = tmdObject.Vertices[tmdPacket.Primitive.IndexV2];

                    if (tmdPacket.Primitive.VertexCount == 4) {
                        _polygonVertices[3] = tmdObject.Vertices[tmdPacket.Primitive.IndexV3];
                    }
                }

                _polygonNormals[0] = tmdObject.Normals[tmdPacket.Primitive.IndexN0];

                if (tmdPacket.Primitive.NormalCount >= 3) {
                    _polygonNormals[1] = tmdObject.Normals[tmdPacket.Primitive.IndexN1];
                    _polygonNormals[2] = tmdObject.Normals[tmdPacket.Primitive.IndexN2];
                }

                if (tmdPacket.Primitive.NormalCount == 4) {
                    _polygonNormals[3] = tmdObject.Normals[tmdPacket.Primitive.IndexN3];
                }

                TransformToClip(tmdPacket.Primitive.VertexCount, _clipPoints, matrix, _polygonVertices);

                Vector3 transformedNormal = Vector3.TransformNormal(_polygonNormals[0], matrix);
                float t = Vector3.Dot(_clipPoints[0], transformedNormal);
                if (t >= 0.0f) {
                    Console.WriteLine($"transformedNormal: {transformedNormal}, {t}");
                    continue;
                }
                TransformToNdc(tmdPacket.Primitive.VertexCount, _ndcPoints, _clipPoints);

                switch (tmdPacket.PrimitiveType) {
                    case PsyQ.TmdPrimitiveType.F3:
                        DrawTmdPrimitiveF3(tmdPacket, commandBuffer, primitiveSort);
                        break;
                    case PsyQ.TmdPrimitiveType.Ft3:
                        DrawTmdPrimitiveFt3(tmdPacket, commandBuffer, primitiveSort);
                        break;
                    case PsyQ.TmdPrimitiveType.G3:
                        DrawTmdPrimitiveG3(tmdPacket, commandBuffer, primitiveSort);
                        break;
                    case PsyQ.TmdPrimitiveType.Gt3:
                        DrawTmdPrimitiveGt3(tmdPacket, commandBuffer, primitiveSort);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        private static void DrawTmdPrimitiveF3(PsyQ.TmdPacket tmdPacket, CommandBuffer commandBuffer, PrimitiveSort primitiveSort) {
            var primitive = (PsyQ.TmdPrimitiveF3)tmdPacket.Primitive;

            var handle = commandBuffer.AllocatePolyF3();
            var poly = commandBuffer.GetPolyF3(handle);

            poly[0].SetCommand();
            poly[0].Color = primitive.Color;
            poly[0].P0 = TransformToScreen(_ndcPoints[0]);
            poly[0].P1 = TransformToScreen(_ndcPoints[1]);
            poly[0].P2 = TransformToScreen(_ndcPoints[2]);

            primitiveSort.Add(_clipPoints, PrimitiveSortPoint.Center, handle);
        }

        private static void DrawTmdPrimitiveFt3(PsyQ.TmdPacket tmdPacket, CommandBuffer commandBuffer, PrimitiveSort primitiveSort) {
            var primitive = (PsyQ.TmdPrimitiveFt3)tmdPacket.Primitive;

            var handle = commandBuffer.AllocatePolyFt3();
            var poly = commandBuffer.GetPolyFt3(handle);

            poly[0].SetCommand();
            poly[0].Color = Rgb888.White;
            poly[0].T0 = primitive.T0;
            poly[0].T1 = primitive.T1;
            poly[0].T2 = primitive.T2;
            poly[0].TPageId = primitive.Tsb.Value;
            poly[0].ClutId = primitive.Cba.Value;
            poly[0].P0 = TransformToScreen(_ndcPoints[0]);
            poly[0].P1 = TransformToScreen(_ndcPoints[1]);
            poly[0].P2 = TransformToScreen(_ndcPoints[2]);

            primitiveSort.Add(_clipPoints, PrimitiveSortPoint.Center, handle);
        }

        private static void DrawTmdPrimitiveG3(PsyQ.TmdPacket tmdPacket, CommandBuffer commandBuffer, PrimitiveSort primitiveSort) {
            var primitive = (PsyQ.TmdPrimitiveG3)tmdPacket.Primitive;

            var handle = commandBuffer.AllocatePolyG3();
            var poly = commandBuffer.GetPolyG3(handle);

            poly[0].SetCommand();
            // XXX: Bad gouraud colors
            poly[0].C0 = new Rgb888(255,   0,   0);
            poly[0].C1 = new Rgb888(  0, 255,   0);
            poly[0].C2 = new Rgb888(  0,   0, 255);
            poly[0].P0 = TransformToScreen(_ndcPoints[0]);
            poly[0].P1 = TransformToScreen(_ndcPoints[1]);
            poly[0].P2 = TransformToScreen(_ndcPoints[2]);

            primitiveSort.Add(_clipPoints, PrimitiveSortPoint.Center, handle);
        }

        private static void DrawTmdPrimitiveGt3(PsyQ.TmdPacket tmdPacket, CommandBuffer commandBuffer, PrimitiveSort primitiveSort) {
            var primitive = (PsyQ.TmdPrimitiveGt3)tmdPacket.Primitive;

            var handle = commandBuffer.AllocatePolyGt3();
            var poly = commandBuffer.GetPolyGt3(handle);

            poly[0].SetCommand();
            // XXX: Bad gouraud colors
            poly[0].C0 = new Rgb888(255,   0,   0);
            poly[0].C1 = new Rgb888(  0, 255,   0);
            poly[0].C2 = new Rgb888(  0,   0, 255);
            poly[0].T0 = primitive.T0;
            poly[0].T1 = primitive.T1;
            poly[0].T2 = primitive.T2;
            poly[0].TPageId = primitive.Tsb.Value;
            poly[0].ClutId = primitive.Cba.Value;
            poly[0].P0 = TransformToScreen(_ndcPoints[0]);
            poly[0].P1 = TransformToScreen(_ndcPoints[1]);
            poly[0].P2 = TransformToScreen(_ndcPoints[2]);

            primitiveSort.Add(_clipPoints, PrimitiveSortPoint.Center, handle);
        }

        private static void TransformToClip(int vertexCount, Vector3[] clipPoints, Matrix4x4 matrix, Vector3[] vertices) {
            for (int i = 0; i < vertexCount; i++) {
                clipPoints[i] = Vector3.Transform(vertices[i], matrix) + matrix.Translation;
            }
        }

        private static void TransformToNdc(int vertexCount, Vector3[] ndcPoints, Vector3[] clipPoints) {
            for (int i = 0; i < vertexCount; i++) {
                Vector3 clipPoint = clipPoints[i];
                float invZ = _ViewDistance / clipPoint.Z;

                ndcPoints[i] = new Vector3(clipPoint.X * invZ,
                                           clipPoint.Y * invZ,
                                           clipPoint.Z * invZ);
            }
        }

        private static Vector2Int TransformToScreen(Vector3 ndcPoint) {
            return new Vector2(ndcPoint.X, _Ratio * -ndcPoint.Y);
        }
    }
}
