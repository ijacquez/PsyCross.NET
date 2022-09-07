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
            new AdjacencyList(2, 1)  // 3
        };

        private static VertexIndices _InteriorVertexIndices = new VertexIndices();
        private static VertexIndices _ExteriorVertexIndices = new VertexIndices();

        private static void ClipNearPlane(Render render, GenPrimitive genPrimitive) {
            if ((BitwiseOrClipFlags(genPrimitive.ClipFlags) & ClipFlags.Near) != ClipFlags.Near) {
                return;
            }

            GenPrimitive.ClearTags(genPrimitive);

            if (genPrimitive.VertexCount == 3) {
                ClipTriangleGenPrimitiveNearPlane(render, genPrimitive);
            } else if (genPrimitive.VertexCount == 4) {
                ClipQuadGenPrimitiveNearPlane(render, genPrimitive);
            }
        }

        private static void ClipTriangleGenPrimitiveNearPlane(Render render, GenPrimitive genPrimitive) {
            CalculateInteriorExteriorVertices(genPrimitive, _InteriorVertexIndices, _ExteriorVertexIndices);

            switch (_InteriorVertexIndices.Count) {
                case 1 when (_ExteriorVertexIndices.Count == 2):
                    ClipTriangleGenPrimitiveNearPlaneCase1(render, genPrimitive, _InteriorVertexIndices, _ExteriorVertexIndices);
                    break;
                case 2 when (_ExteriorVertexIndices.Count == 1):
                    ClipTriangleGenPrimitiveNearPlaneCase2(render, genPrimitive, _InteriorVertexIndices, _ExteriorVertexIndices);
                    break;
                default:
                    throw new Exception("Unknown case");
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

            Texcoord interiorTexcoord = genPrimitive.Texcoords[interiorIndex];
            Texcoord exteriorT1 = genPrimitive.Texcoords[exteriorIndex1];
            Texcoord exteriorT2 = genPrimitive.Texcoords[exteriorIndex2];

            genPrimitive.Texcoords[exteriorIndex1] = ClipLerpTexcoord(render, interiorTexcoord, exteriorT1, t1);
            genPrimitive.Texcoords[exteriorIndex2] = ClipLerpTexcoord(render, interiorTexcoord, exteriorT2, t2);

            genPrimitive.GouraudShadingColorBuffer[0] = Rgb888.Yellow;
            genPrimitive.GouraudShadingColorBuffer[1] = Rgb888.Yellow;
            genPrimitive.GouraudShadingColorBuffer[2] = Rgb888.Yellow;

            GenPrimitive.SetTags(genPrimitive, GenPrimitiveTags.ClipTriCase1);

            // Console.WriteLine($"Case 1: [1;31m{interiorVertex}[m; [1;32m{exteriorV1}[m; [1;33m{exteriorV2}[m ----> [1;31m{genPrimitive.ViewPoints[vertexIndices[0]]}[m; [1;32m{genPrimitive.ViewPoints[vertexIndices[1]]}[m; [1;33m{genPrimitive.ViewPoints[vertexIndices[2]]}[m");
        }

        private static void ClipTriangleGenPrimitiveNearPlaneCase2(Render render, GenPrimitive genPrimitive, VertexIndices interiorIndices, VertexIndices exteriorIndices) {
            // Case 2: Two interior vertices and one exterior vertex
            int exteriorIndex = _ExteriorVertexIndices.Indices[0];
            AdjacencyList adjacencyList = _TriAdjacencyTable[exteriorIndex];
            int interiorIndex1 = adjacencyList.Vertices[0];
            int interiorIndex2 = adjacencyList.Vertices[1];

            GenPrimitive newGenPrimitive = render.AcquireGenPrimitive();

            GenPrimitive.Copy(genPrimitive, newGenPrimitive);
            GenPrimitive.CopyTextureAttribs(genPrimitive, newGenPrimitive);

            Vector3 exteriorVertex = genPrimitive.ViewPoints[exteriorIndex];
            Vector3 interiorV1 = genPrimitive.ViewPoints[interiorIndex1];
            Vector3 interiorV2 = genPrimitive.ViewPoints[interiorIndex2];

            float t1 = FindClipLerpEdgeT(render, interiorV1, exteriorVertex);
            float t2 = FindClipLerpEdgeT(render, interiorV2, exteriorVertex);

            Vector3 lerpedV1 = ClipLerpVertex(render, interiorV1, exteriorVertex, t1);
            Vector3 lerpedV2 = ClipLerpVertex(render, interiorV2, exteriorVertex, t2);

            // Generate two points and from that, pass in the quad
            genPrimitive.ViewPoints[exteriorIndex] = lerpedV1;

            newGenPrimitive.ViewPoints[exteriorIndex] = lerpedV1;
            newGenPrimitive.ViewPoints[interiorIndex1] = interiorV2;
            newGenPrimitive.ViewPoints[interiorIndex2] = lerpedV2;

            Texcoord exteriorTexcoord = genPrimitive.Texcoords[exteriorIndex];
            Texcoord interiorT1 = genPrimitive.Texcoords[interiorIndex1];
            Texcoord interiorT2 = genPrimitive.Texcoords[interiorIndex2];

            genPrimitive.Texcoords[exteriorIndex] = ClipLerpTexcoord(render, interiorT1, exteriorTexcoord, t1);

            newGenPrimitive.Texcoords[exteriorIndex] = genPrimitive.Texcoords[exteriorIndex];
            newGenPrimitive.Texcoords[interiorIndex1] = genPrimitive.Texcoords[interiorIndex2];
            newGenPrimitive.Texcoords[interiorIndex2] = ClipLerpTexcoord(render, interiorT2, exteriorTexcoord, t2);

            genPrimitive.GouraudShadingColorBuffer[0] = Rgb888.Orange;
            genPrimitive.GouraudShadingColorBuffer[1] = Rgb888.Orange;
            genPrimitive.GouraudShadingColorBuffer[2] = Rgb888.Orange;

            newGenPrimitive.GouraudShadingColorBuffer[0] = Rgb888.Cyan;
            newGenPrimitive.GouraudShadingColorBuffer[1] = Rgb888.Cyan;
            newGenPrimitive.GouraudShadingColorBuffer[2] = Rgb888.Cyan;

            GenPrimitive.SetTags(genPrimitive, GenPrimitiveTags.ClipTriCase2);
            GenPrimitive.SetTags(newGenPrimitive, GenPrimitiveTags.ClipTriCase2);

            // Console.WriteLine($"Case 2 (1st tri): [1;31m{exteriorVertex}[m; [1;32m{interiorV1}[m; [1;33m{interiorV2}[m ----> [1;31m{genPrimitive.ViewPoints[exteriorIndex]}[m; [1;32m{genPrimitive.ViewPoints[interiorIndex1]}[m; [1;33m{genPrimitive.ViewPoints[interiorIndex2]}[m");
            // Console.WriteLine($"Case 2 (2nd tri): [1;31m{exteriorVertex}[m; [1;32m{interiorV1}[m; [1;33m{interiorV2}[m ----> [1;31m{newGenPrimitive.ViewPoints[exteriorIndex]}[m; [1;32m{newGenPrimitive.ViewPoints[interiorIndex1]}[m; [1;33m{newGenPrimitive.ViewPoints[interiorIndex2]}[m");
        }

        private static void ClipQuadGenPrimitiveNearPlane(Render render, GenPrimitive genPrimitive) {
            CalculateInteriorExteriorVertices(genPrimitive, _InteriorVertexIndices, _ExteriorVertexIndices);

            switch (_InteriorVertexIndices.Count) {
                case 1 when (_ExteriorVertexIndices.Count == 3):
                    ClipQuadGenPrimitiveNearPlaneCase1(render, genPrimitive, _InteriorVertexIndices, _ExteriorVertexIndices);
                    break;
                case 2 when (_ExteriorVertexIndices.Count == 2):
                    ClipQuadGenPrimitiveNearPlaneCase2(render, genPrimitive, _InteriorVertexIndices, _ExteriorVertexIndices);
                    break;
                case 3 when (_ExteriorVertexIndices.Count == 1):
                    ClipQuadGenPrimitiveNearPlaneCase3(render, genPrimitive, _InteriorVertexIndices, _ExteriorVertexIndices);
                    break;
                default:
                    throw new Exception("Unknown case");
            }
        }

        private static void ClipQuadGenPrimitiveNearPlaneCase1(Render render, GenPrimitive genPrimitive, VertexIndices interiorIndices, VertexIndices exteriorIndices) {
            // Console.WriteLine("Clip Quad Case I");

            //     I
            //    / \
            //---A---B--- A=Lerp(I,E1); B=Lerp(I,E2) and degenerate to a triangle
            //  /     \
            // E1      E2
            //  \     /
            //   \   /
            //    \ /
            //     E3

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

            Rgb888 interiorGsc = genPrimitive.GouraudShadingColors[interiorIndex];
            Rgb888 exteriorGsc1 = genPrimitive.GouraudShadingColors[exteriorIndex1];
            Rgb888 exteriorGsc2 = genPrimitive.GouraudShadingColors[exteriorIndex2];

            genPrimitive.GouraudShadingColors[0] = interiorGsc;
            genPrimitive.GouraudShadingColors[1] = ClipLerpGouraudShadingColor(render, interiorGsc, exteriorGsc1, t1);
            genPrimitive.GouraudShadingColors[2] = ClipLerpGouraudShadingColor(render, interiorGsc, exteriorGsc2, t2);

            Texcoord interiorTexcoord = genPrimitive.Texcoords[interiorIndex];
            Texcoord exteriorT1 = genPrimitive.Texcoords[exteriorIndex1];
            Texcoord exteriorT2 = genPrimitive.Texcoords[exteriorIndex2];

            genPrimitive.Texcoords[0] = interiorTexcoord;
            genPrimitive.Texcoords[1] = ClipLerpTexcoord(render, interiorTexcoord, exteriorT1, t1);
            genPrimitive.Texcoords[2] = ClipLerpTexcoord(render, interiorTexcoord, exteriorT2, t2);

            genPrimitive.Type = (PsyQ.TmdPrimitiveType)(genPrimitive.Type - PsyQ.TmdPrimitiveType.F4);
            genPrimitive.VertexCount = 3;
            genPrimitive.NormalCount = (genPrimitive.NormalCount >= 4) ? 3 : genPrimitive.NormalCount;

            GenPrimitive.SetTags(genPrimitive, GenPrimitiveTags.ClipQuadCase1);
        }

        private static void ClipQuadGenPrimitiveNearPlaneCase2(Render render, GenPrimitive genPrimitive, VertexIndices interiorIndices, VertexIndices exteriorIndices) {
            // Console.WriteLine("Clip Quad Case II");

            // I1-----I2
            //  |     |
            //--A-----B-- A=Lerp(I1,E1); B=Lerp(I2,E2)
            //  |     |
            // E1-----E2
            //
            //
            //
            //

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

            ref Rgb888 interiorGsc1 = ref genPrimitive.GouraudShadingColors[interiorIndex1];
            ref Rgb888 interiorGsc2 = ref genPrimitive.GouraudShadingColors[interiorIndex2];
            ref Rgb888 exteriorGsc1 = ref genPrimitive.GouraudShadingColors[exteriorIndex1];
            ref Rgb888 exteriorGsc2 = ref genPrimitive.GouraudShadingColors[exteriorIndex2];

            exteriorGsc1 = ClipLerpGouraudShadingColor(render, interiorGsc1, exteriorGsc1, t1);
            exteriorGsc2 = ClipLerpGouraudShadingColor(render, interiorGsc2, exteriorGsc2, t2);

            ref Texcoord interiorT1 = ref genPrimitive.Texcoords[interiorIndex1];
            ref Texcoord interiorT2 = ref genPrimitive.Texcoords[interiorIndex2];
            ref Texcoord exteriorT1 = ref genPrimitive.Texcoords[exteriorIndex1];
            ref Texcoord exteriorT2 = ref genPrimitive.Texcoords[exteriorIndex2];

            exteriorT1 = ClipLerpTexcoord(render, interiorT1, exteriorT1, t1);
            exteriorT2 = ClipLerpTexcoord(render, interiorT2, exteriorT2, t2);

            GenPrimitive.SetTags(genPrimitive, GenPrimitiveTags.ClipQuadCase2);

            // Console.WriteLine($"Quad ( after): [1;31m{interiorV1}[m; [1;32m{interiorV2}[m; [1;33m{exteriorV1}[m; [1;34m{exteriorV2}[m {color}");
        }

        private static void ClipQuadGenPrimitiveNearPlaneCase3(Render render, GenPrimitive genPrimitive, VertexIndices interiorIndices, VertexIndices exteriorIndices) {
            // Console.WriteLine("Clip Quad Case III");

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

            Rgb888 lerpedGsc1 = ClipLerpGouraudShadingColor(render, exteriorGsc, interiorGsc, t1);
            Rgb888 lerpedGsc2 = ClipLerpGouraudShadingColor(render, exteriorGsc, opposingInteriorGsc, t2);

            Texcoord lerpedT1 = ClipLerpTexcoord(render, exteriorTexcoord, interiorTexcoord, t1);
            Texcoord lerpedT2 = ClipLerpTexcoord(render, exteriorTexcoord, opposingInteriorTexcoord, t2);

            genPrimitive.ViewPoints[0] = interiorVertex;
            genPrimitive.ViewPoints[1] = opposingInteriorVertex;
            genPrimitive.ViewPoints[2] = lerpedV1;
            genPrimitive.ViewPoints[3] = lerpedV2;

            genPrimitive.GouraudShadingColors[0] = interiorGsc;
            genPrimitive.GouraudShadingColors[1] = opposingInteriorGsc;
            genPrimitive.GouraudShadingColors[2] = lerpedGsc1;
            genPrimitive.GouraudShadingColors[3] = lerpedGsc2;

            genPrimitive.Texcoords[0] = interiorTexcoord;
            genPrimitive.Texcoords[1] = opposingInteriorTexcoord;
            genPrimitive.Texcoords[2] = lerpedT1;
            genPrimitive.Texcoords[3] = lerpedT2;

            // Build the triangle above the near plane
            GenPrimitive triGenPrimitive = render.AcquireGenPrimitive();
            GenPrimitive.CopyTextureAttribs(genPrimitive, triGenPrimitive);

            triGenPrimitive.Type = (PsyQ.TmdPrimitiveType)(genPrimitive.Type - PsyQ.TmdPrimitiveType.F4);
            triGenPrimitive.VertexCount = 3;
            triGenPrimitive.NormalCount = (genPrimitive.NormalCount >= 4) ? 3 : genPrimitive.NormalCount;

            triGenPrimitive.ViewPoints[0] = interiorVertex;
            triGenPrimitive.ViewPoints[1] = opposingInteriorVertex;
            triGenPrimitive.ViewPoints[2] = middleInteriorVertex;

            triGenPrimitive.GouraudShadingColors[0] = interiorGsc;
            triGenPrimitive.GouraudShadingColors[1] = opposingInteriorGsc;
            triGenPrimitive.GouraudShadingColors[2] = middleInteriorGsc;

            triGenPrimitive.Texcoords[0] = interiorTexcoord;
            triGenPrimitive.Texcoords[1] = opposingInteriorTexcoord;
            triGenPrimitive.Texcoords[2] = middleInteriorTexcoord;

            GenPrimitive.SetTags(genPrimitive, GenPrimitiveTags.ClipQuadCase3);
            GenPrimitive.SetTags(triGenPrimitive, GenPrimitiveTags.ClipQuadCase3);
        }

        private static void CalculateInteriorExteriorVertices(GenPrimitive genPrimitive, VertexIndices interiorVertices, VertexIndices exteriorVertices) {
            // Determine which case to cover and find the interior/exterior vertices
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

        private static void GenerateClipFlags(Render render, GenPrimitive genPrimitive) {
            // XXX: This should be using render.Camera.ViewDistance
            float zFactor = 1f / System.MathF.Tan(MathHelper.DegreesToRadians(render.Camera.Fov * 0.5f));

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
                float zTest = zFactor * genPrimitive.ViewPoints[i].Z;

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

        private static void GenerateNearPlaneClipFlags(Render render, GenPrimitive genPrimitive) {
            for (int i = 0; i < genPrimitive.VertexCount; i++) {
                if (genPrimitive.ViewPoints[i].Z < render.Camera.DepthNear) {
                    genPrimitive.ClipFlags[i] |= ClipFlags.Near;
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
