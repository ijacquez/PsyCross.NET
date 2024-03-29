using PsyCross.Devices.Input;
using PsyCross.OpenTK.Types;
using PsyCross.OpenTK.Utilities;
using PsyCross.ResourceManagement;
using System;
using System.Collections.Generic;

namespace PsyCross.OpenTK {
    using global::OpenTK.Graphics.OpenGL4;
    using global::OpenTK.Mathematics;
    using global::OpenTK.Windowing.Common;
    using global::OpenTK.Windowing.Desktop;
    using global::OpenTK.Windowing.GraphicsLibraryFramework;

    public class Window {
        private const string _PositionAttribName      = "in_position";
        private const string _TexcoordAttribName      = "in_texcoord";

        private const string _Is24BitDepthUniformName = "is24BitDepth";

        private readonly Dictionary<Keys, JoyPad> _gamepadKeyMap;
        // private AudioPlayer audioPlayer = new AudioPlayer();

        private int _vSyncCounter;

        private bool _running    = true;
        private bool _viewVram   = false;
        private int _windowScale = 3;

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

        private static float[] _VertexTexcoordBuffer = new float[2 * 3 * 5] {
            -1, -1, 0, 0, 1,
             1, -1, 0, 1, 1,
             1,  1, 0, 1, 0,

            -1, -1, 0, 0, 1,
             1,  1, 0, 1, 0,
            -1,  1, 0, 0, 0,
        };

        public Window() {
            _gameWindowSettings.RenderFrequency = 60;
            _gameWindowSettings.UpdateFrequency = 60;

            _nativeWindowSettings = new NativeWindowSettings() {
                API          = ContextAPI.OpenGL,
                APIVersion   = new Version(4, 5),
                Profile      = ContextProfile.Any,
                StartVisible = true,
                Size         = new Vector2i(Psx.Gpu.Vram.Width, Psx.Gpu.Vram.Height)
            };

            _gameWindow = new GameWindow(_gameWindowSettings, _nativeWindowSettings);
            _gameWindow.Load += OnLoad;
            _gameWindow.Resize += OnResize;
            _gameWindow.UpdateFrame += OnUpdateFrame;
            _gameWindow.RenderFrame += OnRenderFrame;
            _gameWindow.KeyUp += OnKeyUp;
            _gameWindow.KeyDown += OnKeyDown;

            _gameWindow.VSync = VSyncMode.Adaptive;

            _gamepadKeyMap = new Dictionary<Keys, JoyPad>() {
                { Keys.Space, JoyPad.Select },
                { Keys.Z,     JoyPad.L2 },
                { Keys.C,     JoyPad.R2 },
                { Keys.Enter, JoyPad.Start },
                { Keys.Up,    JoyPad.Up },
                { Keys.Right, JoyPad.Right },
                { Keys.Down,  JoyPad.Down },
                { Keys.Left,  JoyPad.Left },
                { Keys.F1,    JoyPad.D1 },
                { Keys.F3,    JoyPad.D3 },
                { Keys.Q,     JoyPad.L1 },
                { Keys.E,     JoyPad.R1 },
                { Keys.W,     JoyPad.Triangle },
                { Keys.D,     JoyPad.Circle },
                { Keys.S,     JoyPad.Cross },
                { Keys.A,     JoyPad.Square },
            };

            _gameWindow.MakeCurrent();
        }

        private void OnLoad() {
            var vertexShaderSource = LoadShaderSource(ShaderType.VertexShader, "Shaders/model_view.vert");
            var fragmentShaderSource = LoadShaderSource(ShaderType.FragmentShader, "Shaders/model_view.frag");

            _shader = new Shader("main", vertexShaderSource, fragmentShaderSource);
            _texture = new Texture("main_tex", Psx.Gpu.Vram.Width, Psx.Gpu.Vram.Height, SizedInternalFormat.Rgb8);

            _texture.SetMinFilter(TextureMinFilter.Nearest);
            _texture.SetMagFilter(TextureMagFilter.Nearest);
            _texture.SetTextureMaxLevel(0);

            _vaoHandle = GL.GenVertexArray();
            GL.BindVertexArray(_vaoHandle);

            _vboSize = _VertexTexcoordBuffer.Length * sizeof(float);

            _vboHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vboHandle);
            GL.BufferData(BufferTarget.ArrayBuffer, _vboSize, (IntPtr)0, BufferUsageHint.StaticDraw);

            UploadVertexBuffer();

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
        }

