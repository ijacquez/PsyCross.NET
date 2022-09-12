using System;
using System.Numerics;
using PsyCross.Math;

namespace PsyCross.Testing.Rendering {
    public static partial class Renderer {
        private sealed class VertexIndices {
            public int[] IndexBuffer { get; } = new int[4];
            public int Count { get; set; }

            public Span<int> Indices => new Span<int>(IndexBuffer, 0, Count);
        }

        private sealed class AdjacencyList {
            private AdjacencyList() {
            }

            public AdjacencyList(params int[] vertices) {
                Vertices = vertices;
            }

            public int[] Vertices { get; }
        }

        private static readonly AdjacencyList[] _TriAdjacencyTable = new AdjacencyList[] {
            new AdjacencyList(1, 2), // 0
            new AdjacencyList(0, 2), // 1
            new AdjacencyList(0, 1), // 2
        };

        private static readonly AdjacencyList[] _QuadAdjacencyTable = new AdjacencyList[] {
            new AdjacencyList(1, 2), // 0
            new AdjacencyList(0, 3), // 1
            new AdjacencyList(0, 3), // 2
            new AdjacencyList(1, 2)  // 3
        };

        private static readonly VertexIndices _InteriorVertexIndices = new VertexIndices();
        private static readonly VertexIndices _ExteriorVertexIndices = new VertexIndices();

        private static float _ZClipFactor = 0f;

        private static void ClipRenderInit(Render render) {
            _ZClipFactor = (0.5f * render.Camera.ScreenWidth) / render.Camera.ViewDistance;
        }

        private static void ClipNearPlane(Render render, GenPrimitive genPrimitive) {
            if ((BitwiseOrClipFlags(genPrimitive.ClipFlags) & ClipFlags.Near) != ClipFlags.Near) {
                return;
            }

            CalculateInteriorExteriorVertices(genPrimitive, _InteriorVertexIndices, _ExteriorVertexIndices);

            if (genPrimitive.VertexCount == 3) {
                ClipTriangleGenPrimitiveNearPlane(render, genPrimitive, _InteriorVertexIndices, _ExteriorVertexIndices);
            } else if (genPrimitive.VertexCount == 4) {
                ClipQuadGenPrimitiveNearPlane(render, genPrimitive, _InteriorVertexIndices, _ExteriorVertexIndices);
            }
        }

        private static void ClipTriangleGenPrimitiveNearPlane(Render render, GenPrimitive genPrimitive, VertexIndices interiorIndices, VertexIndices exteriorIndices) {
            switch (_InteriorVertexIndices.Count) {
                case 1:
                    ClipTriangleGenPrimitiveNearPlaneCase1(render, genPrimitive, interiorIndices, exteriorIndices);
                    break;
                case 2:
                    ClipTriangleGenPrimitiveNearPlaneCase2(render, genPrimitive, interiorIndices, exteriorIndices);
                    break;
            }
        }

