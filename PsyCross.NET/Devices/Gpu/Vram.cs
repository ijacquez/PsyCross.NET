using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PsyCross.Devices.GPU {
    public class Vram {
        private GCHandle BitsHandle { get; }

        public Vram(int width, int height) {
            Height = height;
            Width = width;
            Bits = new uint[Width * Height];
            BitsHandle = GCHandle.Alloc(Bits, GCHandleType.Pinned);
        }

        public uint[] Bits { get; }
        public int Height { get; }
        public int Width { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetPixel(int x, int y, uint color) {
            int index = x + (y * Width);

            ref uint r0 = ref MemoryMarshal.GetArrayDataReference(Bits);
            Unsafe.Add(ref r0, (nint)index) = color;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref uint GetPixelRgb888(int x, int y) {
            int index = x + (y * Width);

            ref uint r0 = ref MemoryMarshal.GetArrayDataReference(Bits);
            ref uint ri = ref Unsafe.Add(ref r0, (nint)index);

            return ref ri;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort GetPixelBgr555(int x, int y) {
            int index = x + (y * Width);
            ref uint r0 = ref MemoryMarshal.GetArrayDataReference(Bits);
            ref uint color = ref Unsafe.Add(ref r0, (nint)index);

            byte m = (byte)((color & 0xFF000000) >> 24);
            byte r = (byte)((color & 0x00FF0000) >> 16 + 3);
            byte g = (byte)((color & 0x0000FF00) >> 8 + 3);
            byte b = (byte)((color & 0x000000FF) >> 3);

            return (ushort)((m << 15) | (b << 10) | (g << 5) | r);
        }
    }
}
