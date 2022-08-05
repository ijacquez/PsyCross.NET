using PsyCross.Devices.Input;
using PsyCross.OpenTK.Types;
using PsyCross.OpenTK.Utilities;
using PsyCross.ResourceManagement;
using System.Collections.Generic;
using System;

namespace PsyCross.OpenTK {
    using global::OpenTK.Graphics.OpenGL4;
    using global::OpenTK.Mathematics;
    using global::OpenTK.Windowing.Common;
    using global::OpenTK.Windowing.Desktop;
    using global::OpenTK.Windowing.GraphicsLibraryFramework;

    public class Window {
        private const string _PositionAttribName = "in_position";
        private const string _TexcoordAttribName = "in_texcoord";

        private PSX _psx;
        private uint[] _displayBuffer;
        private readonly Dictionary<Keys, GamepadInputsEnum> _gamepadKeyMap;
        // private AudioPlayer audioPlayer = new AudioPlayer();
        private int _vSyncCounter;

        private readonly GameWindowSettings _gameWindowSettings = new GameWindowSettings();
        private readonly NativeWindowSettings _nativeWindowSettings;

        private readonly GameWindow _gameWindow;

        private Shader _shader;
        private Texture _texture;
        private int _vaoHandle;

        // VBO structure
        //   Vertex   Texture
        //     float    float
        //   0        3
        //   -------- -------
        //   vx vy vz tx ty
        private int _vboHandle;
        private int _vboSize;
        private float[] _vertexTexcoordBuffer = new float[2 * 3 * 5] {
            -1, -1, 0, 0, 1,
             1, -1, 0, 1, 1,
             1,  1, 0, 1, 0,

            -1, -1, 0, 0, 1,
             1,  1, 0, 1, 0,
            -1,  1, 0, 0, 0,
        };

        private IntPtr _vertexTexcoordPtr;

        private Window() {
        }

        public Window(PSX psx) {
            _psx = psx;

            _gameWindowSettings.RenderFrequency = 60;
            _gameWindowSettings.UpdateFrequency = 60;

            _nativeWindowSettings = new NativeWindowSettings() {
                API          = ContextAPI.OpenGL,
                APIVersion   = new Version(4, 5),
                Profile      = ContextProfile.Any,
                StartVisible = true,
                Size         = new Vector2i(1024, 512)
            };

            _gameWindow = new GameWindow(_gameWindowSettings, _nativeWindowSettings);
            _gameWindow.Load += OnLoad;
            _gameWindow.UpdateFrame += OnUpdateFrame;
            _gameWindow.RenderFrame += OnRenderFrame;
            _gameWindow.KeyUp += OnKeyUp;
            _gameWindow.KeyDown += OnKeyDown;

            _gameWindow.VSync = VSyncMode.On;

            _gamepadKeyMap = new Dictionary<Keys, GamepadInputsEnum>() {
                { Keys.Space, GamepadInputsEnum.Select },
                { Keys.Z,     GamepadInputsEnum.L2 },
                { Keys.C,     GamepadInputsEnum.R2 },
                { Keys.Enter, GamepadInputsEnum.Start },
                { Keys.Up,    GamepadInputsEnum.Up },
                { Keys.Right, GamepadInputsEnum.Right },
                { Keys.Down,  GamepadInputsEnum.Down },
                { Keys.Left,  GamepadInputsEnum.Left },
                { Keys.F1,    GamepadInputsEnum.D1 },
                { Keys.F3,    GamepadInputsEnum.D3 },
                { Keys.Q,     GamepadInputsEnum.L1 },
                { Keys.E,     GamepadInputsEnum.R1 },
                { Keys.W,     GamepadInputsEnum.Triangle },
                { Keys.D,     GamepadInputsEnum.Circle },
                { Keys.S,     GamepadInputsEnum.Cross },
                { Keys.A,     GamepadInputsEnum.Square },
            };

            _gameWindow.MakeCurrent();
        }

