using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using PsyCross.Testing.Rendering;

namespace PsyCross.Testing {
    public class Model {
        private Matrix4x4 _matrix = Matrix4x4.Identity;
        private Vector3 _position;
        private Quaternion _rotation;

        public Vector3 Position {
            get => _position;
            set {
                _position = value;

                _matrix.M41 = _position.X;
                _matrix.M42 = _position.Y;
                _matrix.M43 = _position.Z;
                _matrix.M44 = 1f;
            }
        }

        public Quaternion Rotation {
            get => _rotation;

            set {
                _rotation = value;

                float xX = _rotation.X * _rotation.X;
                float yY = _rotation.Y * _rotation.Y;
                float zZ = _rotation.Z * _rotation.Z;

                float xY = _rotation.X * _rotation.Y;
                float wZ = _rotation.Z * _rotation.W;
                float xZ = _rotation.Z * _rotation.X;
                float wY = _rotation.Y * _rotation.W;
                float yZ = _rotation.Y * _rotation.Z;
                float wX = _rotation.X * _rotation.W;

                _matrix.M11 = 1f - 2f * (yY + zZ);
                _matrix.M12 = 2f * (xY + wZ);
                _matrix.M13 = 2f * (xZ - wY);
                _matrix.M14 = 0f;
                _matrix.M21 = 2f * (xY - wZ);
                _matrix.M22 = 1f - 2f * (zZ + xX);
                _matrix.M23 = 2f * (yZ + wX);
                _matrix.M24 = 0f;
                _matrix.M31 = 2f * (xZ + wY);
                _matrix.M32 = 2f * (yZ - wX);
                _matrix.M33 = 1f - 2f * (yY + xX);
            }
        }

        public Matrix4x4 Matrix => _matrix;

        public Material Material { get; } = new Material();

        public PsyQ.Tmd Tmd { get; }

        public PsyQ.Tim[] Tims { get; }

        private Model() {
        }

        public Model(PsyQ.Tmd tmd, IEnumerable<PsyQ.Tim> tims) {
            Tmd = tmd;
            Tims = (tims == null) ? new PsyQ.Tim[0] : tims.ToArray();
        }
    }
}
