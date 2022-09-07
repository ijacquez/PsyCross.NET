using System;
using System.Numerics;
using PsyCross.Math;

namespace PsyCross.Testing.Rendering {
    public static partial class Renderer {
        public static void DrawTmd(Render render, PsyQ.Tmd tmd) {
            foreach (PsyQ.TmdObject tmdObject in tmd.Objects) {
                DrawTmdObject(render, tmdObject);
            }
        }

        public static void DrawTmdObject(Render render, PsyQ.TmdObject tmdObject) {
            for (int packetIndex = 0; packetIndex < tmdObject.Packets.Length; packetIndex++) {
                PsyQ.TmdPacket tmdPacket = tmdObject.Packets[packetIndex];

                // Release all gen primitives here as any culled primitives
                // won't be released (due to continue(s) in loop)
                render.ReleaseGenPrimitives();

                GenPrimitive genPrimitive = render.AcquireGenPrimitive();

                CollectPrimitiveVerticesData(render, tmdObject, tmdPacket, genPrimitive);

                TransformToView(render, genPrimitive);

                genPrimitive.FaceNormal = CalculateScaledFaceNormal(genPrimitive.ViewPoints);

                // Perform backface culling unless it's "double sided"
                if ((tmdPacket.PrimitiveHeader.Flags & PsyQ.TmdPrimitiveFlags.Fce) != PsyQ.TmdPrimitiveFlags.Fce) {
                    if (TestBackFaceCull(genPrimitive)) {
                        // Console.WriteLine("---------------- Backface Cull ----------------");
                        continue;
                    }
                }

                GenerateClipFlags(render, genPrimitive);

                // Cull primitive if it's outside of any of the six planes

                if (TestOutsideFustrum(genPrimitive)) {
                    // Console.WriteLine($"---------------- Cull ---------------- {genPrimitive.ClipFlags[0]} & {genPrimitive.ClipFlags[1]} & {genPrimitive.ClipFlags[2]} -> {genPrimitive.ViewPoints[0]}; {genPrimitive.ViewPoints[1]}; {genPrimitive.ViewPoints[2]}");
                    continue;
                }

                CollectRemainingPrimitiveData(render, tmdObject, tmdPacket, genPrimitive);

                // XXX: Remove
                if (genPrimitive.VertexCount == 3) {
                    genPrimitive.Type = PsyQ.TmdPrimitiveType.G3;
                } else {
                    genPrimitive.Type = PsyQ.TmdPrimitiveType.G4;
                }
                genPrimitive.GouraudShadingColorBuffer[0] = GetColor(packetIndex);
                genPrimitive.GouraudShadingColorBuffer[1] = GetColor(packetIndex + 1);
                genPrimitive.GouraudShadingColorBuffer[2] = GetColor(packetIndex + 2);
                genPrimitive.GouraudShadingColorBuffer[3] = GetColor(packetIndex + 3);

                genPrimitive.FaceNormal = Vector3.Normalize(genPrimitive.FaceNormal);

                TransformToWorld(render, genPrimitive);

                // XXX: Add a flag to check if primitive (object) is affected by fog
                // CalculateFog(render, genPrimitive);

                // Perform light source calculation
                // XXX: Change this to check lighting from a property getter
                // if ((tmdPacket.PrimitiveHeader.Flags & PsyQ.TmdPrimitiveFlags.Lgt) != PsyQ.TmdPrimitiveFlags.Lgt) {
                //     CalculateLighting(render, genPrimitive);
                // }

                ClipNearPlane(render, genPrimitive);

                // SubdivideGenPrimitive(render, genPrimitive);

                foreach (GenPrimitive currentGenPrimitive in render.GenPrimitives) {
                    if (GenPrimitive.HasFlag(currentGenPrimitive, GenPrimitiveFlags.Discarded)) {
                        continue;
                    }

                    TransformToScreen(render, currentGenPrimitive);

                    CullZeroAreaPrimitives(currentGenPrimitive);

                    if (GenPrimitive.HasFlag(currentGenPrimitive, GenPrimitiveFlags.Discarded)) {
                        continue;
                    }

                    if (TestScreenPointOverflow(currentGenPrimitive)) {
                        // Console.WriteLine("[1;31mOverflow[m");
                        continue;
                    }

                    DrawGenPrimitive(render, currentGenPrimitive);
                }
            }
        }

