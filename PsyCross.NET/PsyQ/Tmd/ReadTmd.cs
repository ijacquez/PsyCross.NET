using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;

namespace PsyCross {
    public static partial class PsyQ {
        // XXX: Move
        private const float _Float2Fixed = 1.0f / 4096.0f;

        public static bool TryReadTmd(byte[] data, out Tmd tmd) {
            const uint TmdMagic = 0x00000041;

            tmd = default(Tmd);

            if (data.Length < Marshal.SizeOf<TmdHeader>()) {
                return false;
            }

            tmd = new Tmd();

            using (var binaryReader = new BinaryReader(data)) {
                var tmdHeader = binaryReader.ReadStruct<TmdHeader>()[0];

                if (tmdHeader.Magic != TmdMagic) {
                    return false;
                }

                if (tmdHeader.ObjectCount == 0) {
                    return false;
                }

                tmd.Objects = new TmdObject[tmdHeader.ObjectCount];

                int objectsPosition = binaryReader.Position;

                var tmdObjectDescs = binaryReader.ReadStruct<TmdObjectDesc>((int)tmdHeader.ObjectCount);

                for (int objectIndex = 0; objectIndex < tmdHeader.ObjectCount; objectIndex++) {
                    ref TmdObjectDesc tmdObjectDesc = ref tmdObjectDescs[objectIndex];

                    if (tmdObjectDesc.VerticesCount == 0) {
                        return false;
                    }

                    if (!tmdHeader.Flags.IsFixP) {
                        tmdObjectDesc.VerticesOffset += (uint)objectsPosition;
                        tmdObjectDesc.NormalsOffset += (uint)objectsPosition;
                        tmdObjectDesc.PrimitivesOffset += (uint)objectsPosition;
                    }

                    tmd.Objects[objectIndex] = new TmdObject();

                    TmdObject tmdObject = tmd.Objects[objectIndex];

                    TmdReadVertices(binaryReader, tmdObjectDesc, tmdObject);
                    TmdReadNormals(binaryReader, tmdObjectDesc, tmdObject);
                    TmdReadPrimitives(binaryReader, tmdObjectDesc, tmdObject);
                }

                return true;
            }
        }

        private static void TmdReadVertices(BinaryReader binaryReader, TmdObjectDesc tmdObjectDesc, TmdObject tmdObject) {
            binaryReader.Seek((int)tmdObjectDesc.VerticesOffset, SeekOrigin.Begin);

            var tmdVertices = binaryReader.ReadStruct<TmdVertex>((int)tmdObjectDesc.VerticesCount);

            tmdObject.Vertices = new Vector3[tmdObjectDesc.VerticesCount];

            for (int vertexIndex = 0; vertexIndex < tmdObjectDesc.VerticesCount; vertexIndex++) {
                Vector3 fixedVertex = new Vector3(tmdVertices[vertexIndex].X,
                                                  tmdVertices[vertexIndex].Y,
                                                  tmdVertices[vertexIndex].Z);

                tmdObject.Vertices[vertexIndex] = fixedVertex * _Float2Fixed;
            }
        }

        private static void TmdReadNormals(BinaryReader binaryReader, TmdObjectDesc tmdObjectDesc, TmdObject tmdObject) {
            binaryReader.Seek((int)tmdObjectDesc.NormalsOffset, SeekOrigin.Begin);

            var tmdNormals = binaryReader.ReadStruct<TmdVertex>((int)tmdObjectDesc.NormalsCount);

            tmdObject.Normals = new Vector3[tmdObjectDesc.NormalsCount];

            for (int normalIndex = 0; normalIndex < tmdObjectDesc.NormalsCount; normalIndex++) {
                Vector3 fixedNormal = new Vector3(tmdNormals[normalIndex].X,
                                                  tmdNormals[normalIndex].Y,
                                                  tmdNormals[normalIndex].Z);

                tmdObject.Normals[normalIndex] = fixedNormal * _Float2Fixed;
            }
        }

