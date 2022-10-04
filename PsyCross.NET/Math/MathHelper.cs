using System.Numerics;
using System.Runtime.CompilerServices;

namespace PsyCross.Math {
    public static class MathHelper {
        private const float _Deg2Rad     = System.MathF.PI / 180f;
        private const float _Rad2Deg     = 180f / System.MathF.PI;
        private const float _FixedPoint  = 4096f;
        private const float _Fixed2Float = 1f / _FixedPoint;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Approximately(float a, float b, float epsilon = 0.001f) =>
            (System.Math.Abs(b - a) < epsilon);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DegreesToRadians(float degrees) => (degrees * _Deg2Rad);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float RadiansToDegrees(float radians) => (radians * _Rad2Deg);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Fixed2Float(short value) => (float)value * _Fixed2Float;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short Float2Fixed(float value) {
            float shiftValue = (_FixedPoint + ((value >= 0f) ? 0.5f : -0.5f));

            return (short)((value * shiftValue));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Lerp(float value1, float value2, float amount) =>
            (value1 + ((value2 - value1) * amount));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Min(float value1, float value2) =>
            System.Math.Min(value1, value2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Min(float value1, float value2, float value3) =>
            System.Math.Min(value1, System.Math.Min(value2, value3));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Min(float value1, float value2, float value3, float value4) =>
            System.Math.Min(value1, System.Math.Min(value2, System.Math.Min(value3, value4)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 TriangleCenterPoint(Vector3 a, Vector3 b, Vector3 c) =>
            ((a + b + c) * 0.33333333f);
    }
}
