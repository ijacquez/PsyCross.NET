using System;

namespace PsyCross.Testing.Rendering.Internal {
    internal class GenPrimitiveLinearAllocator {
        private readonly LinearAllocator<GenPrimitive> _genPrimitiveAllocator;

        private GenPrimitiveLinearAllocator() {
        }

        public GenPrimitiveLinearAllocator(int capacity) {
            _genPrimitiveAllocator = new LinearAllocator<GenPrimitive>(capacity, GenPrimitiveCreator);

            GenPrimitive GenPrimitiveCreator() => new GenPrimitive();
        }

        public ReadOnlySpan<GenPrimitive> GenPrimitives => _genPrimitiveAllocator.Objects;

        public int Count => _genPrimitiveAllocator.Count;

        public GenPrimitive AllocatePrimitive() {
            if (!_genPrimitiveAllocator.AllocateObject(out GenPrimitive genPrimitive)) {
                throw new OutOfMemoryException("GenPrimitiveLinearAllocator: Out of memory.");
            }

            genPrimitive.Flags = GenPrimitiveFlags.None;

            return genPrimitive;
        }

        public void Reset() => _genPrimitiveAllocator.Reset();
    }
}
