using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using PsyCross.Math;

namespace PsyCross {
    public static partial class PsyQ {
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
                // tmdObject.Vertices[vertexIndex].X = (tmdVertices[vertexIndex].X/100.0f);
                // tmdObject.Vertices[vertexIndex].Y = (tmdVertices[vertexIndex].Y/100.0f);
                // tmdObject.Vertices[vertexIndex].Z = (tmdVertices[vertexIndex].Z/100.0f);

                // tmdObject.Vertices[vertexIndex].X = (tmdVertices[vertexIndex].X*(2.5f/500.0f));
                // tmdObject.Vertices[vertexIndex].Y = (tmdVertices[vertexIndex].Y*(2.5f/500.0f));
                // tmdObject.Vertices[vertexIndex].Z = (tmdVertices[vertexIndex].Z*(2.5f/500.0f));

                tmdObject.Vertices[vertexIndex].X = tmdVertices[vertexIndex].X;
                tmdObject.Vertices[vertexIndex].Y = tmdVertices[vertexIndex].Y;
                tmdObject.Vertices[vertexIndex].Z = tmdVertices[vertexIndex].Z;

                Console.WriteLine($"<[1;31m{tmdVertices[vertexIndex].X}, {tmdVertices[vertexIndex].Y}, {tmdVertices[vertexIndex].Z}[m> [1;32m{tmdObject.Vertices[vertexIndex]}[m");
            }
        }

        private static void TmdReadNormals(BinaryReader binaryReader, TmdObjectDesc tmdObjectDesc, TmdObject tmdObject) {
            binaryReader.Seek((int)tmdObjectDesc.NormalsOffset, SeekOrigin.Begin);

            var tmdNormals = binaryReader.ReadStruct<TmdVertex>((int)tmdObjectDesc.NormalsCount);

            tmdObject.Normals = new Vector3[tmdObjectDesc.NormalsCount];

            for (int normalIndex = 0; normalIndex < tmdObjectDesc.NormalsCount; normalIndex++) {
                tmdObject.Normals[normalIndex].X = MathHelper.Fixed2Float(tmdNormals[normalIndex].X);
                tmdObject.Normals[normalIndex].Y = MathHelper.Fixed2Float(tmdNormals[normalIndex].Y);
                tmdObject.Normals[normalIndex].Z = MathHelper.Fixed2Float(tmdNormals[normalIndex].Z);
            }
        }

        private static void TmdReadPrimitives(BinaryReader binaryReader, TmdObjectDesc tmdObjectDesc, TmdObject tmdObject) {
            binaryReader.Seek((int)tmdObjectDesc.PrimitivesOffset, SeekOrigin.Begin);

            tmdObject.Packets = new TmdPacket[tmdObjectDesc.PrimitivesCount];

            for (int primitiveIndex = 0; primitiveIndex < tmdObjectDesc.PrimitivesCount; primitiveIndex++) {
                tmdObject.Packets[primitiveIndex] = new TmdPacket();

                var tmdPacket = tmdObject.Packets[primitiveIndex];

                tmdPacket.PrimitiveHeader = binaryReader.ReadStruct<TmdPrimitiveHeader>()[0];

                bool isModeTge = (tmdPacket.PrimitiveHeader.Mode & TmdPrimitiveMode.Tge) == TmdPrimitiveMode.Tge;
                bool isModeQuad = (tmdPacket.PrimitiveHeader.Mode & TmdPrimitiveMode.Quad) == TmdPrimitiveMode.Quad;
                bool isModeTme = (tmdPacket.PrimitiveHeader.Mode & TmdPrimitiveMode.Tme) == TmdPrimitiveMode.Tme;
                bool isModeIip = (tmdPacket.PrimitiveHeader.Mode & TmdPrimitiveMode.Iip) == TmdPrimitiveMode.Iip;
                bool isFlagGrd = (tmdPacket.PrimitiveHeader.Flags & TmdPrimitiveFlags.Grd) == TmdPrimitiveFlags.Grd;

                //                  Lighting
                // Quad Tme Iip Grd Tge
                // 0    0   0   0   0
                // 0    0   0   0   1
                // 0    0   0   1   0
                // 0    0   0   1   1
                // 0    0   1   0   0
                // 0    0   1   0   1
                // 0    0   1   1   0
                // 0    0   1   1   1
                // 0    1   0   0   0
                // 0    1   0   0   1
                // 0    1   0   1   0
                // 0    1   0   1   1
                // 0    1   1   0   0
                // 0    1   1   0   1
                // 0    1   1   1   0
                // 0    1   1   1   1
                //
                // 1    0   0   0   0
                // 1    0   0   0   1
                // 1    0   0   1   0
                // 1    0   0   1   1
                // 1    0   1   0   0
                // 1    0   1   0   1
                // 1    0   1   1   0
                // 1    0   1   1   1
                // 1    1   0   0   0
                // 1    1   0   0   1
                // 1    1   0   1   0
                // 1    1   0   1   1
                // 1    1   1   0   0
                // 1    1   1   0   1
                // 1    1   1   1   0
                // 1    1   1   1   1

                Console.WriteLine($"({tmdPacket.PrimitiveHeader.PacketWordCount * sizeof(UInt32)}B) [0x{((uint)tmdPacket.PrimitiveHeader.Mode << 8) | (uint)tmdPacket.PrimitiveHeader.Flags:X02}] isModeQuad: {isModeQuad}, isModeTme: {isModeTme}, isModeIip: {isModeIip}, isModeGrd: {isFlagGrd}");

                switch (tmdPacket.PrimitiveHeader.Mode & TmdPrimitiveMode.CodeMask) {
                    case TmdPrimitiveMode.CodePolygon when !isModeQuad && !isModeTme && !isModeIip && !isFlagGrd:
                        if (isModeTge) {
                            TmdReadPrimitivePacket<TmdPrimitiveFn3>(binaryReader, tmdPacket);
                        } else {
                            TmdReadPrimitivePacket<TmdPrimitiveF3>(binaryReader, tmdPacket);
                        }
                        break;
                    case TmdPrimitiveMode.CodePolygon when !isModeQuad && !isModeTme &&  isModeIip && !isFlagGrd:
                        if (isModeTge) {
                            TmdReadPrimitivePacket<TmdPrimitiveGn3>(binaryReader, tmdPacket);
                        } else {
                            TmdReadPrimitivePacket<TmdPrimitiveG3>(binaryReader, tmdPacket);
                        }
                        break;
                    case TmdPrimitiveMode.CodePolygon when !isModeQuad && !isModeTme && !isModeIip &&  isFlagGrd:
                        if (isModeTge) {
                            throw new Exception("Invalid packet");
                        }

                        TmdReadPrimitivePacket<TmdPrimitiveFg3>(binaryReader, tmdPacket);
                        break;
                    case TmdPrimitiveMode.CodePolygon when !isModeQuad && !isModeTme &&  isModeIip &&  isFlagGrd:
                        if (isModeTge) {
                            throw new Exception("Invalid packet");
                        }

                        TmdReadPrimitivePacket<TmdPrimitiveGg3>(binaryReader, tmdPacket);
                        break;
                    case TmdPrimitiveMode.CodePolygon when !isModeQuad &&  isModeTme && !isModeIip && !isFlagGrd:
                        if (isModeTge) {
                            TmdReadPrimitivePacket<TmdPrimitiveFnt3>(binaryReader, tmdPacket);
                        } else {
                            TmdReadPrimitivePacket<TmdPrimitiveFt3>(binaryReader, tmdPacket);
                        }
                        break;
                    case TmdPrimitiveMode.CodePolygon when !isModeQuad &&  isModeTme &&  isModeIip && !isFlagGrd:
                        if (isModeTge) {
                            TmdReadPrimitivePacket<TmdPrimitiveGnt3>(binaryReader, tmdPacket);
                        } else {
                            TmdReadPrimitivePacket<TmdPrimitiveGt3>(binaryReader, tmdPacket);
                        }
                        break;

                    case TmdPrimitiveMode.CodePolygon when  isModeQuad && !isModeTme && !isModeIip && !isFlagGrd:
                        if (isModeTge) {
                            TmdReadPrimitivePacket<TmdPrimitiveFn4>(binaryReader, tmdPacket);
                        } else {
                            TmdReadPrimitivePacket<TmdPrimitiveF4>(binaryReader, tmdPacket);
                        }
                        break;
                    case TmdPrimitiveMode.CodePolygon when  isModeQuad && !isModeTme &&  isModeIip && !isFlagGrd:
                        if (isModeTge) {
                            TmdReadPrimitivePacket<TmdPrimitiveGn4>(binaryReader, tmdPacket);
                        } else {
                            TmdReadPrimitivePacket<TmdPrimitiveG4>(binaryReader, tmdPacket);
                        }
                        break;
                    case TmdPrimitiveMode.CodePolygon when  isModeQuad && !isModeTme && !isModeIip &&  isFlagGrd:
                        if (isModeTge) {
                            throw new Exception("Invalid packet");
                        }

                        TmdReadPrimitivePacket<TmdPrimitiveFg4>(binaryReader, tmdPacket);
                        break;
                    case TmdPrimitiveMode.CodePolygon when  isModeQuad && !isModeTme &&  isModeIip &&  isFlagGrd:
                        if (isModeTge) {
                            throw new Exception("Invalid packet");
                        }

                        TmdReadPrimitivePacket<TmdPrimitiveGg4>(binaryReader, tmdPacket);
                        break;
                    case TmdPrimitiveMode.CodePolygon when  isModeQuad &&  isModeTme && !isModeIip && !isFlagGrd:
                        if (isModeTge) {
                            TmdReadPrimitivePacket<TmdPrimitiveFnt4>(binaryReader, tmdPacket);
                        } else {
                            TmdReadPrimitivePacket<TmdPrimitiveFt4>(binaryReader, tmdPacket);
                        }
                        break;
                    case TmdPrimitiveMode.CodePolygon when  isModeQuad &&  isModeTme &&  isModeIip && !isFlagGrd:
                        if (isModeTge) {
                            TmdReadPrimitivePacket<TmdPrimitiveGnt4>(binaryReader, tmdPacket);
                        } else {
                            TmdReadPrimitivePacket<TmdPrimitiveG4>(binaryReader, tmdPacket);
                        }
                        break;

                    case TmdPrimitiveMode.CodeStraightLine:
                        throw new NotImplementedException();

                    case TmdPrimitiveMode.CodeSprite:
                        throw new NotImplementedException();

                    default:
                        throw new Exception("Unknown primitive");
                }
            }
        }

        private static void TmdReadPrimitivePacket<T>(BinaryReader binaryReader,
                                                      TmdPacket tmdPacket)
        where T : struct, ITmdPrimitive {
            // Debugging only. Disable
            int structSize = Marshal.SizeOf<T>();
            int packetSize = tmdPacket.PrimitiveHeader.PacketWordCount * sizeof(UInt32);

            if (structSize != packetSize) {
                throw new DataMisalignedException($"Reading {structSize} for type {typeof(T)}, expecting to read {packetSize} bytes");
            }

            tmdPacket.Primitive = binaryReader.ReadStruct<T>()[0];

            Console.WriteLine($"{tmdPacket.Primitive.Type}");
        }
    }
}
