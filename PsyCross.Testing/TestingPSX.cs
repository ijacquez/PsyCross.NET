using System;
using System.Numerics;
using PsyCross.Devices.Input;
using PsyCross.Math;
using PsyCross.ResourceManagement;
using PsyCross.Testing.Rendering;

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

            _render.Camera = _camera;
            _render.CommandBuffer = _commandBuffer;
            _render.PrimitiveSort = _primitiveSort;
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

            _render.Material = _model.Material;
            _render.ModelMatrix = _model.Matrix;
            _render.ModelViewMatrix = _camera.GetViewMatrix() * _model.Matrix;
            _render.DrawEnv = _DrawEnv[_EnvIndex];

            Console.WriteLine($"_camera.Position: [1;32m{_camera.Position}, Pitch, Yaw: {_camera.Pitch}, {_camera.Yaw}[m");

            Renderer.DrawTmd(_render, _model.Tmd);

            _primitiveSort.Sort();

            Console.WriteLine($"_commandBuffer.AllocatedCount: {_render.CommandBuffer.AllocatedCount}");

            PsyQ.DrawPrim(_primitiveSort, _commandBuffer);
            PsyQ.DrawSync();

            // Swap buffer
            _EnvIndex ^= 1;

            PsyQ.PutDispEnv(_DispEnv[_EnvIndex]);
            PsyQ.PutDrawEnv(_DrawEnv[_EnvIndex]);
        }
    }
}
