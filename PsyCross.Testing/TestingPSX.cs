using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text;
using System;
using PsyCross.Devices.Input;
using PsyCross.Math;
using PsyCross.ResourceManagement;
using PsyCross.Testing.Rendering;

namespace PsyCross.Testing {
    public class Tile {
        public class Layer {
            public int TileId;

            public int LayerId;

            public Vector3 Position;

            public float Rotation;
        }

        public Layer[] Layers { get; } = new Layer[2] {
            new Layer(),
            new Layer()
        };
    }

    public class Testing {
        private PsyQ.DispEnv[] _DispEnv { get; } = new PsyQ.DispEnv[2];
        private PsyQ.DrawEnv[] _DrawEnv { get; } = new PsyQ.DrawEnv[2];
        private int _EnvIndex;

        private Render _render = new Render();
        private PrimitiveSort _primitiveSort = new PrimitiveSort(65536);
        private CommandBuffer _commandBuffer = new CommandBuffer(65536 * 2);

        public Testing() {
            _DispEnv[0] = new PsyQ.DispEnv(new RectInt(0,             0, _ScreenWidth, _ScreenHeight));
            _DrawEnv[0] = new PsyQ.DrawEnv(new RectInt(0, _ScreenHeight, _ScreenWidth, _ScreenHeight), new Vector2Int(_ScreenWidth / 2, _ScreenHeight + (_ScreenHeight / 2)));
            _DispEnv[1] = new PsyQ.DispEnv(new RectInt(0, _ScreenHeight, _ScreenWidth, _ScreenHeight));
            _DrawEnv[1] = new PsyQ.DrawEnv(new RectInt(0,             0, _ScreenWidth, _ScreenHeight), new Vector2Int(_ScreenWidth / 2, _ScreenHeight / 2));

            _DispEnv[0].IsRgb24 = true;
            _DispEnv[1].IsRgb24 = true;

            _DrawEnv[0].Color = Rgb888.Black;
            _DrawEnv[0].IsClear = true;
            _DrawEnv[0].IsDithered = false;

            _DrawEnv[1].Color = Rgb888.Black;
            _DrawEnv[1].IsClear = true;
            _DrawEnv[1].IsDithered = false;

            _EnvIndex = 0;

            PsyQ.PutDispEnv(_DispEnv[0]);
            PsyQ.PutDrawEnv(_DrawEnv[0]);

            PsyQ.SetDispMask(true);

            PsyQ.DrawSync();

            PsyQ.ClearImage(new RectInt(0, 0, 1024, 512), Rgb888.TextureWhite);
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

            var tmdData = ResourceManager.GetBinaryFile("KF/0000.TMD");
            // var tmdData = ResourceManager.GetBinaryFile("VENUS3G.TMD");
            // var tmdData = ResourceManager.GetBinaryFile("SHUTTLE1.TMD");
            // var tmdData = ResourceManager.GetBinaryFile("CUBE3.TMD");
            // var tmdData = ResourceManager.GetBinaryFile("CUBE3G.TMD");
            // var tmdData = ResourceManager.GetBinaryFile("CUBE3GT.TMD");
            // var tmdData = ResourceManager.GetBinaryFile("tmd_0059.tmd");

            var kfOptions = new PsyQ.ReadTmdOptions() {
                Flags = PsyQ.ReadTmdFlags.ApplyKingsField2JpFixes,
                Scale = 1f / 2048f,
                Axis  = new Matrix4x4(-1f,  0f, 0f, 0f,
                                       0f, -1f, 0f, 0f,
                                       0f,  0f, 1f, 0f,
                                       0f,  0f, 0f, 0f)
            };

            PsyQ.ReadTmd(tmdData, kfOptions, out PsyQ.Tmd tmd);

            _model = new Model(tmd, null);
            _model.Material.AmbientColor = new Rgb888(15, 15, 15);

            _model.Position = new Vector3(0f, 0f, 0f);

            // Good starting position
            // _camera.Position = new Vector3(75.36116f, 66.19299f, 75.67318f);
            // _camera.Pitch = 0f;
            // _camera.Yaw = 180f;

            // Testing corrupted polys
            // _camera.Position = new Vector3(16.764767f, 6.2768593f, 3.1472807f);
            // _camera.Pitch = 0f;
            // _camera.Yaw = -81.47999f;

            // Testing Quad Case III

            // 2.9214277, 6.794515, 7.8214207>, Pitch, Yaw: -2.9221065, -914.25366
            _camera.Position = new Vector3(2.9214277f, 6.794515f, 7.8214207f);
            _camera.Pitch = -2.9221065f;
            _camera.Yaw = -914.25366f;

            _light1 = LightingManager.AllocatePointLight();
            _light1.Color = Rgb888.Blue;
            _light1.Position = new Vector3(0f, 1f, 0f);
            _light1.CutOffDistance = 1000f;
            _light1.Range = 1000f;

            // _light2 = LightingManager.AllocateDirectionalLight();
            // _light2.Direction = new Vector3(0, -1, 0);
            // _light2.Color = Rgb888.Red;

            // var light3 = LightingManager.AllocateDirectionalLight();
            // light3.Direction = new Vector3(0, 1, 0);
            // light3.Color = Rgb888.Blue;

            // _camera.Fov = 90f;
            // _camera.Fov = 123.855f; // Shadow Tower
            _camera.Fov = 102.680f; // King's Field

            _flyCamera = new FlyCamera(_camera);

            _render.Camera = _camera;
            _render.CommandBuffer = _commandBuffer;
            _render.PrimitiveSort = _primitiveSort;

            string tileMapText = ResourceManager.GetTextFile("tilemap.txt");

            using (var stringReader = new StringReader(tileMapText)) {
                StringBuilder stringBuilder = new StringBuilder();

                int memberIndex = 0;
                Tile.Layer layer = null;
                bool eof = false;

                while (!eof) {
                    stringBuilder.Clear();
                    Span<char> buffer = stackalloc char[1];

                    while (true) {
                        int ret = stringReader.Read(buffer);

                        if (ret <= 0) {
                            eof = true;
                            break;
                        }

                        if (buffer[0] == ' ') {
                            break;
                        }

                        stringBuilder.Append(buffer);
                    }

                    if ((memberIndex > 0) && ((memberIndex % 6) == 0)) {
                        // Console.WriteLine($"{layer.TileId} {layer.Position.X} {layer.Position.Y} {layer.Position.Z} {layer.Rotation} >>> {memberIndex}");

                        int mx = (int)(layer.Position.X + 40f);
                        int my = (int)(layer.Position.Z + 40f);
                        if ((layer.TileId == 1) && (mx >= 30) && (mx < 80) && (my >= 0) && (my < 80)) {
                            Console.WriteLine($"{mx},{my} => {layer.TileId} => {layer.Rotation}");
                        }
                        if (layer.LayerId == 1) {
                            _layer1[mx + (my * 80)] = layer;
                        } else if (layer.LayerId == 2) {
                            _layer2[mx + (my * 80)] = layer;
                        }

                        memberIndex %= 12;
                        layer = null;
                    }

                    if (!eof) {
                        if (layer == null) {
                            layer = new Tile.Layer();
                        }

                        float.TryParse(stringBuilder.ToString(), NumberStyles.AllowLeadingSign, null, out float value);

                        switch (memberIndex) {
                            case 0:  case 6: layer.TileId = (int)value; memberIndex++; break;
                            case 1:  case 7: layer.LayerId = (int)value; memberIndex++; break;
                            case 2:  case 8: layer.Position.X = value/2048f; memberIndex++; break;
                            case 3:  case 9: layer.Position.Y = value/2048f; memberIndex++; break;
                            case 4: case 10: layer.Position.Z = value/2048f; memberIndex++; break;
                            case 5: case 11: layer.Rotation = value; memberIndex++; break;
                            // case 5: case 11: layer.Rotation = (4-(((int)value - 180) / -90)) * 90; memberIndex++; break;
                        }

                        // Console.WriteLine($"[1;36mmemberIndex: {memberIndex}[m");
                    }
                }
            }
            // Environment.Exit(0);
        }

