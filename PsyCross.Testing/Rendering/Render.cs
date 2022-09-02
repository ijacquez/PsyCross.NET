using System;
using System.Numerics;

namespace PsyCross.Testing.Rendering {
    public class Render {
        private readonly GenPrimitiveLinearAllocator _genPrimitiveAllocator =
            new GenPrimitiveLinearAllocator(512);

        public Material Material { get; set; }
        public Matrix4x4 ModelMatrix { get; set; }
        public Matrix4x4 ModelViewMatrix { get; set; }
        public PsyQ.DrawEnv DrawEnv { get; set; }

        public Camera Camera { get; set; }
        public CommandBuffer CommandBuffer { get; set; }
        public PrimitiveSort PrimitiveSort { get; set; }

        public ReadOnlySpan<GenPrimitive> GenPrimitives =>
            _genPrimitiveAllocator.GenPrimitives;

        public GenPrimitive AcquireGenPrimitive() =>
            _genPrimitiveAllocator.AllocatePrimitive();

        public void ReleaseGenPrimitives() =>
            _genPrimitiveAllocator.Reset();
    }
}