        private static void ClipTriangleGenPrimitiveNearPlaneCase1(Render render, GenPrimitive genPrimitive, VertexIndices interiorIndices, VertexIndices exteriorIndices) {
            int interiorIndex = _InteriorVertexIndices.Indices[0];
            AdjacencyList adjacencyList = _TriAdjacencyTable[interiorIndex];
            int exteriorIndex1 = adjacencyList.Vertices[0];
            int exteriorIndex2 = adjacencyList.Vertices[1];

            Vector3 interiorVertex = genPrimitive.ViewPoints[interiorIndex];
            Vector3 exteriorV1 = genPrimitive.ViewPoints[exteriorIndex1];
            Vector3 exteriorV2 = genPrimitive.ViewPoints[exteriorIndex2];

            // Interpolate between edge (v0,v1) and find the point along the
            // edge that intersects with the near plane

            float t1 = FindClipLerpEdgeT(render, interiorVertex, exteriorV1);
            float t2 = FindClipLerpEdgeT(render, interiorVertex, exteriorV2);

            // Overwrite vertices
            genPrimitive.ViewPoints[exteriorIndex1] = ClipLerpVertex(render, interiorVertex, exteriorV1, t1);
            genPrimitive.ViewPoints[exteriorIndex2] = ClipLerpVertex(render, interiorVertex, exteriorV2, t2);

            if (GenPrimitive.HasFlag(genPrimitive, GenPrimitiveFlags.Shaded)) {
                Rgb888 interiorGsc = genPrimitive.GouraudShadingColors[interiorIndex];
                Rgb888 exteriorGsc1 = genPrimitive.GouraudShadingColors[exteriorIndex1];
                Rgb888 exteriorGsc2 = genPrimitive.GouraudShadingColors[exteriorIndex2];

                genPrimitive.GouraudShadingColors[exteriorIndex1] = ClipLerpGouraudShadingColor(render, interiorGsc, exteriorGsc1, t1);
                genPrimitive.GouraudShadingColors[exteriorIndex2] = ClipLerpGouraudShadingColor(render, interiorGsc, exteriorGsc2, t2);
            }

            if (GenPrimitive.HasFlag(genPrimitive, GenPrimitiveFlags.Textured)) {
                Texcoord interiorTexcoord = genPrimitive.Texcoords[interiorIndex];
                Texcoord exteriorT1 = genPrimitive.Texcoords[exteriorIndex1];
                Texcoord exteriorT2 = genPrimitive.Texcoords[exteriorIndex2];

                genPrimitive.Texcoords[exteriorIndex1] = ClipLerpTexcoord(render, interiorTexcoord, exteriorT1, t1);
                genPrimitive.Texcoords[exteriorIndex2] = ClipLerpTexcoord(render, interiorTexcoord, exteriorT2, t2);
            }

            // Recalculate the scaled normal as the area of the triangle has
            // changed
            Vector3 faceNormal = MathHelper.CalculateScaledNormal(genPrimitive.ViewPoints[0],
                                                                  genPrimitive.ViewPoints[1],
                                                                  genPrimitive.ViewPoints[2]);

            genPrimitive.FaceArea = MathHelper.CalculateFaceArea(faceNormal);

            render.ClippedGenPrimitives.Add(genPrimitive);
        }

        private static void ClipTriangleGenPrimitiveNearPlaneCase2(Render render, GenPrimitive genPrimitive, VertexIndices interiorIndices, VertexIndices exteriorIndices) {
            // Case 2: Two interior vertices and one exterior vertex
            int exteriorIndex = _ExteriorVertexIndices.Indices[0];
            AdjacencyList adjacencyList = _TriAdjacencyTable[exteriorIndex];
            int interiorIndex1 = adjacencyList.Vertices[0];
            int interiorIndex2 = adjacencyList.Vertices[1];

            GenPrimitive tri1GenPrimitive = render.AcquireGenPrimitive();
            GenPrimitive.Copy(genPrimitive, tri1GenPrimitive);

            Vector3 exteriorVertex = genPrimitive.ViewPoints[exteriorIndex];
            Vector3 interiorV1 = genPrimitive.ViewPoints[interiorIndex1];
            Vector3 interiorV2 = genPrimitive.ViewPoints[interiorIndex2];

            float t1 = FindClipLerpEdgeT(render, interiorV1, exteriorVertex);
            float t2 = FindClipLerpEdgeT(render, interiorV2, exteriorVertex);

            Vector3 lerpedV1 = ClipLerpVertex(render, interiorV1, exteriorVertex, t1);
            Vector3 lerpedV2 = ClipLerpVertex(render, interiorV2, exteriorVertex, t2);

            // Generate two points and from that, pass in the quad
            genPrimitive.ViewPoints[exteriorIndex] = lerpedV1;

            tri1GenPrimitive.ViewPoints[exteriorIndex] = lerpedV1;
            tri1GenPrimitive.ViewPoints[interiorIndex1] = interiorV2;
            tri1GenPrimitive.ViewPoints[interiorIndex2] = lerpedV2;

            if (GenPrimitive.HasFlag(genPrimitive, GenPrimitiveFlags.Shaded)) {
                Rgb888 exteriorGsc = genPrimitive.GouraudShadingColors[exteriorIndex];
                Rgb888 interiorGsc1 = genPrimitive.GouraudShadingColors[interiorIndex1];
                Rgb888 interiorGsc2 = genPrimitive.GouraudShadingColors[interiorIndex2];

                Rgb888 lerpedGsc2 = ClipLerpGouraudShadingColor(render, interiorGsc2, exteriorGsc, t2);

                genPrimitive.GouraudShadingColors[exteriorIndex] = ClipLerpGouraudShadingColor(render, interiorGsc1, exteriorGsc, t1);

                tri1GenPrimitive.GouraudShadingColors[exteriorIndex] = genPrimitive.GouraudShadingColors[exteriorIndex];
                tri1GenPrimitive.GouraudShadingColors[interiorIndex1] = genPrimitive.GouraudShadingColors[interiorIndex2];
                tri1GenPrimitive.GouraudShadingColors[interiorIndex2] = ClipLerpGouraudShadingColor(render, interiorGsc2, exteriorGsc, t2);
            } else {
                // In the case that the primitive is not shaded, we still need
                // to copy the first color. But here, we don't exactly know
                // which is the first color so the easiest approach is to simply
                // copy the entire gouraud shading color buffer
                GenPrimitive.CopyGouraudShadingColors(genPrimitive, tri1GenPrimitive);
            }

            if (GenPrimitive.HasFlag(genPrimitive, GenPrimitiveFlags.Textured)) {
                GenPrimitive.CopyTextureAttribs(genPrimitive, tri1GenPrimitive);

                Texcoord exteriorTexcoord = genPrimitive.Texcoords[exteriorIndex];
                Texcoord interiorT1 = genPrimitive.Texcoords[interiorIndex1];
                Texcoord interiorT2 = genPrimitive.Texcoords[interiorIndex2];

                genPrimitive.Texcoords[exteriorIndex] = ClipLerpTexcoord(render, interiorT1, exteriorTexcoord, t1);

                tri1GenPrimitive.Texcoords[exteriorIndex] = genPrimitive.Texcoords[exteriorIndex];
                tri1GenPrimitive.Texcoords[interiorIndex1] = genPrimitive.Texcoords[interiorIndex2];
                tri1GenPrimitive.Texcoords[interiorIndex2] = ClipLerpTexcoord(render, interiorT2, exteriorTexcoord, t2);
            }

            Vector3 faceNormal = MathHelper.CalculateScaledNormal(genPrimitive.ViewPoints[0],
                                                                  genPrimitive.ViewPoints[1],
                                                                  genPrimitive.ViewPoints[2]);

            genPrimitive.FaceArea = MathHelper.CalculateFaceArea(faceNormal);

            Vector3 tri1FaceNormal = MathHelper.CalculateScaledNormal(tri1GenPrimitive.ViewPoints[0],
                                                                      tri1GenPrimitive.ViewPoints[1],
                                                                      tri1GenPrimitive.ViewPoints[2]);

            tri1GenPrimitive.FaceArea = MathHelper.CalculateFaceArea(tri1FaceNormal);
            tri1GenPrimitive.FaceNormal = genPrimitive.FaceNormal;

            render.ClippedGenPrimitives.Add(genPrimitive);
            render.ClippedGenPrimitives.Add(tri1GenPrimitive);
        }