        private Tile.Layer[] _layer1 = new Tile.Layer[80 * 80];
        private Tile.Layer[] _layer2 = new Tile.Layer[80 * 80];

        private const int _ScreenWidth  = 320;
        private const int _ScreenHeight = 240;

        private readonly Camera _camera = new Camera(_ScreenWidth, _ScreenHeight);
        private readonly FlyCamera _flyCamera;

        private Model _model;
        private PointLight _light1;
        private DirectionalLight _light2;

        int _tmdObjectIndex = 0;

        Vector3 XXX = new Vector3(3.5f, 3.3f, 0f);

        public void Update() {
            _commandBuffer.Reset();
            _primitiveSort.Reset();

            if (!Psx.Input.HasFlag(JoyPad.Square)) {
                _flyCamera.Update();
                _light1.Position = _camera.Position;
            } else {
                Console.WriteLine($"[1;34m{XXX}[m");

                if (Psx.Input.HasFlag(JoyPad.Triangle)) {
                    if ((Psx.Input & JoyPad.Up) == JoyPad.Up) {
                        XXX += Vector3.UnitY * Psx.Time.DeltaTime;
                    }

                    if ((Psx.Input & JoyPad.Down) == JoyPad.Down) {
                        XXX += -Vector3.UnitY * Psx.Time.DeltaTime;
                    }
                } else {
                    if ((Psx.Input & JoyPad.Up) == JoyPad.Up) {
                        XXX += Vector3.UnitZ * Psx.Time.DeltaTime;
                    }

                    if ((Psx.Input & JoyPad.Down) == JoyPad.Down) {
                        XXX += -Vector3.UnitZ * Psx.Time.DeltaTime;
                    }
                }

                if ((Psx.Input & JoyPad.Left) == JoyPad.Left) {
                    XXX += -Vector3.UnitX * Psx.Time.DeltaTime;
                }

                if ((Psx.Input & JoyPad.Right) == JoyPad.Right) {
                    XXX += Vector3.UnitX * Psx.Time.DeltaTime;
                }
            }

            if ((Psx.Input & JoyPad.Start) == JoyPad.Select) {
                _tmdObjectIndex--;
            }
            if ((Psx.Input & JoyPad.Start) == JoyPad.Start) {
                _tmdObjectIndex++;
            }
            _tmdObjectIndex %= _model.Tmd.Objects.Length;

            _render.Material = _model.Material;
            _render.ModelMatrix = _model.Matrix;
            _render.ModelViewMatrix = _camera.GetViewMatrix();
            // _render.ModelViewMatrix = _camera.GetViewMatrix() * _model.Matrix;
            _render.DrawEnv = _DrawEnv[_EnvIndex];

            Console.WriteLine($"_camera.Position: [1;32m{_camera.Position}, Pitch, Yaw: {_camera.Pitch}, {_camera.Yaw}[m");

            int xOff=(int)(10f*XXX.X);
            int yOff=(int)(10f*XXX.Y);
            for (int yy = 0; yy < 10; yy++) {
                for (int xx = 0; xx < 20; xx++) {
                    NewMethod(yy, xx, yOff, xOff, _layer1);
                    // NewMethod(yy, xx, yOff, xOff, _layer2);
                }
            }

            _primitiveSort.Sort();

            Console.WriteLine($"_commandBuffer.AllocatedCount: {_render.CommandBuffer.AllocatedCount}");

            PsyQ.DrawPrim(_primitiveSort, _commandBuffer);
            PsyQ.DrawSync();

            // Swap buffer
            _EnvIndex ^= 1;

            PsyQ.PutDispEnv(_DispEnv[_EnvIndex]);
            PsyQ.PutDrawEnv(_DrawEnv[_EnvIndex]);
        }

