using System;
using System.Numerics;
using PsyCross.Math;

namespace PsyCross.Testing.Rendering {
    public static partial class Renderer {
        private struct SubdivTriple {
            public Vector3 ViewPoint { get; set; }

            public Rgb888 GouraudShadingColor { get; set; }

            public Texcoord Texcoord { get; set; }

            public static SubdivTriple FromGenPrimitive(GenPrimitive genPrimitive, int index) =>
                new SubdivTriple {
                    ViewPoint           = genPrimitive.ViewPoints[index],
                    GouraudShadingColor = (GenPrimitive.HasFlag(genPrimitive, GenPrimitiveFlags.Shaded))
                                              ? genPrimitive.GouraudShadingColors[index]
                                              : genPrimitive.GouraudShadingColors[0],
                    Texcoord            = genPrimitive.Texcoords[index]
            };
        }

        // XXX: Move this to SubdivisionRenderInit in order to access the near depth
        private static float SubdivStart = 0.5f; // render.Camera.DepthNear;
        private static float SubdivEnd = SubdivStart + 1f;

        private static float SubdivDifferenceDenom = 1f / (SubdivEnd - SubdivStart);
        private static float SubdivCoefficient = -(SubdivStart * SubdivEnd) * SubdivDifferenceDenom;
        private static float SubdivOffset = SubdivEnd * SubdivDifferenceDenom;

        private static int CalculateSubdivIntensity(GenPrimitive genPrimitive, float z) {
            float intensity = 2f * System.Math.Clamp(((SubdivCoefficient / z) + SubdivOffset), 0f, 1f);

            // if (z < 1f) {
            if ((z < 1f) && (genPrimitive.FaceArea >= 0.125f)) {
                return 2;
            }

            if (z < 2f) {
            // if ((z < 2f) && (genPrimitive.FaceArea > 0.25f)) {
                return 1;
            }

            return 0;
        }

        private static void SubdivisionRenderInit(Render render) {
        }

        private static void SubdivideGenPrimitive(Render render, GenPrimitive genPrimitive) {
            if (genPrimitive.VertexCount == 3) {
                SubdivideTriangleGenPrimitive(render, genPrimitive);
            } else {
                SubdivideQuadGenPrimitive(render, genPrimitive);
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

                GenPrimitive.Copy(baseGenPrimitive, genPrimitive);

                genPrimitive.ViewPoints[0] = spa.ViewPoint;
                genPrimitive.ViewPoints[1] = spb.ViewPoint;
                genPrimitive.ViewPoints[2] = spc.ViewPoint;

                genPrimitive.GouraudShadingColors[0] = spa.GouraudShadingColor;

                if (GenPrimitive.HasFlag(genPrimitive, GenPrimitiveFlags.Shaded)) {
                    genPrimitive.GouraudShadingColors[1] = spb.GouraudShadingColor;
                    genPrimitive.GouraudShadingColors[2] = spc.GouraudShadingColor;
                }

                // // XXX: Remove
                // genPrimitive.Type = PsyQ.TmdPrimitiveType.G3;
                // genPrimitive.Flags |= GenPrimitiveFlags.Shaded;
                // Rgb888 color = GetRandomColor();
                // genPrimitive.GouraudShadingColors[0] = color;
                // genPrimitive.GouraudShadingColors[1] = color;
                // genPrimitive.GouraudShadingColors[2] = color;

                if (GenPrimitive.HasFlag(genPrimitive, GenPrimitiveFlags.Textured)) {
                    GenPrimitive.CopyTextureAttribs(baseGenPrimitive, genPrimitive);

                    genPrimitive.Texcoords[0] = spa.Texcoord;
                    genPrimitive.Texcoords[1] = spb.Texcoord;
                    genPrimitive.Texcoords[2] = spc.Texcoord;
                }

                render.SubdividedGenPrimitives.Add(genPrimitive);
            } else {
                // Get the midpoints of each edge of the triangle. From that,
                // manually subdivide
                ReadOnlySpan<SubdivTriple> midPoint = stackalloc SubdivTriple[] {
                    CalculateMidPoint(baseGenPrimitive.Flags, spa, spb),
                    CalculateMidPoint(baseGenPrimitive.Flags, spb, spc),
                    CalculateMidPoint(baseGenPrimitive.Flags, spc, spa)
                };

                SubdivideTriangleGenPrimitive(render, baseGenPrimitive,         spa, midPoint[0], midPoint[2], level - 1);
                SubdivideTriangleGenPrimitive(render, baseGenPrimitive, midPoint[0],         spb, midPoint[1], level - 1);
                SubdivideTriangleGenPrimitive(render, baseGenPrimitive, midPoint[2], midPoint[1],         spc, level - 1);
                SubdivideTriangleGenPrimitive(render, baseGenPrimitive, midPoint[0], midPoint[1], midPoint[2], level - 1);
            }
        }

