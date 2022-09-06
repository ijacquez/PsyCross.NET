using System;
using System.Numerics;
using PsyCross.Math;

namespace PsyCross.Testing.Rendering {
    public static partial class Renderer {
        private static void ClipNearPlane(Render render, GenPrimitive genPrimitive) {
            if ((BitwiseOrClipFlags(genPrimitive.ClipFlags) & ClipFlags.Near) != ClipFlags.Near) {
                return;
            }

            if (genPrimitive.VertexCount == 3) {
                ClipTriangleGenPrimitiveNearPlane(render, genPrimitive);
            } else if (genPrimitive.VertexCount == 4) {
                ClipQuadGenPrimitiveNearPlane(render, genPrimitive);
            }
        }

        private class VertexIndices {
            public int[] IndexBuffer { get; } = new int[4];
            public int Count { get; set; }

            public Span<int> Indices => new Span<int>(IndexBuffer, 0, Count);
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

        private static VertexIndices _InteriorVertexIndices = new VertexIndices();
        private static VertexIndices _ExteriorVertexIndices = new VertexIndices();

        private static void ClipQuadGenPrimitiveNearPlane(Render render, GenPrimitive genPrimitive) {
            CalculateInteriorExteriorVertices(genPrimitive, _InteriorVertexIndices, _ExteriorVertexIndices);

            switch (_InteriorVertexIndices.Count) {
                case 1:
                    ClipQuadGenPrimitiveNearPlaneCase1(render, genPrimitive, _InteriorVertexIndices, _ExteriorVertexIndices);
                    break;
                case 2:
                    ClipQuadGenPrimitiveNearPlaneCase2(render, genPrimitive, _InteriorVertexIndices, _ExteriorVertexIndices);
                    break;
                case 3:
                    ClipQuadGenPrimitiveNearPlaneCase3(render, genPrimitive, _InteriorVertexIndices, _ExteriorVertexIndices);
                    break;
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

            ref Vector3 interiorV1 = ref genPrimitive.ViewPoints[interiorIndices.Indices[0]];
            ref Vector3 exteriorV1 = ref genPrimitive.ViewPoints[exteriorIndices.Indices[0]];
            ref Vector3 exteriorV2 = ref genPrimitive.ViewPoints[exteriorIndices.Indices[1]];

            float t1 = FindClipLerpEdgeT(render, interiorV1, exteriorV1);
            float t2 = FindClipLerpEdgeT(render, interiorV1, exteriorV2);

            exteriorV1 = ClipLerpVertex(render, interiorV1, exteriorV1, t1);
            exteriorV2 = ClipLerpVertex(render, interiorV1, exteriorV2, t2);

            ref Rgb888 interiorGsc1 = ref genPrimitive.GouraudShadingColors[interiorIndices.Indices[0]];
            ref Rgb888 exteriorGsc1 = ref genPrimitive.GouraudShadingColors[exteriorIndices.Indices[0]];
            ref Rgb888 exteriorGsc2 = ref genPrimitive.GouraudShadingColors[exteriorIndices.Indices[1]];

            exteriorGsc1 = ClipLerpGouraudShadingColor(render, interiorGsc1, exteriorGsc1, t1);
            exteriorGsc2 = ClipLerpGouraudShadingColor(render, interiorGsc1, exteriorGsc2, t2);

            ref Texcoord interiorT1 = ref genPrimitive.Texcoords[interiorIndices.Indices[0]];
            ref Texcoord exteriorT1 = ref genPrimitive.Texcoords[exteriorIndices.Indices[0]];
            ref Texcoord exteriorT2 = ref genPrimitive.Texcoords[exteriorIndices.Indices[1]];

            exteriorT1 = ClipLerpTexcoord(render, interiorT1, exteriorT1, t1);
            exteriorT2 = ClipLerpTexcoord(render, interiorT1, exteriorT2, t2);

            genPrimitive.Type = (PsyQ.TmdPrimitiveType)(genPrimitive.Type - PsyQ.TmdPrimitiveType.F4);
            genPrimitive.VertexCount = 3;
            genPrimitive.NormalCount = (genPrimitive.NormalCount >= 4) ? 3 : genPrimitive.NormalCount;
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

            ref Vector3 interiorV1 = ref genPrimitive.ViewPoints[interiorIndices.Indices[0]];
            ref Vector3 interiorV2 = ref genPrimitive.ViewPoints[interiorIndices.Indices[1]];
            ref Vector3 exteriorV1 = ref genPrimitive.ViewPoints[exteriorIndices.Indices[0]];
            ref Vector3 exteriorV2 = ref genPrimitive.ViewPoints[exteriorIndices.Indices[1]];

            float t1 = FindClipLerpEdgeT(render, interiorV1, exteriorV1);
            float t2 = FindClipLerpEdgeT(render, interiorV2, exteriorV2);

            exteriorV1 = ClipLerpVertex(render, interiorV1, exteriorV1, t1);
            exteriorV2 = ClipLerpVertex(render, interiorV2, exteriorV2, t2);

            ref Rgb888 interiorGsc1 = ref genPrimitive.GouraudShadingColors[interiorIndices.Indices[0]];
            ref Rgb888 interiorGsc2 = ref genPrimitive.GouraudShadingColors[interiorIndices.Indices[1]];
            ref Rgb888 exteriorGsc1 = ref genPrimitive.GouraudShadingColors[exteriorIndices.Indices[0]];
            ref Rgb888 exteriorGsc2 = ref genPrimitive.GouraudShadingColors[exteriorIndices.Indices[1]];

            exteriorGsc1 = ClipLerpGouraudShadingColor(render, interiorGsc1, exteriorGsc1, t1);
            exteriorGsc2 = ClipLerpGouraudShadingColor(render, interiorGsc2, exteriorGsc2, t2);

            ref Texcoord interiorT1 = ref genPrimitive.Texcoords[interiorIndices.Indices[0]];
            ref Texcoord interiorT2 = ref genPrimitive.Texcoords[interiorIndices.Indices[1]];
            ref Texcoord exteriorT1 = ref genPrimitive.Texcoords[exteriorIndices.Indices[0]];
            ref Texcoord exteriorT2 = ref genPrimitive.Texcoords[exteriorIndices.Indices[1]];

            exteriorT1 = ClipLerpTexcoord(render, interiorT1, exteriorT1, t1);
            exteriorT2 = ClipLerpTexcoord(render, interiorT2, exteriorT2, t2);

            // Console.WriteLine($"Quad ( after): [1;31m{interiorV1}[m; [1;32m{interiorV2}[m; [1;33m{exteriorV1}[m; [1;34m{exteriorV2}[m {color}");
        }

        private static void ClipQuadGenPrimitiveNearPlaneCase3(Render render, GenPrimitive genPrimitive, VertexIndices interiorVertices, VertexIndices exteriorVertices) {
            // Console.WriteLine("Clip Quad Case III");

            Span<ClipFlags> tri1ClipFlags = stackalloc ClipFlags[3];
            Span<ClipFlags> tri2ClipFlags = stackalloc ClipFlags[3];

            // Before triangulating the quad primitive, first check if
            // it's at all clipping the near plane
            TriangulateQuadOrder(genPrimitive.ClipFlags, tri1ClipFlags, tri2ClipFlags);

            ClipFlags tri1ClipFlagOrMask = BitwiseOrClipFlags(tri1ClipFlags);
            ClipFlags tri2ClipFlagOrMask = BitwiseOrClipFlags(tri2ClipFlags);

            // Have the current generated primitive be the first triangle
            GenPrimitive tri1GenPrimitive = genPrimitive;
            GenPrimitive tri2GenPrimitive = render.AcquireGenPrimitive();

            Span<Vector3> tri1ViewPoints = tri1GenPrimitive.ViewPoints;
            Span<Vector3> tri2ViewPoints = tri2GenPrimitive.ViewPoints;

            Span<Rgb888> tri1GouraudShadingColors = tri1GenPrimitive.GouraudShadingColors;
            Span<Rgb888> tri2GouraudShadingColors = tri2GenPrimitive.GouraudShadingColors;

            Span<Texcoord> tri1Texcoords = tri1GenPrimitive.Texcoords;
            Span<Texcoord> tri2Texcoords = tri2GenPrimitive.Texcoords;

            TriangulateQuadOrder(genPrimitive.ViewPoints, tri1ViewPoints, tri2ViewPoints);
            TriangulateQuadOrder(genPrimitive.GouraudShadingColors, tri1GouraudShadingColors, tri2GouraudShadingColors);
            TriangulateQuadOrder(genPrimitive.Texcoords, tri1Texcoords, tri2Texcoords);

            tri1ClipFlags.CopyTo(tri1GenPrimitive.ClipFlags);
            tri2ClipFlags.CopyTo(tri2GenPrimitive.ClipFlags);

            tri1GenPrimitive.Type = (PsyQ.TmdPrimitiveType)(tri1GenPrimitive.Type - PsyQ.TmdPrimitiveType.F4);

            tri1GenPrimitive.VertexCount = 3;
            tri1GenPrimitive.NormalCount = (tri1GenPrimitive.NormalCount >= 4) ? 3 : tri1GenPrimitive.NormalCount;

            tri2GenPrimitive.Flags = tri1GenPrimitive.Flags;
            tri2GenPrimitive.Type = tri1GenPrimitive.Type;
            tri2GenPrimitive.VertexCount = tri1GenPrimitive.VertexCount;
            tri2GenPrimitive.NormalCount = tri1GenPrimitive.NormalCount;
            tri2GenPrimitive.FaceNormal = tri1GenPrimitive.FaceNormal;
            tri2GenPrimitive.TPageId = tri1GenPrimitive.TPageId;
            tri2GenPrimitive.ClutId = tri1GenPrimitive.ClutId;

            ClipFlags tri1ClipFlagAndMask = BitwiseAndClipFlags(tri1ClipFlags);
            ClipFlags tri2ClipFlagAndMask = BitwiseAndClipFlags(tri2ClipFlags);

            // Consider the case when a little over half the quad
            // intersects the near plane. When triangulated, one
            // triangle is now intersecting the near plane while the
            // other is completely behind the near plane. At this
            // point, we need to cull the triangle completely
            if (tri1ClipFlagAndMask != ClipFlags.None) {
                GenPrimitive.Discard(tri1GenPrimitive);
            } else if ((tri1ClipFlagOrMask & ClipFlags.Near) == ClipFlags.Near) {
                // Console.WriteLine("Clipping Tri1");
                ClipTriangleGenPrimitiveNearPlane(render, tri1GenPrimitive);
            }

            if (tri2ClipFlagAndMask != ClipFlags.None) {
                GenPrimitive.Discard(tri2GenPrimitive);
            } else if ((tri2ClipFlagOrMask & ClipFlags.Near) == ClipFlags.Near) {
                // Console.WriteLine("Clipping Tri2");
                ClipTriangleGenPrimitiveNearPlane(render, tri2GenPrimitive);
            }
        }

        private static void ClipTriangleGenPrimitiveNearPlane(Render render, GenPrimitive genPrimitive) {
            CalculateInteriorExteriorVertices(genPrimitive, _InteriorVertexIndices, _ExteriorVertexIndices);

            // Case 1: One interior vertex and two exterior vertices
            if (_InteriorVertexIndices.Count == 1) {
                ReadOnlySpan<int> vertexIndices = new int[3] {
                     _InteriorVertexIndices.Indices[0],
                    (_InteriorVertexIndices.Indices[0] + 1) % genPrimitive.VertexCount,
                    (_InteriorVertexIndices.Indices[0] + 2) % genPrimitive.VertexCount
                };

                Vector3 interiorVertex = genPrimitive.ViewPoints[vertexIndices[0]];
                Vector3 exteriorV1 = genPrimitive.ViewPoints[vertexIndices[1]];
                Vector3 exteriorV2 = genPrimitive.ViewPoints[vertexIndices[2]];

                // Interpolate between edge (v0,v1) and find the point along the
                // edge that intersects with the near plane

                float t1 = FindClipLerpEdgeT(render, interiorVertex, exteriorV1);
                float t2 = FindClipLerpEdgeT(render, interiorVertex, exteriorV2);

                // Overwrite vertices
                genPrimitive.ViewPoints[vertexIndices[1]] = ClipLerpVertex(render, interiorVertex, exteriorV1, t1);
                genPrimitive.ViewPoints[vertexIndices[2]] = ClipLerpVertex(render, interiorVertex, exteriorV2, t2);

                Texcoord interiorTexcoord = genPrimitive.Texcoords[vertexIndices[0]];
                Texcoord exteriorT1 = genPrimitive.Texcoords[vertexIndices[1]];
                Texcoord exteriorT2 = genPrimitive.Texcoords[vertexIndices[2]];

                genPrimitive.Texcoords[vertexIndices[1]] = ClipLerpTexcoord(render, interiorTexcoord, exteriorT1, t1);
                genPrimitive.Texcoords[vertexIndices[2]] = ClipLerpTexcoord(render, interiorTexcoord, exteriorT2, t2);

                // Console.WriteLine($"Case 1: [1;31m{interiorVertex}[m; [1;32m{exteriorV1}[m; [1;33m{exteriorV2}[m ----> [1;31m{genPrimitive.ViewPoints[vertexIndices[0]]}[m; [1;32m{genPrimitive.ViewPoints[vertexIndices[1]]}[m; [1;33m{genPrimitive.ViewPoints[vertexIndices[2]]}[m");
            } else { // Case 2: Two interior vertices and one exterior vertex
                ReadOnlySpan<int> vertexIndices = stackalloc int[3] {
                    _ExteriorVertexIndices.Indices[0],
                   (_ExteriorVertexIndices.Indices[0] + 1) % genPrimitive.VertexCount,
                   (_ExteriorVertexIndices.Indices[0] + 2) % genPrimitive.VertexCount
                };

                GenPrimitive newGenPrimitive = render.AcquireGenPrimitive();

                GenPrimitive.Copy(genPrimitive, newGenPrimitive);

                Vector3 exteriorVertex = genPrimitive.ViewPoints[vertexIndices[0]];
                Vector3 interiorV1 = genPrimitive.ViewPoints[vertexIndices[1]];
                Vector3 interiorV2 = genPrimitive.ViewPoints[vertexIndices[2]];

                float t1 = FindClipLerpEdgeT(render, interiorV1, exteriorVertex);
                float t2 = FindClipLerpEdgeT(render, interiorV2, exteriorVertex);

                Vector3 lerpedV1 = ClipLerpVertex(render, interiorV1, exteriorVertex, t1);
                Vector3 lerpedV2 = ClipLerpVertex(render, interiorV2, exteriorVertex, t2);

                // Generate two points and from that, pass in the quad
                genPrimitive.ViewPoints[vertexIndices[0]] = lerpedV1;

                newGenPrimitive.ViewPoints[vertexIndices[0]] = lerpedV1;
                newGenPrimitive.ViewPoints[vertexIndices[1]] = interiorV2;
                newGenPrimitive.ViewPoints[vertexIndices[2]] = lerpedV2;

                Texcoord exteriorTexcoord = genPrimitive.Texcoords[vertexIndices[0]];
                Texcoord interiorT1 = genPrimitive.Texcoords[vertexIndices[1]];
                Texcoord interiorT2 = genPrimitive.Texcoords[vertexIndices[2]];

                genPrimitive.Texcoords[vertexIndices[0]] = ClipLerpTexcoord(render, interiorT1, exteriorTexcoord, t1);

                newGenPrimitive.Texcoords[vertexIndices[0]] = genPrimitive.Texcoords[vertexIndices[0]];
                newGenPrimitive.Texcoords[vertexIndices[1]] = genPrimitive.Texcoords[vertexIndices[2]];
                newGenPrimitive.Texcoords[vertexIndices[2]] = ClipLerpTexcoord(render, interiorT2, exteriorTexcoord, t2);

                // Console.WriteLine($"Case 2 (1st tri): [1;31m{exteriorVertex}[m; [1;32m{interiorV1}[m; [1;33m{interiorV2}[m ----> [1;31m{genPrimitive.ViewPoints[vertexIndices[0]]}[m; [1;32m{genPrimitive.ViewPoints[vertexIndices[1]]}[m; [1;33m{genPrimitive.ViewPoints[vertexIndices[2]]}[m");
                // Console.WriteLine($"Case 2 (2nd tri): [1;31m{exteriorVertex}[m; [1;32m{interiorV1}[m; [1;33m{interiorV2}[m ----> [1;31m{newGenPrimitive.ViewPoints[vertexIndices[0]]}[m; [1;32m{newGenPrimitive.ViewPoints[vertexIndices[1]]}[m; [1;33m{newGenPrimitive.ViewPoints[vertexIndices[2]]}[m");
            }
        }

        private static float FindClipLerpEdgeT(Render render, Vector3 aVertex, Vector3 bVertex) =>
            System.Math.Clamp((render.Camera.DepthNear - aVertex.Z) / (bVertex.Z - aVertex.Z), 0f, 1f);

        private static Vector3 ClipLerpVertex(Render render, Vector3 aVertex, Vector3 bVertex, float t) {
            Vector3 lerpedVertex = Vector3.Lerp(aVertex, bVertex, t);

            lerpedVertex.Z = render.Camera.DepthNear;

            return lerpedVertex;
        }

        private static Rgb888 ClipLerpGouraudShadingColor(Render render, Rgb888 aRgb888, Rgb888 bRgb888, float t) {
            Vector3 aVector = (Vector3)aRgb888;
            Vector3 bVector = (Vector3)bRgb888;
            Vector3 lerpedVertex = Vector3.Lerp(aVector, bVector, t);

            return (Rgb888)lerpedVertex;
        }

        private static Texcoord ClipLerpTexcoord(Render render, Texcoord aTexcoord, Texcoord bTexcoord, float t) {
            Vector2 aVector = new Vector2(aTexcoord.X, aTexcoord.Y);
            Vector2 bVector = new Vector2(bTexcoord.X, bTexcoord.Y);
            Vector2 lerpedVertex = Vector2.Lerp(aVector, bVector, t);

            return new Texcoord((byte)lerpedVertex.X, (byte)lerpedVertex.Y);
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
                } else {
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
