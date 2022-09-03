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

        public Texture(string name, int width, int height, SizedInternalFormat internalFormat) {
            Name = name;
            Width = width;
            Height = height;
            InternalFormat = internalFormat;

            ObjectUtility.CreateTexture(TextureTarget.Texture2D, Name, out int texture);
            Handle = texture;
            GL.TextureStorage2D(Handle, 1, InternalFormat, Width, Height);
            DebugUtility.CheckGLError("Storage2d");
        }

        public void Use(TextureUnit unit) {
            GL.ActiveTexture(unit);
            GL.BindTexture(TextureTarget.Texture2D, Handle);
        }

        public void Update(ushort[] data, PixelFormat pixelFormat, PixelType pixelType) {
            GL.TextureSubImage2D(Handle, 0, 0, 0, Width, Height, pixelFormat, pixelType, data);
            DebugUtility.CheckGLError("SubImage");
        }

        public void Update(uint[] data, PixelFormat pixelFormat, PixelType pixelType) {
            GL.TextureSubImage2D(Handle, 0, 0, 0, Width, Height, pixelFormat, pixelType, data);
            DebugUtility.CheckGLError("SubImage");
        }

        public void Update(IntPtr data, PixelFormat pixelFormat, PixelType pixelType) {
            GL.TextureSubImage2D(Handle, 0, 0, 0, Width, Height, pixelFormat, pixelType, data);
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

        public void SetTextureMaxLevel(int level) {
            GL.TextureParameter(Handle, TextureParameterName.TextureMaxLevel, level);
        }

        public void SetWrap(TextureCoord coord, TextureWrapMode mode) {
            GL.TextureParameter(Handle, (TextureParameterName)coord, (int)mode);
        }

        public void Dispose() {
            GL.DeleteTexture(Handle);
        }
    }
}
