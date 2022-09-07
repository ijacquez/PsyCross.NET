using System;
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
    }
}