        private void OnLoad() {
            var vertexShaderSource = LoadShaderSource(ShaderType.VertexShader, "Shaders/model_view.vert");
            var fragmentShaderSource = LoadShaderSource(ShaderType.FragmentShader, "Shaders/model_view.frag");

            _shader = new Shader("main", vertexShaderSource, fragmentShaderSource);
            _texture = new Texture("main_tex", 1024, 512, data: null, srgb: true);

            _vaoHandle = GL.GenVertexArray();
            GL.BindVertexArray(_vaoHandle);

            _vboSize = _vertexTexcoordBuffer.Length * sizeof(float);
            _vertexTexcoordPtr = (IntPtr)0;

            _vboHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vboHandle);
            GL.BufferData(BufferTarget.ArrayBuffer, _vboSize, (IntPtr)0, BufferUsageHint.StaticDraw);

            GL.BufferSubData(BufferTarget.ArrayBuffer,
                             _vertexTexcoordPtr,
                             _vertexTexcoordBuffer.Length * sizeof(float),
                             _vertexTexcoordBuffer);

            int positionLocation = _shader.GetAttribLocation(_PositionAttribName);
            DebugUtility.CheckGLError("SetShader: PositionAttribName Shader.GetAttribLocation");
            GL.EnableVertexAttribArray(positionLocation);
            GL.VertexAttribPointer(positionLocation,
                                   3,
                                   VertexAttribPointerType.Float,
                                   normalized: false,
                                   stride: 5 * sizeof(float),
                                   offset: 0);
            DebugUtility.CheckGLError("SetShader: PositionAttribName GL.VertexAttribPointer");

            int texcoordLocation = _shader.GetAttribLocation(_TexcoordAttribName);
            GL.EnableVertexAttribArray(texcoordLocation);
            GL.VertexAttribPointer(texcoordLocation,
                                   2,
                                   VertexAttribPointerType.Float,
                                   normalized: false,
                                   stride: 5 * sizeof(float),
                                   offset: 3 * sizeof(float));
            DebugUtility.CheckGLError("SetShader: TexcoordAttribName GL.VertexAttribPointer");

            GL.ClearColor(0.5f, 0.5f, 0.5f, 1.0f);
        }

        private void OnRenderFrame(FrameEventArgs args) {
            Render();

            _texture.Use(TextureUnit.Texture0);
            _shader.Bind();

            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.CullFace);
            GL.Disable(EnableCap.RasterizerDiscard);
            GL.Enable(EnableCap.Texture2D);

            GL.BindVertexArray(_vaoHandle);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vboHandle);

            GL.DrawArrays(PrimitiveType.Triangles, first: 0, (_vertexTexcoordBuffer.Length / 5));

            _gameWindow.SwapBuffers();
        }

        private void OnUpdateFrame(FrameEventArgs args) {
            _psx.UpdateFrame();
        }

        private void OnKeyDown(KeyboardKeyEventArgs e) {
            GamepadInputsEnum? button = GetGamepadButton(e.Key);

            if (button != null) {
                _psx.JoyPadDown(button.Value);
            }
        }

        private void OnKeyUp(KeyboardKeyEventArgs e) {
            GamepadInputsEnum? button = GetGamepadButton(e.Key);

            if (button != null) {
                _psx.JoyPadUp(button.Value);
            }
        }

        private GamepadInputsEnum? GetGamepadButton(Keys keyCode) {
            if (_gamepadKeyMap.TryGetValue(keyCode, out GamepadInputsEnum gamepadButtonValue)) {
                return gamepadButtonValue;
            }

            return null;
        }

        public void Run() {
            _gameWindow.Run();
        }

        private void Render() {
            _vSyncCounter++;
            _displayBuffer = _psx.VRAM.Bits;

            _texture.Update(_displayBuffer);
        }

        private static ShaderSource LoadShaderSource(ShaderType shaderType, string filePath) {
            return new ShaderSource() {
                Type   = shaderType,
                Source = ResourceManager.GetTextFile(filePath)
            };
        }

        // public void SetDisplayMode(int horizontalRes, int verticalRes, bool is24BitDepth) {
        //     //throw new System.NotImplementedException();
        // }

        // public void SetHorizontalRange(ushort displayX1, ushort displayX2) {
        //     //throw new System.NotImplementedException();
        // }

        // public void SetVRAMStart(ushort displayVRAMXStart, ushort displayVRAMYStart) {
        //     //throw new System.NotImplementedException();
        // }

        // public void SetVerticalRange(ushort displayY1, ushort displayY2) {
        //     //throw new System.NotImplementedException();
        // }
    }
}
