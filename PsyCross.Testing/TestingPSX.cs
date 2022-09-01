using System;
using System.Numerics;
using PsyCross.Devices.Input;
using PsyCross.Math;
using PsyCross.ResourceManagement;

namespace PsyCross.Testing {
    public class Testing {
        private PsyQ.DispEnv[] _DispEnv { get; } = new PsyQ.DispEnv[2];
        private PsyQ.DrawEnv[] _DrawEnv { get; } = new PsyQ.DrawEnv[2];
        private int _EnvIndex;

        private Render _render = new Render();
        private PrimitiveSort _primitiveSort = new PrimitiveSort(65536);
        private CommandBuffer _commandBuffer = new CommandBuffer(65536);

        public Testing() {
            _DispEnv[0] = new PsyQ.DispEnv(new RectInt(0,             0, _ScreenWidth, _ScreenHeight));
            _DrawEnv[0] = new PsyQ.DrawEnv(new RectInt(0, _ScreenHeight, _ScreenWidth, _ScreenHeight), new Vector2Int(_ScreenWidth / 2, _ScreenHeight + (_ScreenHeight / 2)));
            _DispEnv[1] = new PsyQ.DispEnv(new RectInt(0, _ScreenHeight, _ScreenWidth, _ScreenHeight));
            _DrawEnv[1] = new PsyQ.DrawEnv(new RectInt(0,             0, _ScreenWidth, _ScreenHeight), new Vector2Int(_ScreenWidth / 2, _ScreenHeight / 2));

            _DrawEnv[0].Color = new Rgb888(0x10, 0x60, 0x10);
            _DrawEnv[0].IsClear = true;

            _DrawEnv[1].Color = new Rgb888(0x10, 0x60, 0x10);
            _DrawEnv[1].IsClear = true;

            _EnvIndex = 0;

            PsyQ.PutDispEnv(_DispEnv[0]);
            PsyQ.PutDrawEnv(_DrawEnv[0]);

            PsyQ.SetDispMask(true);

            PsyQ.DrawSync();

            PsyQ.ClearImage(new RectInt(0, 0, 1024, 512), Rgb888.Magenta);
            var timData = ResourceManager.GetBinaryFile("pebles.tim");
            if (PsyQ.TryReadTim(timData, out PsyQ.Tim tim)) {
                PsyQ.LoadImage(tim.ImageHeader.Rect, tim.Header.Flags.BitDepth, tim.Image);

                int _tPageId = PsyQ.GetTPage(tim.Header.Flags.BitDepth,
                                             (ushort)tim.ImageHeader.Rect.X,
                                             (ushort)tim.ImageHeader.Rect.Y);

                if (tim.Header.Flags.HasClut) {
                    int _clutId = PsyQ.LoadClut(tim.Cluts[0].Clut, (uint)tim.ClutHeader.P.X, (uint)tim.ClutHeader.P.Y);
                }
            }

            var tmdData = ResourceManager.GetBinaryFile("OUT.TMD");
            // var tmdData = ResourceManager.GetBinaryFile("VENUS3G.TMD");
            // var tmdData = ResourceManager.GetBinaryFile("SHUTTLE1.TMD");
            // var tmdData = ResourceManager.GetBinaryFile("CUBE3.TMD");
            // var tmdData = ResourceManager.GetBinaryFile("CUBE3G.TMD");
            // var tmdData = ResourceManager.GetBinaryFile("CUBE3GT.TMD");
            // var tmdData = ResourceManager.GetBinaryFile("tmd_0059.tmd");
            if (PsyQ.TryReadTmd(tmdData, out PsyQ.Tmd tmd)) {
                _model = new Model(tmd, null);
                _model.Material.AmbientColor = new Rgb888(15, 15, 15);
            }

            _model.Position = new Vector3(0f, 0f, 0f);
            _camera.Position = new Vector3(0f, 0f, -5f);
            // _camera.Yaw = -11;

            _light1 = LightingManager.AllocatePointLight();
            _light1.Color = Rgb888.Blue;
            _light1.Position = new Vector3(0f, 1f, 0f);
            _light1.CutOffDistance = 100f;
            _light1.Range = 20f;

            _light2 = LightingManager.AllocateDirectionalLight();
            _light2.Direction = new Vector3(0, -1, 0);
            _light2.Color = Rgb888.Red;

            _camera.Fov = 70;

            _flyCamera = new FlyCamera(_camera);
        }

        private const int _ScreenWidth  = 320;
        private const int _ScreenHeight = 240;

