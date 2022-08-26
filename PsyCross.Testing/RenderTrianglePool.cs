using System;

namespace PsyCross.Testing {
    public class GenPrimitiveAllocator {
        private readonly LinearAllocator<GenPrimitive> _genPrimitiveAllocator;

        private GenPrimitiveAllocator() {
        }

        public GenPrimitiveAllocator(int capacity) {
            _genPrimitiveAllocator = new LinearAllocator<GenPrimitive>(capacity, () => new GenPrimitive());
        }

        public ReadOnlySpan<GenPrimitive> GenPrimitives => _genPrimitiveAllocator.Objects;

        public int Count => _genPrimitiveAllocator.Count;

        public GenPrimitive AllocatePrimitive() {
            if (!_genPrimitiveAllocator.AllocateObject(out GenPrimitive primitive)) {
                return null;
            }

            return primitive;
        }

        public void Reset() => _genPrimitiveAllocator.Reset();
    }
}
