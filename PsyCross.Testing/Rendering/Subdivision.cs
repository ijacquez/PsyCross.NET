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

        private const float _Level1Z        = 2f;

        private const float _Level2Z        = 1f;
        private const float _Level2FaceArea = 0.125f;

        // XXX: Move this to SubdivisionRenderInit in order to access the near depth
        private static float _SubdivStart = 0.5f; // render.Camera.DepthNear;
        private static float _SubdivEnd = _SubdivStart + 1f;

        private static float _SubdivDifferenceDenom = 1f / (_SubdivEnd - _SubdivStart);
        private static float _SubdivCoefficient = -(_SubdivStart * _SubdivEnd) * _SubdivDifferenceDenom;
        private static float _SubdivOffset = _SubdivEnd * _SubdivDifferenceDenom;

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

        private static void SubdivideTriangleGenPrimitive(Render render, GenPrimitive genPrimitive) {
            float minViewPoint = MathHelper.Min(genPrimitive.ViewPoints[0].Z,
                                                genPrimitive.ViewPoints[1].Z,
                                                genPrimitive.ViewPoints[2].Z);

            int subdivLevel = CalculateSubdivIntensity(genPrimitive, minViewPoint);

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

        private static void SubdivideQuadGenPrimitive(Render render, GenPrimitive genPrimitive) {
            render.SubdividedGenPrimitives.Add(genPrimitive);

            float minViewPoint = MathHelper.Min(genPrimitive.ViewPoints[0].Z,
                                                genPrimitive.ViewPoints[1].Z,
                                                genPrimitive.ViewPoints[2].Z,
                                                genPrimitive.ViewPoints[3].Z);

            int subdivLevel = (int)CalculateSubdivIntensity(genPrimitive, minViewPoint);

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

        private static int CalculateSubdivIntensity(GenPrimitive genPrimitive, float z) {
            float intensity = 2f * System.Math.Clamp(((_SubdivCoefficient / z) + _SubdivOffset), 0f, 1f);

            if ((z < _Level2Z) && (genPrimitive.FaceArea > _Level2FaceArea)) {
                return 2;
            }

            if (z < _Level1Z) {
                return 1;
            }

            return 0;
        }
    }
}