        private static void ClipQuadGenPrimitiveNearPlane(Render render, GenPrimitive genPrimitive, VertexIndices interiorIndices, VertexIndices exteriorIndices) {
            CalculateInteriorExteriorVertices(genPrimitive, _InteriorVertexIndices, _ExteriorVertexIndices);

            switch (_InteriorVertexIndices.Count) {
                case 1 when (_ExteriorVertexIndices.Count == 3):
                    ClipQuadGenPrimitiveNearPlaneCase1(render, genPrimitive, interiorIndices, exteriorIndices);
                    break;
                case 2 when (_ExteriorVertexIndices.Count == 2):
                    ClipQuadGenPrimitiveNearPlaneCase2(render, genPrimitive, interiorIndices, exteriorIndices);
                    break;
                case 3 when (_ExteriorVertexIndices.Count == 1):
                    ClipQuadGenPrimitiveNearPlaneCase3(render, genPrimitive, interiorIndices, exteriorIndices);
                    break;
            }
        }

        private static void ClipQuadGenPrimitiveNearPlaneCase1(Render render, GenPrimitive genPrimitive, VertexIndices interiorIndices, VertexIndices exteriorIndices) {
            //      I
            //     / \
            // ---A---B--- A=Lerp(I,E1); B=Lerp(I,E2) and degenerate to a triangle
            //   /     \
            //  E1      E2
            //   \     /
            //    \   /
            //     \ /
            //      E3

            int interiorIndex = interiorIndices.Indices[0];
            AdjacencyList adjacencyList = _QuadAdjacencyTable[interiorIndex];
            int exteriorIndex1 = adjacencyList.Vertices[0];
            int exteriorIndex2 = adjacencyList.Vertices[1];

            Vector3 interiorVertex = genPrimitive.ViewPoints[interiorIndex];
            // Exterior vertex indices relative to the interior vertex
            Vector3 exteriorV1 = genPrimitive.ViewPoints[exteriorIndex1];
            Vector3 exteriorV2 = genPrimitive.ViewPoints[exteriorIndex2];

            float t1 = FindClipLerpEdgeT(render, interiorVertex, exteriorV1);
            float t2 = FindClipLerpEdgeT(render, interiorVertex, exteriorV2);

            genPrimitive.ViewPoints[0] = interiorVertex;
            genPrimitive.ViewPoints[1] = ClipLerpVertex(render, interiorVertex, exteriorV1, t1);
            genPrimitive.ViewPoints[2] = ClipLerpVertex(render, interiorVertex, exteriorV2, t2);

            if (GenPrimitive.HasFlag(genPrimitive, GenPrimitiveFlags.Shaded)) {
                Rgb888 interiorGsc = genPrimitive.GouraudShadingColors[interiorIndex];
                Rgb888 exteriorGsc1 = genPrimitive.GouraudShadingColors[exteriorIndex1];
                Rgb888 exteriorGsc2 = genPrimitive.GouraudShadingColors[exteriorIndex2];

                genPrimitive.GouraudShadingColors[0] = interiorGsc;
                genPrimitive.GouraudShadingColors[1] = ClipLerpGouraudShadingColor(render, interiorGsc, exteriorGsc1, t1);
                genPrimitive.GouraudShadingColors[2] = ClipLerpGouraudShadingColor(render, interiorGsc, exteriorGsc2, t2);
            }

            if (GenPrimitive.HasFlag(genPrimitive, GenPrimitiveFlags.Textured)) {
                Texcoord interiorTexcoord = genPrimitive.Texcoords[interiorIndex];
                Texcoord exteriorT1 = genPrimitive.Texcoords[exteriorIndex1];
                Texcoord exteriorT2 = genPrimitive.Texcoords[exteriorIndex2];

                genPrimitive.Texcoords[0] = interiorTexcoord;
                genPrimitive.Texcoords[1] = ClipLerpTexcoord(render, interiorTexcoord, exteriorT1, t1);
                genPrimitive.Texcoords[2] = ClipLerpTexcoord(render, interiorTexcoord, exteriorT2, t2);
            }

            GenPrimitive.Decompose(genPrimitive);

            Vector3 faceNormal = MathHelper.CalculateScaledNormal(genPrimitive.ViewPoints[0],
                                                                  genPrimitive.ViewPoints[1],
                                                                  genPrimitive.ViewPoints[2]);

            genPrimitive.FaceArea = MathHelper.CalculateFaceArea(faceNormal);

            render.ClippedGenPrimitives.Add(genPrimitive);
        }