        private void NewMethod(int yy, int xx, int yOff, int xOff, Tile.Layer[] layer) {
            Tile.Layer tile = layer[(xx + xOff) + (80 * (yy + yOff))];
            Vector3 position = Vector3.Zero;
            if (tile != null) {
                // position.X = (tile.Position.X)+40f;
                // position.Y = -tile.Position.Y;
                // position.Z = (tile.Position.Z)+40f;
                position.X = xx * 1;
                position.Y = -tile.Position.Y * 1;
                position.Z = yy * 1;

                Matrix4x4 m = Matrix4x4.CreateRotationY(MathHelper.DegreesToRadians(tile.Rotation));
                Vector3 forward = Vector3.TransformNormal(Vector3.UnitZ, m);
                Matrix4x4 pos = Matrix4x4.CreateTranslation(position);
                Matrix4x4 matrix = m * pos;

                // _model.Rotation = Quaternion.CreateFromYawPitchRoll(MathHelper.DegreesToRadians(tile.Rotation), 0f, 0f);
                // _model.Position = position;
                _render.ModelViewMatrix = matrix * _render.Camera.GetViewMatrix();
                // Console.WriteLine($"{tile.TileId} at {position} {tile.Rotation}");
                Renderer.DrawTmdObject(_render, _model.Tmd.Objects[tile.TileId]);
            }
        }
    }
}
