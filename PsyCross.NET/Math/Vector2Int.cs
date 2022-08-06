using System.Numerics;
using System.Runtime.CompilerServices;

namespace PsyCross.Math {
    public struct Vector2Int {
        public int X { get; set; }

        public int Y { get; set; }

        public Vector2Int(int x, int y) {
            X = x;
            Y = y;
        }

        public static Vector2Int Zero => new Vector2Int(0, 0);

        public static Vector2Int One => new Vector2Int(1, 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector2(Vector2Int v) => new Vector2(v.X, v.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector2Int(Vector2 v) => new Vector2Int((int)v.X, (int)v.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Int operator-(Vector2Int v) => new Vector2Int(-v.X, -v.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Int operator+(Vector2Int a, Vector2Int b) => new Vector2Int(a.X + b.X, a.Y + b.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Int operator-(Vector2Int a, Vector2Int b) => new Vector2Int(a.X - b.X, a.Y - b.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Int operator*(Vector2Int a, Vector2Int b) => new Vector2Int(a.X * b.X, a.Y * b.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Int operator*(int a, Vector2Int b) => new Vector2Int(a * b.X, a * b.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Int operator*(Vector2Int a, int b) => new Vector2Int(a.X * b, a.Y * b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Int operator/(Vector2Int a, int b) => new Vector2Int(a.X / b, a.Y / b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator==(Vector2Int lhs, Vector2Int rhs) => (lhs.X == rhs.X) && (lhs.Y == rhs.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator!=(Vector2Int lhs, Vector2Int rhs) => !(lhs == rhs);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object other)
        {
            if (!(other is Vector2Int)) {
                return false;
            }

            return Equals((Vector2Int)other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Vector2Int other) => (X == other.X) && (Y == other.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => X.GetHashCode() ^ (Y.GetHashCode() << 2);
    }
}