        private static void SubdivideTriangleGenPrimitive(Render render, GenPrimitive genPrimitive)
        {
            // float centerViewPoint = (genPrimitive.ViewPoints[0].Z +
            //                          genPrimitive.ViewPoints[1].Z +
            //                          genPrimitive.ViewPoints[2].Z) / 3f;

            float minViewPoint = MathHelper.Min(genPrimitive.ViewPoints[0].Z,
                                                genPrimitive.ViewPoints[1].Z,
                                                genPrimitive.ViewPoints[2].Z);

            // float maxViewPoint = MathHelper.Max(genPrimitive.ViewPoints[0].Z,
            //                                     genPrimitive.ViewPoints[1].Z,
            //                                     genPrimitive.ViewPoints[2].Z);

            int subdivLevel = CalculateSubdivIntensity(genPrimitive, minViewPoint);

            // XXX: Remove. Testing. This just changes color to
            // DebugColorPrims(render, genPrimitive, subdivLevel, isQuad: false);

            // Actual subdivision code
            if (subdivLevel == 0) {
                render.SubdividedGenPrimitives.Add(genPrimitive);
            } else {
                SubdivideTriangleGenPrimitive(render,
                                              genPrimitive,
                                              SubdivTriple.FromGenPrimitive(genPrimitive, 0),
                                              SubdivTriple.FromGenPrimitive(genPrimitive, 1),
                                              SubdivTriple.FromGenPrimitive(genPrimitive, 2),
                                              subdivLevel);

                GenPrimitive.Discard(genPrimitive);
            }
        }

        // XXX: Remove?
        private static void DebugColorPrims(Render render, GenPrimitive genPrimitive, int subdivLevel, bool isQuad) {
            if (subdivLevel == 0) {
            } else if (subdivLevel == 1) {
                genPrimitive.Flags |= GenPrimitiveFlags.Shaded;
                genPrimitive.GouraudShadingColors[0] = Rgb888.Blue;
                genPrimitive.GouraudShadingColors[1] = Rgb888.Blue;
                genPrimitive.GouraudShadingColors[2] = Rgb888.Blue;

                if (isQuad) {
                    genPrimitive.Type = PsyQ.TmdPrimitiveType.G4;
                    genPrimitive.GouraudShadingColorBuffer[3] = Rgb888.Blue;
                } else {
                    genPrimitive.Type = PsyQ.TmdPrimitiveType.G3;
                }
            } else if (subdivLevel == 2) {
                genPrimitive.Type = PsyQ.TmdPrimitiveType.G3;
                genPrimitive.Flags |= GenPrimitiveFlags.Shaded;
                genPrimitive.GouraudShadingColors[0] = Rgb888.Yellow;
                genPrimitive.GouraudShadingColors[1] = Rgb888.Yellow;
                genPrimitive.GouraudShadingColors[2] = Rgb888.Yellow;
                if (isQuad) {
                    genPrimitive.Type = PsyQ.TmdPrimitiveType.G4;
                    genPrimitive.GouraudShadingColorBuffer[3] = Rgb888.Yellow;
                } else {
                    genPrimitive.Type = PsyQ.TmdPrimitiveType.G3;
                }
            }
            render.SubdividedGenPrimitives.Add(genPrimitive);
        }

