using System;
using System.Numerics;
using PsyCross.Math;

namespace PsyCross.Testing.Rendering {
    public class GenPrimitive {
        public GenPrimitiveFlags Flags { get; set; }
        public PsyQ.TmdPrimitiveType Type { get; set; }
        public int VertexCount { get; set; } = 3;
        public int NormalCount { get; set; }

        public Vector3[] PolygonVertexBuffer { get; } = new Vector3[4];
        public Span<Vector3> PolygonVertices => PolygonVertexBuffer.AsSpan(0, VertexCount);

        public Vector3[] PolygonNormalBuffer { get; } = new Vector3[4];
        public Span<Vector3> PolygonNormals => PolygonNormalBuffer.AsSpan(0, VertexCount);

        public Rgb888[] GouraudShadingColorBuffer { get; } = new Rgb888[4];
        public Span<Rgb888> GouraudShadingColors => GouraudShadingColorBuffer.AsSpan(0, VertexCount);

        public Texcoord[] TexcoordBuffer { get; } = new Texcoord[4];
        public Span<Texcoord> Texcoords => TexcoordBuffer.AsSpan(0, VertexCount);

        public Vector3 FaceNormal { get; set; }

        public ClipFlags[] ClipFlagBuffer { get; } = new ClipFlags[4];
        public Span<ClipFlags> ClipFlags => ClipFlagBuffer.AsSpan(0, VertexCount);

        public Vector3[] WorldPointBuffer { get; } = new Vector3[4];
        public Span<Vector3> WorldPoints => WorldPointBuffer.AsSpan(0, VertexCount);

        public Vector3[] ViewPointBuffer { get; } = new Vector3[4];
        public Span<Vector3> ViewPoints => ViewPointBuffer.AsSpan(0, VertexCount);

        public Vector2Int[] ScreenPointBuffer { get; } = new Vector2Int[4];
        public Span<Vector2Int> ScreenPoints => ScreenPointBuffer.AsSpan(0, VertexCount);

        public ushort TPageId { get; set; }
        public ushort ClutId { get; set; }

        public static void Discard(GenPrimitive genPrimitive) {
            genPrimitive.Flags |= GenPrimitiveFlags.Discarded;
        }

        public static void ClearClipFlags(GenPrimitive genPrimitive) {
            genPrimitive.ClipFlags[0] = PsyCross.Testing.ClipFlags.None;
            genPrimitive.ClipFlags[1] = PsyCross.Testing.ClipFlags.None;
            genPrimitive.ClipFlags[2] = PsyCross.Testing.ClipFlags.None;
            genPrimitive.ClipFlags[genPrimitive.ClipFlags.Length - 1] = PsyCross.Testing.ClipFlags.None;
        }

        public static void Copy(GenPrimitive fromGenPrimitive, GenPrimitive toGenPrimitive) {
            toGenPrimitive.Flags = fromGenPrimitive.Flags;
            toGenPrimitive.Type = fromGenPrimitive.Type;
            toGenPrimitive.VertexCount = fromGenPrimitive.VertexCount;
            toGenPrimitive.NormalCount = fromGenPrimitive.NormalCount;
            fromGenPrimitive.PolygonVertices.CopyTo(toGenPrimitive.PolygonVertices);
            fromGenPrimitive.PolygonNormals.CopyTo(toGenPrimitive.PolygonNormals);
            fromGenPrimitive.GouraudShadingColors.CopyTo(toGenPrimitive.GouraudShadingColors);
            fromGenPrimitive.Texcoords.CopyTo(toGenPrimitive.Texcoords);
            toGenPrimitive.FaceNormal = fromGenPrimitive.FaceNormal;
            toGenPrimitive.TPageId = fromGenPrimitive.TPageId;
            toGenPrimitive.ClutId = fromGenPrimitive.ClutId;
        }
    }
}