        private static void ClipQuadGenPrimitiveNearPlaneCase2(Render render, GenPrimitive genPrimitive, VertexIndices interiorIndices, VertexIndices exteriorIndices) {
            //  I1-----I2
            //   |     |
            // --A-----B-- A=Lerp(I1,E1); B=Lerp(I2,E2)
            //   |     |
            //  E1-----E2

            int interiorIndex1 = interiorIndices.Indices[0];
            int interiorIndex2 = interiorIndices.Indices[1];
            AdjacencyList adjacencyList1 = _QuadAdjacencyTable[interiorIndex1];
            AdjacencyList adjacencyList2 = _QuadAdjacencyTable[interiorIndex2];
            int exteriorIndex1 = (adjacencyList1.Vertices[0] != interiorIndex2) ? adjacencyList1.Vertices[0] : adjacencyList1.Vertices[1];
            int exteriorIndex2 = (adjacencyList2.Vertices[0] != interiorIndex1) ? adjacencyList2.Vertices[0] : adjacencyList2.Vertices[1];

            ref Vector3 interiorV1 = ref genPrimitive.ViewPoints[interiorIndex1];
            ref Vector3 interiorV2 = ref genPrimitive.ViewPoints[interiorIndex2];
            ref Vector3 exteriorV1 = ref genPrimitive.ViewPoints[exteriorIndex1];
            ref Vector3 exteriorV2 = ref genPrimitive.ViewPoints[exteriorIndex2];

            float t1 = FindClipLerpEdgeT(render, interiorV1, exteriorV1);
            float t2 = FindClipLerpEdgeT(render, interiorV2, exteriorV2);

            exteriorV1 = ClipLerpVertex(render, interiorV1, exteriorV1, t1);
            exteriorV2 = ClipLerpVertex(render, interiorV2, exteriorV2, t2);

            if (GenPrimitive.HasFlag(genPrimitive, GenPrimitiveFlags.Shaded)) {
                ref Rgb888 interiorGsc1 = ref genPrimitive.GouraudShadingColors[interiorIndex1];
                ref Rgb888 interiorGsc2 = ref genPrimitive.GouraudShadingColors[interiorIndex2];
                ref Rgb888 exteriorGsc1 = ref genPrimitive.GouraudShadingColors[exteriorIndex1];
                ref Rgb888 exteriorGsc2 = ref genPrimitive.GouraudShadingColors[exteriorIndex2];

                exteriorGsc1 = ClipLerpGouraudShadingColor(render, interiorGsc1, exteriorGsc1, t1);
                exteriorGsc2 = ClipLerpGouraudShadingColor(render, interiorGsc2, exteriorGsc2, t2);
            }

            if (GenPrimitive.HasFlag(genPrimitive, GenPrimitiveFlags.Textured)) {
                ref Texcoord interiorT1 = ref genPrimitive.Texcoords[interiorIndex1];
                ref Texcoord interiorT2 = ref genPrimitive.Texcoords[interiorIndex2];
                ref Texcoord exteriorT1 = ref genPrimitive.Texcoords[exteriorIndex1];
                ref Texcoord exteriorT2 = ref genPrimitive.Texcoords[exteriorIndex2];

                exteriorT1 = ClipLerpTexcoord(render, interiorT1, exteriorT1, t1);
                exteriorT2 = ClipLerpTexcoord(render, interiorT2, exteriorT2, t2);
            }

            Vector3 faceNormal = MathHelper.CalculateScaledNormal(genPrimitive.ViewPoints[0],
                                                                  genPrimitive.ViewPoints[1],
                                                                  genPrimitive.ViewPoints[2]);

            genPrimitive.FaceArea = MathHelper.CalculateFaceArea(faceNormal);

            render.ClippedGenPrimitives.Add(genPrimitive);
        }