        private static void SubdivideQuadGenPrimitive(Render render, GenPrimitive genPrimitive) {
            render.SubdividedGenPrimitives.Add(genPrimitive);

            // float centerViewPoint = (genPrimitive.ViewPoints[0].Z +
            //                          genPrimitive.ViewPoints[1].Z +
            //                          genPrimitive.ViewPoints[2].Z +
            //                          genPrimitive.ViewPoints[3].Z) / 4f;

            float minViewPoint = MathHelper.Min(genPrimitive.ViewPoints[0].Z,
                                                genPrimitive.ViewPoints[1].Z,
                                                genPrimitive.ViewPoints[2].Z,
                                                genPrimitive.ViewPoints[3].Z);

            // float maxViewPoint = MathHelper.Max(genPrimitive.ViewPoints[0].Z,
            //                                     genPrimitive.ViewPoints[1].Z,
            //                                     genPrimitive.ViewPoints[2].Z,
            //                                     genPrimitive.ViewPoints[3].Z);

            int subdivLevel = (int)CalculateSubdivIntensity(genPrimitive, minViewPoint);
            // int subdivLevel = 1;

            // DebugColorPrims(render, genPrimitive, subdivLevel, isQuad: true);

            if (subdivLevel == 0) {
                render.SubdividedGenPrimitives.Add(genPrimitive);
            } else {
                SubdivideQuadGenPrimitive(render,
                                          genPrimitive,
                                          SubdivTriple.FromGenPrimitive(genPrimitive, 0),
                                          SubdivTriple.FromGenPrimitive(genPrimitive, 1),
                                          SubdivTriple.FromGenPrimitive(genPrimitive, 2),
                                          SubdivTriple.FromGenPrimitive(genPrimitive, 3),
                                          subdivLevel);

                GenPrimitive.Discard(genPrimitive);
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

                GenPrimitive.Copy(baseGenPrimitive, genPrimitive);

                genPrimitive.ViewPoints[0] = spa.ViewPoint;
                genPrimitive.ViewPoints[1] = spb.ViewPoint;
                genPrimitive.ViewPoints[2] = spc.ViewPoint;
                genPrimitive.ViewPoints[3] = spd.ViewPoint;

                genPrimitive.GouraudShadingColors[0] = spa.GouraudShadingColor;

                if (GenPrimitive.HasFlag(genPrimitive, GenPrimitiveFlags.Shaded)) {
                    genPrimitive.GouraudShadingColors[1] = spb.GouraudShadingColor;
                    genPrimitive.GouraudShadingColors[2] = spc.GouraudShadingColor;
                    genPrimitive.GouraudShadingColors[3] = spd.GouraudShadingColor;
                }

                // // XXX: Remove
                // genPrimitive.Type = PsyQ.TmdPrimitiveType.G4;
                // genPrimitive.Flags |= GenPrimitiveFlags.Shaded;
                // Rgb888 color = GetRandomColor();
                // genPrimitive.GouraudShadingColors[0] = color;
                // genPrimitive.GouraudShadingColors[1] = color;
                // genPrimitive.GouraudShadingColors[2] = color;
                // genPrimitive.GouraudShadingColors[3] = color;

                if (GenPrimitive.HasFlag(genPrimitive, GenPrimitiveFlags.Textured)) {
                    GenPrimitive.CopyTextureAttribs(baseGenPrimitive, genPrimitive);

                    genPrimitive.Texcoords[0] = spa.Texcoord;
                    genPrimitive.Texcoords[1] = spb.Texcoord;
                    genPrimitive.Texcoords[2] = spc.Texcoord;
                    genPrimitive.Texcoords[3] = spd.Texcoord;
                }

                render.SubdividedGenPrimitives.Add(genPrimitive);
            } else {
                // Vertex order for a quad:
                //   D--B
                //   |  |
                //   C--A
                ReadOnlySpan<SubdivTriple> midPoints = stackalloc SubdivTriple[] {
                    CalculateMidPoint(baseGenPrimitive.Flags, spa, spb),
                    CalculateMidPoint(baseGenPrimitive.Flags, spa, spc),
                    CalculateMidPoint(baseGenPrimitive.Flags, spc, spd),
                    CalculateMidPoint(baseGenPrimitive.Flags, spd, spb)
                };

                SubdivTriple centerPoint = CalculateMidPoint(baseGenPrimitive.Flags, midPoints[0], midPoints[2]);

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

        private static bool TestSubdivLevel(GenPrimitive genPrimitive, int subdivLevel) {
            if (subdivLevel == 0) {
                return true;
            }

            // if ((subdivLevel == 1) && (genPrimitive.FaceArea < 1f)) {
            //     return true;
            // }

            // if ((subdivLevel == 2) && (genPrimitive.FaceArea < 0.5f)) {
            //     return true;
            // }

            return false;
        }

        private static SubdivTriple CalculateMidPoint(GenPrimitiveFlags genPrimitiveFlags, SubdivTriple a, SubdivTriple b, SubdivTriple c, SubdivTriple d) {
            SubdivTriple triple = new SubdivTriple();

            triple.ViewPoint = 0.25f * (a.ViewPoint + b.ViewPoint + c.ViewPoint + d.ViewPoint);


            if (genPrimitiveFlags.HasFlag(GenPrimitiveFlags.Shaded)) {
                Vector3 aColor = (Vector3)a.GouraudShadingColor;
                Vector3 bColor = (Vector3)b.GouraudShadingColor;
                Vector3 cColor = (Vector3)c.GouraudShadingColor;
                Vector3 dColor = (Vector3)d.GouraudShadingColor;

                triple.GouraudShadingColor = (Rgb888)(0.25f * (aColor + bColor + cColor + dColor));
            } else {
                triple.GouraudShadingColor = a.GouraudShadingColor;
            }

            if (genPrimitiveFlags.HasFlag(GenPrimitiveFlags.Textured)) {
                Vector2 aTexcoord = (Vector2)a.Texcoord;
                Vector2 bTexcoord = (Vector2)b.Texcoord;
                Vector2 cTexcoord = (Vector2)c.Texcoord;
                Vector2 dTexcoord = (Vector2)d.Texcoord;;

                triple.Texcoord = (Texcoord)(0.25f * (aTexcoord + bTexcoord + cTexcoord + dTexcoord));
            }

            return triple;
        }

        private static SubdivTriple CalculateMidPoint(GenPrimitiveFlags genPrimitiveFlags, SubdivTriple a, SubdivTriple b) {
            SubdivTriple triple = new SubdivTriple();

            triple.ViewPoint = 0.5f * (a.ViewPoint + b.ViewPoint);

            if (genPrimitiveFlags.HasFlag(GenPrimitiveFlags.Shaded)) {
                Vector3 aColor = (Vector3)a.GouraudShadingColor;
                Vector3 bColor = (Vector3)b.GouraudShadingColor;

                triple.GouraudShadingColor = (Rgb888)(0.5f * (aColor + bColor));
            } else {
                triple.GouraudShadingColor = a.GouraudShadingColor;
            }

            if (genPrimitiveFlags.HasFlag(GenPrimitiveFlags.Textured)) {
                Vector2 aTexcoord = (Vector2)a.Texcoord;
                Vector2 bTexcoord = (Vector2)b.Texcoord;

                triple.Texcoord = (Texcoord)(0.5f * (aTexcoord + bTexcoord));
            }

            return triple;
        }
    }
}
