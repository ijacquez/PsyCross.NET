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
            if (distance.Z < 1f) {
                subdivLevel = 2;
            } else if (distance.Z < 2f) {
                subdivLevel = 1;
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
                    genPrimitive.Texcoords[0] = spa.Texcoord;
                    genPrimitive.Texcoords[1] = spb.Texcoord;
                    genPrimitive.Texcoords[2] = spc.Texcoord;

                    GenPrimitive.CopyTextureAttribs(baseGenPrimitive, genPrimitive);
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
                    genPrimitive.Texcoords[0] = spa.Texcoord;
                    genPrimitive.Texcoords[1] = spb.Texcoord;
                    genPrimitive.Texcoords[2] = spc.Texcoord;
                    genPrimitive.Texcoords[3] = spd.Texcoord;

                    GenPrimitive.CopyTextureAttribs(baseGenPrimitive, genPrimitive);
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
    }
}
