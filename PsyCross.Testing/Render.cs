using System;
using System.Numerics;

namespace PsyCross.Testing {
    public class Render {
        // XXX: Remove GenPrimitiveAllocator class and use the allocator directly
        private readonly GenPrimitiveAllocator _genPrimitiveAllocator = new GenPrimitiveAllocator(32);

        public Material Material { get; set; }
        public Matrix4x4 ModelMatrix { get; set; }
        public Matrix4x4 ModelViewMatrix { get; set; }

        public Camera Camera { get; set; }
        public CommandBuffer CommandBuffer { get; set; }
        public PrimitiveSort PrimitiveSort { get; set; }

        public ReadOnlySpan<GenPrimitive> GenPrimitives => _genPrimitiveAllocator.GenPrimitives;

        public GenPrimitive AcquireGenPrimitive() {
            var genPrimitive = _genPrimitiveAllocator.AllocatePrimitive();

            genPrimitive.Flags = GenPrimitiveFlags.None;

            return genPrimitive;
        }

        public void ReleaseGenPrimitives() => _genPrimitiveAllocator.Reset();
    }
}