        private static void ClipQuadGenPrimitiveNearPlaneCase3(Render render, GenPrimitive genPrimitive, VertexIndices interiorIndices, VertexIndices exteriorIndices) {
            // We really only need one exterior vertex to find everything else
            int exteriorIndex = exteriorIndices.Indices[0];
            AdjacencyList exteriorAdjList = _QuadAdjacencyTable[exteriorIndex];

            int interiorIndex = exteriorAdjList.Vertices[0];

            AdjacencyList interiorAdjList = _QuadAdjacencyTable[interiorIndex];

            int opposingInteriorIndex = exteriorAdjList.Vertices[1];
            int middleInteriorIndex = (interiorAdjList.Vertices[0] != exteriorIndex) ? interiorAdjList.Vertices[0] : interiorAdjList.Vertices[1];

            Vector3 interiorVertex = genPrimitive.ViewPoints[interiorIndex];
            Vector3 opposingInteriorVertex = genPrimitive.ViewPoints[opposingInteriorIndex];
            Vector3 middleInteriorVertex = genPrimitive.ViewPoints[middleInteriorIndex];
            Vector3 exteriorVertex = genPrimitive.ViewPoints[exteriorIndex];

            Rgb888 interiorGsc = genPrimitive.GouraudShadingColors[interiorIndex];
            Rgb888 opposingInteriorGsc = genPrimitive.GouraudShadingColors[opposingInteriorIndex];
            Rgb888 middleInteriorGsc = genPrimitive.GouraudShadingColors[middleInteriorIndex];
            Rgb888 exteriorGsc = genPrimitive.GouraudShadingColors[exteriorIndex];

            Texcoord interiorTexcoord = genPrimitive.Texcoords[interiorIndex];
            Texcoord opposingInteriorTexcoord = genPrimitive.Texcoords[opposingInteriorIndex];
            Texcoord middleInteriorTexcoord = genPrimitive.Texcoords[middleInteriorIndex];
            Texcoord exteriorTexcoord = genPrimitive.Texcoords[exteriorIndex];

            float t1 = FindClipLerpEdgeT(render, exteriorVertex, interiorVertex);
            float t2 = FindClipLerpEdgeT(render, exteriorVertex, opposingInteriorVertex);

            Vector3 lerpedV1 = ClipLerpVertex(render, exteriorVertex, interiorVertex, t1);
            Vector3 lerpedV2 = ClipLerpVertex(render, exteriorVertex, opposingInteriorVertex, t2);

            bool isShaded = GenPrimitive.HasFlag(genPrimitive, GenPrimitiveFlags.Shaded);

            Rgb888 lerpedGsc1 = default;
            Rgb888 lerpedGsc2 = default;

            if (isShaded) {
                lerpedGsc1 = ClipLerpGouraudShadingColor(render, exteriorGsc, interiorGsc, t1);
                lerpedGsc2 = ClipLerpGouraudShadingColor(render, exteriorGsc, opposingInteriorGsc, t2);
            }

            bool isTextured = GenPrimitive.HasFlag(genPrimitive, GenPrimitiveFlags.Textured);

            Texcoord lerpedTexcoord1 = default;
            Texcoord lerpedTexcoord2 = default;

            if (isTextured) {
                lerpedTexcoord1 = ClipLerpTexcoord(render, exteriorTexcoord, interiorTexcoord, t1);
                lerpedTexcoord2 = ClipLerpTexcoord(render, exteriorTexcoord, opposingInteriorTexcoord, t2);
            }

            // Case 1: The shared edge between the two triangles that make up
            //         this quad primitive intersects with the near plane
            if ((exteriorAdjList.Vertices[0] == 0) || (exteriorAdjList.Vertices[1] == 0)) {
                //         I2
                //        /|\
                //       / | \
                //      /  |  \
                //     I0  |  I3
                //      \  |  /
                // ------A-C-B------ Notice that E makes an edge with the first vertex
                //        \|/
                //         E
                //
                // Three lerped points are needed: A, B, and C
                //
                // From this, two quads are generated

                float t3 = FindClipLerpEdgeT(render, exteriorVertex, middleInteriorVertex);

                Vector3 lerpedV3 = ClipLerpVertex(render, exteriorVertex, middleInteriorVertex, t3);

                // Quad 1 points: V0, L1, V2, L3
                // Quad 2 points: V2, L3, L2, V3
                //
                // Where Ln is the lerped vertex.

                // Recall that the triangulated quad:
                //
                //  v0--v2
                //  |  / |
                //  | /  |
                //  v1--v3
                //
                //  Vertices passed to GPU: v0, v1, v2, v3
                //
                //  Triangulated within the GPU: v0, v1, v2
                //                               v1, v2, v3
                genPrimitive.ViewPoints[0] = interiorVertex;
                genPrimitive.ViewPoints[1] = lerpedV1;
                genPrimitive.ViewPoints[2] = middleInteriorVertex;
                genPrimitive.ViewPoints[3] = lerpedV3;

                GenPrimitive quad1GenPrimitive = render.AcquireGenPrimitive();
                GenPrimitive.Copy(genPrimitive, quad1GenPrimitive);

                quad1GenPrimitive.ViewPoints[0] = middleInteriorVertex;
                quad1GenPrimitive.ViewPoints[1] = lerpedV3;
                quad1GenPrimitive.ViewPoints[2] = opposingInteriorVertex;
                quad1GenPrimitive.ViewPoints[3] = lerpedV2;

                if (isShaded) {
                    Rgb888 lerpedGsc3 = ClipLerpGouraudShadingColor(render, exteriorGsc, middleInteriorGsc, t3);

                    genPrimitive.GouraudShadingColors[0] = interiorGsc;
                    genPrimitive.GouraudShadingColors[1] = lerpedGsc1;
                    genPrimitive.GouraudShadingColors[2] = middleInteriorGsc;
                    genPrimitive.GouraudShadingColors[3] = lerpedGsc3;

                    quad1GenPrimitive.GouraudShadingColors[0] = middleInteriorGsc;
                    quad1GenPrimitive.GouraudShadingColors[1] = lerpedGsc3;
                    quad1GenPrimitive.GouraudShadingColors[2] = opposingInteriorGsc;
                    quad1GenPrimitive.GouraudShadingColors[3] = lerpedGsc2;
                } else {
                    // In the case that the primitive is not shaded, we still
                    // need to copy the first color
                    GenPrimitive.CopyGouraudShadingColors(genPrimitive, quad1GenPrimitive);
                }

                if (isTextured) {
                    GenPrimitive.CopyTextureAttribs(genPrimitive, quad1GenPrimitive);

                    Texcoord lerpedTexcoord3 = ClipLerpTexcoord(render, exteriorTexcoord, middleInteriorTexcoord, t3);

                    genPrimitive.Texcoords[0] = interiorTexcoord;
                    genPrimitive.Texcoords[1] = lerpedTexcoord1;
                    genPrimitive.Texcoords[2] = middleInteriorTexcoord;
                    genPrimitive.Texcoords[3] = lerpedTexcoord3;

                    quad1GenPrimitive.Texcoords[0] = middleInteriorTexcoord;
                    quad1GenPrimitive.Texcoords[1] = lerpedTexcoord3;
                    quad1GenPrimitive.Texcoords[2] = opposingInteriorTexcoord;
                    quad1GenPrimitive.Texcoords[3] = lerpedTexcoord2;
                }

                render.ClippedGenPrimitives.Add(genPrimitive);
                render.ClippedGenPrimitives.Add(quad1GenPrimitive);
            } else { // Case 2: No intersection of the shared edge with the near plane
                //         V
                //        / \
                //       /   \      T1
                //      /     \
                //     V-------V
                //      \     /     T2: v1 v2 L2, T3: V2 L2 L1
                // ------A---B------
                //        \ /
                //         V
                //
                // When there is one exterior point, we're left with a triangle
                // and a quad. The quad is formed with the two lerped points A
                // and B. Currently, we triangluate this quad

                genPrimitive.ViewPoints[0] = interiorVertex;
                genPrimitive.ViewPoints[1] = middleInteriorVertex;
                genPrimitive.ViewPoints[2] = opposingInteriorVertex;

                GenPrimitive quad1GenPrimitive = render.AcquireGenPrimitive();
                GenPrimitive.Copy(genPrimitive, quad1GenPrimitive);

                quad1GenPrimitive.ViewPoints[0] = interiorVertex;
                quad1GenPrimitive.ViewPoints[1] = opposingInteriorVertex;
                quad1GenPrimitive.ViewPoints[2] = lerpedV1;
                quad1GenPrimitive.ViewPoints[3] = lerpedV2;

                // Decompose here as we're decomposing the original primitive
                // but not the new generated primitive
                GenPrimitive.Decompose(genPrimitive);

                if (isShaded) {
                    genPrimitive.GouraudShadingColors[0] = interiorGsc;
                    genPrimitive.GouraudShadingColors[1] = middleInteriorGsc;
                    genPrimitive.GouraudShadingColors[2] = opposingInteriorGsc;

                    quad1GenPrimitive.GouraudShadingColors[0] = interiorGsc;
                    quad1GenPrimitive.GouraudShadingColors[1] = opposingInteriorGsc;
                    quad1GenPrimitive.GouraudShadingColors[2] = lerpedGsc1;
                    quad1GenPrimitive.GouraudShadingColors[3] = lerpedGsc2;
                } else {
                    // In the case that the primitive is not shaded, we still
                    // need to copy the first color
                    GenPrimitive.CopyGouraudShadingColors(genPrimitive, quad1GenPrimitive);
                }

                if (isTextured) {
                    GenPrimitive.CopyTextureAttribs(genPrimitive, quad1GenPrimitive);

                    genPrimitive.Texcoords[0] = interiorTexcoord;
                    genPrimitive.Texcoords[1] = middleInteriorTexcoord;
                    genPrimitive.Texcoords[2] = opposingInteriorTexcoord;

                    quad1GenPrimitive.Texcoords[0] = interiorTexcoord;
                    quad1GenPrimitive.Texcoords[1] = opposingInteriorTexcoord;
                    quad1GenPrimitive.Texcoords[2] = lerpedTexcoord1;
                    quad1GenPrimitive.Texcoords[3] = lerpedTexcoord2;
                }

                Vector3 faceNormal = MathHelper.CalculateScaledNormal(genPrimitive.ViewPoints[0],
                                                                      genPrimitive.ViewPoints[1],
                                                                      genPrimitive.ViewPoints[2]);

                genPrimitive.FaceArea = MathHelper.CalculateFaceArea(faceNormal);

                Vector3 quad1FaceNormal = MathHelper.CalculateScaledNormal(quad1GenPrimitive.ViewPoints[0],
                                                                           quad1GenPrimitive.ViewPoints[1],
                                                                           quad1GenPrimitive.ViewPoints[2]);

                quad1GenPrimitive.FaceArea = MathHelper.CalculateFaceArea(quad1FaceNormal);
                quad1GenPrimitive.FaceNormal = genPrimitive.FaceNormal;

                render.ClippedGenPrimitives.Add(genPrimitive);
                render.ClippedGenPrimitives.Add(quad1GenPrimitive);
            }
        }

