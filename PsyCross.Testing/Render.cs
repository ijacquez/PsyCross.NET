using System;
using System.Numerics;

namespace PsyCross.Testing {
    public class Render {
        private readonly GenPrimitiveAllocator _genPrimitiveAllocator =
            new GenPrimitiveAllocator(32);
        private readonly GenPrimitiveAllocator _subdivGenPrimitiveAllocator =
            new GenPrimitiveAllocator(4);

        public Material Material { get; set; }
        public Matrix4x4 ModelMatrix { get; set; }
        public Matrix4x4 ModelViewMatrix { get; set; }

        public Camera Camera { get; set; }
        public CommandBuffer CommandBuffer { get; set; }
        public PrimitiveSort PrimitiveSort { get; set; }

        public ReadOnlySpan<GenPrimitive> GenPrimitives =>
            _genPrimitiveAllocator.GenPrimitives;

        public ReadOnlySpan<GenPrimitive> SubdivGenPrimitives =>
            _genPrimitiveAllocator.GenPrimitives;

        public GenPrimitive AcquireGenPrimitive() =>
            _genPrimitiveAllocator.AllocatePrimitive();

        public GenPrimitive AcquireSubdivGenPrimitive() =>
            _subdivGenPrimitiveAllocator.AllocatePrimitive();

        public void ReleaseGenPrimitives() {
            _genPrimitiveAllocator.Reset();
            _subdivGenPrimitiveAllocator.Reset();
        }
    }
}
