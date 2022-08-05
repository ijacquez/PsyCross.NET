using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using PsyCross.Devices.Input;
using System.Collections.Generic;
using System;
using OpenTK.Mathematics;

namespace PsyCross.OpenTK {
    public class Window {
        private PSX _psx;
        private int[] _displayBuffer;
        private readonly Dictionary<Keys, GamepadInputsEnum> _gamepadKeyMap;
        // private AudioPlayer audioPlayer = new AudioPlayer();
        private int _vSyncCounter;

        private readonly GameWindowSettings _gameWindowSettings = new GameWindowSettings();
        private readonly NativeWindowSettings _nativeWindowSettings;

        private readonly GameWindow _gameWindow;

        private Window() {
        }

        public Window(PSX psx) {
            _psx = psx;

            _gameWindowSettings.RenderFrequency = 60;
            _gameWindowSettings.UpdateFrequency = 60;

            _nativeWindowSettings = new NativeWindowSettings() {
                API          = ContextAPI.OpenGL,
                APIVersion   = new Version(4, 5),
                Profile      = ContextProfile.Compatability,
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
                { Keys.Space, GamepadInputsEnum.Space},
                { Keys.Z , GamepadInputsEnum.Z },
                { Keys.C , GamepadInputsEnum.C },
                { Keys.Enter , GamepadInputsEnum.Enter },
                { Keys.Up , GamepadInputsEnum.Up },
                { Keys.Right , GamepadInputsEnum.Right },
                { Keys.Down , GamepadInputsEnum.Down },
                { Keys.Left , GamepadInputsEnum.Left },
                { Keys.F1 , GamepadInputsEnum.D1 },
                { Keys.F3 , GamepadInputsEnum.D3 },
                { Keys.Q , GamepadInputsEnum.Q },
                { Keys.E , GamepadInputsEnum.E },
                { Keys.W , GamepadInputsEnum.W },
                { Keys.D , GamepadInputsEnum.D },
                { Keys.S , GamepadInputsEnum.S },
                { Keys.A , GamepadInputsEnum.A },
            };

            _gameWindow.MakeCurrent();
        }

        private void OnLoad() {
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Lequal);
            GL.Enable(EnableCap.Texture2D);
            GL.ClearColor(1, 1, 1, 1);
        }

        private void OnRenderFrame(FrameEventArgs args) {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            int id = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, id);

            GL.TexImage2D(TextureTarget.Texture2D, 0,
                PixelInternalFormat.Rgb,
                1024, 512, 0,
                PixelFormat.Bgra,
                PixelType.UnsignedByte,
                _displayBuffer);

            GL.TexParameter(TextureTarget.Texture2D,
                TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D,
                TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

            GL.Begin(PrimitiveType.Quads);
            GL.TexCoord2(0, 1); GL.Vertex2(-1, -1);
            GL.TexCoord2(1, 1); GL.Vertex2(1, -1);
            GL.TexCoord2(1, 0); GL.Vertex2(1, 1);
            GL.TexCoord2(0, 0); GL.Vertex2(-1, 1);
            GL.End();

            GL.DeleteTexture(id);

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