        private static void CalculateInteriorExteriorVertices(GenPrimitive genPrimitive, VertexIndices interiorVertices, VertexIndices exteriorVertices) {
            int intIndex = 0;
            int extIndex = 0;

            for (int vIndex = 0; vIndex < genPrimitive.VertexCount; vIndex++) {
                if ((genPrimitive.ClipFlags[vIndex] & ClipFlags.Near) == ClipFlags.Near) {
                    exteriorVertices.IndexBuffer[extIndex] = vIndex;
                    extIndex++;
                } else {
                    interiorVertices.IndexBuffer[intIndex] = vIndex;
                    intIndex++;
                }
            }

            interiorVertices.Count = intIndex;
            exteriorVertices.Count = extIndex;
        }

        private static float FindClipLerpEdgeT(Render render, Vector3 aVertex, Vector3 bVertex) =>
            System.Math.Clamp((render.Camera.DepthNear - aVertex.Z) / (bVertex.Z - aVertex.Z), 0f, 1f);

        private static Vector3 ClipLerpVertex(Render render, Vector3 aVertex, Vector3 bVertex, float t) =>
            new Vector3(MathHelper.Lerp(aVertex.X, bVertex.X, t),
                        MathHelper.Lerp(aVertex.Y, bVertex.Y, t),
                        render.Camera.DepthNear);

