using System;
using System.Numerics;
using PsyCross.Math;

namespace PsyCross.Testing {
    public class GenPrimitive {
        public PsyQ.TmdPrimitiveType PrimitiveType { get; set; }
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

        public void CopyFrom(GenPrimitive fromGenPrimitive) {
            PrimitiveType = fromGenPrimitive.PrimitiveType;
            VertexCount = fromGenPrimitive.VertexCount;
            NormalCount = fromGenPrimitive.NormalCount;
            Array.Copy(fromGenPrimitive.PolygonVertices, PolygonVertices, fromGenPrimitive.VertexCount);
            Array.Copy(fromGenPrimitive.PolygonNormals, PolygonNormals, fromGenPrimitive.NormalCount);
            Array.Copy(fromGenPrimitive.GouraudShadingColors, GouraudShadingColors, fromGenPrimitive.VertexCount);
            Array.Copy(fromGenPrimitive.Texcoords, Texcoords, fromGenPrimitive.VertexCount);
            FaceNormal = fromGenPrimitive.FaceNormal;
            TPageId = fromGenPrimitive.TPageId;
            ClutId = fromGenPrimitive.ClutId;
        }
    }
}
