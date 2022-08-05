using System;
using System.Collections.Generic;
using System.Numerics;

namespace PsyCross.Testing {
    public class PrimitiveSort {
        private class PrimitiveComparer : IComparer<Primitive> {
            public int Compare(Primitive x, Primitive y) {
                Console.WriteLine($"{x.Z} ? {y.Z}");

                // if (x.Z < y.Z) { return -1; }
                // if (x.Z > y.Z) { return 1; }
                // return 0;

                return Math.Sign(x.Z - y.Z);
            }
        }

        private static readonly PrimitiveComparer _Comparer = new PrimitiveComparer();

        private readonly List<Primitive> _primitives = new List<Primitive>();

        public IReadOnlyList<Primitive> Primitives => _primitives;

        public void Add(Vector3[] points, PrimitiveSortPoint sortPoint, object attributes) {
            _primitives.Add(new Primitive(points, sortPoint, attributes));
        }

        public void Clear() {
            _primitives.Clear();
        }

        public void Sort() {
            _primitives.Sort(_Comparer);
        }
    }
}
