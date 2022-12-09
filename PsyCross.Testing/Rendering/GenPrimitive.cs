using PsyCross.Math;
using System;
using System.Numerics;

namespace PsyCross.Testing.Rendering {
    public class GenPrimitive {
        public GenPrimitiveFlags Flags { get; set; }
        public PsyQ.TmdPrimitiveType Type { get; set; }
        public int VertexCount { get; set; } = 3;
        public int NormalCount { get; set; }

        public Vector3[] VertexBuffer { get; } = new Vector3[4];
        public Span<Vector3> Vertices => VertexBuffer.AsSpan(0, VertexCount);

        public Vector3[] VertexNormalBuffer { get; } = new Vector3[4];
        public Span<Vector3> VertexNormals => VertexNormalBuffer.AsSpan(0, VertexCount);

        public Rgb888[] GouraudShadingColorBuffer { get; } = new Rgb888[4];
        public Span<Rgb888> GouraudShadingColors => GouraudShadingColorBuffer.AsSpan(0, VertexCount);

        public Texcoord[] TexcoordBuffer { get; } = new Texcoord[4];
        public Span<Texcoord> Texcoords => TexcoordBuffer.AsSpan(0, VertexCount);

        public Vector3 FaceNormal { get; set; }
        public float FaceArea { get; set; }

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

        public static void Decompose(GenPrimitive genPrimitive) {
            genPrimitive.Type = (PsyQ.TmdPrimitiveType)(genPrimitive.Type - PsyQ.TmdPrimitiveType.F4);
            genPrimitive.VertexCount = 3;
            genPrimitive.NormalCount = (genPrimitive.NormalCount >= 4) ? 3 : genPrimitive.NormalCount;
        }

        public static void Discard(GenPrimitive genPrimitive) {
            genPrimitive.Flags |= GenPrimitiveFlags.Discarded;
        }

        public static void Discard(GenPrimitive genPrimitive, string message) {
            // XXX: Debug
            Console.Write($"[1;31mDiscard[m");
            Console.WriteLine((string.IsNullOrEmpty(message)) ? string.Empty : $" [1;32m{message}[m");

            Discard(genPrimitive);
        }

        public static void ClearClipFlags(GenPrimitive genPrimitive) {
            genPrimitive.ClipFlagBuffer[0] = Rendering.ClipFlags.None;
            genPrimitive.ClipFlagBuffer[1] = Rendering.ClipFlags.None;
            genPrimitive.ClipFlagBuffer[2] = Rendering.ClipFlags.None;
            genPrimitive.ClipFlagBuffer[3] = Rendering.ClipFlags.None;
        }

        public static void Copy(GenPrimitive fromGenPrimitive, GenPrimitive toGenPrimitive) {
            toGenPrimitive.Flags = fromGenPrimitive.Flags;
            toGenPrimitive.Type = fromGenPrimitive.Type;
            toGenPrimitive.VertexCount = fromGenPrimitive.VertexCount;
            toGenPrimitive.NormalCount = fromGenPrimitive.NormalCount;
        }

        public static void CopyGouraudShadingColors(GenPrimitive fromGenPrimitive, GenPrimitive toGenPrimitive) =>
            fromGenPrimitive.GouraudShadingColors.CopyTo(toGenPrimitive.GouraudShadingColors);

        public static void CopyVertices(GenPrimitive fromGenPrimitive, GenPrimitive toGenPrimitive) =>
            fromGenPrimitive.Vertices.CopyTo(toGenPrimitive.Vertices);

        public static void CopyVertexNormals(GenPrimitive fromGenPrimitive, GenPrimitive toGenPrimitive) {
            fromGenPrimitive.VertexNormals.CopyTo(toGenPrimitive.VertexNormals);
        }

        public static void CopyViewPoints(GenPrimitive fromGenPrimitive, GenPrimitive toGenPrimitive) =>
            fromGenPrimitive.ViewPoints.CopyTo(toGenPrimitive.ViewPoints);

        public static void CopyWorldPoints(GenPrimitive fromGenPrimitive, GenPrimitive toGenPrimitive) =>
            fromGenPrimitive.WorldPoints.CopyTo(toGenPrimitive.WorldPoints);

        public static void CopyTexcoords(GenPrimitive fromGenPrimitive, GenPrimitive toGenPrimitive) =>
            fromGenPrimitive.Texcoords.CopyTo(toGenPrimitive.Texcoords);

        public static void CopyTextureAttribs(GenPrimitive fromGenPrimitive, GenPrimitive toGenPrimitive) {
            toGenPrimitive.TPageId = fromGenPrimitive.TPageId;
            toGenPrimitive.ClutId = fromGenPrimitive.ClutId;
        }

        public static void ClearFlags(GenPrimitive genPrimitive) {
            genPrimitive.Flags = GenPrimitiveFlags.None;
        }

        public static bool HasFlag(GenPrimitive genPrimitive, GenPrimitiveFlags flagsMask) =>
            ((genPrimitive.Flags & flagsMask) != GenPrimitiveFlags.None);

        public static bool HasFlags(GenPrimitive genPrimitive, GenPrimitiveFlags flagsMask) =>
            HasFlag(genPrimitive, flagsMask);
    }
}
