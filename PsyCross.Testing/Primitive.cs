using System;
using System.Numerics;

namespace PsyCross.Testing {
    public sealed class Primitive {
        public Vector3[] Points { get; } = new Vector3[3];

        public PrimitiveSortPoint SortPoint { get; }

        public object Attributes { get; set; }

        public float Z { get; }

        public Primitive(Vector3[] points, PrimitiveSortPoint sortPoint, object attributes) {
            Array.Copy(points, Points, Points.Length);
            SortPoint = sortPoint;
            Attributes = attributes;

            switch (sortPoint) {
                case PrimitiveSortPoint.Center:
                    Z = (Points[0].Z + Points[1].Z + Points[2].Z) / 3.0f;
                    break;
                case PrimitiveSortPoint.Min:
                    throw new NotImplementedException();
                    // break;
                case PrimitiveSortPoint.Max:
                    throw new NotImplementedException();
                    // break;
            }
        }
    }
}