        private readonly Camera _camera = new Camera(_ScreenWidth, _ScreenHeight);
        private readonly FlyCamera _flyCamera;

        private Model _model;
        private PointLight _light1;
        private DirectionalLight _light2;

        public void Update() {
            _commandBuffer.Reset();
            _primitiveSort.Reset();

            if (!Psx.Input.HasFlag(JoyPad.Square)) {
                _flyCamera.Update();
            } else {
                Console.WriteLine($"[1;34m{_light1.Position}[m");

                if (Psx.Input.HasFlag(JoyPad.Triangle)) {
                    if ((Psx.Input & JoyPad.Up) == JoyPad.Up) {
                        _light1.Position += Vector3.UnitY * Psx.Time.DeltaTime;
                    }

                    if ((Psx.Input & JoyPad.Down) == JoyPad.Down) {
                        _light1.Position += -Vector3.UnitY * Psx.Time.DeltaTime;
                    }
                } else {
                    if ((Psx.Input & JoyPad.Up) == JoyPad.Up) {
                        _light1.Position += Vector3.UnitZ * Psx.Time.DeltaTime;
                    }

                    if ((Psx.Input & JoyPad.Down) == JoyPad.Down) {
                        _light1.Position += -Vector3.UnitZ * Psx.Time.DeltaTime;
                    }
                }

                if ((Psx.Input & JoyPad.Left) == JoyPad.Left) {
                    _light1.Position += -Vector3.UnitX * Psx.Time.DeltaTime;
                }

                if ((Psx.Input & JoyPad.Right) == JoyPad.Right) {
                    _light1.Position += Vector3.UnitX * Psx.Time.DeltaTime;
                }
            }

            _render.Camera = _camera;
            _render.Material = _model.Material;
            _render.ModelMatrix = _model.Matrix;
            _render.ModelViewMatrix = _camera.GetViewMatrix() * _model.Matrix;
            _render.CommandBuffer = _commandBuffer;
            _render.PrimitiveSort = _primitiveSort;

            Console.WriteLine($"_camera.Position: [1;32m{_camera.Position}, Pitch, Yaw: {_camera.Pitch}, {_camera.Yaw}[m");

            DrawTmd(_render, _model.Tmd);

            _primitiveSort.Sort();

            Console.WriteLine($"_commandBuffer.AllocatedCount: {_render.CommandBuffer.AllocatedCount}");

            PsyQ.DrawPrim(_primitiveSort, _commandBuffer);
            PsyQ.DrawSync();

            // Swap buffer
            _EnvIndex ^= 1;

            PsyQ.PutDispEnv(_DispEnv[_EnvIndex]);
            PsyQ.PutDrawEnv(_DrawEnv[_EnvIndex]);
        }

        private static void DrawTmd(Render render, PsyQ.Tmd tmd) {
            foreach (PsyQ.TmdObject tmdObject in tmd.Objects) {
                DrawTmdObject(render, tmdObject);
            }
        }