        private static void TmdReadPrimitives(BinaryReader binaryReader, TmdObjectDesc tmdObjectDesc, TmdObject tmdObject) {
            binaryReader.Seek((int)tmdObjectDesc.PrimitivesOffset, SeekOrigin.Begin);

            tmdObject.Packets = new TmdPacket[tmdObjectDesc.PrimitivesCount];

            for (int primitiveIndex = 0; primitiveIndex < tmdObjectDesc.PrimitivesCount; primitiveIndex++) {
                tmdObject.Packets[primitiveIndex] = new TmdPacket();

                var tmdPacket = tmdObject.Packets[primitiveIndex];

                tmdPacket.PrimitiveHeader = binaryReader.ReadStruct<TmdPrimitiveHeader>()[0];

                bool isModeQuad = (tmdPacket.PrimitiveHeader.Mode & TmdPrimitiveMode.Quad) == TmdPrimitiveMode.Quad;
                bool isModeTme = (tmdPacket.PrimitiveHeader.Mode & TmdPrimitiveMode.Tme) == TmdPrimitiveMode.Tme;
                bool isModeIip = (tmdPacket.PrimitiveHeader.Mode & TmdPrimitiveMode.Iip) == TmdPrimitiveMode.Iip;

                bool isFlagGrd = (tmdPacket.PrimitiveHeader.Flags & TmdPrimitiveFlags.Grd) == TmdPrimitiveFlags.Grd;

                // Quad Tme Iip Grd
                // 0    0   0   0
                // 0    0   1   0
                // 0    0   0   1
                // 0    0   1   1
                // 0    1   0   0
                // 0    1   1   0
                //
                // 1    0   0   0
                // 1    0   1   0
                // 1    0   0   1
                // 1    0   1   1
                // 1    1   0   0
                // 1    1   1   0

                switch (tmdPacket.PrimitiveHeader.Mode & TmdPrimitiveMode.CodeMask) {
                    case TmdPrimitiveMode.CodePolygon when !isModeQuad && !isModeTme && !isModeIip && !isFlagGrd:
                        TmdReadPrimitivePacket<TmdPrimitiveF3>(TmdPrimitiveType.F3, binaryReader, tmdPacket);
                        break;
                    case TmdPrimitiveMode.CodePolygon when !isModeQuad && !isModeTme &&  isModeIip && !isFlagGrd:
                        TmdReadPrimitivePacket<TmdPrimitiveG3>(TmdPrimitiveType.G3, binaryReader, tmdPacket);
                        break;
                    case TmdPrimitiveMode.CodePolygon when !isModeQuad && !isModeTme && !isModeIip &&  isFlagGrd:
                        TmdReadPrimitivePacket<TmdPrimitiveFg3>(TmdPrimitiveType.Fg3, binaryReader, tmdPacket);
                        break;
                    case TmdPrimitiveMode.CodePolygon when !isModeQuad && !isModeTme &&  isModeIip &&  isFlagGrd:
                        TmdReadPrimitivePacket<TmdPrimitiveGg3>(TmdPrimitiveType.Gg3, binaryReader, tmdPacket);
                        break;
                    case TmdPrimitiveMode.CodePolygon when !isModeQuad &&  isModeTme && !isModeIip && !isFlagGrd:
                        TmdReadPrimitivePacket<TmdPrimitiveFt3>(TmdPrimitiveType.Ft3, binaryReader, tmdPacket);
                        break;
                    case TmdPrimitiveMode.CodePolygon when !isModeQuad &&  isModeTme &&  isModeIip && !isFlagGrd:
                        TmdReadPrimitivePacket<TmdPrimitiveGt3>(TmdPrimitiveType.Gt3, binaryReader, tmdPacket);
                        break;

                    case TmdPrimitiveMode.CodePolygon when  isModeQuad && !isModeTme && !isModeIip && !isFlagGrd:
                        throw new NotImplementedException();
                    case TmdPrimitiveMode.CodePolygon when  isModeQuad && !isModeTme &&  isModeIip && !isFlagGrd:
                        throw new NotImplementedException();
                    case TmdPrimitiveMode.CodePolygon when  isModeQuad && !isModeTme && !isModeIip &&  isFlagGrd:
                        throw new NotImplementedException();
                    case TmdPrimitiveMode.CodePolygon when  isModeQuad && !isModeTme &&  isModeIip &&  isFlagGrd:
                        throw new NotImplementedException();
                    case TmdPrimitiveMode.CodePolygon when  isModeQuad &&  isModeTme && !isModeIip && !isFlagGrd:
                        throw new NotImplementedException();
                    case TmdPrimitiveMode.CodePolygon when  isModeQuad &&  isModeTme &&  isModeIip && !isFlagGrd:
                        throw new NotImplementedException();

                    case TmdPrimitiveMode.CodeStraightLine:
                        throw new NotImplementedException();

                    case TmdPrimitiveMode.CodeSprite:
                        throw new NotImplementedException();

                    default:
                        throw new NotImplementedException();
                }
            }
        }

        private static void TmdReadPrimitivePacket<T>(TmdPrimitiveType primitiveType,
                                                      BinaryReader binaryReader,
                                                      TmdPacket tmdPacket) where T : struct, ITmdPrimitive {
            // Console.WriteLine($"primitiveType: {primitiveType}");
            tmdPacket.PrimitiveType = primitiveType;
            tmdPacket.Primitive = binaryReader.ReadStruct<T>()[0];
        }
    }
}
