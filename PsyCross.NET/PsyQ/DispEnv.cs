using PsyCross.Math;

namespace PsyCross {
    public static partial class PsyQ {
        public class DispEnv {
            public RectInt Rect { get; set; }

            public RectInt ScreenRect { get; set; }

            public bool IsRgb24 { get; set; }

            public bool IsInterlaced => false;

            public DispEnv(Rect rect) : this(rect, isRgb24: false) {
            }

            public DispEnv(Rect rect, bool isRgb24) {
                Rect = rect;
                ScreenRect = new Rect(0, 0, 0, 0);
                IsRgb24 = isRgb24;
            }
        }
    }
}
