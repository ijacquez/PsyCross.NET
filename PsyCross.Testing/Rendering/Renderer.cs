using System;
using System.Numerics;
using PsyCross.Math;

namespace PsyCross.Testing.Rendering {
    public static class Renderer {
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

                // if (genPrimitive.NormalCount == 0) {
                    genPrimitive.FaceNormal = CalculateScaledFaceNormal(genPrimitive.ViewPoints);
                // } else {
                //     genPrimitive.FaceNormal = genPrimitive.PolygonNormals[0];
                // }

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


                if (genPrimitive.VertexCount == 3) {
                    genPrimitive.Type = PsyQ.TmdPrimitiveType.G3;
                } else {
                    genPrimitive.Type = PsyQ.TmdPrimitiveType.G4;
                }
                genPrimitive.GouraudShadingColorBuffer[0] = GetColor(packetIndex);
                genPrimitive.GouraudShadingColorBuffer[1] = GetColor(packetIndex+1);
                genPrimitive.GouraudShadingColorBuffer[2] = GetColor(packetIndex+2);
                genPrimitive.GouraudShadingColorBuffer[3] = GetColor(packetIndex+3);

                genPrimitive.FaceNormal = Vector3.Normalize(genPrimitive.FaceNormal);

                TransformToWorld(render, genPrimitive);

                // CalculateFog(render, genPrimitive);

                // Perform light source calculation
                // XXX: Change this to check lighting from a property getter
                if ((tmdPacket.PrimitiveHeader.Flags & PsyQ.TmdPrimitiveFlags.Lgt) != PsyQ.TmdPrimitiveFlags.Lgt) {
                    // CalculateLighting(render, genPrimitive);
                }

                SubdivideGenPrimitive(render, genPrimitive);