        private static void DrawGenPrimitive(Render render, GenPrimitive genPrimitive) {
            var commandHandle = DrawPrimitive(render, genPrimitive);
            // XXX: Move the sort point code out and take in only the Z value
            render.PrimitiveSort.Add(genPrimitive.ViewPoints, PrimitiveSortPoint.Center, commandHandle);
        }

        // XXX: Remove (or move to a DebugHelper)
        private static Random _GetRandomColorRandom = new Random();
        private static Rgb888[] _HugeTable = new Rgb888[4096];
        static Renderer() { for (int i = 0; i < _HugeTable.Length; i++) { _HugeTable[i] = new Rgb888((byte)(_GetRandomColorRandom.NextDouble() * 255), (byte)(_GetRandomColorRandom.NextDouble() * 255), (byte)(_GetRandomColorRandom.NextDouble() * 255)); } }
        private static Rgb888 GetRandomColor() => _HugeTable[System.Math.Abs(_GetRandomColorRandom.Next()) % 4095];
        private static Rgb888 GetColor(int index) => _HugeTable[System.Math.Abs(index) % 4095];

        private static void CollectPrimitiveVerticesData(Render render, PsyQ.TmdObject tmdObject, PsyQ.TmdPacket tmdPacket, GenPrimitive genPrimitive) {
            genPrimitive.VertexCount = tmdPacket.Primitive.VertexCount;

            genPrimitive.PolygonVertexBuffer[0] = tmdObject.Vertices[tmdPacket.Primitive.IndexV0];
            genPrimitive.PolygonVertexBuffer[1] = tmdObject.Vertices[tmdPacket.Primitive.IndexV1];
            genPrimitive.PolygonVertexBuffer[2] = tmdObject.Vertices[System.Math.Max(tmdPacket.Primitive.IndexV2, 0)];
            genPrimitive.PolygonVertexBuffer[3] = tmdObject.Vertices[System.Math.Max(tmdPacket.Primitive.IndexV3, 0)];
        }

        private static void CollectRemainingPrimitiveData(Render render, PsyQ.TmdObject tmdObject, PsyQ.TmdPacket tmdPacket, GenPrimitive genPrimitive) {
            genPrimitive.NormalCount = tmdPacket.Primitive.NormalCount;
            genPrimitive.Type = tmdPacket.Primitive.Type;

            if (tmdPacket.Primitive.NormalCount > 0) {
                genPrimitive.PolygonNormalBuffer[0] = tmdObject.Normals[tmdPacket.Primitive.IndexN0];
                genPrimitive.PolygonNormalBuffer[1] = (tmdPacket.Primitive.IndexN1 >= 0) ? tmdObject.Normals[tmdPacket.Primitive.IndexN1] : genPrimitive.PolygonNormals[0];
                genPrimitive.PolygonNormalBuffer[2] = (tmdPacket.Primitive.IndexN2 >= 0) ? tmdObject.Normals[tmdPacket.Primitive.IndexN2] : genPrimitive.PolygonNormals[0];
                genPrimitive.PolygonNormalBuffer[3] = (tmdPacket.Primitive.IndexN3 >= 0) ? tmdObject.Normals[tmdPacket.Primitive.IndexN3] : genPrimitive.PolygonNormals[0];
            }

            if ((tmdPacket.PrimitiveHeader.Mode & PsyQ.TmdPrimitiveMode.Tme) == PsyQ.TmdPrimitiveMode.Tme) {
                genPrimitive.TexcoordBuffer[0] = tmdPacket.Primitive.T0;
                genPrimitive.TexcoordBuffer[1] = tmdPacket.Primitive.T1;
                genPrimitive.TexcoordBuffer[2] = tmdPacket.Primitive.T2;
                genPrimitive.TexcoordBuffer[3] = tmdPacket.Primitive.T3;

                genPrimitive.TPageId = tmdPacket.Primitive.Tsb.Value;
                genPrimitive.ClutId = tmdPacket.Primitive.Cba.Value;
            }

            genPrimitive.GouraudShadingColorBuffer[0] = tmdPacket.Primitive.C0;
            genPrimitive.GouraudShadingColorBuffer[1] = tmdPacket.Primitive.C1;
            genPrimitive.GouraudShadingColorBuffer[2] = tmdPacket.Primitive.C2;
            genPrimitive.GouraudShadingColorBuffer[3] = tmdPacket.Primitive.C3;
        }

        private static bool TestAnyOutsideFustrum(GenPrimitive genPrimitive) =>
            (BitwiseOrClipFlags(genPrimitive.ClipFlags) != ClipFlags.None);

