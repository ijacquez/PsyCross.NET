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

        public bool IsLit {
            get {
                switch (Type) {
                    case PsyQ.TmdPrimitiveType.F3:
                    case PsyQ.TmdPrimitiveType.G3:
                    case PsyQ.TmdPrimitiveType.Fg3:
                    case PsyQ.TmdPrimitiveType.Gg3:
                    case PsyQ.TmdPrimitiveType.Ft3:
                    case PsyQ.TmdPrimitiveType.Gt3:
                    case PsyQ.TmdPrimitiveType.F4:
                    case PsyQ.TmdPrimitiveType.G4:
                    case PsyQ.TmdPrimitiveType.Fg4:
                    case PsyQ.TmdPrimitiveType.Gg4:
                    case PsyQ.TmdPrimitiveType.Ft4:
                    case PsyQ.TmdPrimitiveType.Gt4:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public bool IsTextured {
            get {
                switch (Type) {
                    case PsyQ.TmdPrimitiveType.Ft3:
                    case PsyQ.TmdPrimitiveType.Gt3:
                    case PsyQ.TmdPrimitiveType.Fnt3:
                    case PsyQ.TmdPrimitiveType.Gnt3:
                    case PsyQ.TmdPrimitiveType.Ft4:
                    case PsyQ.TmdPrimitiveType.Gt4:
                    case PsyQ.TmdPrimitiveType.Fnt4:
                    case PsyQ.TmdPrimitiveType.Gnt4:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public static void Discard(GenPrimitive genPrimitive) {
            genPrimitive.Flags |= GenPrimitiveFlags.Discarded;
        }

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