        private static Rgb888 ClipLerpGouraudShadingColor(Render render, Rgb888 aRgb888, Rgb888 bRgb888, float t) {
            Vector3 aVector = (Vector3)aRgb888;
            Vector3 bVector = (Vector3)bRgb888;
            Vector3 lerpedVertex = Vector3.Lerp(aVector, bVector, t);

            return (Rgb888)lerpedVertex;
        }

        private static Texcoord ClipLerpTexcoord(Render render, Texcoord aTexcoord, Texcoord bTexcoord, float t) {
            Vector2 aVector = (Texcoord)aTexcoord;
            Vector2 bVector = (Texcoord)bTexcoord;
            Vector2 lerpedVertex = Vector2.Lerp(aVector, bVector, t);

            return (Texcoord)lerpedVertex;
        }

        private static void CalculateClipFlags(Render render, GenPrimitive genPrimitive) {
            GenPrimitive.ClearClipFlags(genPrimitive);

            for (int i = 0; i < genPrimitive.VertexCount; i++) {
                if (genPrimitive.ViewPoints[i].Z < render.Camera.DepthNear) {
                    genPrimitive.ClipFlags[i] |= ClipFlags.Near;
                } else if (genPrimitive.ViewPoints[i].Z > render.Camera.DepthFar) {
                    genPrimitive.ClipFlags[i] |= ClipFlags.Far;
                }

                // One multiplication and a comparison is faster than a dot product

                // This is still confusing to me. If FOV=90, then the slope
                // of the right plane (on XZ-axis) is 1. Taking into account
                // a FOV less than 90, we must take tan(theta/2) into
                // account (half-FOV). So: X=tan(theta/2)*Z
                float zTest = _ZClipFactor * genPrimitive.ViewPoints[i].Z;

                if (genPrimitive.ViewPoints[i].X > zTest) {
                    genPrimitive.ClipFlags[i] |= ClipFlags.Right;
                } else if (genPrimitive.ViewPoints[i].X < -zTest) {
                    genPrimitive.ClipFlags[i] |= ClipFlags.Left;
                }

                if (genPrimitive.ViewPoints[i].Y > zTest) {
                    genPrimitive.ClipFlags[i] |= ClipFlags.Top;
                } else if (genPrimitive.ViewPoints[i].Y < -zTest) {
                    genPrimitive.ClipFlags[i] |= ClipFlags.Bottom;
                }
            }
        }

        private static ClipFlags BitwiseOrClipFlags(ReadOnlySpan<ClipFlags> clipFlags) =>
            (clipFlags[0] | clipFlags[1] | clipFlags[2] | clipFlags[clipFlags.Length - 1]);

        private static ClipFlags BitwiseAndClipFlags(ReadOnlySpan<ClipFlags> clipFlags) =>
            // If vertex count is 3, third vertex (index 2) clip flag will be
            // bitwise AND'd twice. If vertex count is 4, (index 3) will be
            // bitwise AND'd
            (clipFlags[0] & clipFlags[1] & clipFlags[2] & clipFlags[clipFlags.Length - 1]);
    }
}
