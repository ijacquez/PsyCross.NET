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

        // XXX: Find a better way to "clone"... maybe a CoW approach? Overkill?
        internal void CopyTo(GenPrimitive toGenPrimitive) {
            toGenPrimitive.PrimitiveType = PrimitiveType;
            toGenPrimitive.VertexCount = VertexCount;
            toGenPrimitive.NormalCount = NormalCount;
            Array.Copy(PolygonVertices, toGenPrimitive.PolygonVertices, PolygonVertices.Length);
            Array.Copy(PolygonNormals, toGenPrimitive.PolygonNormals, PolygonNormals.Length);
            Array.Copy(GouraudShadingColors, toGenPrimitive.GouraudShadingColors, GouraudShadingColors.Length);
            Array.Copy(Texcoords, toGenPrimitive.Texcoords, Texcoords.Length);
            toGenPrimitive.FaceNormal = FaceNormal;
            Array.Copy(ClipFlags, toGenPrimitive.ClipFlags, ClipFlags.Length);
            Array.Copy(WorldPoints, toGenPrimitive.WorldPoints, WorldPoints.Length);
            Array.Copy(ViewPoints, toGenPrimitive.ViewPoints, ViewPoints.Length);
            Array.Copy(ClipPoints, toGenPrimitive.ClipPoints, ClipPoints.Length);
            Array.Copy(ScreenPoints, toGenPrimitive.ScreenPoints, ScreenPoints.Length);

            toGenPrimitive.TPageId = TPageId;
            toGenPrimitive.ClutId = ClutId;
        }
    }
}
