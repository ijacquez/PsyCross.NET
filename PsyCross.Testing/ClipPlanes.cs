using System.Numerics;

namespace PsyCross.Testing {
    public class ClipPlanes {
        public Plane Near {
            get => Planes[0];
            set => Planes[0] = value;
        }

        public Plane Far {
            get => Planes[1];
            set => Planes[1] = value;
        }

        public Plane Left {
            get => Planes[2];
            set => Planes[2] = value;
        }

        public Plane Right {
            get => Planes[3];
            set => Planes[3] = value;
        }

        public Plane Top {
            get => Planes[4];
            set => Planes[4] = value;
        }

        public Plane Bottom {
            get => Planes[5];
            set => Planes[5] = value;
        }

        public Plane[] Planes { get; } = new Plane[6];
    }
}
