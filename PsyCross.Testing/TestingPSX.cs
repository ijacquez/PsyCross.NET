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
        private Render _render = new Render();
        private PrimitiveSort _primitiveSort = new PrimitiveSort(65536);
        private CommandBuffer _commandBuffer = new CommandBuffer(65536);

        public Testing() {
            // _renderStates[0].DispEnv = new PsyQ.DispEnv(new RectInt(0,                    0, _camera.ScreenWidth, _camera.ScreenHeight));
            // _renderStates[0].DrawEnv = new PsyQ.DrawEnv(new RectInt(0, _camera.ScreenHeight, _camera.ScreenWidth, _camera.ScreenHeight), new Vector2Int(0, _camera.ScreenHeight));
            // _renderStates[1].DispEnv = new PsyQ.DispEnv(new RectInt(0, _camera.ScreenHeight, _camera.ScreenWidth, _camera.ScreenHeight));
            // _renderStates[1].DrawEnv = new PsyQ.DrawEnv(new RectInt(0,                    0, _camera.ScreenWidth, _camera.ScreenHeight), new Vector2Int(0, 0));

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

            PsyQ.ClearImage(new RectShort(0, 0, 1024, 512), Rgb888.Yellow);
            // var timData = ResourceManager.GetBinaryFile("PAT4T.TIM");
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

            var tmdData = ResourceManager.GetBinaryFile("OUT.TMD");
            // var tmdData = ResourceManager.GetBinaryFile("VENUS3G.TMD");
            // var tmdData = ResourceManager.GetBinaryFile("SHUTTLE1.TMD");
            // var tmdData = ResourceManager.GetBinaryFile("CUBE3.TMD");
            // var tmdData = ResourceManager.GetBinaryFile("CUBE3G.TMD");
            // var tmdData = ResourceManager.GetBinaryFile("CUBE3GT.TMD");
            // var tmdData = ResourceManager.GetBinaryFile("tmd_0059.tmd");
            if (PsyQ.TryReadTmd(tmdData, out PsyQ.Tmd tmd)) {
                _model = new Model(tmd, null);
            }

            _model.Position = new Vector3(0, 0, 0);
            _camera.Position = new Vector3(0, 1, -10);
            // _camera.Position = new Vector3(-5.0768924f, 1.2252761f, -5.8041654f);

            _light1 = LightingManager.AllocatePointLight();
            _light1.Color = Rgb888.White;
            _light1.Position = new Vector3(0, 0, 0.5f);
            _light1.ConstantAttenuation = 1.0f;
            _light1.DiffuseIntensity = 0.5f;
            _light1.CutOffDistance = 3.0f;

            // var light2 = LightingManager.AllocatePointLight();
            // light2.Position = new Vector3(-0.5f, 0.5f, 0.5f);
            _light2 = LightingManager.AllocateDirectionalLight();
            _light2.Direction = new Vector3(0, 0.707f, 0.707f);
            _light2.ConstantAttenuation = 0.1f;
            _light2.DiffuseIntensity = 1.0f;
            _light2.Color = Rgb888.Yellow;

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

        // private float yaw = 0.0f;
        // private float pitch = 0.0f;
        // private float roll = 0.0f;

        public void Update() {
            _commandBuffer.Reset();
            _primitiveSort.Reset();

            // _model.Rotation = Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll);
            // yaw += Psx.Time.DeltaTime * (30.0f * _Deg2Rad);
            // pitch += Psx.Time.DeltaTime * (15.0f * _Deg2Rad);
            // roll += Psx.Time.DeltaTime * (180.0f * _Deg2Rad);

            _flyCamera.Update();

            _render.Camera = _camera;
            _render.ModelMatrix = _model.Matrix;
            _render.ModelViewMatrix = _camera.GetViewMatrix() * _model.Matrix;
            _render.CommandBuffer = _commandBuffer;
            _render.PrimitiveSort = _primitiveSort;

            Console.WriteLine($"_camera.Position: [1;32m{_camera.Position}[m");

            DrawTmd(_render, _model.Tmd);

            _primitiveSort.Sort();

            Console.WriteLine($"_commandBuffer.AllocatedCount: {_render.CommandBuffer.AllocatedCount}");

            PsyQ.DrawPrim(_primitiveSort, _commandBuffer);
            PsyQ.DrawSync();

            // Swap buffer
            _renderStateIndex ^= 1;

            PsyQ.PutDispEnv(_renderStates[_renderStateIndex].DispEnv);
            PsyQ.PutDrawEnv(_renderStates[_renderStateIndex].DrawEnv);
        }

        private static void DrawTmd(Render render, PsyQ.Tmd tmd) {
            foreach (PsyQ.TmdObject tmdObject in tmd.Objects) {
                DrawTmdObject(render, tmdObject);
            }
        }

        private static void DrawTmdObject(Render render, PsyQ.TmdObject tmdObject) {
            for (int packetIndex = 0; packetIndex < tmdObject.Packets.Length; packetIndex++) {
                // Release here as any culled primitives won't be released (due to continue in loop)
                render.ReleaseGenPrimitives();

                PsyQ.TmdPacket tmdPacket = tmdObject.Packets[packetIndex];

                GenPrimitive genPrimitive = render.AcquireGenPrimitive();

                CollectPolygonalData(render, tmdObject, tmdPacket, genPrimitive);

                TransformToView(render, genPrimitive);

                GenerateClipFlags(render, genPrimitive);

                // Cull primitive if it's outside of any of the six planes
                if (TestOutOfFustrum(genPrimitive)) {
                    // Console.WriteLine($"---------------- Cull ---------------- {genPrimitive.ClipFlags[0]} & {genPrimitive.ClipFlags[1]} & {genPrimitive.ClipFlags[2]} -> {genPrimitive.ViewPoints[0]}; {genPrimitive.ViewPoints[1]}; {genPrimitive.ViewPoints[2]}");
                    continue;
                }

                // Perform backface culling unless it's "double sided"
                if ((tmdPacket.PrimitiveHeader.Flags & PsyQ.TmdPrimitiveFlags.Fce) != PsyQ.TmdPrimitiveFlags.Fce) {
                    if (TestBackFaceCull(genPrimitive)) {
                        // Console.WriteLine("---------------- Backface Cull ----------------");
                        continue;
                    }
                }

                // Clip any primitives intersecting with the near plane
                if (((genPrimitive.ClipFlags[0] | genPrimitive.ClipFlags[1] | genPrimitive.ClipFlags[2]) & ClipFlags.Near) == ClipFlags.Near) {
                    Span<int> interiorVertexIndices = stackalloc int[2];
                    Span<int> exteriorVertexIndices = stackalloc int[2];
                    int interiorVertexCount = 0;

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

                    if (interiorVertexCount > 0) {
                        // Case 1: One interior vertex and two exterior vertices
                        if (interiorVertexCount == 1) {
                            Span<int> vertexIndices = new int[3] {
                                 interiorVertexIndices[0],
                                (interiorVertexIndices[0] + 1) % genPrimitive.VertexCount,
                                (interiorVertexIndices[0] + 2) % genPrimitive.VertexCount
                            };

                            ref Vector3 interiorVertex = ref genPrimitive.ViewPoints[vertexIndices[0]];
                            ref Vector3 exteriorV1 = ref genPrimitive.ViewPoints[vertexIndices[1]];
                            ref Vector3 exteriorV2 = ref genPrimitive.ViewPoints[vertexIndices[2]];

                            // Interpolate between edge (v0,v1) and find the
                            // point along the edge that intersects with the
                            // near plane

                            // Overwrite vertex
                            exteriorV1 = ClipLerpVertex(render, interiorVertex, exteriorV1);
                            // Overwrite vertex
                            exteriorV2 = ClipLerpVertex(render, interiorVertex, exteriorV2);

                            // XXX: Recalculate normal
                            // XXX: Recalculate texcoords
                        } else { // Case 2: Two interior vertices and one exterior vertex
                            Span<int> vertexIndices = stackalloc int[3] {
                                exteriorVertexIndices[0],
                                (exteriorVertexIndices[0] + 1) % genPrimitive.VertexCount,
                                (exteriorVertexIndices[0] + 2) % genPrimitive.VertexCount
                            };

                            GenPrimitive newGenPrimitive = render.AcquireGenPrimitive(genPrimitive);

                            Vector3 exteriorVertex = genPrimitive.ViewPoints[vertexIndices[0]];
                            Vector3 interiorV1 = genPrimitive.ViewPoints[vertexIndices[1]];
                            Vector3 interiorV2 = genPrimitive.ViewPoints[vertexIndices[2]];

                            Vector3 lerpedV1 = ClipLerpVertex(render, interiorV1, exteriorVertex);
                            Vector3 lerpedV2 = ClipLerpVertex(render, interiorV2, exteriorVertex);

                            // Generate two points and from that, pass in the quad
                            genPrimitive.ViewPoints[vertexIndices[0]] = lerpedV1;

                            newGenPrimitive.ViewPoints[vertexIndices[0]] = lerpedV1;
                            newGenPrimitive.ViewPoints[vertexIndices[1]] = lerpedV2;
                            newGenPrimitive.ViewPoints[vertexIndices[2]] = interiorV2;

                            // XXX: Recalculate normal
                            // XXX: Recalculate texcoords
                        }
                    }
                }

                // XXX: Move this to another method
                foreach (GenPrimitive currentGenPrimitive in render.GenPrimitives) {
                    TransformToClip(render, currentGenPrimitive);

                    TransformToScreen(render, currentGenPrimitive);

                    if (TestScreenPointOverflow(currentGenPrimitive)) {
                        // Console.WriteLine($"Overflow vertex cull -> {currentGenPrimitive.PrimitiveType}");
                        continue;
                    }

                    // Perform light source calculation
                    if ((tmdPacket.PrimitiveHeader.Flags & PsyQ.TmdPrimitiveFlags.Lgt) != PsyQ.TmdPrimitiveFlags.Lgt) {
                        CalculateLighting(render, currentGenPrimitive);
                    }

                    DrawPrimitive(render, tmdPacket, currentGenPrimitive);
                }
            }
        }

        private static void DrawPrimitive(Render render, PsyQ.TmdPacket tmdPacket, GenPrimitive genPrimitive) {
            switch (genPrimitive.PrimitiveType) {
                case PsyQ.TmdPrimitiveType.F3:
                    DrawPrimitiveF3(tmdPacket, render, genPrimitive);
                    break;
                case PsyQ.TmdPrimitiveType.Ft3:
                    DrawPrimitiveFt3(render, genPrimitive);
                    break;
                case PsyQ.TmdPrimitiveType.G3:
                    DrawPrimitiveG3(render, genPrimitive);
                    break;
                case PsyQ.TmdPrimitiveType.Gt3:
                    DrawPrimitiveGt3(render, genPrimitive);
                    break;
                case PsyQ.TmdPrimitiveType.F4:
                    // DrawPrimitiveF4(render, genPrimitive);
                    break;
                case PsyQ.TmdPrimitiveType.Ft4:
                    // DrawPrimitiveFt4(render, genPrimitive);
                    break;
                case PsyQ.TmdPrimitiveType.G4:
                    DrawPrimitiveG4(render, genPrimitive);
                    break;
                case PsyQ.TmdPrimitiveType.Gt4:
                    DrawPrimitiveGt4(render, genPrimitive);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private static Vector3 ClipLerpVertex(Render render, Vector3 aVertex, Vector3 bVertex) {
            float f = bVertex.Z - aVertex.Z;
            float t = (render.Camera.DepthNear - aVertex.Z) / f;
            Vector3 l = Vector3.Lerp(aVertex, bVertex, t);

            return new Vector3(l.X, l.Y, render.Camera.DepthNear);
        }

        private static void CollectPolygonalData(Render render, PsyQ.TmdObject tmdObject, PsyQ.TmdPacket tmdPacket, GenPrimitive genPrimitive) {
            genPrimitive.VertexCount = tmdPacket.Primitive.VertexCount;
            genPrimitive.PrimitiveType = tmdPacket.PrimitiveType;

            genPrimitive.PolygonVertices[0] = tmdObject.Vertices[tmdPacket.Primitive.IndexV0];
            genPrimitive.PolygonVertices[1] = tmdObject.Vertices[tmdPacket.Primitive.IndexV1];

            if (genPrimitive.VertexCount >= 3) {
                genPrimitive.PolygonVertices[2] = tmdObject.Vertices[tmdPacket.Primitive.IndexV2];

                if (genPrimitive.VertexCount == 4) {
                    genPrimitive.PolygonVertices[3] = tmdObject.Vertices[tmdPacket.Primitive.IndexV3];
                }
            }

            genPrimitive.PolygonNormals[0] = tmdObject.Normals[tmdPacket.Primitive.IndexN0];

            if (tmdPacket.Primitive.NormalCount >= 3) {
                genPrimitive.PolygonNormals[1] = tmdObject.Normals[tmdPacket.Primitive.IndexN1];
                genPrimitive.PolygonNormals[2] = tmdObject.Normals[tmdPacket.Primitive.IndexN2];
            }

            if (tmdPacket.Primitive.NormalCount == 4) {
                genPrimitive.PolygonNormals[3] = tmdObject.Normals[tmdPacket.Primitive.IndexN3];
            }

            // XXX: Debugging a triangle
            // genPrimitive.PolygonVertices[0] = new Vector3(-10,  10, 0);
            // genPrimitive.PolygonVertices[1] = new Vector3( 10, -10, 0);
            // genPrimitive.PolygonVertices[2] = new Vector3( 10,  10, 0);

            genPrimitive.Texcoords[0] = tmdPacket.Primitive.T0;

            if (tmdPacket.Primitive.NormalCount >= 3) {
                genPrimitive.Texcoords[1] = tmdPacket.Primitive.T1;
                genPrimitive.Texcoords[2] = tmdPacket.Primitive.T2;
            }

            if (tmdPacket.Primitive.NormalCount == 4) {
                genPrimitive.Texcoords[3] = tmdPacket.Primitive.T3;
            }

            genPrimitive.TPageId = tmdPacket.Primitive.Tsb.Value;
            genPrimitive.ClutId = tmdPacket.Primitive.Cba.Value;
        }

        private static void GenerateClipFlags(Render render, GenPrimitive genPrimitive) {
            float zFactor = 1.0f / System.MathF.Tan(MathHelper.DegreesToRadians(render.Camera.Fov * 0.5f));

            for (int i = 0; i < genPrimitive.VertexCount; i++) {
                genPrimitive.ClipFlags[i] = ClipFlags.None;

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

        private static bool TestOutOfFustrum(GenPrimitive genPrimitive) {
            return (genPrimitive.ClipFlags[0] & genPrimitive.ClipFlags[1] & genPrimitive.ClipFlags[2]) != ClipFlags.None;
        }

        private static void CalculateLighting(Render render, GenPrimitive genPrimitive) {
            for (int vertexIndex = 0; vertexIndex < genPrimitive.VertexCount; vertexIndex++) {
                Vector3 normal = Vector3.TransformNormal(genPrimitive.PolygonNormals[vertexIndex], render.ModelViewMatrix);

                Vector3 lightVector = Vector3.Zero;

                for (int lightIndex = 0; lightIndex < LightingManager.PointLights.Count; lightIndex++) {
                    PointLight light = LightingManager.PointLights[lightIndex];
                    Vector3 distance = light.Position - genPrimitive.ClipPoints[vertexIndex];

                    lightVector += CalculateLightVector(light, distance, normal);
                }

                for (int lightIndex = 0; lightIndex < LightingManager.DirectionalLights.Count; lightIndex++) {
                    DirectionalLight light = LightingManager.DirectionalLights[lightIndex];
                    Vector3 distance = -light.Direction;

                    lightVector += CalculateLightVector(light, distance, normal);
                }

                Vector3 clampedLightVector = Vector3.Min(lightVector, 255.0f * Vector3.One);

                genPrimitive.GouraudShadingColors[vertexIndex].R = (byte)clampedLightVector.X;
                genPrimitive.GouraudShadingColors[vertexIndex].G = (byte)clampedLightVector.Y;
                genPrimitive.GouraudShadingColors[vertexIndex].B = (byte)clampedLightVector.Z;
            }
        }

        private static Vector3 CalculateLightVector(Light light, Vector3 distance, Vector3 normal) {
            // XXX: Move this out
            Rgb888 matAmbientColor = new Rgb888(15, 15, 15);
            // XXX: Move this out
            Rgb888 matDiffuseColor = new Rgb888(15, 15, 15);

            float distanceLength = distance.Length();

            if (distanceLength >= light.CutOffDistance) {
                return Vector3.Zero;
            }

            float diffuseColorR = light.Color.R + matDiffuseColor.R;
            float diffuseColorG = light.Color.G + matDiffuseColor.G;
            float diffuseColorB = light.Color.B + matDiffuseColor.B;

            Vector3 l = Vector3.Normalize(distance);

            float intensity = System.Math.Max(Vector3.Dot(normal, l), 0.0f);
            float attenuation = 1.0f / (1.0f + (light.ConstantAttenuation * distanceLength));

            float r = attenuation * (intensity * light.DiffuseIntensity * diffuseColorR) + matAmbientColor.R;
            float g = attenuation * (intensity * light.DiffuseIntensity * diffuseColorG) + matAmbientColor.G;
            float b = attenuation * (intensity * light.DiffuseIntensity * diffuseColorB) + matAmbientColor.B;

            return new Vector3(r, g, b);
        }

        private static bool TestBackFaceCull(GenPrimitive genPrimitive) {
            Vector3 faceNormal = CalculateScaledFaceNormal(genPrimitive.ViewPoints);

            return (Vector3.Dot(-genPrimitive.ViewPoints[0], faceNormal) <= 0.0f);
        }

        private static bool TestScreenPointOverflow(GenPrimitive genPrimitive) {
            // Vertices have a range of [-1024,1023] even though each component
            // is 16-bit. If any of the components exceed the range, we need to
            // cull the primitive entirely. Otherwise, we will see graphical
            // errors
            for (int i = 0; i < genPrimitive.VertexCount; i++) {
                int sx = genPrimitive.ScreenPoints[i].X;
                int sy = genPrimitive.ScreenPoints[i].Y;

                if ((genPrimitive.ScreenPoints[i].X < -1024) || (genPrimitive.ScreenPoints[i].X > 1023) ||
                    (genPrimitive.ScreenPoints[i].Y < -1024) || (genPrimitive.ScreenPoints[i].Y > 1023)) {

                    Console.WriteLine($"Overflow: {genPrimitive.ScreenPoints[i]}");

                    return true;
                }
            }

            return false;
        }

        private static void DrawPrimitiveF3(PsyQ.TmdPacket tmdPacket, Render render, GenPrimitive genPrimitive) {
            // XXX: Remove use of color?
            var primitive = (PsyQ.TmdPrimitiveF3)tmdPacket.Primitive;

            var handle = render.CommandBuffer.AllocatePolyF3();
            var poly = render.CommandBuffer.GetPolyF3(handle);

            poly[0].SetCommand();
            poly[0].Color = primitive.Color;
            poly[0].P0 = genPrimitive.ScreenPoints[0];
            poly[0].P1 = genPrimitive.ScreenPoints[1];
            poly[0].P2 = genPrimitive.ScreenPoints[2];

            render.PrimitiveSort.Add(genPrimitive.ViewPoints, PrimitiveSortPoint.Center, handle);
        }

        private static void DrawPrimitiveFt3(Render render, GenPrimitive genPrimitive) {
            var handle = render.CommandBuffer.AllocatePolyFt3();
            var poly = render.CommandBuffer.GetPolyFt3(handle);

            poly[0].SetCommand();
            poly[0].Color = Rgb888.White;
            poly[0].T0 = genPrimitive.Texcoords[0];
            poly[0].T1 = genPrimitive.Texcoords[1];
            poly[0].T2 = genPrimitive.Texcoords[2];
            poly[0].TPageId = genPrimitive.TPageId;
            poly[0].ClutId = genPrimitive.ClutId;
            poly[0].P0 = genPrimitive.ScreenPoints[0];
            poly[0].P1 = genPrimitive.ScreenPoints[1];
            poly[0].P2 = genPrimitive.ScreenPoints[2];

            render.PrimitiveSort.Add(genPrimitive.ViewPoints, PrimitiveSortPoint.Center, handle);
        }

        private static void DrawPrimitiveG3(Render render, GenPrimitive genPrimitive) {
            var handle = render.CommandBuffer.AllocatePolyG3();
            var poly = render.CommandBuffer.GetPolyG3(handle);

            poly[0].SetCommand();

            poly[0].C0 = Rgb888.Red;
            poly[0].C1 = Rgb888.Green;
            poly[0].C2 = Rgb888.Blue;
            // poly[0].C0 = genPrimitive.GouraudShadingColors[0];
            // poly[0].C1 = genPrimitive.GouraudShadingColors[1];
            // poly[0].C2 = genPrimitive.GouraudShadingColors[2];

            poly[0].P0 = genPrimitive.ScreenPoints[0];
            poly[0].P1 = genPrimitive.ScreenPoints[1];
            poly[0].P2 = genPrimitive.ScreenPoints[2];

            render.PrimitiveSort.Add(genPrimitive.ViewPoints, PrimitiveSortPoint.Center, handle);
        }

        private static void DrawPrimitiveGt3(Render render, GenPrimitive genPrimitive) {
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

            render.PrimitiveSort.Add(genPrimitive.ViewPoints, PrimitiveSortPoint.Center, handle);
        }

        private static void DrawPrimitiveG4(Render render, GenPrimitive genPrimitive) {
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

            render.PrimitiveSort.Add(genPrimitive.ViewPoints, PrimitiveSortPoint.Center, handle);
        }

        private static void DrawPrimitiveGt4(Render render, GenPrimitive genPrimitive) {
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

            render.PrimitiveSort.Add(genPrimitive.ViewPoints, PrimitiveSortPoint.Center, handle);
        }

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

        private static void TransformToClip(Render render, GenPrimitive genPrimitive) {
            float d = render.Camera.ViewDistance;

            for (int i = 0; i < genPrimitive.VertexCount; i++) {
                float inverseZ = d / genPrimitive.ViewPoints[i].Z;

                genPrimitive.ClipPoints[i].X = (genPrimitive.ViewPoints[i].X) * inverseZ;
                genPrimitive.ClipPoints[i].Y = (genPrimitive.ViewPoints[i].Y) * inverseZ;
                genPrimitive.ClipPoints[i].Z = d * genPrimitive.ViewPoints[i].Z;
            }
        }

        private static void TransformToScreen(Render render, GenPrimitive genPrimitive) {
            for (int i = 0; i < genPrimitive.VertexCount; i++) {
                Vector3 clipPoint = genPrimitive.ClipPoints[i];

                genPrimitive.ScreenPoints[i].X = (int)( clipPoint.X * render.Camera.ScreenWidth);
                genPrimitive.ScreenPoints[i].Y = (int)(-clipPoint.Y * render.Camera.ScreenWidth);
            }
        }
    }
}
