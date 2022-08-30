using System;
using System.Numerics;
using PsyCross.Math;

namespace PsyCross.Testing {
    public class GenPrimitive {
        public GenPrimitiveFlags Flags { get; set; }
        public PsyQ.TmdPrimitiveType Type { get; set; }
        public int VertexCount { get; set; }
        public int NormalCount { get; set; }
        public Vector3[] PolygonVertices { get; } = new Vector3[4];
        public Vector3[] PolygonNormals { get; } = new Vector3[4];
        public Rgb888[] GouraudShadingColors { get; } = new Rgb888[4];
        public Texcoord[] Texcoords { get; } = new Texcoord[4];

        public Vector3 FaceNormal { get; set; }

        public ClipFlags[] ClipFlags { get; } = new ClipFlags[4];
        public Vector3[] WorldPoints { get; } = new Vector3[4];
        public Vector3[] ViewPoints { get; } = new Vector3[4];
        public Vector3[] ClipPoints { get; } = new Vector3[4];
        public Vector2Int[] ScreenPoints { get; } = new Vector2Int[4];

        public ushort TPageId { get; set; }
        public ushort ClutId { get; set; }

        public static void Copy(GenPrimitive fromGenPrimitive, GenPrimitive toGenPrimitive) {
            toGenPrimitive.Flags = fromGenPrimitive.Flags;
            toGenPrimitive.Type = fromGenPrimitive.Type;
            toGenPrimitive.VertexCount = fromGenPrimitive.VertexCount;
            toGenPrimitive.NormalCount = fromGenPrimitive.NormalCount;
            Array.Copy(fromGenPrimitive.PolygonVertices, toGenPrimitive.PolygonVertices, fromGenPrimitive.VertexCount);
            Array.Copy(fromGenPrimitive.PolygonNormals, toGenPrimitive.PolygonNormals, fromGenPrimitive.NormalCount);
            Array.Copy(fromGenPrimitive.GouraudShadingColors, toGenPrimitive.GouraudShadingColors, fromGenPrimitive.VertexCount);
            Array.Copy(fromGenPrimitive.Texcoords, toGenPrimitive.Texcoords, fromGenPrimitive.VertexCount);
            toGenPrimitive.FaceNormal = fromGenPrimitive.FaceNormal;
            toGenPrimitive.TPageId = fromGenPrimitive.TPageId;
            toGenPrimitive.ClutId = fromGenPrimitive.ClutId;
        }
    }
}
