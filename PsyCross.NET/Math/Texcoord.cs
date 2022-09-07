using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PsyCross.Math {
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 2)]
    public struct Texcoord {
        [FieldOffset( 0)] public byte X;
        [FieldOffset( 1)] public byte Y;

        public Texcoord(byte x, byte y) {
            X = x;
            Y = y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector2(Texcoord v) => new Vector2(v.X, v.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Texcoord(Vector2 v) => new Texcoord((byte)v.X, (byte)v.Y);

        public override string ToString() => $"<{X}, {Y}>";
    }
}