        private static bool TestOutsideFustrum(GenPrimitive genPrimitive) =>
            (BitwiseAndClipFlags(genPrimitive.ClipFlags) != ClipFlags.None);

        private static bool TestBackFaceCull(Vector3 viewPoint, Vector3 faceNormal) =>
            (Vector3.Dot(-viewPoint, faceNormal) <= 0f);

        private static bool TestBackFaceCull(GenPrimitive genPrimitive) =>
            TestBackFaceCull(genPrimitive.ViewPoints[0], genPrimitive.FaceNormal);

        private static void CullZeroAreaPrimitives(GenPrimitive genPrimitive) {
            ref Vector2Int aVertex = ref genPrimitive.ScreenPoints[0];
            ref Vector2Int bVertex = ref genPrimitive.ScreenPoints[1];
            ref Vector2Int cVertex = ref genPrimitive.ScreenPoints[2];

            bool tri1AreaZero = CalculateScreenPointArea(aVertex, bVertex, cVertex);

            switch (genPrimitive.VertexCount) {
                case 3 when tri1AreaZero:
                    GenPrimitive.Discard(genPrimitive);
                    break;
                case 4:
                    ref Vector2Int dVertex = ref genPrimitive.ScreenPoints[3];

                    bool tri2AreaZero = CalculateScreenPointArea(aVertex, bVertex, cVertex);

                    if (tri1AreaZero && tri2AreaZero) {
                        GenPrimitive.Discard(genPrimitive);
                    }
                    break;
            }
        }

        private static bool CalculateScreenPointArea(Vector2Int a, Vector2Int b, Vector2Int c) =>
            // Remember that Y is inverted here
            ((b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X) == 0);

        private static bool TestScreenPointOverflow(GenPrimitive genPrimitive) {
            // Vertices have a range of [-1024,1023] even though each component
            // is 16-bit. If any of the components exceed the range, we need to
            // cull the primitive entirely. Otherwise, we will see graphical
            // errors
            for (int i = 0; i < genPrimitive.VertexCount; i++) {
                int sx = genPrimitive.ScreenPoints[i].X;
                int sy = genPrimitive.ScreenPoints[i].Y;

                if ((sx < -1024) || (sx > 1023) || (sy < -1024) || (sy > 1023)) {
                    return true;
                }

                // genPrimitive.ScreenPoints[i].X = System.Math.Clamp(genPrimitive.ScreenPoints[i].X, -1024, 1023);
                // genPrimitive.ScreenPoints[i].Y = System.Math.Clamp(genPrimitive.ScreenPoints[i].Y, -1024, 1023);
            }

            return false;
        }

        private static Vector3 CalculateFaceNormal(Vector3[] points) =>
            Vector3.Normalize(CalculateScaledFaceNormal(points));

        private static Vector3 CalculateScaledFaceNormal(ReadOnlySpan<Vector3> points) {
            Vector3 a = points[2] - points[0];
            Vector3 b = points[1] - points[0];

            return Vector3.Cross(a, b);
        }

        private static void TransformToWorld(Render render, GenPrimitive genPrimitive) {
            for (int i = 0; i < genPrimitive.VertexCount; i++) {
                genPrimitive.WorldPoints[i] = Vector3.Transform(genPrimitive.Vertices[i], render.ModelMatrix);
            }
        }

        private static Vector3 TransformToView(Render render, Vector3 point) =>
            Vector3.Transform(point, render.ModelViewMatrix);

        private static void TransformToView(Render render, GenPrimitive genPrimitive) {
            for (int i = 0; i < genPrimitive.VertexCount; i++) {
                genPrimitive.ViewPoints[i] = TransformToView(render, genPrimitive.Vertices[i]);
            }
        }

        private static Vector3 TransformToClip(Render render, Vector3 point) {
            float inverseZ = render.Camera.ViewDistance / point.Z;

            return new Vector3(point.X * inverseZ, point.Y * inverseZ, render.Camera.ViewDistance * point.Z);
        }

        private static void TransformToScreen(Render render, GenPrimitive genPrimitive) {
            for (int i = 0; i < genPrimitive.VertexCount; i++) {
                Vector3 clipPoint = TransformToClip(render, genPrimitive.ViewPoints[i]);

                genPrimitive.ScreenPoints[i].X = (int) clipPoint.X;
                genPrimitive.ScreenPoints[i].Y = (int)-clipPoint.Y;
            }
        }
    }
}
