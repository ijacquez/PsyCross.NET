using PsyCross.Math;

namespace PsyCross {
    public static partial class PsyQ {
        public class DrawEnv {
            public RectInt ClipRect { get; set; }

            public Vector2Int Offset { get; set; }

            public Rgb888 Color { get; set; }

            public bool IsDithered { get; set; }

            public bool IsDraw { get; }

            public bool IsClear { get; set; }

            public DrawEnv(RectInt clipRect, Vector2Int offset) {
                ClipRect = clipRect;
                Offset = offset;
                IsDithered = false;
                IsDraw = true;
                IsClear = false;
            }
        }
    }
}