        private static void DrawTmdObject(Render render, PsyQ.TmdObject tmdObject) {
            for (int packetIndex = 0; packetIndex < tmdObject.Packets.Length; packetIndex++) {
                // Release all gen primitives here as any culled primitives
                // won't be released (due to continue(s) in loop)
                render.ReleaseGenPrimitives();

                PsyQ.TmdPacket tmdPacket = tmdObject.Packets[packetIndex];

                GenPrimitive genPrimitive = render.AcquireGenPrimitive();

                CollectPrimitiveVerticesData(render, tmdObject, tmdPacket, genPrimitive);

                TransformToView(render, genPrimitive);

                GenerateClipFlags(render, genPrimitive);

                // Cull primitive if it's outside of any of the six planes
                if (TestOutOfFustrum(genPrimitive)) {
                    // Console.WriteLine($"---------------- Cull ---------------- {genPrimitive.ClipFlags[0]} & {genPrimitive.ClipFlags[1]} & {genPrimitive.ClipFlags[2]} -> {genPrimitive.ViewPoints[0]}; {genPrimitive.ViewPoints[1]}; {genPrimitive.ViewPoints[2]}");
                    continue;
                }

                CollectRemainingPrimitiveData(render, tmdObject, tmdPacket, genPrimitive);

                genPrimitive.FaceNormal = CalculateScaledFaceNormal(genPrimitive.ViewPoints);

                // Perform backface culling unless it's "double sided"
                if ((tmdPacket.PrimitiveHeader.Flags & PsyQ.TmdPrimitiveFlags.Fce) != PsyQ.TmdPrimitiveFlags.Fce) {
                    if (TestBackFaceCull(genPrimitive)) {
                        // Console.WriteLine("---------------- Backface Cull ----------------");
                        continue;
                    }
                }

                genPrimitive.FaceNormal = Vector3.Normalize(genPrimitive.FaceNormal);

                foreach (GenPrimitive currentGenPrimitive in render.GenPrimitives) {
                    if ((currentGenPrimitive.Flags & GenPrimitiveFlags.Discarded) == GenPrimitiveFlags.Discarded) {
                        continue;
                    }

                    // Perform light source calculation
                    if ((tmdPacket.PrimitiveHeader.Flags & PsyQ.TmdPrimitiveFlags.Lgt) != PsyQ.TmdPrimitiveFlags.Lgt) {
                        TransformToWorld(render, currentGenPrimitive);

                        CalculateLighting(render, currentGenPrimitive);
                    }

                    // Get the distance from the primitive and calculate the
                    // subdivision level

                    // XXX: Move to a method that gets you the min/max/center of a primitive
                    float distanceSquared = System.Math.Min(currentGenPrimitive.ViewPoints[0].Z,
                                                            System.Math.Min(currentGenPrimitive.ViewPoints[1].Z,
                                                                            currentGenPrimitive.ViewPoints[2].Z));

                    // This is all janky, but the bigger the divisor, the more
                    // area subdivision is performed
                    // XXX: Move the 1f value somewhere... Render maybe?
                    int subdivLevel = 2 - System.Math.Min(2, (int)(distanceSquared / 1f));

                    if (subdivLevel > 0) {
                        if (currentGenPrimitive.VertexCount == 3) {
                            SubdivideTriangleGenPrimitive(render,
                                                          currentGenPrimitive,
                                                          SubdivTriple.FromGenPrimitive(currentGenPrimitive, 0),
                                                          SubdivTriple.FromGenPrimitive(currentGenPrimitive, 1),
                                                          SubdivTriple.FromGenPrimitive(currentGenPrimitive, 2),
                                                          subdivLevel);
                        } else {
                            SubdivideQuadGenPrimitive(render,
                                                      currentGenPrimitive,
                                                      SubdivTriple.FromGenPrimitive(currentGenPrimitive, 0),
                                                      SubdivTriple.FromGenPrimitive(currentGenPrimitive, 1),
                                                      SubdivTriple.FromGenPrimitive(currentGenPrimitive, 2),
                                                      SubdivTriple.FromGenPrimitive(currentGenPrimitive, 3),
                                                      subdivLevel);
                        }

                        GenPrimitive.Discard(currentGenPrimitive);
                    }
                }

                foreach (GenPrimitive currentGenPrimitive in render.GenPrimitives) {
                    if ((currentGenPrimitive.Flags & GenPrimitiveFlags.Discarded) == GenPrimitiveFlags.Discarded) {
                        continue;
                    }

                    TransformToScreen(render, currentGenPrimitive);

                    if (TestScreenPointOverflow(currentGenPrimitive)) {
                        // Console.WriteLine("[1;31mOverflow[m");
                        continue;
                    }

                    if (TestScreenPrimitiveArea(currentGenPrimitive)) {
                        // Console.WriteLine("[1;31mArea<=0[m");
                        continue;
                    }

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

                if (TestOutOfFustrum(genPrimitive)) {
                    // Console.WriteLine($"---------------- Subdiv Cull ---------------- {genPrimitive.ClipFlags[0]} & {genPrimitive.ClipFlags[1]} & {genPrimitive.ClipFlags[2]} -> {genPrimitive.ViewPoints[0]}; {genPrimitive.ViewPoints[1]}; {genPrimitive.ViewPoints[2]}");
                    GenPrimitive.Discard(genPrimitive);
                } else {
                    genPrimitive.GouraudShadingColors[0] = spa.GouraudShadingColor;
                    genPrimitive.GouraudShadingColors[1] = spb.GouraudShadingColor;
                    genPrimitive.GouraudShadingColors[2] = spc.GouraudShadingColor;

                    genPrimitive.Texcoords[0] = spa.Texcoord;
                    genPrimitive.Texcoords[1] = spb.Texcoord;
                    genPrimitive.Texcoords[2] = spc.Texcoord;

                    ClipTriangleGenPrimitiveNearPlane(render, genPrimitive);
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

        private static Random _Random = new Random(); // XXX: Remove

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

                if (TestOutOfFustrum(genPrimitive)) {
                    // Console.WriteLine($"---------------- Subdiv Cull ---------------- {genPrimitive.ClipFlags[0]} & {genPrimitive.ClipFlags[1]} & {genPrimitive.ClipFlags[2]} -> {genPrimitive.ViewPoints[0]}; {genPrimitive.ViewPoints[1]}; {genPrimitive.ViewPoints[2]}");
                    GenPrimitive.Discard(genPrimitive);
                } else {
                    genPrimitive.GouraudShadingColors[0] = spa.GouraudShadingColor;
                    genPrimitive.GouraudShadingColors[1] = spb.GouraudShadingColor;
                    genPrimitive.GouraudShadingColors[2] = spc.GouraudShadingColor;
                    genPrimitive.GouraudShadingColors[2] = spd.GouraudShadingColor;

                    genPrimitive.Texcoords[0] = spa.Texcoord;
                    genPrimitive.Texcoords[1] = spb.Texcoord;
                    genPrimitive.Texcoords[2] = spc.Texcoord;
                    genPrimitive.Texcoords[3] = spd.Texcoord;

                    var triangleGenPrimitives = TriangulateQuadGenPrimitive(render, genPrimitive);

                    ClipTriangleGenPrimitiveNearPlane(render, triangleGenPrimitives[0]);
                    ClipTriangleGenPrimitiveNearPlane(render, triangleGenPrimitives[1]);
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

        private static void ClipTriangleGenPrimitiveNearPlane(Render render, GenPrimitive genPrimitive) {
            ClipFlags clipFlagsMask = genPrimitive.ClipFlags[0] | genPrimitive.ClipFlags[1] | genPrimitive.ClipFlags[2];

            if ((clipFlagsMask & ClipFlags.Near) != ClipFlags.Near) {
                // Console.WriteLine($"--------> No near Clip ({genPrimitive.ClipFlags[0]}) ({genPrimitive.ClipFlags[1]}) ({genPrimitive.ClipFlags[2]})");
                return;
            }

            if (TestOutOfFustrum(genPrimitive)) {
                // Console.WriteLine($"--------> Cull ({genPrimitive.ClipFlags[0]}) ({genPrimitive.ClipFlags[1]}) ({genPrimitive.ClipFlags[2]})");
                GenPrimitive.Discard(genPrimitive);
                return;
            }

            Span<int> interiorVertexIndices = stackalloc int[3];
            Span<int> exteriorVertexIndices = stackalloc int[3];

            int interiorVertexCount = 0;

            // Determine which case to cover and find the interior/exterior vertices
            for (int vertexIndex = 0, i = 0, j = 0; vertexIndex < genPrimitive.VertexCount; vertexIndex++) {
                if ((genPrimitive.ClipFlags[vertexIndex] & ClipFlags.Near) == ClipFlags.Near) {
                    exteriorVertexIndices[i] = vertexIndex;
                    i++;
                } else {
                    interiorVertexCount++;
                    interiorVertexIndices[j] = vertexIndex;
                    j++;
                }
            }

            // XXX: Why do we check?
            if ((interiorVertexCount == 0) || (interiorVertexCount == 3)) {
                Console.WriteLine($"Weird interior vtx count ({genPrimitive.ClipFlags[0]}) ({genPrimitive.ClipFlags[1]}) ({genPrimitive.ClipFlags[2]})");
                GenPrimitive.Discard(genPrimitive);
                return;
            }

            // Case 1: One interior vertex and two exterior vertices
            if (interiorVertexCount == 1) {
                Span<int> vertexIndices = new int[3] {
                     interiorVertexIndices[0],
                    (interiorVertexIndices[0] + 1) % genPrimitive.VertexCount,
                    (interiorVertexIndices[0] + 2) % genPrimitive.VertexCount
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
            } else { // Case 2: Two interior vertices and one exterior vertex
                Span<int> vertexIndices = stackalloc int[3] {
                     exteriorVertexIndices[0],
                    (exteriorVertexIndices[0] + 1) % genPrimitive.VertexCount,
                    (exteriorVertexIndices[0] + 2) % genPrimitive.VertexCount
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
            }
        }

        private static GenPrimitive[] _TriangulateQuadGenPrimitives { get; } = new GenPrimitive[2];

        private static ReadOnlySpan<GenPrimitive> TriangulateQuadGenPrimitive(Render render, GenPrimitive degenerateGenPrim) {
            const int V0 = 2;
            const int V1 = 1;
            const int V2 = 3;

            GenPrimitive otherTriGenPrim = render.AcquireGenPrimitive();

            degenerateGenPrim.Type = (PsyQ.TmdPrimitiveType)(degenerateGenPrim.Type - PsyQ.TmdPrimitiveType.F4);
            degenerateGenPrim.VertexCount = 3;
            degenerateGenPrim.NormalCount = (degenerateGenPrim.NormalCount >= 4) ? 3 : degenerateGenPrim.NormalCount;

            otherTriGenPrim.Type = degenerateGenPrim.Type;
            otherTriGenPrim.VertexCount = degenerateGenPrim.VertexCount;
            otherTriGenPrim.NormalCount = degenerateGenPrim.NormalCount;

            otherTriGenPrim.PolygonVertices[0] = degenerateGenPrim.PolygonVertices[V0];
            otherTriGenPrim.PolygonVertices[1] = degenerateGenPrim.PolygonVertices[V1];
            otherTriGenPrim.PolygonVertices[2] = degenerateGenPrim.PolygonVertices[V2];

            otherTriGenPrim.PolygonNormals[0] = degenerateGenPrim.PolygonNormals[V0];
            otherTriGenPrim.PolygonNormals[1] = degenerateGenPrim.PolygonNormals[V1];
            otherTriGenPrim.PolygonNormals[2] = degenerateGenPrim.PolygonNormals[V2];

            otherTriGenPrim.ViewPoints[0] = degenerateGenPrim.ViewPoints[V0];
            otherTriGenPrim.ViewPoints[1] = degenerateGenPrim.ViewPoints[V1];
            otherTriGenPrim.ViewPoints[2] = degenerateGenPrim.ViewPoints[V2];

            otherTriGenPrim.Texcoords[0] = degenerateGenPrim.Texcoords[V0];
            otherTriGenPrim.Texcoords[1] = degenerateGenPrim.Texcoords[V1];
            otherTriGenPrim.Texcoords[2] = degenerateGenPrim.Texcoords[V2];

            otherTriGenPrim.GouraudShadingColors[0] = degenerateGenPrim.GouraudShadingColors[V0];
            otherTriGenPrim.GouraudShadingColors[1] = degenerateGenPrim.GouraudShadingColors[V1];
            otherTriGenPrim.GouraudShadingColors[2] = degenerateGenPrim.GouraudShadingColors[V2];

            otherTriGenPrim.ClipFlags[0] = degenerateGenPrim.ClipFlags[V0];
            otherTriGenPrim.ClipFlags[1] = degenerateGenPrim.ClipFlags[V1];
            otherTriGenPrim.ClipFlags[2] = degenerateGenPrim.ClipFlags[V2];

            otherTriGenPrim.FaceNormal = degenerateGenPrim.FaceNormal;
            otherTriGenPrim.TPageId = degenerateGenPrim.TPageId;
            otherTriGenPrim.ClutId = degenerateGenPrim.ClutId;

            _TriangulateQuadGenPrimitives[0] = degenerateGenPrim;
            _TriangulateQuadGenPrimitives[1] = otherTriGenPrim;

            return new ReadOnlySpan<GenPrimitive>(_TriangulateQuadGenPrimitives);
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

        private static Texcoord ClipLerpTexcoord(Render render, Texcoord aTexcoord, Texcoord bTexcoord, float t) {
            Vector2 aVector = new Vector2(aTexcoord.X, aTexcoord.Y);
            Vector2 bVector = new Vector2(bTexcoord.X, bTexcoord.Y);
            Vector2 lerpedVertex = Vector2.Lerp(aVector, bVector, t);

            return new Texcoord((byte)lerpedVertex.X, (byte)lerpedVertex.Y);
        }

        private static void CollectPrimitiveVerticesData(Render render, PsyQ.TmdObject tmdObject, PsyQ.TmdPacket tmdPacket, GenPrimitive genPrimitive) {
            genPrimitive.VertexCount = tmdPacket.Primitive.VertexCount;

            genPrimitive.PolygonVertices[0] = tmdObject.Vertices[tmdPacket.Primitive.IndexV0];
            genPrimitive.PolygonVertices[1] = tmdObject.Vertices[tmdPacket.Primitive.IndexV1];
            genPrimitive.PolygonVertices[2] = tmdObject.Vertices[System.Math.Max(tmdPacket.Primitive.IndexV2, 0)];
            genPrimitive.PolygonVertices[3] = tmdObject.Vertices[System.Math.Max(tmdPacket.Primitive.IndexV3, 0)];
        }

        private static void CollectRemainingPrimitiveData(Render render, PsyQ.TmdObject tmdObject, PsyQ.TmdPacket tmdPacket, GenPrimitive genPrimitive) {
            genPrimitive.NormalCount = tmdPacket.Primitive.NormalCount;
            genPrimitive.Type = tmdPacket.Primitive.Type;

            if (tmdPacket.Primitive.NormalCount > 0) {
                genPrimitive.PolygonNormals[0] = tmdObject.Normals[tmdPacket.Primitive.IndexN0];
                genPrimitive.PolygonNormals[1] = (tmdPacket.Primitive.IndexN1 >= 0) ? tmdObject.Normals[tmdPacket.Primitive.IndexN1] : genPrimitive.PolygonNormals[0];
                genPrimitive.PolygonNormals[2] = (tmdPacket.Primitive.IndexN2 >= 0) ? tmdObject.Normals[tmdPacket.Primitive.IndexN2] : genPrimitive.PolygonNormals[0];
                genPrimitive.PolygonNormals[3] = (tmdPacket.Primitive.IndexN3 >= 0) ? tmdObject.Normals[tmdPacket.Primitive.IndexN3] : genPrimitive.PolygonNormals[0];
            }

            if ((tmdPacket.PrimitiveHeader.Mode & PsyQ.TmdPrimitiveMode.Tme) == PsyQ.TmdPrimitiveMode.Tme) {
                genPrimitive.Texcoords[0] = tmdPacket.Primitive.T0;
                genPrimitive.Texcoords[1] = tmdPacket.Primitive.T1;
                genPrimitive.Texcoords[2] = tmdPacket.Primitive.T2;
                genPrimitive.Texcoords[3] = tmdPacket.Primitive.T3;

                genPrimitive.TPageId = tmdPacket.Primitive.Tsb.Value;
                genPrimitive.ClutId = tmdPacket.Primitive.Cba.Value;
            }

            genPrimitive.GouraudShadingColors[0] = tmdPacket.Primitive.C0;
            genPrimitive.GouraudShadingColors[1] = tmdPacket.Primitive.C1;
            genPrimitive.GouraudShadingColors[2] = tmdPacket.Primitive.C2;
            genPrimitive.GouraudShadingColors[3] = tmdPacket.Primitive.C3;
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

        private static bool TestOutOfFustrum(GenPrimitive genPrimitive) =>
            // If vertex count is 3, third vertex (index 2) clip flag will be
            // bitwise AND'd twice. If vertex count is 4, (index 3) will be
            // bitwise AND'd
            ((genPrimitive.ClipFlags[0] & genPrimitive.ClipFlags[1] & genPrimitive.ClipFlags[2] & genPrimitive.ClipFlags[genPrimitive.VertexCount - 1]) != ClipFlags.None);

        private static void CalculateLighting(Render render, GenPrimitive genPrimitive) {
            Span<Vector3> lightIntensityVectors = stackalloc Vector3[genPrimitive.NormalCount];

            int lightCount = LightingManager.PointLights.Count + LightingManager.DirectionalLights.Count;
            float normalizeVectorFactor = 1f / 255f;

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

                lightIntensityVectors[index] = lightVector * normalizeVectorFactor;
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

        private static bool TestBackFaceCull(GenPrimitive genPrimitive) =>
            (Vector3.Dot(-genPrimitive.ViewPoints[0], genPrimitive.FaceNormal) <= 0f);

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

        private static Vector3 CalculateScaledFaceNormal(Vector3[] points) {
            Vector3 a = points[2] - points[0];
            Vector3 b = points[1] - points[0];

            return Vector3.Cross(a, b);
        }

        private static void TransformToWorld(Render render, GenPrimitive genPrimitive) {
            for (int i = 0; i < genPrimitive.VertexCount; i++) {
                genPrimitive.WorldPoints[i] = Vector3.Transform(genPrimitive.PolygonVertices[i], render.ModelMatrix);
            }
        }

        private static void TransformToView(Render render, GenPrimitive genPrimitive) {
            for (int i = 0; i < genPrimitive.VertexCount; i++) {
                genPrimitive.ViewPoints[i] = Vector3.Transform(genPrimitive.PolygonVertices[i], render.ModelViewMatrix);
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