        private void OnResize(ResizeEventArgs e) {
            GL.Viewport(0, 0, e.Size.X, e.Size.Y);
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

            UploadVertexBuffer();

            _shader.SetBool(_Is24BitDepthUniformName, Psx.Gpu.Is24BitDepth);

            GL.DrawArrays(PrimitiveType.Triangles, first: 0, (_VertexTexcoordBuffer.Length / 5));

            _gameWindow.SwapBuffers();
        }

        private void OnUpdateFrame(FrameEventArgs e) {
            if (!_running) {
                _gameWindow.Close();

                return;
            }

            Psx.OnUpdateFrame(e.Time);

            if (_viewVram) {
                _gameWindow.Size = _nativeWindowSettings.Size;

                UpdateTexcoords(0, 0, Psx.Gpu.Vram.Width, Psx.Gpu.Vram.Height);
            } else {
                _gameWindow.Size = new Vector2i(Psx.Gpu.DisplayHorizontalRes,
                                                Psx.Gpu.DisplayVerticalRes) * _windowScale;

                UpdateTexcoords(Psx.Gpu.DisplayVramStart.X,
                                Psx.Gpu.DisplayVramStart.Y,
                                Psx.Gpu.DisplayHorizontalRes,
                                Psx.Gpu.DisplayVerticalRes);
            }
        }

        private void OnKeyDown(KeyboardKeyEventArgs e) {
            JoyPad? button = GetKeys(e.Key);

            if (button != null) {
                Psx.OnJoyPadDown(button.Value);
            }

            switch (e.Key) {
                case Keys.Escape:
                    _running = false;
                    break;
                case Keys.Tab:
                    _viewVram ^= true;
                    break;
            }
        }

        private void OnKeyUp(KeyboardKeyEventArgs e) {
            JoyPad? button = GetKeys(e.Key);

            if (button != null) {
                Psx.OnJoyPadUp(button.Value);
            }
        }

        private JoyPad? GetKeys(Keys keyCode) {
            if (_gamepadKeyMap.TryGetValue(keyCode, out JoyPad gamepadButtonValue)) {
                return gamepadButtonValue;
            }

            return null;
        }

        public void Run() {
            _gameWindow.Run();
        }

        private void Render() {
            _vSyncCounter++;

            _texture.Update(Psx.Gpu.Vram.Bits, PixelFormat.Bgra, PixelType.UnsignedByte);
        }

        private void UpdateTexcoords(int x, int y, int width, int height) {
            // 0, 1,
            // 1, 1,
            // 1, 0,
            //
            // 0, 1,
            // 1, 0,
            // 0, 0

            float textureWidth = (float)_texture.Width;
            float textureHeight = (float)_texture.Height;

            Vector2 textureOrigin = new Vector2(x / textureWidth,
                                                y / textureHeight);

            Span<float> uv0;
            Span<float> uv1;
            Span<float> uv2;

            uv0 = GetTexcoord(triangleIndex: 0, 0);
            uv1 = GetTexcoord(triangleIndex: 0, 1);
            uv2 = GetTexcoord(triangleIndex: 0, 2);

            uv0[0] = textureOrigin.X;
            uv0[1] = textureOrigin.Y + (height / textureHeight);

            uv1[0] = textureOrigin.X + (width / textureWidth);
            uv1[1] = textureOrigin.Y + (height / textureHeight);

            uv2[0] = textureOrigin.X + (width / textureWidth);
            uv2[1] = textureOrigin.Y;

            uv0 = GetTexcoord(triangleIndex: 1, 0);
            uv1 = GetTexcoord(triangleIndex: 1, 1);
            uv2 = GetTexcoord(triangleIndex: 1, 2);

            uv0[0] = textureOrigin.X;
            uv0[1] = textureOrigin.Y + (height / textureHeight);

            uv1[0] = textureOrigin.X + (width / textureWidth);
            uv1[1] = textureOrigin.Y;

            uv2[0] = textureOrigin.X;
            uv2[1] = textureOrigin.Y;

            Span<float> GetTexcoord(int triangleIndex, int ty) =>
                _VertexTexcoordBuffer.AsSpan((triangleIndex * 5 * 3) + ((5 * ty) + 3), 2);
        }

        private void UploadVertexBuffer() {
            GL.BufferSubData(BufferTarget.ArrayBuffer,
                             (IntPtr)0,
                             _VertexTexcoordBuffer.Length * sizeof(float),
                             _VertexTexcoordBuffer);
        }

        private static ShaderSource LoadShaderSource(ShaderType shaderType, string filePath) {
            return new ShaderSource() {
                Type   = shaderType,
                Source = ResourceManager.GetTextFile(filePath)
            };
        }
    }
}
