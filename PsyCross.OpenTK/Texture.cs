using PsyCross.OpenTK.Utilities;
using System;

namespace PsyCross.OpenTK.Types {
    using global::OpenTK.Graphics.OpenGL4;

    internal class Texture : IDisposable {
        public const SizedInternalFormat Srgb8Alpha8 = (SizedInternalFormat)All.Srgb8Alpha8;
        public const SizedInternalFormat RGB32F      = (SizedInternalFormat)All.Rgb32f;

        public const GetPName MaxTextureMaxAnisotropy = (GetPName)0x84FF;

        public static float MaxAniso { get; }

        static Texture() {
            MaxAniso = GL.GetFloat(MaxTextureMaxAnisotropy);
        }

        public string Name { get; }

        public int Handle { get; }

        public int Width { get; }

        public int Height { get; }

        public SizedInternalFormat InternalFormat { get; }

        private Texture() {
        }

        public Texture(string name, int width, int height, uint[] data, bool srgb) {
            Name = name;
            Width = width;
            Height = height;
            InternalFormat = srgb ? Srgb8Alpha8 : SizedInternalFormat.Rgba8;

            DebugUtility.CheckGLError("Clear");

            ObjectUtility.CreateTexture(TextureTarget.Texture2D, Name, out int texture);
            Handle = texture;
            GL.TextureStorage2D(Handle, 1, InternalFormat, Width, Height);
            DebugUtility.CheckGLError("Storage2d");

            if (data != null) {
                GL.TextureSubImage2D(Handle, level: 0, xoffset: 0, yoffset: 0, Width, Height, PixelFormat.Bgra, PixelType.UnsignedByte, data);
                DebugUtility.CheckGLError("SubImage");
            }

            GL.TextureParameter(Handle, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            DebugUtility.CheckGLError("WrapS");
            GL.TextureParameter(Handle, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            DebugUtility.CheckGLError("WrapT");

            GL.TextureParameter(Handle, TextureParameterName.TextureMaxLevel, 0);
        }

        public Texture(string name, int handler, int width, int height, SizedInternalFormat internalFormat) {
            Name = name;
            Handle = handler;
            Width = width;
            Height = height;
            InternalFormat = internalFormat;
        }

        public Texture(string name, int width, int height, IntPtr data, bool srgb = false) {
            Name = name;
            Width = width;
            Height = height;
            InternalFormat = srgb ? Srgb8Alpha8 : SizedInternalFormat.Rgba8;

            DebugUtility.CheckGLError("Clear");

            ObjectUtility.CreateTexture(TextureTarget.Texture2D, Name, out int texture);
            Handle = texture;
            GL.TextureStorage2D(Handle, 1, InternalFormat, Width, Height);
            DebugUtility.CheckGLError("Storage2d");

            if (data != null) {
                GL.TextureSubImage2D(Handle, level: 0, xoffset: 0, yoffset: 0, Width, Height, PixelFormat.Bgra, PixelType.UnsignedByte, data);
                DebugUtility.CheckGLError("SubImage");
            }

            GL.TextureParameter(Handle, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            DebugUtility.CheckGLError("WrapS");
            GL.TextureParameter(Handle, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            DebugUtility.CheckGLError("WrapT");

            GL.TextureParameter(Handle, TextureParameterName.TextureMaxLevel, 0);
        }

        public void Use(TextureUnit unit) {
            GL.ActiveTexture(unit);
            GL.BindTexture(TextureTarget.Texture2D, Handle);
        }

        public void Update(uint[] data) {
            GL.TextureSubImage2D(Handle, 0, 0, 0, Width, Height, PixelFormat.Bgra, PixelType.UnsignedByte, data);
            DebugUtility.CheckGLError("SubImage");
        }

        public void Update(IntPtr data) {
            GL.TextureSubImage2D(Handle, 0, 0, 0, Width, Height, PixelFormat.Bgra, PixelType.UnsignedByte, data);
            DebugUtility.CheckGLError("SubImage");
        }

        public void SetMinFilter(TextureMinFilter filter) {
            GL.TextureParameter(Handle, TextureParameterName.TextureMinFilter, (int)filter);
            DebugUtility.CheckGLError("Filtering");
        }

        public void SetMagFilter(TextureMagFilter filter) {
            GL.TextureParameter(Handle, TextureParameterName.TextureMagFilter, (int)filter);
            DebugUtility.CheckGLError("Filtering");
        }

        public void SetAnisotropy(float level) {
            const TextureParameterName TextureMaxAnisotropy = (TextureParameterName)0x84FE;

            GL.TextureParameter(Handle, TextureMaxAnisotropy, System.Math.Clamp(level, 1, MaxAniso));
        }

        public void SetLod(int @base, int min, int max) {
            GL.TextureParameter(Handle, TextureParameterName.TextureLodBias, @base);
            GL.TextureParameter(Handle, TextureParameterName.TextureMinLod, min);
            GL.TextureParameter(Handle, TextureParameterName.TextureMaxLod, max);
        }

        public void SetWrap(TextureCoord coord, TextureWrapMode mode) {
            GL.TextureParameter(Handle, (TextureParameterName)coord, (int)mode);
        }

        public void Dispose() {
            GL.DeleteTexture(Handle);
        }
    }
}
