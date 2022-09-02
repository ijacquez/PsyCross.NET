using System;
using System.Collections.Generic;
using System.Numerics;

namespace PsyCross {
    public class PrimitiveSort {
        private class PrimitiveComparer : IComparer<Primitive> {
            public int Compare(Primitive x, Primitive y) => System.Math.Sign(x.Z - y.Z);
        }

        private static readonly PrimitiveComparer _Comparer = new PrimitiveComparer();

        private readonly Primitive[] _primitives;
        private int _index;

        private PrimitiveSort() {
        }

        public PrimitiveSort(int primitiveCount) {
            _primitives = new Primitive[primitiveCount];

            for (int index = 0; index < primitiveCount; index++) {
                _primitives[index] = new Primitive();
            }
        }

        public ReadOnlySpan<Primitive> Primitives => _primitives.AsSpan(0, _index);

        public void Add(ReadOnlySpan<Vector3> points, PrimitiveSortPoint sortPoint, CommandHandle commandHandle) {
            _primitives[_index].CommandHandle = commandHandle;
            _primitives[_index].Z = Primitive.CalculateZ(sortPoint, points);
            _index++;
        }

        public void Reset() {
            _index = 0;
        }

        public void Sort() {
            Array.Sort(_primitives, 0, _index, _Comparer);
        }
    }
}
