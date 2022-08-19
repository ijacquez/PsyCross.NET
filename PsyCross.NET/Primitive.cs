using System.Numerics;

namespace PsyCross {
    public sealed class Primitive {
        public CommandHandle CommandHandle { get; set; }

        public float Z { get; internal set; }

        internal Primitive() {
        }

        internal static float CalculateZ(PrimitiveSortPoint sortPoint, Vector3[] points) {
            switch (sortPoint) {
                case PrimitiveSortPoint.Min:
                    return System.Math.Min(points[0].Z, System.Math.Min(points[1].Z, points[2].Z));
                case PrimitiveSortPoint.Max:
                    return System.Math.Max(points[0].Z, System.Math.Max(points[1].Z, points[2].Z));
                default:
                case PrimitiveSortPoint.Center:
                    return (points[0].Z + points[1].Z + points[2].Z) / (float)points.Length;
            }
        }
    }
}