                foreach (GenPrimitive currentGenPrimitive in render.GenPrimitives) {
                    if ((currentGenPrimitive.Flags & GenPrimitiveFlags.Discarded) == GenPrimitiveFlags.Discarded) {
                        continue;
                    }

                    TransformToScreen(render, currentGenPrimitive);

                    if (TestScreenPointOverflow(currentGenPrimitive)) {
                        // Console.WriteLine("[1;31mOverflow[m");
                        continue;
                    }

                    // if (TestScreenPrimitiveArea(currentGenPrimitive)) {
                    //     // Console.WriteLine("[1;31mArea<=0[m");
                    //     continue;
                    // }

                    DrawGenPrimitive(render, currentGenPrimitive);
                }
            }
        }

        // XXX: Move to a file?
        private struct SubdivTriple {
            public Vector3 ViewPoint { get; set; }

            public Rgb888 GouraudShadingColor { get; set; }

            public Texcoord Texcoord { get; set; }

            public static SubdivTriple FromGenPrimitive(GenPrimitive genPrimitive, int index) =>
                new SubdivTriple {
                ViewPoint           = genPrimitive.ViewPoints[index],
                GouraudShadingColor = genPrimitive.GouraudShadingColors[index],
                Texcoord            = genPrimitive.Texcoords[index]
            };
        }

        private static void SubdivideGenPrimitive(Render render, GenPrimitive genPrimitive) {
            // Get the distance from the primitive and calculate the
            // subdivision level
            //

            // XXX: Move to a method that gets you the min/max/center of a primitive
            Vector3 minViewPoint = Vector3.Min(genPrimitive.ViewPoints[0],
                                               Vector3.Min(genPrimitive.ViewPoints[1],
                                                           genPrimitive.ViewPoints[2]));

            Vector3 distance = minViewPoint;

            int subdivLevel = 0;

            // XXX: Move the 1f value somewhere... Render maybe?
            if (distance.Z < 10f) {
                subdivLevel = 2;
            } else if (distance.Z < 5f) {
                subdivLevel = 3;
            }

            if (subdivLevel > 0) {
                if (genPrimitive.VertexCount == 3) {
                    SubdivideTriangleGenPrimitive(render,
                                                  genPrimitive,
                                                  SubdivTriple.FromGenPrimitive(genPrimitive, 0),
                                                  SubdivTriple.FromGenPrimitive(genPrimitive, 1),
                                                  SubdivTriple.FromGenPrimitive(genPrimitive, 2),
                                                  subdivLevel);
                } else {
                    SubdivideQuadGenPrimitive(render,
                                              genPrimitive,
                                              SubdivTriple.FromGenPrimitive(genPrimitive, 0),
                                              SubdivTriple.FromGenPrimitive(genPrimitive, 1),
                                              SubdivTriple.FromGenPrimitive(genPrimitive, 2),
                                              SubdivTriple.FromGenPrimitive(genPrimitive, 3),
                                              subdivLevel);
                }

                GenPrimitive.Discard(genPrimitive);
            }
        }

        private static void SubdivideTriangleGenPrimitive(Render render,
                                                          GenPrimitive baseGenPrimitive,
                                                          SubdivTriple spa,
                                                          SubdivTriple spb,
                                                          SubdivTriple spc,
                                                          int level) {
            if (level == 0) {
                GenPrimitive genPrimitive = render.AcquireGenPrimitive();

                // XXX: Clean this up
                // XXX: Slow... copies more than what we need
                GenPrimitive.Copy(baseGenPrimitive, genPrimitive);

                genPrimitive.ViewPoints[0] = spa.ViewPoint;
                genPrimitive.ViewPoints[1] = spb.ViewPoint;
                genPrimitive.ViewPoints[2] = spc.ViewPoint;

                GenPrimitive.ClearClipFlags(genPrimitive);

                GenerateNearPlaneClipFlags(render, genPrimitive);

                if (TestOutsideFustrum(genPrimitive)) {
                    // Console.WriteLine($"---------------- Subdiv Cull ---------------- {genPrimitive.ClipFlags[0]} & {genPrimitive.ClipFlags[1]} & {genPrimitive.ClipFlags[2]} -> {genPrimitive.ViewPoints[0]}; {genPrimitive.ViewPoints[1]}; {genPrimitive.ViewPoints[2]}");
                    GenPrimitive.Discard(genPrimitive);
                } else {
                    genPrimitive.GouraudShadingColors[0] = spa.GouraudShadingColor;
                    genPrimitive.GouraudShadingColors[1] = spb.GouraudShadingColor;
                    genPrimitive.GouraudShadingColors[2] = spc.GouraudShadingColor;

                    genPrimitive.Texcoords[0] = spa.Texcoord;
                    genPrimitive.Texcoords[1] = spb.Texcoord;
                    genPrimitive.Texcoords[2] = spc.Texcoord;

                    if ((BitwiseOrClipFlags(genPrimitive.ClipFlags) & ClipFlags.Near) == ClipFlags.Near) {
                        ClipTriangleGenPrimitiveNearPlane(render, genPrimitive);
                    }
                }
            } else {
                // Get the midpoints of each edge of the triangle. From that,
                // manually subdivide
                ReadOnlySpan<SubdivTriple> midPoint = stackalloc SubdivTriple[] {
                    CalculateMidPoint(spa, spb),
                    CalculateMidPoint(spb, spc),
                    CalculateMidPoint(spc, spa)
                };

                SubdivideTriangleGenPrimitive(render, baseGenPrimitive,         spa, midPoint[0], midPoint[2], level - 1);
                SubdivideTriangleGenPrimitive(render, baseGenPrimitive, midPoint[0],         spb, midPoint[1], level - 1);
                SubdivideTriangleGenPrimitive(render, baseGenPrimitive, midPoint[2], midPoint[1],         spc, level - 1);
                SubdivideTriangleGenPrimitive(render, baseGenPrimitive, midPoint[0], midPoint[1], midPoint[2], level - 1);
            }
        }

        private static void SubdivideQuadGenPrimitive(Render render,
                                                      GenPrimitive baseGenPrimitive,
                                                      SubdivTriple spa,
                                                      SubdivTriple spb,
                                                      SubdivTriple spc,
                                                      SubdivTriple spd,
                                                      int level) {
            if (level == 0) {
                GenPrimitive genPrimitive = render.AcquireGenPrimitive();

                // XXX: Clean this up
                // XXX: Slow... copies more than what we need
                GenPrimitive.Copy(baseGenPrimitive, genPrimitive);

                genPrimitive.ViewPoints[0] = spa.ViewPoint;
                genPrimitive.ViewPoints[1] = spb.ViewPoint;
                genPrimitive.ViewPoints[2] = spc.ViewPoint;
                genPrimitive.ViewPoints[3] = spd.ViewPoint;

                GenPrimitive.ClearClipFlags(genPrimitive);

                GenerateNearPlaneClipFlags(render, genPrimitive);

                if (TestOutsideFustrum(genPrimitive)) {
                    // Console.WriteLine($"---------------- Subdiv Cull ---------------- {genPrimitive.ClipFlags[0]} & {genPrimitive.ClipFlags[1]} & {genPrimitive.ClipFlags[2]} -> {genPrimitive.ViewPoints[0]}; {genPrimitive.ViewPoints[1]}; {genPrimitive.ViewPoints[2]}");
                    GenPrimitive.Discard(genPrimitive);
                } else {
                    genPrimitive.GouraudShadingColors[0] = spa.GouraudShadingColor;
                    genPrimitive.GouraudShadingColors[1] = spb.GouraudShadingColor;
                    genPrimitive.GouraudShadingColors[2] = spc.GouraudShadingColor;
                    genPrimitive.GouraudShadingColors[3] = spd.GouraudShadingColor;

                    genPrimitive.Texcoords[0] = spa.Texcoord;
                    genPrimitive.Texcoords[1] = spb.Texcoord;
                    genPrimitive.Texcoords[2] = spc.Texcoord;
                    genPrimitive.Texcoords[3] = spd.Texcoord;

                    if ((BitwiseOrClipFlags(genPrimitive.ClipFlags) & ClipFlags.Near) == ClipFlags.Near) {
                        ClipQuadGenPrimitiveNearPlane(render, genPrimitive);
                    }
                }
            } else {
                // Vertex order for a quad:
                //   D--B
                //   |  |
                //   C--A
                ReadOnlySpan<SubdivTriple> midPoints = stackalloc SubdivTriple[] {
                    CalculateMidPoint(spa, spb),
                        CalculateMidPoint(spa, spc),
                        CalculateMidPoint(spc, spd),
                        CalculateMidPoint(spd, spb),
                };

                SubdivTriple centerPoint = CalculateMidPoint(midPoints[0], midPoints[2]);

                SubdivideQuadGenPrimitive(render, baseGenPrimitive,          spa, midPoints[0], midPoints[1],  centerPoint, level - 1);
                SubdivideQuadGenPrimitive(render, baseGenPrimitive, midPoints[0],          spb,  centerPoint, midPoints[3], level - 1);
                SubdivideQuadGenPrimitive(render, baseGenPrimitive, midPoints[1],  centerPoint,          spc, midPoints[2], level - 1);
                SubdivideQuadGenPrimitive(render, baseGenPrimitive,  centerPoint, midPoints[3], midPoints[2],          spd, level - 1);
            }
        }

        private static void TriangulateQuadOrder<T>(ReadOnlySpan<T> quadPoints, Span<T> tri1Points, Span<T> tri2Points) where T : struct {
            tri1Points[0] = quadPoints[0];
            tri1Points[1] = quadPoints[1];
            tri1Points[2] = quadPoints[2];

            tri2Points[0] = quadPoints[1];
            tri2Points[1] = quadPoints[2];
            tri2Points[2] = quadPoints[3];
        }

        private static void DrawGenPrimitive(Render render, GenPrimitive genPrimitive) {
            var commandHandle = DrawPrimitive(render, genPrimitive);
            // XXX: Move the sort point code out and take in only the Z value
            render.PrimitiveSort.Add(genPrimitive.ViewPoints, PrimitiveSortPoint.Center, commandHandle);
        }

        private static SubdivTriple CalculateMidPoint(SubdivTriple a, SubdivTriple b, SubdivTriple c, SubdivTriple d) {
            SubdivTriple triple = new SubdivTriple();

            triple.ViewPoint = 0.25f * (a.ViewPoint + b.ViewPoint + c.ViewPoint + d.ViewPoint);

            Rgb888 color;

            color.R = (byte)((a.GouraudShadingColor.R + b.GouraudShadingColor.R + c.GouraudShadingColor.R + d.GouraudShadingColor.R) / 4);
            color.G = (byte)((a.GouraudShadingColor.G + b.GouraudShadingColor.G + c.GouraudShadingColor.G + d.GouraudShadingColor.G) / 4);
            color.B = (byte)((a.GouraudShadingColor.B + b.GouraudShadingColor.B + c.GouraudShadingColor.B + d.GouraudShadingColor.B) / 4);

            triple.GouraudShadingColor = color;

            Texcoord texcoord;

            texcoord.X = (byte)((a.Texcoord.X + b.Texcoord.X + c.Texcoord.X + d.Texcoord.X) / 4);
            texcoord.Y = (byte)((a.Texcoord.Y + b.Texcoord.Y + c.Texcoord.Y + d.Texcoord.Y) / 4);

            triple.Texcoord = texcoord;

            return triple;
        }

        private static SubdivTriple CalculateMidPoint(SubdivTriple a, SubdivTriple b) {
            SubdivTriple c = new SubdivTriple();

            c.ViewPoint = 0.5f * (a.ViewPoint + b.ViewPoint);

            Rgb888 color;

            color.R = (byte)((a.GouraudShadingColor.R + b.GouraudShadingColor.R) / 2);
            color.G = (byte)((a.GouraudShadingColor.G + b.GouraudShadingColor.G) / 2);
            color.B = (byte)((a.GouraudShadingColor.B + b.GouraudShadingColor.B) / 2);

            c.GouraudShadingColor = color;

            Texcoord texcoord;

            texcoord.X = (byte)((a.Texcoord.X + b.Texcoord.X) / 2);
            texcoord.Y = (byte)((a.Texcoord.Y + b.Texcoord.Y) / 2);

            c.Texcoord = texcoord;

            return c;
        }

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

        public class VertexIndices {
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

        private static Random _GetRandomColorRandom = new Random();

        private static Rgb888[] _HugeTable = new Rgb888[4096];
        static Renderer() {
            for (int i = 0; i < _HugeTable.Length; i++) {
                _HugeTable[i] = new Rgb888((byte)(_GetRandomColorRandom.NextDouble() * 255),
                                           (byte)(_GetRandomColorRandom.NextDouble() * 255),
                                           (byte)(_GetRandomColorRandom.NextDouble() * 255));
            }
        }

        private static Rgb888 GetRandomColor() =>
            _HugeTable[System.Math.Abs(_GetRandomColorRandom.Next()) % 4095];

        private static Rgb888 GetColor(int index) =>
            _HugeTable[System.Math.Abs(index) % 4095];

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

            // ref Rgb888 interiorGsc1 = ref genPrimitive.GouraudShadingColors[interiorIndices.Indices[0]];
            // ref Rgb888 exteriorGsc1 = ref genPrimitive.GouraudShadingColors[exteriorIndices.Indices[0]];
            // ref Rgb888 exteriorGsc2 = ref genPrimitive.GouraudShadingColors[exteriorIndices.Indices[1]];

            // exteriorGsc1 = ClipLerpGouraudShadingColor(render, interiorGsc1, exteriorGsc1, t1);
            // exteriorGsc2 = ClipLerpGouraudShadingColor(render, interiorGsc1, exteriorGsc2, t2);

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

        private static CommandHandle DrawPrimitive(Render render, GenPrimitive genPrimitive) {
            // Console.WriteLine($"DrawPrimitive {genPrimitive.Type}");
            switch (genPrimitive.Type) {
                // With lighting
                case PsyQ.TmdPrimitiveType.F3:
                    return DrawPrimitiveF3(render, genPrimitive);
                case PsyQ.TmdPrimitiveType.Fg3:
                    return DrawPrimitiveFg3(render, genPrimitive);
                case PsyQ.TmdPrimitiveType.Ft3:
                    return DrawPrimitiveFt3(render, genPrimitive);
                case PsyQ.TmdPrimitiveType.G3:
                case PsyQ.TmdPrimitiveType.Gg3:
                    return DrawPrimitiveG3(render, genPrimitive);
                case PsyQ.TmdPrimitiveType.Gt3:
                    return DrawPrimitiveGt3(render, genPrimitive);
                case PsyQ.TmdPrimitiveType.F4:
                    return DrawPrimitiveF4(render, genPrimitive);
                case PsyQ.TmdPrimitiveType.Ft4:
                    return DrawPrimitiveFt4(render, genPrimitive);
                case PsyQ.TmdPrimitiveType.G4:
                case PsyQ.TmdPrimitiveType.Gg4:
                    return DrawPrimitiveG4(render, genPrimitive);
                case PsyQ.TmdPrimitiveType.Gt4:
                    return DrawPrimitiveGt4(render, genPrimitive);

                    // Without lighting
                case PsyQ.TmdPrimitiveType.Fn3:
                    return DrawPrimitiveFn3(render, genPrimitive);
                case PsyQ.TmdPrimitiveType.Fnt3:
                    return DrawPrimitiveFnt3(render, genPrimitive);
                case PsyQ.TmdPrimitiveType.Gn3:
                    return DrawPrimitiveGn3(render, genPrimitive);
                case PsyQ.TmdPrimitiveType.Gnt3:
                    return DrawPrimitiveGnt3(render, genPrimitive);
                case PsyQ.TmdPrimitiveType.Fn4:
                    return DrawPrimitiveFn4(render, genPrimitive);
                case PsyQ.TmdPrimitiveType.Fnt4:
                    return DrawPrimitiveFnt4(render, genPrimitive);
                case PsyQ.TmdPrimitiveType.Gn4:
                    return DrawPrimitiveGn4(render, genPrimitive);
                case PsyQ.TmdPrimitiveType.Gnt4:
                    return DrawPrimitiveGnt4(render, genPrimitive);
                default:
                    throw new NotImplementedException($"Primitive type not implemented: {genPrimitive.Type}.");
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

        private static bool TestAnyOutsideFustrum(GenPrimitive genPrimitive) =>
            (BitwiseOrClipFlags(genPrimitive.ClipFlags) != ClipFlags.None);

        private static bool TestOutsideFustrum(GenPrimitive genPrimitive) =>
            (BitwiseAndClipFlags(genPrimitive.ClipFlags) != ClipFlags.None);

        private static void CalculateFog(Render render, GenPrimitive genPrimitive) {
            // Perform fog
            // float FogStart = render.Camera.DepthNear + 2.5f;
            // float FogEnd = FogStart*10;
            float FogStart = 0.8f;
            float FogEnd = 4f;

            float FogDifferenceDenom = 1f / (FogEnd - FogStart);
            // DQA
            float FogCoefficient = -(FogStart * FogEnd) * FogDifferenceDenom;
            // DQB
            float FogOffset = FogEnd * FogDifferenceDenom;
            // XXX: Move over as a function
            Func<float, float> CalculateFogIntensity = (z) =>
                System.Math.Clamp(((FogCoefficient / z) + FogOffset), 0f, 1f);

            Vector3 bgColor = render.DrawEnv.Color;
            Vector3 ambientColor = Rgb888.White;

            for (int i = 0; i < genPrimitive.VertexCount; i++) {
                float z = System.Math.Max(genPrimitive.ViewPoints[i].Z, render.Camera.DepthNear);

                // https://www.sjbaker.org/steve/omniv/love_your_z_buffer.html
                // Fog intensity: [0..1]
                float fogIntensity = CalculateFogIntensity(z);

                Vector3 lerpedColor = Vector3.Lerp(ambientColor, bgColor, fogIntensity);
                Rgb888 color = lerpedColor;

                genPrimitive.GouraudShadingColors[i] = color;
            }
        }

        private static void CalculateLighting(Render render, GenPrimitive genPrimitive) {
            const float NormalizeVectorFactor = 1f / 255f;

            Span<Vector3> lightIntensityVectors = stackalloc Vector3[genPrimitive.NormalCount];

            int lightCount = LightingManager.PointLights.Count + LightingManager.DirectionalLights.Count;

            // Take into account flat or vertex shading
            for (int index = 0; index < genPrimitive.NormalCount; index++) {
                Vector3 normal = genPrimitive.PolygonNormals[index];

                Vector3 pointLightVector = Vector3.Zero;

                for (int lightIndex = 0; lightIndex < LightingManager.PointLights.Count; lightIndex++) {
                    PointLight light = LightingManager.PointLights[lightIndex];

                    // If flat shading, use the center point of the primitive
                    // instead of the first vertex
                    Vector3 point = genPrimitive.WorldPoints[index];

                    if (genPrimitive.NormalCount == 1) {
                        for (int i = 1; i < genPrimitive.VertexCount; i++) {
                            point += genPrimitive.WorldPoints[i];
                        }

                        point /= genPrimitive.VertexCount;
                    }

                    pointLightVector += CalculatePointLightVector(light, point, normal);
                }

                Vector3 directionalLightVector = Vector3.Zero;

                for (int lightIndex = 0; lightIndex < LightingManager.DirectionalLights.Count; lightIndex++) {
                    DirectionalLight light = LightingManager.DirectionalLights[lightIndex];

                    directionalLightVector += CalculateDirectionalLightVector(light, normal, render.Material);
                }

                Vector3 lightVector = pointLightVector + directionalLightVector;

                lightIntensityVectors[index] = lightVector * NormalizeVectorFactor;
            }

            // If flat shading, one normal is used for all vertices. Otherwise,
            // use a normal per vertex
            for (int index = 0; index < genPrimitive.VertexCount; index++) {
                Vector3 lightIntensityVector = lightIntensityVectors[index % genPrimitive.NormalCount];

                Vector3 colorVector;

                colorVector.X = (lightIntensityVector.X * (float)genPrimitive.GouraudShadingColors[index].R) + render.Material.AmbientColor.R;
                colorVector.Y = (lightIntensityVector.Y * (float)genPrimitive.GouraudShadingColors[index].G) + render.Material.AmbientColor.G;
                colorVector.Z = (lightIntensityVector.Z * (float)genPrimitive.GouraudShadingColors[index].B) + render.Material.AmbientColor.B;

                Vector3 clampedColorVector = Vector3.Min(colorVector, 255f * Vector3.One);

                genPrimitive.GouraudShadingColors[index].R = (byte)clampedColorVector.X;
                genPrimitive.GouraudShadingColors[index].G = (byte)clampedColorVector.Y;
                genPrimitive.GouraudShadingColors[index].B = (byte)clampedColorVector.Z;
            }
        }

        private static Vector3 CalculatePointLightVector(PointLight light, Vector3 point, Vector3 normal) {
            Vector3 distance = light.Position - point;
            float distanceLength = distance.Length();

            if (distanceLength >= light.CutOffDistance) {
                return Vector3.Zero;
            }

            Vector3 l = Vector3.Normalize(distance);

            float nDotL = System.Math.Clamp(Vector3.Dot(normal, l), 0f, 1f);
            float distanceToLight = 1f - System.Math.Clamp(distanceLength * light.RangeReciprocal, 0f, 1f);
            float attenuation = distanceToLight * distanceToLight;

            float r = attenuation * (nDotL * light.Color.R);
            float g = attenuation * (nDotL * light.Color.G);
            float b = attenuation * (nDotL * light.Color.B);

            return new Vector3(r, g, b);
        }

        private static Vector3 CalculateDirectionalLightVector(DirectionalLight light, Vector3 normal, Material material) {
            float nDotL = System.Math.Clamp(Vector3.Dot(normal, -light.NormalizedDirection), 0f, 1f);

            float r = nDotL * light.Color.R;
            float g = nDotL * light.Color.G;
            float b = nDotL * light.Color.B;

            return new Vector3(r, g, b);
        }

        private static bool TestBackFaceCull(Vector3 viewPoint, Vector3 faceNormal) =>
            (Vector3.Dot(-viewPoint, faceNormal) <= 0f);

        private static bool TestBackFaceCull(GenPrimitive genPrimitive) =>
            TestBackFaceCull(genPrimitive.ViewPoints[0], genPrimitive.FaceNormal);

        private static bool TestScreenPrimitiveArea(GenPrimitive genPrimitive) {
            Vector2Int a = genPrimitive.ScreenPoints[2] - genPrimitive.ScreenPoints[0];
            Vector2Int b = genPrimitive.ScreenPoints[1] - genPrimitive.ScreenPoints[0];

            int z1 = (a.X * b.Y) - (a.Y * b.X);

            if (z1 <= 0) {
                return true;
            }

            if (genPrimitive.VertexCount == 4) {
                // Vertex order for a quad:
                //   D--B
                //   |  |
                //   C--A
                //
                //      B
                //      |
                //   C--A First test
                //
                //   D--B Second test
                //   |
                //   C

                Vector2Int c = genPrimitive.ScreenPoints[2] - genPrimitive.ScreenPoints[3];
                Vector2Int d = genPrimitive.ScreenPoints[1] - genPrimitive.ScreenPoints[3];

                int z2 = (a.X * b.Y) - (a.Y * b.X);

                if (z2 <= 0) {
                    return true;
                }
            }

            return false;
        }

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

        private static CommandHandle DrawPrimitiveF3(Render render, GenPrimitive genPrimitive) {
            var handle = render.CommandBuffer.AllocatePolyF3();
            var poly = render.CommandBuffer.GetPolyF3(handle);

            poly[0].SetCommand();
            poly[0].Color = genPrimitive.GouraudShadingColors[0];
            poly[0].P0 = genPrimitive.ScreenPoints[0];
            poly[0].P1 = genPrimitive.ScreenPoints[1];
            poly[0].P2 = genPrimitive.ScreenPoints[2];

            return handle;
        }

        private static CommandHandle DrawPrimitiveFg3(Render render, GenPrimitive genPrimitive) {
            var handle = render.CommandBuffer.AllocatePolyG3();
            var poly = render.CommandBuffer.GetPolyG3(handle);

            poly[0].SetCommand();
            poly[0].C0 = genPrimitive.GouraudShadingColors[0];
            poly[0].C1 = genPrimitive.GouraudShadingColors[1];
            poly[0].C2 = genPrimitive.GouraudShadingColors[2];
            poly[0].P0 = genPrimitive.ScreenPoints[0];
            poly[0].P1 = genPrimitive.ScreenPoints[1];
            poly[0].P2 = genPrimitive.ScreenPoints[2];

            return handle;
        }

        private static CommandHandle DrawPrimitiveFt3(Render render, GenPrimitive genPrimitive) {
            var handle = render.CommandBuffer.AllocatePolyFt3();
            var poly = render.CommandBuffer.GetPolyFt3(handle);

            poly[0].SetCommand();
            poly[0].Color = genPrimitive.GouraudShadingColors[0];
            poly[0].T0 = genPrimitive.Texcoords[0];
            poly[0].T1 = genPrimitive.Texcoords[1];
            poly[0].T2 = genPrimitive.Texcoords[2];
            poly[0].TPageId = genPrimitive.TPageId;
            poly[0].ClutId = genPrimitive.ClutId;
            poly[0].P0 = genPrimitive.ScreenPoints[0];
            poly[0].P1 = genPrimitive.ScreenPoints[1];
            poly[0].P2 = genPrimitive.ScreenPoints[2];

            return handle;
        }

        private static CommandHandle DrawPrimitiveG3(Render render, GenPrimitive genPrimitive) {
            var handle = render.CommandBuffer.AllocatePolyG3();
            var poly = render.CommandBuffer.GetPolyG3(handle);

            poly[0].SetCommand();

            poly[0].C0 = genPrimitive.GouraudShadingColors[0];
            poly[0].C1 = genPrimitive.GouraudShadingColors[1];
            poly[0].C2 = genPrimitive.GouraudShadingColors[2];

            poly[0].P0 = genPrimitive.ScreenPoints[0];
            poly[0].P1 = genPrimitive.ScreenPoints[1];
            poly[0].P2 = genPrimitive.ScreenPoints[2];

            return handle;
        }

        private static CommandHandle DrawPrimitiveGt3(Render render, GenPrimitive genPrimitive) {
            var handle = render.CommandBuffer.AllocatePolyGt3();
            var poly = render.CommandBuffer.GetPolyGt3(handle);

            poly[0].SetCommand();
            poly[0].C0 = genPrimitive.GouraudShadingColors[0];
            poly[0].C1 = genPrimitive.GouraudShadingColors[1];
            poly[0].C2 = genPrimitive.GouraudShadingColors[2];
            poly[0].T0 = genPrimitive.Texcoords[0];
            poly[0].T1 = genPrimitive.Texcoords[1];
            poly[0].T2 = genPrimitive.Texcoords[2];
            poly[0].TPageId = genPrimitive.TPageId;
            poly[0].ClutId = genPrimitive.ClutId;
            poly[0].P0 = genPrimitive.ScreenPoints[0];
            poly[0].P1 = genPrimitive.ScreenPoints[1];
            poly[0].P2 = genPrimitive.ScreenPoints[2];

            return handle;
        }

        private static CommandHandle DrawPrimitiveF4(Render render, GenPrimitive genPrimitive) {
            var handle = render.CommandBuffer.AllocatePolyG4();
            var poly = render.CommandBuffer.GetPolyG4(handle);

            poly[0].SetCommand();

            poly[0].C0 = genPrimitive.GouraudShadingColors[0];
            poly[0].C1 = genPrimitive.GouraudShadingColors[1];
            poly[0].C2 = genPrimitive.GouraudShadingColors[2];
            poly[0].C3 = genPrimitive.GouraudShadingColors[3];

            poly[0].P0 = genPrimitive.ScreenPoints[0];
            poly[0].P1 = genPrimitive.ScreenPoints[1];
            poly[0].P2 = genPrimitive.ScreenPoints[2];
            poly[0].P3 = genPrimitive.ScreenPoints[3];

            return handle;
        }

        private static CommandHandle DrawPrimitiveG4(Render render, GenPrimitive genPrimitive) {
            var handle = render.CommandBuffer.AllocatePolyG4();
            var poly = render.CommandBuffer.GetPolyG4(handle);

            poly[0].SetCommand();

            poly[0].C0 = genPrimitive.GouraudShadingColors[0];
            poly[0].C1 = genPrimitive.GouraudShadingColors[1];
            poly[0].C2 = genPrimitive.GouraudShadingColors[2];
            poly[0].C3 = genPrimitive.GouraudShadingColors[3];
            poly[0].P0 = genPrimitive.ScreenPoints[0];
            poly[0].P1 = genPrimitive.ScreenPoints[1];
            poly[0].P2 = genPrimitive.ScreenPoints[2];
            poly[0].P3 = genPrimitive.ScreenPoints[3];

            return handle;
        }

        private static CommandHandle DrawPrimitiveFt4(Render render, GenPrimitive genPrimitive) {
            var handle = render.CommandBuffer.AllocatePolyFt4();
            var poly = render.CommandBuffer.GetPolyFt4(handle);

            poly[0].SetCommand();
            poly[0].Color = genPrimitive.GouraudShadingColors[0];
            poly[0].T0 = genPrimitive.Texcoords[0];
            poly[0].T1 = genPrimitive.Texcoords[1];
            poly[0].T2 = genPrimitive.Texcoords[2];
            poly[0].T3 = genPrimitive.Texcoords[3];
            poly[0].TPageId = genPrimitive.TPageId;
            poly[0].ClutId = genPrimitive.ClutId;
            poly[0].P0 = genPrimitive.ScreenPoints[0];
            poly[0].P1 = genPrimitive.ScreenPoints[1];
            poly[0].P2 = genPrimitive.ScreenPoints[2];
            poly[0].P3 = genPrimitive.ScreenPoints[3];

            return handle;
        }

        private static CommandHandle DrawPrimitiveGt4(Render render, GenPrimitive genPrimitive) {
            var handle = render.CommandBuffer.AllocatePolyGt4();
            var poly = render.CommandBuffer.GetPolyGt4(handle);

            poly[0].SetCommand();
            poly[0].C0 = genPrimitive.GouraudShadingColors[0];
            poly[0].C1 = genPrimitive.GouraudShadingColors[1];
            poly[0].C2 = genPrimitive.GouraudShadingColors[2];
            poly[0].C3 = genPrimitive.GouraudShadingColors[3];
            poly[0].T0 = genPrimitive.Texcoords[0];
            poly[0].T1 = genPrimitive.Texcoords[1];
            poly[0].T2 = genPrimitive.Texcoords[2];
            poly[0].T3 = genPrimitive.Texcoords[3];
            poly[0].TPageId = genPrimitive.TPageId;
            poly[0].ClutId = genPrimitive.ClutId;
            poly[0].P0 = genPrimitive.ScreenPoints[0];
            poly[0].P1 = genPrimitive.ScreenPoints[1];
            poly[0].P2 = genPrimitive.ScreenPoints[2];
            poly[0].P3 = genPrimitive.ScreenPoints[3];

            return handle;
        }

        private static CommandHandle DrawPrimitiveFn3(Render render, GenPrimitive genPrimitive) =>
            DrawPrimitiveF3(render, genPrimitive);

        private static CommandHandle DrawPrimitiveFnt3(Render render, GenPrimitive genPrimitive) {
            var handle = render.CommandBuffer.AllocatePolyFt3();
            var poly = render.CommandBuffer.GetPolyFt3(handle);

            poly[0].SetCommand();
            poly[0].Color = genPrimitive.GouraudShadingColors[0];
            poly[0].T0 = genPrimitive.Texcoords[0];
            poly[0].T1 = genPrimitive.Texcoords[1];
            poly[0].T2 = genPrimitive.Texcoords[2];
            poly[0].TPageId = genPrimitive.TPageId;
            poly[0].ClutId = genPrimitive.ClutId;
            poly[0].P0 = genPrimitive.ScreenPoints[0];
            poly[0].P1 = genPrimitive.ScreenPoints[1];
            poly[0].P2 = genPrimitive.ScreenPoints[2];

            return handle;
        }

        private static CommandHandle DrawPrimitiveGn3(Render render, GenPrimitive genPrimitive) =>
            DrawPrimitiveG3(render, genPrimitive);

        private static CommandHandle DrawPrimitiveGnt3(Render render, GenPrimitive genPrimitive) =>
            DrawPrimitiveGt3(render, genPrimitive);

        private static CommandHandle DrawPrimitiveFn4(Render render, GenPrimitive genPrimitive) =>
            DrawPrimitiveF4(render, genPrimitive);

        private static CommandHandle DrawPrimitiveFnt4(Render render, GenPrimitive genPrimitive) {
            var handle = render.CommandBuffer.AllocatePolyFt4();
            var poly = render.CommandBuffer.GetPolyFt4(handle);

            poly[0].SetCommand();
            poly[0].Color = genPrimitive.GouraudShadingColors[0];
            poly[0].T0 = genPrimitive.Texcoords[0];
            poly[0].T1 = genPrimitive.Texcoords[1];
            poly[0].T2 = genPrimitive.Texcoords[2];
            poly[0].T3 = genPrimitive.Texcoords[3];
            poly[0].TPageId = genPrimitive.TPageId;
            poly[0].ClutId = genPrimitive.ClutId;
            poly[0].P0 = genPrimitive.ScreenPoints[0];
            poly[0].P1 = genPrimitive.ScreenPoints[1];
            poly[0].P2 = genPrimitive.ScreenPoints[2];
            poly[0].P3 = genPrimitive.ScreenPoints[3];

            return handle;
        }

        private static CommandHandle DrawPrimitiveGn4(Render render, GenPrimitive genPrimitive) =>
            DrawPrimitiveG4(render, genPrimitive);

        private static CommandHandle DrawPrimitiveGnt4(Render render, GenPrimitive genPrimitive) =>
            DrawPrimitiveGt4(render, genPrimitive);

        private static Vector3 CalculateFaceNormal(Vector3[] points) =>
            Vector3.Normalize(CalculateScaledFaceNormal(points));

        private static Vector3 CalculateScaledFaceNormal(ReadOnlySpan<Vector3> points) {
            Vector3 a = points[2] - points[0];
            Vector3 b = points[1] - points[0];

            return Vector3.Cross(a, b);
        }

        private static void TransformToWorld(Render render, GenPrimitive genPrimitive) {
            for (int i = 0; i < genPrimitive.VertexCount; i++) {
                genPrimitive.WorldPoints[i] = Vector3.Transform(genPrimitive.PolygonVertices[i], render.ModelMatrix);
            }
        }

        private static Vector3 TransformToView(Render render, Vector3 point) =>
            Vector3.Transform(point, render.ModelViewMatrix);

        private static void TransformToView(Render render, GenPrimitive genPrimitive) {
            for (int i = 0; i < genPrimitive.VertexCount; i++) {
                genPrimitive.ViewPoints[i] = TransformToView(render, genPrimitive.PolygonVertices[i]);
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
