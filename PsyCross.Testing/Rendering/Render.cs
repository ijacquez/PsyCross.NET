using System.Collections.Generic;
using System.Numerics;
using PsyCross.Testing.Rendering.Internal;

namespace PsyCross.Testing.Rendering {
    public class Render {
        private readonly GenPrimitiveLinearAllocator _genPrimitiveAllocator =
            new GenPrimitiveLinearAllocator(8192);

        public Material Material { get; set; }
        public Matrix4x4 ModelMatrix { get; set; }
        public Matrix4x4 ModelViewMatrix { get; set; }
        public PsyQ.DrawEnv DrawEnv { get; set; }

        public Camera Camera { get; set; }
        public CommandBuffer CommandBuffer { get; set; }
        public PrimitiveSort PrimitiveSort { get; set; }

        public IList<GenPrimitive> ClippedGenPrimitives { get; } =
            new List<GenPrimitive>(128);

        public IList<GenPrimitive> SubdividedGenPrimitives { get; } =
            new List<GenPrimitive>(128);

        public GenPrimitive AcquireGenPrimitive() =>
            _genPrimitiveAllocator.AllocatePrimitive();

        private void ReleaseGenPrimitives() =>
            _genPrimitiveAllocator.Reset();

        public static void Reset(Render render) {
            render.ClippedGenPrimitives.Clear();
            render.SubdividedGenPrimitives.Clear();

            render.ReleaseGenPrimitives();
        }
    }
}
